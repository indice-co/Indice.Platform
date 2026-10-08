using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Agents.AI.Workflows.Checkpointing;

namespace Indice.Features.Agents.Core.Extensions;

/// <summary>Extension methods for <see cref="TypeId"/>.</summary>
public static class TypeIdExtensions
{
    private static readonly ConcurrentDictionary<string, Type?> _cache = new(StringComparer.Ordinal);

    /// <summary>Resolves the CLR <see cref="Type"/> represented by a <see cref="TypeId"/>, or <c>null</c> if it cannot be found.</summary>
    public static Type? ToClrType(this TypeId typeId)
        => _cache.GetOrAdd(typeId.ToString(), _ => Resolve(typeId));

    /// <summary>Resolves the CLR <see cref="Type"/> represented by a <see cref="TypeId"/>, throwing if it cannot be found.</summary>
    public static Type ToRequiredClrType(this TypeId typeId)
        => typeId.ToClrType() ?? throw new InvalidOperationException($"Could not resolve type '{typeId}'.");

    private static Type? Resolve(TypeId typeId) {
        // 1. Fast path: fully assembly-qualified name (TypeName may already embed qualified generic args).
        var type = Type.GetType($"{typeId.TypeName}, {typeId.AssemblyName}", throwOnError: false);
        if (type is not null) {
            return type;
        }

        // 2. Version-agnostic: use the simple assembly name only (the same rule TypeId.Equals/IsMatch use).
        var simpleAssemblyName = GetSimpleAssemblyName(typeId.AssemblyName);
        if (simpleAssemblyName is not null) {
            type = Type.GetType($"{typeId.TypeName}, {simpleAssemblyName}", throwOnError: false);
            if (type is not null) {
                return type;
            }
        }

        // 3. Custom resolvers: match against already-loaded assemblies (and try loading by simple name).
        type = Type.GetType(
            $"{typeId.TypeName}, {typeId.AssemblyName}",
            assemblyResolver: name => AppDomain.CurrentDomain.GetAssemblies()
                                          .FirstOrDefault(a => string.Equals(a.GetName().Name, name.Name, StringComparison.Ordinal))
                                      ?? TryLoad(name),
            typeResolver: (asm, name, ignoreCase) => asm?.GetType(name, throwOnError: false, ignoreCase),
            throwOnError: false);
        if (type is not null) {
            return type;
        }

        // 4. Last resort: scan loaded assemblies with the library's own polymorphic matching rules.
        return AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.IsDynamic)
                        .SelectMany(SafeGetTypes)
                        .FirstOrDefault(typeId.IsMatch);
    }

    private static Assembly? TryLoad(AssemblyName name) {
        try { return Assembly.Load(new AssemblyName(name.Name!)); } catch { return null; }
    }

    private static string? GetSimpleAssemblyName(string fullName) {
        try { return new AssemblyName(fullName).Name; } catch { return fullName.Split(',')[0].Trim(); }
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly) {
        try { return assembly.GetTypes(); } catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
    }
}