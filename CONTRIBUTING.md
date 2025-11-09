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

- **Target frameworks**: .NET Standard 2.0 for core, .NET 8.0+ for tests/samples
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

- Update version in all `.csproj` files
- Update `README.md`, documentation, and samples for new features
- Update `publish-packages.ps1` versions
- Create a GitHub Release with detailed changelog
- Publish to NuGet using automated script

## Code of Conduct

- Be respectful and collaborative
- Focus on constructive feedback
- Help newcomers and answer questions

---

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).

For questions, open a [GitHub Discussion](https://github.com/animatlabs/workflow-forge/discussions).
