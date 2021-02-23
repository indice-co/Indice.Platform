using Indice.Extensions;
using Xunit;

namespace Indice.Common.Tests
{
    public class TupleExtensionsTests
    {
        [Fact]
        public void CanIdentifyValueTuple() {
            var tuple = (
                Property1: "Property1Value",
                Property2: "Property1Value",
                Property3: 3
            );
            var isTuple = tuple.IsValueTuple();
            Assert.True(isTuple);
        }
    }
}
