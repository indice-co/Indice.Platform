using Indice.Features.Agents.Core.Models;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Cards;

/// <summary>
/// Renders a retrieved <see cref="CustomerDataRecord"/> as the HTML card the chat surface shows once the
/// person has been verified against the data.
/// </summary>
public interface ICustomerDataCardRenderer
{
    /// <summary>
    /// Renders <paramref name="record"/> into a <c>text/html</c> <see cref="DataContent"/> part, using the
    /// template registered for <see cref="CustomerDataRecord.DataType"/> and falling back to a generic card.
    /// </summary>
    DataContent Render(CustomerDataRecord record);
}
