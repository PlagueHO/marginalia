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

## Dependency Updates

Dependabot follows the `dependabot-monthly-v1` policy. Version updates are
scheduled for 09:00 `Pacific/Auckland` on the first calendar day of each month,
including weekends. GitHub may queue a run, so this is a requested maintenance
window rather than a guarantee that updates start or finish at that exact time.

Each configured ecosystem and directory has one `<ecosystem>-minor-and-patch`
group for routine minor and patch updates. In this repository, the canonical
groups are `github-actions-minor-and-patch`,
`devcontainers-minor-and-patch`, `docker-minor-and-patch`,
`dotnet-sdk-minor-and-patch`, and `nuget-minor-and-patch`; the three npm
directories use `npm-minor-and-patch`. Security groups are
`github-actions-security`, `devcontainers-security`, `docker-security`,
`dotnet-sdk-security`, `nuget-security`, and `npm-security` for each npm
directory. Major updates remain separate pull requests, except for the existing
major-version ignores for `react` and `react-dom` in `/marginalia-app`. A
three-day release-age cooldown applies to routine version updates.

Security updates remain event-driven and are grouped separately as
`<ecosystem>-security`; they are not held for the monthly schedule or the
version-update cooldown. If an update cannot wait for the normal window, a
repository maintainer can request a manual Dependabot update check in GitHub.
That check may run outside the monthly schedule.

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
