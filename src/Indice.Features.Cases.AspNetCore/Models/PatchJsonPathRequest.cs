using Indice.Features.Cases.Extensions;
using Json.Patch;
using Json.Pointer;

namespace Indice.Features.Cases.Models;

/// <summary>PatchJsonPathRequest</summary>
public class PatchJsonPathRequest
{
    public OperationType Op { get; set; }
    
    public string Path { get; set; }
    
    public object Value { get; set; }
    
    public string From { get; set; }

    internal PatchOperation ToPatchOperation() {
        var value = Value.ParseAsJsonNode();
        
        return Op switch {
            OperationType.Add => PatchOperation.Add(JsonPointer.Parse(Path), value),
            OperationType.Replace => PatchOperation.Replace(JsonPointer.Parse(Path), value),
            OperationType.Move => PatchOperation.Move(JsonPointer.Parse(From), JsonPointer.Parse(Path)),
            OperationType.Remove => PatchOperation.Remove(JsonPointer.Parse(Path)),
            OperationType.Copy => PatchOperation.Copy(JsonPointer.Parse(From), JsonPointer.Parse(Path)),
            OperationType.Test => PatchOperation.Test(JsonPointer.Parse(Path), value),
            _ => throw new NotSupportedException($"The operation '{Op.ToString()}' is not supported. Valid operations are: 'add', 'replace', 'remove', 'copy', 'move', and 'test'.")
        };
    }
}