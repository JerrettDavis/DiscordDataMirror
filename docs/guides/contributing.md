# Contributing to DiscordDataMirror

Thank you for your interest in contributing! This document provides guidelines and information for contributors.

## Code of Conduct

Be respectful and inclusive. We welcome contributors of all backgrounds and experience levels.

## Ways to Contribute

### 🐛 Report Bugs

Found a bug? [Open an issue](https://github.com/JerrettDavis/DiscordDataMirror/issues/new) with:
- Clear description of the problem
- Steps to reproduce
- Expected vs actual behavior
- Environment details (OS, .NET version, etc.)
- Relevant logs (redact sensitive info)

### 💡 Suggest Features

Have an idea? [Open a discussion](https://github.com/JerrettDavis/DiscordDataMirror/discussions) to:
- Describe the feature
- Explain the use case
- Discuss implementation approaches

### 📝 Improve Documentation

Documentation improvements are always welcome:
- Fix typos and errors
- Add examples
- Clarify confusing sections
- Translate to other languages

### 🔧 Submit Code

Ready to code? Follow the process below.

## Development Setup

### Prerequisites

- .NET 10 SDK
- Docker Desktop
- Git
- IDE (Visual Studio, VS Code, Rider)

### Clone and Build

```bash
# Fork the repo on GitHub, then:
git clone https://github.com/YOUR_USERNAME/DiscordDataMirror.git
cd DiscordDataMirror

# Build
dotnet build

# Run tests
dotnet test
```

### Running Locally

```bash
cd src/DiscordDataMirror.AppHost
dotnet run
```

See [DEVELOPMENT.md](../DEVELOPMENT.md) for detailed setup instructions.

## Coding Guidelines

### Architecture

We follow Domain-Driven Design and Clean Architecture:

```
Domain → Application → Infrastructure → Presentation
```

- **Domain**: Entities, value objects, domain events
- **Application**: Commands, queries, handlers (CQRS via MediatR)
- **Infrastructure**: EF Core, Discord.Net, external services
- **Presentation**: Blazor dashboard, bot worker service

### Code Style

We use standard .NET conventions:

- **Naming**: PascalCase for public members, camelCase for private
- **Formatting**: Run `dotnet format` before committing
- **Documentation**: XML comments on public APIs

### Testing

- Write tests for new features
- Maintain existing test coverage
- Unit tests for domain/application logic
- Integration tests for infrastructure

### Commit Messages

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add user search functionality
fix: resolve null reference in message handler
docs: update configuration guide
test: add unit tests for sync service
refactor: extract message parsing logic
chore: update dependencies
```

## Pull Request Process

### 1. Create a Branch

```bash
git checkout -b feat/your-feature-name
# or
git checkout -b fix/issue-number-description
```

### 2. Make Changes

- Write code following our guidelines
- Add/update tests
- Update documentation if needed
- Run `dotnet format` to fix styling

### 3. Test Locally

```bash
# Run all tests
dotnet test

# Run specific tests
dotnet test --filter "FullyQualifiedName~YourTestClass"
```

### 4. Push and Create PR

```bash
git push origin feat/your-feature-name
```

Then open a Pull Request on GitHub with:
- Clear title describing the change
- Description of what and why
- Link to related issues
- Screenshots for UI changes

### 5. Code Review

- Address reviewer feedback
- Keep discussions constructive
- Squash commits if requested

### 6. Merge

Once approved, a maintainer will merge your PR. 🎉

## Project Structure

```
DiscordDataMirror/
├── src/
│   ├── DiscordDataMirror.AppHost/        # Aspire orchestrator
│   ├── DiscordDataMirror.ServiceDefaults/ # Shared Aspire config
│   ├── DiscordDataMirror.Domain/          # Domain layer
│   ├── DiscordDataMirror.Application/     # Application layer
│   ├── DiscordDataMirror.Infrastructure/  # Infrastructure layer
│   ├── DiscordDataMirror.Bot/             # Discord bot
│   └── DiscordDataMirror.Dashboard/       # Blazor dashboard
├── tests/
│   ├── DiscordDataMirror.Domain.Tests/
│   ├── DiscordDataMirror.Application.Tests/
│   └── DiscordDataMirror.Integration.Tests/
├── docs/                                   # Documentation
└── scripts/                                # Utility scripts
```

## Key Technologies

- **.NET 10** — Runtime
- **Aspire** — Orchestration and observability
- **Entity Framework Core** — Database ORM
- **MediatR** — CQRS implementation
- **Discord.Net** — Discord API client
- **Blazor Server** — Dashboard UI
- **MudBlazor** — UI components
- **PostgreSQL** — Database
- **xUnit** — Testing framework

## Getting Help

- **Questions**: Open a [discussion](https://github.com/JerrettDavis/DiscordDataMirror/discussions)
- **Bugs**: Open an [issue](https://github.com/JerrettDavis/DiscordDataMirror/issues)
- **Chat**: Join our Discord server (coming soon)

## Recognition

Contributors are recognized in:
- GitHub contributors list
- Release notes
- Project README (for significant contributions)

Thank you for contributing! 🙏
