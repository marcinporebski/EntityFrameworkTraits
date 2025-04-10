namespace EntityFrameworkTraits;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

[Generator]
public class TraitBasedOnApplyTraitInvocationGenerator : AbstractBaseGenerator, IIncrementalGenerator
{
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var traitInvocations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is InvocationExpressionSyntax invocation &&
                                            invocation.ToString().Contains("ApplyTrait"),
                transform: static (ctx, _) => GetTraitApplication(ctx))
            .Where(static info => info is not null);

        var classInfos = traitInvocations
            .Collect()
            .Select(static (invocations, _) =>
            {
                return invocations
                    .GroupBy(i => (i?.entityNamespace, i?.entityName))
                    .Select(g => new ClassInfo
                    {
                        Namespace = g.Key.entityNamespace ?? string.Empty,
                        ClassName = g.Key.entityName ?? string.Empty,
                        Interfaces = g.Where(i => i != null).Select(i => i?.traitInterface).ToImmutableArray()
                    })
                    .ToImmutableArray();
            });

        context.RegisterSourceOutput(classInfos, (spc, candidates) =>
        {
            foreach (var classInfo in candidates)
            {
                var generatedCode = GenerateImplementation(classInfo!);
                
                spc.AddSource($"{classInfo!.ClassName}.EFTrait.g.cs", SourceText.From(generatedCode, Encoding.UTF8));
            }
        });
    }

    private static (string entityNamespace, string entityName, INamedTypeSymbol traitInterface)? GetTraitApplication(GeneratorSyntaxContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.Text: "ApplyTrait", TypeArgumentList.Arguments.Count: 2 } genericName })
        {
            var semanticModel = context.SemanticModel;
            var typeArgs = genericName.TypeArgumentList.Arguments;

            var entityTypeSymbol = semanticModel.GetSymbolInfo(typeArgs[0]).Symbol as INamedTypeSymbol;
            var traitTypeSymbol = semanticModel.GetSymbolInfo(typeArgs[1]).Symbol as INamedTypeSymbol;

            if (entityTypeSymbol != null && traitTypeSymbol != null)
            {
                return (entityTypeSymbol.ContainingNamespace.ToString(), entityTypeSymbol.Name, traitTypeSymbol);
            }
        }

        return null;
    }
}