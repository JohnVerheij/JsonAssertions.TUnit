# JsonAssertions.TUnit

[![NuGet](https://img.shields.io/nuget/v/JsonAssertions.TUnit.svg)](https://www.nuget.org/packages/JsonAssertions.TUnit/)
[![Downloads](https://img.shields.io/nuget/dt/JsonAssertions.TUnit.svg)](https://www.nuget.org/packages/JsonAssertions.TUnit/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **Scope:** Test projects only. Not intended for production code.

TUnit-native JSON assertions for .NET. Fluent entry points over TUnit's `Assert.That(...)` pipeline for asserting against `System.Text.Json` documents and HTTP response bodies. AOT-compatible, trimmable, no runtime reflection in the assertion path.

> **Full documentation, design notes, and roadmap:** [github.com/JohnVerheij/JsonAssertions.TUnit](https://github.com/JohnVerheij/JsonAssertions.TUnit)

## Status: v0.1.0

Each entry point is available over a JSON `string`, a `System.Text.Json.JsonElement`, and an `HttpResponseMessage` (whose body is read as the JSON document).

| Entry point | Behaviour |
|---|---|
| `HasJsonProperty(path)` | Asserts a property exists at the dot-separated `path`. |
| `DoesNotHaveJsonProperty(path)` | Asserts no property exists at the dot-separated `path`. |
| `HasJsonValue(path, expected)` | Asserts the value at `path` equals `expected` (a `string`, `bool`, or number). |
| `HasJsonArrayLength(path, length)` | Asserts the value at `path` is a JSON array of the given length. |
| `HasNonEmptyJsonArray(path)` / `HasEmptyJsonArray(path)` | Asserts the value at `path` is a non-empty / empty JSON array. |
| `HasJsonValueKind(path, kind)` | Asserts the value at `path` is of the given `JsonValueKind`. |

The point over a hand-rolled `TryGetProperty(...).IsTrue()` helper is the **failure message**: every assertion renders a path-context block saying *where* resolution stopped, not merely that it did.

## Install

```bash
dotnet add package JsonAssertions.TUnit
```

**Requirements:** TUnit 1.44.39 or later, .NET 10. `System.Text.Json` is in-box on .NET 10, so the package carries no runtime dependency beyond TUnit.

## Quick start

```csharp
using System.Text.Json;

[Test]
public async Task ResponseBodyHasExpectedShape(CancellationToken ct)
{
    string json = """{"user":{"name":"alice","age":30},"roles":["admin"]}""";

    await Assert.That(json).HasJsonProperty("user.name");
    await Assert.That(json).HasJsonValue("user.age", 30);
    await Assert.That(json).HasJsonArrayLength("roles", 1);
}
```

The fluent entry points auto-import via `TUnit.Assertions.Extensions`. The same entry points work on a `JsonElement`, and directly on an `HttpResponseMessage`:

```csharp
// Reads the response body and asserts against it. The cancellation token flows to the read.
await Assert.That(response).HasJsonProperty("user.name", ct);
await Assert.That(response).HasJsonValue("user.age", 30, ct);
```

When an assertion fails, the message names the failure point:

```text
to have a JSON property at path "user.address.city"
  resolved as far as: user.address
  no property "city" on "user.address"
```

A response body or string that is not valid JSON fails the assertion with an explained message rather than throwing a raw `JsonException`.

## Two namespaces

The single package places types in two namespaces, the same shape as the rest of the assertion family:

| Type | Namespace | Auto-imported? |
|---|---|---|
| `JsonPath`, `JsonPathResolution`, `JsonValueComparison`, `JsonShape` (framework-agnostic core) | `JsonAssertions` | No (needs `using JsonAssertions;`) |
| Source-generated assertion entry points | `TUnit.Assertions.Extensions` | Yes (TUnit auto-imports) |

## Roadmap

- Deserialise-then-predicate assertions.
- Semantic JSON equality and subset / fragment matching.

## Family

Part of an assertion family for TUnit:

- [LogAssertions.TUnit](https://github.com/JohnVerheij/LogAssertions.TUnit)
- [SnapshotAssertions.TUnit](https://github.com/JohnVerheij/SnapshotAssertions.TUnit)
- [TimeAssertions.TUnit](https://github.com/JohnVerheij/TimeAssertions.TUnit)
- [MathAssertions.TUnit](https://github.com/JohnVerheij/MathAssertions.TUnit)

## License

[MIT](https://github.com/JohnVerheij/JsonAssertions.TUnit/blob/main/LICENSE). Copyright (c) 2026 John Verheij.
