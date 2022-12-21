namespace Indice.Features.Cases.Data.Models
{
    public class DbCaseTypeCategory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? Order { get; set; }
        public string? Translations { get; set; }
    }
}
