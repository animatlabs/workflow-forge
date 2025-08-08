# Contributing to WorkflowForge

Thank you for your interest in improving WorkflowForge. Contributions are welcome and appreciated.

## Ways to Contribute
- Report bugs and request features via GitHub Issues
- Improve documentation (clarity, correctness, examples)
- Add or improve samples and benchmarks
- Fix bugs and add tests

## Development Setup
```bash
git clone https://github.com/animatlabs/workflow-forge.git
cd workflow-forge
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

## Pull Request Guidelines
- Keep PRs focused; one logical change per PR
- Include tests for code changes where applicable
- Update documentation and samples if behavior changes
- Ensure `dotnet build` and `dotnet test` pass

## Coding Standards
- Target .NET 8.0 or later
- Prefer explicit, descriptive naming
- Add XML docs for public APIs
- Avoid breaking changes; if unavoidable, document clearly

## Documentation Standards
- Use factual, concise language (no emojis)
- Ensure code snippets compile or are clearly illustrative
- Keep links relative and valid within the repo

## Release Process
- Update `README.md`, docs and samples for new features
- Update `publish-packages.ps1` versions as needed
- Create a GitHub Release with changelog

By contributing, you agree that your contributions will be licensed under the MIT License.

