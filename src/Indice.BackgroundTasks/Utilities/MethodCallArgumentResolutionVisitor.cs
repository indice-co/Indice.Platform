using System.Collections.Generic;
using System.Linq.Expressions;

namespace Indice.BackgroundTasks
{
    /// <summary>
    /// Αν <see cref="ExpressionVisitor"/> whichs helps analyze an expression's method and arguments. 
    /// </summary>
    public class MethodCallArgumentResolutionVisitor : ExpressionVisitor
    {
        /// <summary>
        /// Visits the children of the <see cref="MethodCallExpression"/>.
        /// </summary>
        /// <param name="expression">The representation of the method to call.</param>
        protected override Expression VisitMethodCall(MethodCallExpression expression) {
            var argumentExpressions = new List<Expression>();
            foreach (var argument in expression.Arguments) {
                // Create a representation of a lambda expression given the argument expression.
                var argumentLambda = Expression.Lambda(argument);
                // Compile the expression and invoke the argument lambda to get the argument value.
                var argumentValue = argumentLambda.Compile().DynamicInvoke();
                // Represent the argument value as a constant expression.
                var valueExpression = Expression.Constant(argumentValue, argument.Type);
                argumentExpressions.Add(valueExpression);
            }
            // Construct a representation of the expression's method and arguments.
            return Expression.Call(expression.Object, expression.Method, argumentExpressions);
        }
    }
}
