# Contributing to Indice.Platform

👋 Thanks for helping out! `Indice.Platform` is a suite of .NET libraries published to NuGet under `Indice.*` and `Indice.Features.*`. It is mainly maintained by the [Indice](https://www.indice.gr/) platform team, but contributions from outside the team are more than welcome.

| | Contents | |
|---|---|---|
| 🚀 [Getting started](#-getting-started) | 🚦 [Workflow](#-the-workflow-in-5-steps) | 🐛 [Issues & plans](#-issues--plans) |
| 🗺️ [Where things live](#%EF%B8%8F-where-things-live) | 🌿 [Branches & PR titles](#-branches--pr-titles) | ✍️ [Code rules](#%EF%B8%8F-code-rules-the-short-list) |
| 🧪 [Tests](#-tests) | 📦 [Versions & changelog](#-versions--changelog) | ✅ [PR checklist](#-pr-checklist) |
| 🙋 [Questions](#-questions) | | |

## 🚀 Getting started

You need the **.NET SDK 10** (the exact version is pinned in [`global.json`](global.json)) and **Node.js 24** with npm, which builds the Angular SPAs.

Clone the repo, then run this script from the repo root:

```powershell
.\local-feed.ps1
```

It builds the whole solution in `Release`, packs every library into `.\artifacts`, and creates a local NuGet feed in `.\.nuget\packages`. Add that folder as a package source in your own app to try your changes before opening a PR. Nothing is published.

## 🚦 The workflow in 5 steps

1. **Find or open an issue.** Every pull request needs a GitHub issue — *no issue, no PR*.
2. **Write the plan in the issue.** Say how you intend to solve it before you write the code.
3. **Branch off `develop`.**
4. **Code, test, document.**
5. **Open a PR to `develop`** that links the issue (`Closes #123`).

## 🐛 Issues & plans

**A good issue** tells us:

| Type | Include |
|---|---|
| 🐞 Bug | Package + version, target framework, steps to reproduce, expected vs actual |
| ✨ Feature | The problem you are trying to solve, not only the solution you have in mind |

**The plan** is 3–6 bullets on how the issue will be resolved: what changes, in which packages, and whether it produces breaking changes.

- ✅ **Preferred:** post the plan in the issue, so it can be discussed before the work is done.
- ↪️ **Alternative:** if it is not in the issue, it **must** be in the PR description.

## 🗺️ Where things live

| Path | What |
|---|---|
| `src/Indice.*` | Base libraries shared by every product (`Common`, `AspNetCore`, `Services`, `Hosting`, …) |
| `src/Indice.Features.<Product>.Core` | Domain models, services, the product's EF `DbContext` |
| `src/Indice.Features.<Product>.Server` (or `.AspNetCore`) | The Minimal API endpoints |
| `src/Indice.Features.<Product>.App` | The Angular SPA |
| `src/Indice.Features.<Product>.UI` | The .NET host package that embeds the built SPA |
| `test/` | Test projects, mirroring `src/` |

The full package list is in the [`README.md`](README.md).

## 🌿 Branches & PR titles

| | Pattern | Example |
|---|---|---|
| Feature branch | `feature/<area>/<topic>` | `feature/agents/logging` |
| Fix branch | `fix/<area>/<topic>` | `fix/cases/attachment-download` |
| PR title | `<emoji> [area] Summary` | `🤖 [agents] Add Operator workflow` |

Pull requests always target **`develop`**.

| Area | Tag | Emoji |
|---|---|---|
| Base libraries | `[core]` / `[services]` | 🛠️ |
| Identity | `[identity]` | 🛡️ |
| Messages | `[messages]` | 📨 |
| Cases | `[cases]` | 📋 |
| Risk | `[risk]` | 🔍 |
| Agents | `[agents]` | 🤖 |
| Multitenancy | `[multitenancy]` | 🏢 |
| Dependency / framework upkeep | `[servicing]` | ⚙️ |

## ✍️ Code rules (the short list)

- ⭐ **Follow the official best practices** — Microsoft / .NET guidelines for C# code, the Angular style guide for the SPAs. The rules below are our additions on top.
- 📝 **XML docs** on every public type, method and property (`<inheritdoc/>` on implementations).
- 🌐 **Minimal APIs**, split as `*Api.cs` → `*Handlers.cs` → `*Service`. Return `TypedResults`; list endpoints return `ResultSet<T>`.
- ✔️ **FluentValidation** for validation — no DataAnnotations.
- 🧾 **`System.Text.Json`** — never Newtonsoft.
- 🗄️ **EF Core:** entity is `DbXxx`, DTO is `Xxx`; map with `IEntityTypeConfiguration<T>`; use the `DbContext` directly, no repository layer.
- 🔐 **Secure by default:** `.RequireAuthorization()` on every route group, reuse existing policy names.
- 🧩 **DI:** register services with `TryAddTransient` so consumers can override them.
- 🎯 **All three target frameworks** (`net8.0`, `net9.0`, `net10.0`) must compile.
- 🚫 **No new NuGet dependencies or new abstractions** without agreeing on them in the issue first.

This is not the complete rulebook. For more, look for the Indice skills (if you have access to them) or read the existing code and follow what it does.

## 🧪 Tests

We use **xUnit v3**. Some testing is always preferred over none — cover what you change, even if it is just a few tests.

## 📦 Versions & changelog

- ✅ **You:** add an entry to the package's `CHANGELOG.md` when a change is breaking or needs action from consumers (a SQL script, a config change, a renamed API). Not every package has one — skip it if there is none.
- 🚫 **Not you:** don't change `VersionPrefix*` in [`src/Directory.Build.props`](src/Directory.Build.props) and don't publish packages. Maintainers bump versions and publish to NuGet.

## ✅ PR checklist

- [ ] Linked to an issue (`Closes #123`)
- [ ] The plan is in the issue, or in this PR's description
- [ ] `dotnet build Indice.Platform.slnx` succeeds
- [ ] Tests added or updated, and passing
- [ ] Public API has XML docs
- [ ] `CHANGELOG.md` updated if the change is breaking
- [ ] CI is green — build, tests, CodeQL and dependency review run on every PR

## 🙋 Questions

Feel free to contact the Indice platform team, or open an [issue](https://github.com/indice-co/Indice.Platform/issues) and ask.
