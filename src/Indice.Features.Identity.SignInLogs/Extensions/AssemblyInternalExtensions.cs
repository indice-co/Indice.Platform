namespace System.Reflection;

internal static class AssemblyInternalExtensions
{
    public static IEnumerable<Type> GetClassesAssignableFrom<TType>() => 
        Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type => type.IsClass && typeof(TType).IsAssignableFrom(type));
}
