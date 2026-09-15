namespace WorkflowForge.Packaging.Smoke.Tests;

/// <summary>
/// Serializes the packaging tests: they each invoke <c>dotnet pack</c> against the repository and
/// would otherwise collide on the same intermediate output.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PackagingCollection
{
    public const string Name = "Packaging";
}
