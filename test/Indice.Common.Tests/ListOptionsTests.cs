using System;
using Indice.Types;
using Xunit;

namespace Indice.Common.Tests
{
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
        public string Number { get; set; }
        public string ParentNumber { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public DateTime? PeriodFrom { get; set; }
        public DateTime? PeriodTo { get; set; }
        public DateTime? ModifiedFrom { get; set; }
        public DateTime? ModifiedTo { get; set; }
        public DateTime? DueFrom { get; set; }
        public DateTime? DueTo { get; set; }
        public DocumentStatus[] Status { get; set; }
        public RecordType? RecordType { get; set; }
        public string RecipientCode { get; set; }
        public string RecipientName { get; set; }
        public Guid? RecipientId { get; set; }
        public string RecipientOrganizationName { get; set; }
        public string RecipientContactName { get; set; }
        public Guid[] TypeId { get; set; }
        public Guid[] ProductId { get; set; }
        public string CustomerReference { get; set; }
        public string PaymentCode { get; set; }
        public string Title { get; set; }
        public bool? IsMarked { get; set; }
        public bool? HasSyncError { get; set; }
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

    public enum RecordType : short
    {
        AccountsPayable = -1,
        AccountsNeutral = 0,
        AccountsReceivable = 1
    }
}
