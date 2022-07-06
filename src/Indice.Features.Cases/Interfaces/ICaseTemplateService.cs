using System.Threading.Tasks;
using Indice.Features.Cases.Models.Responses;

namespace Indice.Features.Cases.Interfaces
{
    public interface ICaseTemplateService
    {
        Task<string> RenderTemplateAsync(CaseDetails @case);
    }
}
