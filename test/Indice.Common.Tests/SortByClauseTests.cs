using System;
using Indice.Types;
using Xunit;
using System.Linq;

namespace Indice.Common.Tests
{
    public class SortByClauseTests
    {
        [Fact]
        public void SortByClauseParsingTest()
        {
            var options = new ListOptions() {
                Sort = "code-"
            };
            var list = options.GetSortings().ToArray();
            Assert.Single(list);
            Assert.Equal("code", list[0].Path);
            Assert.Equal(options.Sort, list[0].ToString());
        }
    }
}
