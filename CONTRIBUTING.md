---
title: Contributing Guide
description: How to contribute code, documentation, and issues to Marginalia.
---

## Before You Start

Thank you for contributing to Marginalia.

Use these defaults when setting up your environment:

* Node.js 22 LTS
* pnpm 10+
* .NET 10 SDK
* Docker Desktop

## Local Setup

1. Fork and clone the repository.
1. Install frontend dependencies.
1. Restore backend dependencies.

```powershell
cd marginalia-app
pnpm install

cd ..\marginalia-service
dotnet restore Marginalia.slnx
```

## Development Workflows

Run the full stack locally with Aspire:

```powershell
cd marginalia-service
dotnet run --project src/Orchestration/AppHost/Marginalia.AppHost.csproj
```

Run frontend-only development:

```powershell
cd marginalia-app
pnpm dev
```

## Quality Gates

Before opening a pull request, run the same core checks used in CI:

```powershell
# Frontend
cd marginalia-app
pnpm lint
pnpm test

# Backend
cd ..\marginalia-service
dotnet format Marginalia.slnx --verify-no-changes
dotnet test --solution Marginalia.slnx --filter TestCategory=Unit

# Markdown
cd ..
pnpm lint:md:ci
```

## Pull Requests

When submitting a pull request:

* Keep changes scoped to one problem.
* Include tests or explain why tests are not needed.
* Update documentation when behavior changes.
* Use the pull request template and reference related issues.

## Reporting Bugs and Requesting Features

Use GitHub issue templates for:

* Bug reports
* Feature requests
* Maintenance chores

Include clear reproduction steps and expected behavior.

## Code Style

Follow repository conventions defined in:

* AGENTS.md
* .github/copilot-instructions.md
* .github/instructions/*.instructions.md
