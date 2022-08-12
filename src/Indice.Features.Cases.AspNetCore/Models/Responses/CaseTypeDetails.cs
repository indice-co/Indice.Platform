using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.Features.Cases.Models.Responses
{
    public class CaseTypeDetails
    {
        public Guid? Id { get; set; }

        public string Code { get; set; }

        public string Title { get; set; }

        public string DataSchema { get; set; }

        public string? Layout { get; set; }

        public string? Translations { get; set; }

        public string? LayoutTranslations { get; set; }
    }
}
