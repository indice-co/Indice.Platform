using Indice.Features.Cases.Core.Extensions;
using Json.Patch;
using Json.Pointer;

namespace Indice.Features.Cases.Core.Models;

/// <summary>PatchJsonPathRequest</summary>
public class PatchJsonPathRequest
{
    /// <summary>
    /// Operation objects MUST have exactly one "op" member, whose value
    /// indicates the operation to perform.  Its value MUST be one of "add",
    /// "remove", "replace", "move", "copy", or "test"
    /// Operation Type https://datatracker.ietf.org/doc/html/rfc6902#section-4
    /// </summary>
    public OperationType Op { get; set; }
    
    /// <summary>The Path on which to perform the Operation.</summary>
    public string Path { get; set; } = null!;
    
    /// <summary>The Value of the Operation, can differ depending on the Operation</summary>
    public object? Value { get; set; }

    /// <summary>Only Used for Move and Copy Operations.</summary>
    public string? From { get; set; }

    /// <summary>Con</summary>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public PatchOperation ToPatchOperation() {
        var value = Value?.ParseAsJsonNode();
        
        return Op switch {
            OperationType.Add => PatchOperation.Add(JsonPointer.Parse(Path), value),
            OperationType.Replace => PatchOperation.Replace(JsonPointer.Parse(Path), value),
            OperationType.Move => PatchOperation.Move(JsonPointer.Parse(From!), JsonPointer.Parse(Path)),
            OperationType.Remove => PatchOperation.Remove(JsonPointer.Parse(Path)),
            OperationType.Copy => PatchOperation.Copy(JsonPointer.Parse(From!), JsonPointer.Parse(Path)),
            OperationType.Test => PatchOperation.Test(JsonPointer.Parse(Path), value),
            _ => throw new NotSupportedException($"The operation '{Op}' is not supported. Valid operations are: 'add', 'replace', 'remove', 'copy', 'move', and 'test'.")
        };
    }
}