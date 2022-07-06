using System.Threading.Tasks;

namespace Indice.Features.Cases.Interfaces
{
    public interface ICasePdfService
    {
        Task<byte[]> HtmlToPdfAsync(string htmlTemplate, bool isPortrait = true);
    }
}
