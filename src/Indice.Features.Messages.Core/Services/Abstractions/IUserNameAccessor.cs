namespace Indice.Features.Messages.Core.Services.Abstractions;

/// <summary>Contains a method to resolve username in various environments.</summary>
public interface IUserNameAccessor
{
    /// <summary>The priority of the accessor when more than one implementations exist.</summary>
    public int Priority { get; }
    /// <summary>Resolves the username.</summary>
    string Resolve();
}
