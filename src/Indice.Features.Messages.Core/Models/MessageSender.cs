namespace Indice.Features.Messages.Core.Models;

/// <summary>The representation of a sender id visible in the recipients phone, email address etc.</summary>
public class MessageSender
{
    /// <summary>Sender id.</summary>
    public Guid Id { get; set; }
    /// <summary>Sender.</summary>
    public string? Sender { get; set; }
    /// <summary>Sender Name.</summary>
    public string? DisplayName { get; set; }
    /// <summary>Specifies the principal that created the sender.</summary>
    public string? CreatedBy { get; set; }
    /// <summary>Specifies when a sender was created.</summary>
    public DateTimeOffset? CreatedAt { get; set; }
    /// <summary>Specifies the principal that update the sender.</summary>
    public string? UpdatedBy { get; set; }
    /// <summary>Specifies when a sender was updated.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
    /// <summary>Indicates the default sender.</summary>
    public bool IsDefault { get; set; }
    /// <summary>Checks for id existence.</summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Sender);

    /// <inheritdoc/>
    public override string ToString() => IsEmpty ? base.ToString()! : $"{DisplayName} <{Sender}>";
}
