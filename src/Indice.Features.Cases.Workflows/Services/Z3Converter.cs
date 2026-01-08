using System.Linq.Expressions;
using Microsoft.Z3;

public class Z3ExpressionConverter
{
    private readonly Context _ctx;
    private readonly Dictionary<string, Expr> _variables;

    public Z3ExpressionConverter(Context ctx, Dictionary<string, Expr> variables) {
        _ctx = ctx;
        _variables = variables;
    }

    public BoolExpr? ConvertToZ3(Expression expr) {
        return Walk(expr) as BoolExpr;
    }

    private Expr Walk(Expression expr) {
        return expr switch {
            BinaryExpression binary => WalkBinary(binary),
            MemberExpression member => WalkMember(member),
            ParameterExpression parameter => WalkParameter(parameter),
            ConstantExpression constant => WalkConstant(constant),
            UnaryExpression { NodeType: ExpressionType.Convert } unary => Walk(unary.Operand),
            // todo: what else?
            _ => throw new NotSupportedException($"Unsupported expression type: {expr.NodeType}")
        };
    }

    private Expr WalkParameter(ParameterExpression node) {
        if (_variables.TryGetValue(node.Name, out var z3Var)) {
            return z3Var;
        }

        throw new KeyNotFoundException($"Unknown variable: {node.Name}");
    }

    private Expr WalkBinary(BinaryExpression node) {
        var left = Walk(node.Left);
        var right = Walk(node.Right);

        return node.NodeType switch {
            ExpressionType.GreaterThan => _ctx.MkGt((ArithExpr)left, (ArithExpr)right),
            ExpressionType.LessThan => _ctx.MkLt((ArithExpr)left, (ArithExpr)right),
            ExpressionType.GreaterThanOrEqual => _ctx.MkGe((ArithExpr)left, (ArithExpr)right),
            ExpressionType.LessThanOrEqual => _ctx.MkLe((ArithExpr)left, (ArithExpr)right),
            ExpressionType.Equal => left.Sort.Equals(right.Sort)
                ? _ctx.MkEq(left, right)
                : throw new InvalidOperationException($"Type mismatch in equality: {left.Sort} vs {right.Sort}"),
            ExpressionType.NotEqual => _ctx.MkNot(_ctx.MkEq(left, right)),
            ExpressionType.AndAlso => _ctx.MkAnd((BoolExpr)left, (BoolExpr)right),
            ExpressionType.OrElse => _ctx.MkOr((BoolExpr)left, (BoolExpr)right),
            _ => throw new NotSupportedException($"Unsupported binary op: {node.NodeType}")
        };
    }

    private Expr WalkMember(MemberExpression node) {
        if (node.Expression is ParameterExpression) {
            return _variables[node.Member.Name];
        }

        throw new NotSupportedException($"Unsupported member access: {node}");
    }

    private Expr WalkConstant(ConstantExpression node) {
        return node.Value switch {
            int i => _ctx.MkInt(i),
            long l => _ctx.MkInt(l),
            double d => _ctx.MkReal(d.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            float f => _ctx.MkReal(f.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            string s => _ctx.MkString(s),
            bool b => b ? _ctx.MkTrue() : _ctx.MkFalse(),
            _ => throw new NotSupportedException($"Unsupported constant type: {node.Type}")
        };
    }
}