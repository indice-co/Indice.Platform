namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// Points a conversation at a record living in an external system (a case, a service pickup, an order).
/// Supplied by the hosting UI as query string parameters (<c>?refid=...&amp;reftype=...</c>) and forwarded on
/// conversation creation, it grounds the customer-data workflow so it can skip the discovery back and forth.
/// </summary>
/// <param name="Id">The external identifier, e.g. a case number.</param>
/// <param name="Type">The kind of record <paramref name="Id"/> points at, e.g. <c>ServicePickup</c>. Also selects the card template used to present the data.</param>
public record ExternalReference(string Id, string Type);
