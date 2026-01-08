using System.Linq.Expressions;
using Indice.Features.Cases.Workflows.Models.Decision;
using Microsoft.Z3;
using RulesEngine.ExpressionBuilders;
using RulesEngine.Models;

namespace Indice.Features.Cases.Workflows.Services;

public record Z3ValidationResult(bool Success, string Error);

public class Z3Validator
{
    public Z3ValidationResult Validate(DecisionDefinition definition, Rule[] rules) {
        // var parameters = definition.Variables
        //     .DistinctBy(v => v.Name)
        //     .Select(v => new Parameter(
        //         v.Name,
        //         v.Type switch {
        //             DecisionVariableType.Int => typeof(int),
        //             DecisionVariableType.String => typeof(string),
        //             DecisionVariableType.Date => typeof(int),
        //             DecisionVariableType.Bool => typeof(bool),
        //             _ => throw new NotSupportedException()
        //         }))
        //     .ToArray();
        
        var parameters = definition.Variables
            .DistinctBy(v => v.Name)
            .Select(v => Expression.Parameter(
                v.Type switch {
                    DecisionVariableType.Int => typeof(int),
                    DecisionVariableType.String => typeof(string),
                    DecisionVariableType.Date => typeof(int),
                    DecisionVariableType.Bool => typeof(bool),
                    _ => throw new NotSupportedException($"Type {v.Type} not supported")
                },
                v.Name
            ))
            .ToArray();

        using var ctx = new Context();
        var variables = parameters.ToDictionary(parameter => parameter.Name, parameter => parameter.Type switch {
            { } t when t == typeof(int) => ctx.MkIntConst(parameter.Name),
            { } t when t == typeof(string) => ctx.MkConst(parameter.Name, ctx.StringSort),
            { } t when t == typeof(bool) => ctx.MkBoolConst(parameter.Name),
            _ => throw new ArgumentOutOfRangeException()
        });

        var converter = new Z3ExpressionConverter(ctx, variables);
        var ruleExprs = new List<BoolExpr>();

        var parser = new RuleExpressionParser(new ReSettings());

        foreach (var rule in rules) {
            var parsed = parser.Parse(rule.Expression, parameters.ToArray(), typeof(bool));
            var z3Expr = converter.ConvertToZ3(parsed) ?? throw new InvalidOperationException();
            ruleExprs.Add(z3Expr);
        }

        // 1. Completeness - all input space is exhausted, no gaps -> expect to be unsatisfiable
        var combined = ctx.MkOr(ruleExprs.ToArray());
        var uncovered = ctx.MkNot(combined);

        var solver = ctx.MkSolver();
        var variableDefs = definition.Variables
            .DistinctBy(v => v.Name)
            .ToArray();
        
        AssertTypeDomains(ctx, solver, variables, variableDefs);
        solver.Assert(uncovered);

        if (solver.Check() != Status.UNSATISFIABLE) {
            var error = "Rules are NOT exhaustive! \nExample unhandled Value:\n";
            foreach (var @const in solver.Model.Consts) {
                error += @const.Key.Name + ": " + @const.Value + "\n";
            }

            return new Z3ValidationResult(false, error);
        }

        solver.Reset();

        // 2. No Overlaps Unique `Ri ∧ Rj` -> expect to be unsatisfiable
        for (int i = 0; i < ruleExprs.Count; i++) {
            for (int j = i + 1; j < ruleExprs.Count; j++) {
                var toCheck = ctx.MkAnd(ruleExprs[i], ruleExprs[j]);
                solver = ctx.MkSolver();
                AssertTypeDomains(ctx, solver, variables, variableDefs);
                solver.Assert(toCheck);
                if (solver.Check() != Status.UNSATISFIABLE) {
                    var error = $"Rules {i+1} and {j+1} are overlapping! \nExample overlapping Value:\n";
                    foreach (var @const in solver.Model.Consts) {
                        error += @const.Key.Name + ": " + @const.Value + "\n";
                    }

                    return new Z3ValidationResult(false, error);
                }
            }
        }
        
        return new Z3ValidationResult(true, null);

        // 3. Subsumption R1 ∧ ¬R2 -> expect to be unsatisfiable
        // for (int i = 0; i < ruleExprs.Count; i++) {
        //     for (int j = i + 1; j < ruleExprs.Count; j++) {
        //         var toCheck = ctx.MkAnd(ruleExprs[i], ctx.MkNot(ruleExprs[j]));
        //         using var solver1 = ctx.MkSolver();
        //         solver1.Assert(toCheck);
        //         if (solver1.Check() == Status.SATISFIABLE) {
        //             using var solver2 = ctx.MkSolver();
        //             solver2.Assert(ctx.MkAnd(ruleExprs[j], ctx.MkNot(ruleExprs[i])));
        //             if (solver2.Check() == Status.SATISFIABLE) {
        //                 var error = "Rules are contradictory! \nExample contradictory Value:\n";
        //                 foreach (var @const in solver2.Model.Consts) {
        //                     error += @const.Key.Name + ": " + @const.Value + "\n";
        //                 }
        //                 return TypedResults.ValidationProblem(errors: new Dictionary<string, string[]>(), detail: error);
        //             }
        //         }
        //     }
        // }
        
        // todo:
        // subsumption + output validation
        // priority handling: masked, misleading
        // normalization: 1NF, 2NF, 3NF
    }

    private static void AssertTypeDomains(
        Context ctx,
        Solver solver,
        Dictionary<string, Expr> variables,
        IEnumerable<DecisionVariableDefinition> variableDefs
    ) {
        foreach (var def in variableDefs) {
            if (def.Type == DecisionVariableType.String && def.AllowedValues?.Any() == true) {
                var varExpr = (SeqExpr)variables[def.Name];

                var allowedExprs = def.AllowedValues
                    .Select(v => ctx.MkEq(varExpr, ctx.MkString(v)))
                    .ToArray();

                solver.Assert(ctx.MkOr(allowedExprs));
            }

            if (def.Type == DecisionVariableType.Date) {
                var d = (ArithExpr)variables[def.Name];
                solver.Assert(ctx.MkAnd(
                    ctx.MkGe(d, ctx.MkInt(19000101)),
                    ctx.MkLe(d, ctx.MkInt(21001231))
                ));
            }

            if (def.Type == DecisionVariableType.Bool) {
                var b = (BoolExpr)variables[def.Name];
                solver.Assert(ctx.MkOr(b, ctx.MkNot(b)));
            }
        }
    }
}