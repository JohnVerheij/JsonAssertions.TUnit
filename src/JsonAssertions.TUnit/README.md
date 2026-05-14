# JsonAssertions.TUnit

[![NuGet](https://img.shields.io/nuget/v/JsonAssertions.TUnit.svg)](https://www.nuget.org/packages/JsonAssertions.TUnit/)
[![Downloads](https://img.shields.io/nuget/dt/JsonAssertions.TUnit.svg)](https://www.nuget.org/packages/JsonAssertions.TUnit/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **Scope:** Test projects only. Not intended for production code.

TUnit-native JSON assertions for .NET. Fluent entry points over TUnit's `Assert.That(...)` pipeline for asserting against `System.Text.Json` documents. AOT-compatible, trimmable, no runtime reflection in the assertion path.

> **Full documentation, design notes, and roadmap:** [github.com/JohnVerheij/JsonAssertions.TUnit](https://github.com/JohnVerheij/JsonAssertions.TUnit)

## Status: v0.1.0

Two concepts: **path existence** and **value-at-path**. Each entry point is available over a JSON `string` and a `System.Text.Json.JsonElement`.

| Entry point | Behaviour |
|---|---|
| `HasJsonProperty(path)` | Asserts a property exists at the dot-separated `path`. |
| `DoesNotHaveJsonProperty(path)` | Asserts no property exists at the dot-separated `path`. |
| `HasJsonValue(path, expected)` | Asserts the value at `path` equals `expected` (a `string`, `bool`, or number). |

The point over a hand-rolled `TryGetProperty(...).IsTrue()` helper is the **failure message**: it says *where* on the path resolution stopped, not merely that it did.

## Install

```bash
dotnet add package JsonAssertions.TUnit
```

**Requirements:** TUnit 1.44.0 or later, .NET 10. `System.Text.Json` is in-box on .NET 10, so the package carries no runtime dependency beyond TUnit.

## Quick start

```csharp
using System.Text.Json;

[Test]
public async Task ResponseBodyHasExpectedShape(CancellationToken ct)
{
    string json = """{"user":{"name":"alice","age":30,"active":true}}""";

    await Assert.That(json).HasJsonProperty("user.name");
    await Assert.That(json).DoesNotHaveJsonProperty("user.email");
    await Assert.That(json).HasJsonValue("user.name", "alice");
    await Assert.That(json).HasJsonValue("user.age", 30);
    await Assert.That(json).HasJsonValue("user.active", true);
}
```

The fluent entry points auto-import via `TUnit.Assertions.Extensions`. The same entry points are available on a `JsonElement` for tests that already hold a parsed document.

When an assertion fails, the message names the failure point:

```text
to have a JSON property at path "user.address.city"
  resolved as far as: user.address
  no property "city" on "user.address"
```

## Path syntax

A dot-separated sequence of property names, navigated from the asserted element. A leading `$.` JSONPath-style root prefix is accepted and ignored. A path that traverses a non-object value resolves to "not found" rather than throwing. An empty, whitespace, or empty-segment path throws `ArgumentException`. Array indexing and wildcards are not part of the 0.1.0 surface.

## Two namespaces

The single package places types in two namespaces, the same shape as the rest of the assertion family:

| Type | Namespace | Auto-imported? |
|---|---|---|
| `JsonPath`, `JsonPathResolution`, `JsonValueComparison` (framework-agnostic core) | `JsonAssertions` | No (needs `using JsonAssertions;`) |
| `HasJsonProperty()` / `DoesNotHaveJsonProperty()` / `HasJsonValue()` (source-generated entry points) | `TUnit.Assertions.Extensions` | Yes (TUnit auto-imports) |

## Roadmap

- **Shape assertions** (key set, array length, value kinds).
- **`HttpResponseMessage`** as a first-class entry point.
- Semantic JSON equality and subset / fragment matching.

## Family

Part of an assertion family for TUnit:

- [LogAssertions.TUnit](https://github.com/JohnVerheij/LogAssertions.TUnit)
- [SnapshotAssertions.TUnit](https://github.com/JohnVerheij/SnapshotAssertions.TUnit)
- [TimeAssertions.TUnit](https://github.com/JohnVerheij/TimeAssertions.TUnit)
- [MathAssertions.TUnit](https://github.com/JohnVerheij/MathAssertions.TUnit)

## License

[MIT](https://github.com/JohnVerheij/JsonAssertions.TUnit/blob/main/LICENSE). Copyright (c) 2026 John Verheij.
