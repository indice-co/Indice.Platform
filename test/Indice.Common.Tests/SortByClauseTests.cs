using Indice.Types;
using Xunit;

namespace Indice.Common.Tests;

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
    [InlineData("(datetime)total-")]
    [InlineData("(String)total-")]
    [InlineData("(number)total+")]
    [InlineData("total+")]
    [InlineData("total-")]
    public void SortByClauseParsingTest(string input) {
        var sortBy = SortByClause.Parse(input);
        Assert.Equal(input, sortBy.ToString(), ignoreCase:true);
    }

    [Fact]
    public void SortByClauseListOptionsAddTest() {
        var options = new ListOptions() {
            Sort = "code-"
        };
        options.AddSort((SortByClause)"total-");
        Assert.Equal("code-,total-", options.Sort);
        options.AddSort((SortByClause)"date");
        Assert.Equal("code-,total-,date+", options.Sort);
        options.RemoveSort((SortByClause)"total-");
        Assert.Equal("code-,date+", options.Sort);
        options.AddSort((SortByClause)"date-");
        Assert.Equal("code-,date-", options.Sort);
    }
}
