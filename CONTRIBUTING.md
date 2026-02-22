# Contributing to WorkflowForge

Thank you for your interest in improving WorkflowForge. Contributions are welcome and appreciated.

## Ways to Contribute

- Report bugs and request features via [GitHub Issues](https://github.com/animatlabs/workflow-forge/issues)
- Improve documentation (clarity, correctness, examples)
- Add or improve samples and benchmarks
- Fix bugs and add tests
- Enhance extensions or create new ones

## Development Setup

```bash
git clone https://github.com/animatlabs/workflow-forge.git
cd workflow-forge
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

## Pull Request Guidelines

- **Keep PRs focused**: One logical change per PR
- **Include tests**: Add or update tests for code changes where applicable
- **Update documentation**: Modify docs and samples if behavior changes
- **Verify builds**: Ensure `dotnet build` and `dotnet test` pass without warnings
- **Provide context**: Explain why the change is needed and how it works

## Coding Standards

- **Target frameworks**: .NET Standard 2.0 for core libraries; net48, net8.0, and net10.0 for tests, benchmarks, and samples
- **Naming**: Use explicit, descriptive names (avoid abbreviations)
- **XML Documentation**: Add XML docs for all public APIs
- **Async/await**: Use async patterns for I/O operations
- **Dependencies**: Core library must remain dependency-free
- **Breaking changes**: Avoid if possible; if unavoidable, document clearly with migration guide

## Documentation Standards

- **Tone**: Professional, factual, concise (no emojis)
- **Code examples**: Ensure all code snippets compile or mark as pseudocode
- **Links**: Use relative links within repo; verify external links are valid
- **SEO**: Use descriptive headings and keywords for discoverability
- **Accuracy**: Verify all claims against actual codebase behavior

## Testing Requirements

- **Unit tests**: Cover all public API changes
- **Parallel safety**: Tests must run safely in parallel (no shared state)
- **Benchmarks**: Add benchmarks for performance-sensitive changes
- **Samples**: Update interactive samples for user-facing features

## Release Process

1. **Version Bump**: Update `<Version>` and `<PackageReleaseNotes>` in all 13 `.csproj` files
2. **Documentation**: Update `README.md`, `CHANGELOG.md`, and `docs/` with new version and benchmark data
3. **Build & Test**: Run `dotnet build` and `dotnet test` across all target frameworks (net48, net8.0, net10.0)
4. **Pack**: Run `dotnet pack` to generate `.nupkg` and `.snupkg` packages
5. **Sign** (if configured): Sign packages with `dotnet nuget sign` using code-signing certificate
6. **Publish**: Use `python scripts/publish-packages.py --version <version> --publish --api-key <key>` or trigger the GitHub Actions publish workflow
7. **Tag & Release**: Create a GitHub Release with the tag matching the version and reference the CHANGELOG

See [`scripts/README.md`](scripts/README.md) for detailed instructions on strong-name signing (SNK), NuGet package signing (PFX), and GitHub Actions secrets setup.

## Code of Conduct

- Be respectful and collaborative
- Focus on constructive feedback
- Help newcomers and answer questions

---

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).

For questions, open a [GitHub Discussion](https://github.com/animatlabs/workflow-forge/discussions).
