using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Indice.BackgroundTasks
{
    /// <summary>
    /// Extensions on various <see cref="Expression"/> objects.
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Analyzes an expression to a <see cref="BackgroundTaskInfo"/> object.
        /// </summary>
        /// <param name="expression">The expression to analyze.</param>
        public static BackgroundTaskInfo ToBackgroundTaskInfo(this Expression<Func<CancellationToken, Task>> expression) {
            var methodCallArgumentResolutionVisitor = new MethodCallArgumentResolutionVisitor();
            var expressionWithArgumentsResolved = (Expression<Func<CancellationToken, Task>>)methodCallArgumentResolutionVisitor.Visit(expression);
            var method = ((MethodCallExpression)expressionWithArgumentsResolved.Body);
            var methodInfo = method.Method;
            var arguments = method.Arguments.Select(x => {
                var value = ((ConstantExpression)x).Value;
                return value;
            }).ToArray();
            return methodInfo.ToBackgroundTaskInfo(arguments);
        }

        //public static BackgroundTaskInfo ToBackgroundTaskInfo(this Expression<Action> expression) {
        //    var methodCallArgumentResolutionVisitor = new MethodCallArgumentResolutionVisitor();
        //    var expressionWithArgumentsResolved = (Expression<Action>)methodCallArgumentResolutionVisitor.Visit(expression);
        //    var method = ((MethodCallExpression)expressionWithArgumentsResolved.Body);
        //    var m = method.Method;
        //    var args = method.Arguments
        //        .Select(a => {
        //            var value = ((ConstantExpression)a).Value;
        //            return value;
        //        })
        //        .ToArray();
        //    return new BackgroundTaskInfo { };
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="arguments"></param>
        public static BackgroundTaskInfo ToBackgroundTaskInfo(this MethodInfo methodInfo, object[] arguments) {
            var methodParameters = methodInfo.GetParameters();
            var argumentTypes = new Type[methodParameters.Length];
            for (var i = 0; i < methodParameters.Length; ++i) {
                argumentTypes[i] = methodParameters[i].ParameterType;
            }
            var taskInfo = new BackgroundTaskInfo {
                AssemblyName = methodInfo.DeclaringType.Assembly.FullName,
                TypeName = methodInfo.DeclaringType.FullName,
                MethodName = methodInfo.Name,
                Arguments = arguments,
                ArgumentTypes = argumentTypes,
                Id = Guid.NewGuid().ToString(),
                CreatedAtUtc = DateTime.UtcNow,
                ReturnType = methodInfo.ReturnType
            };
            return taskInfo;
        }
    }
}
