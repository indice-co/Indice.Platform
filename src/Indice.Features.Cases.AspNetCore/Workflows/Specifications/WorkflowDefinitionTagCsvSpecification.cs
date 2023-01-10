using System.Linq.Expressions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;

namespace Indice.Features.Cases.Workflows.Specifications
{
    /// <summary>
    /// WorkflowDefinition Specification for CSV tag.
    /// </summary>
    internal class WorkflowDefinitionTagCsvSpecification : Specification<WorkflowDefinition>
    {
        /// <summary>
        /// The tag to search for.
        /// </summary>
        public string Tag { get; }

        public WorkflowDefinitionTagCsvSpecification(string tag) {
            Tag = tag;
        }

        public override Expression<Func<WorkflowDefinition, bool>> ToExpression() {
            Expression<Func<WorkflowDefinition, bool>> expression = (WorkflowDefinition x) => x.Tag != null && (x.Tag == Tag || x.Tag.StartsWith($"{Tag},") || x.Tag.EndsWith($",{Tag}") || x.Tag.Contains($",{Tag},"));
            expression = expression.WithVersion(VersionOptions.LatestOrPublished);
            return expression;
        }
    }
}
