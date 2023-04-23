namespace Indice.Features.Identity.UI;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class IdentityUIAttribute : Attribute
{
    public IdentityUIAttribute(Type implementationTemplate) => 
        Template = implementationTemplate;

    public Type Template { get; }
}
