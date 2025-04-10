namespace EntityFrameworkTraits;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ApplyTraitAttribute : Attribute
{
    public Type[] Interfaces { get; }

    public ApplyTraitAttribute(params Type[] interfaces)
    {
        Interfaces = interfaces;
    }
}
