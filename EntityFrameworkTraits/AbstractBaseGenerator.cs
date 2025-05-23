using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace EntityFrameworkTraits;

public abstract class AbstractBaseGenerator: IIncrementalGenerator
{ 
    public abstract void Initialize(IncrementalGeneratorInitializationContext context);
    

    public static string GenerateImplementation(ClassInfo info)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"namespace {info.Namespace};");
        sb.AppendLine();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine($"public partial class {info.ClassName} : {string.Join(", ", info.Interfaces.Select(i => i.ContainingNamespace + "." + i.Name))}");
        sb.AppendLine("{");

        // Collect all properties from all interfaces
        var allProperties = info.Interfaces
            .SelectMany(i => i.GetMembers().OfType<IPropertySymbol>())
            .ToList();

        // Group by property name
        var grouped = allProperties
            .GroupBy(p => p.Name)
            .ToList();

        foreach (var group in grouped)
        {
            var first = group.First();
            var types = group.Select(p => p.Type.ToDisplayString()).Distinct().ToList();

            if (types.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Trait conflict in {info.ClassName}: property '{group.Key}' has multiple types: {string.Join(", ", types)}");
            }

            // Emit property only once
            sb.AppendLine($"    public {first.Type.ToDisplayString()} {first.Name} {{ get; set; }}");
        }

        sb.AppendLine("}");
        sb.AppendLine("// </auto-generated>");
        return sb.ToString();
    }

    
    public record ClassInfo
    {
        public string Namespace { get; set; } = "";
        public string ClassName { get; set; } = "";
        public ImmutableArray<INamedTypeSymbol?> Interfaces { get; set; }
    }
}