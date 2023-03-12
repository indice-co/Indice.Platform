using Indice.Types;
using Xunit;

namespace Indice.Common.Tests;

public class ListOptionsTests
{
    [Fact]
    public void CanConvertListOptionsToDictionary() {
        var now = DateTime.UtcNow;
        var listOptions = new ListOptions<DocumentListFilter> {
            Page = 1,
            Size = 50,
            Filter = new DocumentListFilter {
                Code = "abc123",
                From = now,
                IsMarked = true,
                Status = new[] { DocumentStatus.Issued, DocumentStatus.Paid }
            }
        };
        var parameters = listOptions.ToDictionary();
        Assert.True(parameters["page"].ToString() == "1");
        Assert.True(parameters["size"].ToString() == "50");
        Assert.True(parameters["filter.Code"].ToString() == "abc123");
        Assert.True(parameters["filter.From"].ToString() == now.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        Assert.True(parameters["filter.IsMarked"].ToString() == bool.TrueString);
        Assert.True(((string[])parameters["filter.Status"])[0] == "Issued");
        Assert.True(((string[])parameters["filter.Status"])[1] == "Paid");
    }
}

public class DocumentListFilter
{
    public string Code { get; set; }
    public DateTime? From { get; set; }
    public DocumentStatus[] Status { get; set; }
    public bool? IsMarked { get; set; }
}

public enum DocumentStatus : short
{
    Draft = 0,
    Issued = 1,
    Overdue = 2,
    Partial = 3,
    Paid = 4,
    Void = 5,
    Deleted = 6
}
