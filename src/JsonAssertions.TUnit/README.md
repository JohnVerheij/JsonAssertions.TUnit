# JsonAssertions.TUnit

[![NuGet](https://img.shields.io/nuget/v/JsonAssertions.TUnit.svg)](https://www.nuget.org/packages/JsonAssertions.TUnit/)
[![Downloads](https://img.shields.io/nuget/dt/JsonAssertions.TUnit.svg)](https://www.nuget.org/packages/JsonAssertions.TUnit/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **Scope:** Test projects only. Not intended for production code.

TUnit-native JSON assertions for .NET. Fluent entry points over TUnit's `Assert.That(...)` pipeline for asserting against `System.Text.Json` documents. AOT-compatible, trimmable, no runtime reflection in the assertion path.

> **Full documentation, design notes, and roadmap:** [github.com/JohnVerheij/JsonAssertions.TUnit](https://github.com/JohnVerheij/JsonAssertions.TUnit)

## Status: v0.0.1 (skeleton release)

The 0.0.1 scope is intentionally narrow. The release exists to establish the repository, claim the `JsonAssertions.TUnit` package identifier on nuget.org, and lock the API style and quality bar before the wider catalog ships at 0.1.0.

It ships one concept: **path existence** over a JSON string or a `JsonElement`.

| Entry point | Behaviour |
|---|---|
| `HasJsonProperty(string path)` | Asserts a property exists at the dot-separated `path`. |
| `DoesNotHaveJsonProperty(string path)` | Asserts no property exists at the dot-separated `path`. |

Both are available on a JSON `string` and on a `System.Text.Json.JsonElement`.

## Install

```bash
dotnet add package JsonAssertions.TUnit
```

**Requirements:** TUnit 1.44.0 or later, .NET 10. `System.Text.Json` is in-box on .NET 10, so the package carries no runtime dependency beyond TUnit.

## Quick start

```csharp
using System.Text.Json;

[Test]
public async Task ResponseBodyHasUserName(CancellationToken ct)
{
    string json = """{"user":{"name":"alice"}}""";

    await Assert.That(json).HasJsonProperty("user.name");
    await Assert.That(json).DoesNotHaveJsonProperty("user.email");
}
```

The fluent entry points auto-import via `TUnit.Assertions.Extensions`; no extra `using` directive is needed beyond standard TUnit usings.

A leading `$.` JSONPath-style root prefix is accepted and ignored, so `$.user.name` and `user.name` resolve identically.

## Two namespaces

The single package places types in two namespaces, the same shape as the rest of the assertion family (a framework-agnostic core plus a TUnit adapter):

| Type | Namespace | Auto-imported? |
|---|---|---|
| `JsonPath` (framework-agnostic path navigation) | `JsonAssertions` | No (needs `using JsonAssertions;`) |
| `HasJsonProperty()` / `DoesNotHaveJsonProperty()` (source-generated entry points) | `TUnit.Assertions.Extensions` | Yes (TUnit auto-imports) |

## Roadmap to v0.1.0

The wider surface lands at 0.1.0 as a reviewed pull request:

- **Value-at-path** assertions (assert the value at a path equals an expected value).
- **Shape** assertions (key set, array length, value kinds).
- `HttpResponseMessage` as a first-class entry point.
- Failure messages that surface the resolved path context, the load-bearing reason this is a package rather than a hand-rolled helper.

## Family

Part of an assertion family for TUnit:

- [LogAssertions.TUnit](https://github.com/JohnVerheij/LogAssertions.TUnit)
- [SnapshotAssertions.TUnit](https://github.com/JohnVerheij/SnapshotAssertions.TUnit)
- [TimeAssertions.TUnit](https://github.com/JohnVerheij/TimeAssertions.TUnit)
- [MathAssertions.TUnit](https://github.com/JohnVerheij/MathAssertions.TUnit)

## License

[MIT](https://github.com/JohnVerheij/JsonAssertions.TUnit/blob/main/LICENSE). Copyright (c) 2026 John Verheij.
