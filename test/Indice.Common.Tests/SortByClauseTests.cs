using System;
using Indice.Types;
using Xunit;
using System.Linq;

namespace Indice.Common.Tests
{
    public class SortByClauseTests
    {
        [Fact]
        public void SortByClauseListOptionsTest()
        {
            var options = new ListOptions() {
                Sort = "code-"
            };
            var list = options.GetSortings().ToArray();
            Assert.Single(list);
            Assert.Equal("code", list[0].Path);
            Assert.Equal(options.Sort, list[0].ToString());
        }


        [Theory]
        [InlineData("(number)total-")]
        public void SortByClauseParsingTest(string input) {
            var sortBy = SortByClause.Parse(input);
            Assert.Equal(input, sortBy.ToString());
        }
    }
}
