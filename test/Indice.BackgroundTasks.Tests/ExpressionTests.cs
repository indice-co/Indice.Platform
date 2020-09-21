using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Indice.BackgroundTasks.Tests
{
    public class ExpressionTests
    {
        private readonly ITestOutputHelper _output;

        public ExpressionTests(ITestOutputHelper output) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact]
        public async Task CanTransformExpressionToBackgroundTaskInfo() {
            var result = await CalculateTotal(3, 4);
            Expression<Func<CancellationToken, Task>> expression1 = (cancellationToken) => CalculateTotal(3, 4);
            var backgroundTaskInfo = expression1.ToBackgroundTaskInfo();
            //Expression<Action> expression2 = () => WriteDebugMessage();
            //backgroundTaskInfo = expression2.ToBackgroundTaskInfo();
        }

        private static async Task<int> CalculateTotal(int x, int y) {
            await Task.Delay(1000);
            return x + y;
        }

        private void WriteDebugMessage() {
            _output.WriteLine("I am a message on the out");
        }
    }
}
