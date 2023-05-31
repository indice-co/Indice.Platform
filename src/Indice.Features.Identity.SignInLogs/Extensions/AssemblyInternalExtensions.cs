namespace System.Reflection;

internal static class AssemblyInternalExtensions
{
    public static IEnumerable<Type> GetClassesAssignableFrom<TType>(Assembly assembly) => 
        assembly.GetTypes().Where(type => type.IsClass && typeof(TType).IsAssignableFrom(type));
}
