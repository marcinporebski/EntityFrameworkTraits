namespace EntityFrameworkTraits;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

[Generator]
public class TraitBasedOnAnnotationGenerator : AbstractBaseGenerator, IIncrementalGenerator
{
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Filter for classes with ApplyTrait attribute
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => s is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: (ctx, _) => GetCandidateClass(ctx))
            .Where(t => t is not null)
            .Collect();
        
        context.RegisterSourceOutput(classDeclarations, (spc, candidates) =>
        {
            foreach (var classInfo in candidates)
            {
                var generatedCode = GenerateImplementation(classInfo!);
                
                spc.AddSource($"{classInfo!.ClassName}.EFTrait.g.cs", SourceText.From(generatedCode, Encoding.UTF8));
            }
        });
    }

    public static ClassInfo? GetCandidateClass(GeneratorSyntaxContext context)
    {
        var classSyntax = (ClassDeclarationSyntax)context.Node;
        var model = context.SemanticModel;

        foreach (var attrList in classSyntax.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                if (attr.Name.ToString() == "ApplyTrait")
                {
                    var classSymbol = model.GetDeclaredSymbol(classSyntax);
                    
                    var interfaces = attr.ArgumentList?.Arguments
                        .SelectMany(arg =>
                        {
                            if (arg.Expression is TypeOfExpressionSyntax typeOfExpr)
                            {
                                var typeSymbol = model.GetTypeInfo(typeOfExpr.Type).Type;
                                return [typeSymbol as INamedTypeSymbol];
                            }
                            else if (arg.Expression is ImplicitArrayCreationExpressionSyntax arrayExpr)
                            {
                                return arrayExpr.Initializer.Expressions
                                    .OfType<TypeOfExpressionSyntax>()
                                    .Select(typeOfExpressionSyntax =>
                                    {
                                        var typeSymbol = model.GetTypeInfo(typeOfExpressionSyntax.Type).Type;
                                        return typeSymbol as INamedTypeSymbol;
                                    });
                            }
                            return [];
                        })
                        .Where(t => t != null)
                        .ToImmutableArray();


                    if (classSymbol != null && interfaces != null)
                    {
                        return new ClassInfo
                        {
                            Namespace = classSymbol.ContainingNamespace.ToString(),
                            ClassName = classSymbol.Name,
                            Interfaces = interfaces.Value
                        };
                    }
                }
            }
        }

        return null;
    }

}