using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing.Model;

namespace EntityFrameworkTraits.Tests;

using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

public class MyCustomSourceGeneratorTest<TGenerator, TVerifier> 
    : CSharpSourceGeneratorTest<TGenerator, TVerifier>
    where TGenerator : IIncrementalGenerator, new()
    where TVerifier : IVerifier, new()
{
    public MyCustomSourceGeneratorTest()
    {

    }

    protected Task  VerifyDiagnosticsAsync(EvaluatedProjectState primaryProject,
        ImmutableArray<EvaluatedProjectState> additionalProjects, DiagnosticResult[] expected, IVerifier verifier,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

}

