# JsonAssertions.TUnit

[![CI](https://github.com/JohnVerheij/JsonAssertions.TUnit/actions/workflows/ci.yml/badge.svg)](https://github.com/JohnVerheij/JsonAssertions.TUnit/actions/workflows/ci.yml)
[![CodeQL](https://github.com/JohnVerheij/JsonAssertions.TUnit/actions/workflows/codeql.yml/badge.svg)](https://github.com/JohnVerheij/JsonAssertions.TUnit/actions/workflows/codeql.yml)
[![codecov](https://codecov.io/gh/JohnVerheij/JsonAssertions.TUnit/branch/main/graph/badge.svg)](https://codecov.io/gh/JohnVerheij/JsonAssertions.TUnit)
[![NuGet](https://img.shields.io/nuget/v/JsonAssertions.TUnit.svg)](https://www.nuget.org/packages/JsonAssertions.TUnit/)
[![Downloads](https://img.shields.io/nuget/dt/JsonAssertions.TUnit.svg)](https://www.nuget.org/packages/JsonAssertions.TUnit/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

TUnit-native JSON assertions for .NET. Fluent entry points over TUnit's `Assert.That(...)` pipeline for asserting against `System.Text.Json` documents. AOT-compatible, trimmable, no runtime reflection in the assertion path.

> **Scope:** Test projects only. Not intended for production code.

---

## Status: v0.2.0

Property existence, value-at-path, value-predicate, value-one-of, value-parsable-as-`T`, and shape (kind / array-length / non-empty / boolean / non-empty-string) assertions. Each fluent entry point is available over a JSON `string`, a `System.Text.Json.JsonElement`, and an `HttpResponseMessage` (whose body is read as the JSON document):

| Entry point | Behaviour |
|---|---|
| `HasJsonProperty(string path)` | Asserts a property exists at the path. |
| `DoesNotHaveJsonProperty(string path)` | Asserts no property exists at the path. |
| `HasJsonValue(string path, string\|bool\|number expected)` | Asserts the value at `path` equals `expected`. |
| `HasJsonValueOneOf(string path, string[]\|double[] candidates)` | Asserts the value at `path` is one of the given strings or numbers. |
| `HasJsonValueMatching(string path, Func<JsonElement, bool> predicate)` | Asserts the value at `path` satisfies the predicate. |
| `HasJsonValueParsableAs<T>(string path)` *where T : IParsable&lt;T&gt;* | Asserts the value at `path` is a JSON string parseable as `T` (covers `Guid`, `DateTimeOffset`, `Uri`, ...). |
| `HasJsonValueKind(string path, JsonValueKind kind)` | Asserts the value at `path` is of the given kind. |
| `HasJsonBoolean(string path)` | Asserts the value at `path` is a JSON boolean (either `true` or `false`). |
| `HasNonEmptyJsonString(string path)` | Asserts the value at `path` is a non-empty JSON string. |
| `HasJsonArrayLength(string path, int length)` | Asserts the value at `path` is a JSON array of the given length. |
| `HasNonEmptyJsonArray(string path)` / `HasEmptyJsonArray(string path)` | Asserts the value at `path` is a non-empty / empty JSON array. |

The point over a hand-rolled `TryGetProperty(...).IsTrue()` helper is the **failure message**: every assertion renders a path-context block saying *where* resolution stopped, not merely that it did.

---

## Install

```bash
dotnet add package JsonAssertions.TUnit
```

**Requirements:** TUnit 1.44.39 or later, .NET 10. `System.Text.Json` is in-box on .NET 10, so the package carries no runtime dependency beyond TUnit. The package is AOT-compatible, trimmable, and uses no runtime reflection in the assertion path.

## Quick start

```csharp
using System.Text.Json;

[Test]
public async Task ResponseBodyHasExpectedShape(CancellationToken ct)
{
    string json = """{"user":{"name":"alice","age":30,"active":true,"id":"550e8400-e29b-41d4-a716-446655440000"},"items":[{"name":"x"}],"status":"Healthy"}""";

    await Assert.That(json).HasJsonProperty("user.name");
    await Assert.That(json).DoesNotHaveJsonProperty("user.email");
    await Assert.That(json).HasJsonValue("user.age", 30);
    await Assert.That(json).HasJsonValue("user.active", true);
    await Assert.That(json).HasJsonProperty("items[0].name");
    await Assert.That(json).HasJsonValueOneOf("status", ["Healthy", "Degraded", "Unhealthy"]);
    await Assert.That(json).HasJsonValueParsableAs<Guid>("user.id");
}
```

The fluent entry points auto-import via `TUnit.Assertions.Extensions`; no extra `using` directive is needed beyond standard TUnit usings.

The same entry points are available on a `JsonElement`, and directly on an `HttpResponseMessage` whose body is read as the JSON document. The `HttpResponseMessage` overloads are asynchronous and take an optional `CancellationToken` that flows to the body read:

```csharp
using var document = JsonDocument.Parse(json);
await Assert.That(document.RootElement).HasJsonValue("user.name", "alice");

// Directly on an HTTP response - the body is read and asserted against:
await Assert.That(response).HasJsonProperty("user.name", ct);
await Assert.That(response).HasJsonArrayLength("items", 3, ct);
```

A response body or string that is not valid JSON fails the assertion with an explained message rather than throwing a raw `JsonException`; a body that cannot be parsed does not vacuously satisfy `DoesNotHaveJsonProperty`.

When an assertion fails, the message names the failure point rather than just reporting a `false`:

```text
to have a JSON property at path "user.address.city"
  resolved as far as: user.address
  no property "city" on "user.address"
```

## Path syntax

A path is a sequence of dot-separated property names and zero-based bracket indices, navigated from the asserted element (or, for the `string` overload, from the parsed document's root):

- `user.address.city` resolves `user`, then `address`, then `city`.
- `items[0].name` resolves the first element of `items`, then `name` on it. Indices are zero-based, non-negative integers. Property and index segments compose freely (`objects[0].planData[1].pickPlanId`).
- `$` is the JSONPath root reference: `$` alone resolves to the asserted element itself; `$.user.name` is equivalent to `user.name`; `$[0]` (and bare `[0]`) is equivalent against a root array.
- A path that traverses a non-object value where a property is expected (or a non-array where an index is expected) resolves to "not found" rather than throwing; the failure message names which segment blocked the resolution.
- An empty path, a whitespace path, an empty / non-numeric / negative bracket index, an unclosed `[`, a property name directly after `]` without a `.` separator, or a doubled or leading / trailing dot throws `ArgumentException`.

Wildcard segments (e.g. `[*]`) are not part of the 0.2.0 surface; see the roadmap below.

## Two namespaces

The single package places types in two namespaces, the same shape as the rest of the assertion family (a framework-agnostic core plus a TUnit adapter):

| Type | Namespace | Auto-imported? |
|---|---|---|
| `JsonPath`, `JsonPathResolution`, `JsonValueComparison`, `JsonShape` (framework-agnostic core) | `JsonAssertions` | No (needs `using JsonAssertions;`) |
| Source-generated assertion entry points (`HasJsonProperty()`, `HasJsonValue()`, `HasJsonArrayLength()`, ...) | `TUnit.Assertions.Extensions` | Yes (TUnit auto-imports) |

`JsonPath.Resolve(JsonElement, string)` is the framework-agnostic primitive: it returns a `JsonPathResolution` carrying the resolved element on success and the failure-point context (how far it got, which segment failed, what kind of value blocked it) on failure. `JsonValueComparison.Matches` compares a resolved element against an expected value, and `JsonShape` provides the array-length / value-kind predicates. The TUnit entry points are thin `[GenerateAssertion]` wrappers over them. A future package split (a bare-identifier `JsonAssertions` core package plus this adapter) would fall along exactly this namespace seam; shipping the seam now keeps that option open without committing to it.

## Modern .NET 10+ practices on display

The package is a deliberate showcase of modern .NET conventions:

- **AOT-compatible** (`IsAotCompatible=true`), trimmable (`IsTrimmable=true`), no runtime reflection in the assertion path.
- **Source-generated assertion entries** via TUnit's `[GenerateAssertion]`. No interface implementation required, no reflection at runtime.
- **C# 14 file-scoped namespaces** + `Nullable=enable` + `TreatWarningsAsErrors=true` + five Roslyn analyzer packs at full strength (Meziantou, SonarAnalyzer, Roslynator, Microsoft.VisualStudio.Threading, DotNetProjectFile).
- **`Microsoft.CodeAnalysis.BannedApiAnalyzers`** enforces no-reflection at build time via a shared `BannedSymbols.txt`.

## Stability intent (pre-1.0)

This is a 0.x release and the public API may evolve.

- **Additive changes** (new entry points, new input overloads) ship in any patch without breaking ApiCompat.
- **Breaking changes** to existing signatures bump the minor version (0.X.0) and are called out in the [CHANGELOG](CHANGELOG.md). The 0.0.1 -> 0.1.0 step evolved the `[GenerateAssertion]` source-method return types from `bool` to `AssertionResult` to enable the path-context failure messages; the generated TUnit chain extensions were unaffected at chain-syntax level. The 0.1.0 -> 0.2.0 step was purely additive (new segment forms in the existing path grammar, plus the five new entry points listed above).
- `PackageValidationBaselineVersion` pins to the previous shipped version so ApiCompat breakage is caught at pack time; `CompatibilitySuppressions.xml` records accepted differences.
- Failure-message text is not part of the stable public surface; pin behaviour against the `JsonPath` / `JsonValueComparison` / `JsonShape` primitives, not against full message-text equality.
The 1.0 milestone signals API stability.

## Roadmap

The next increments, each as a reviewed pull request:

- Semantic JSON equality and subset / fragment matching (`IsEquivalentJsonTo`, `ContainsJson`).
- Wildcard path segments (`items[*]` and similar) when consumer evidence accumulates.

### Out of scope

- **Production-code use** (per the scope blockquote above).
- **JSON schema validation** out of scope for this package: it needs a JSON Schema engine (a runtime dependency `System.Text.Json` does not provide) and is a different mental model from path / value / shape assertions. A future opt-in adapter package could add it if demand appears, keeping the schema-engine dependency out of this zero-dependency core.
- **A JSON serializer or parser.** The package builds on `System.Text.Json`; it does not replace it.

## Family

Part of an assertion family for TUnit, each package independently versioned, targeting the same .NET TFM at any moment:

- **[`LogAssertions.TUnit`](https://www.nuget.org/packages/LogAssertions.TUnit/):** fluent log assertions over `Microsoft.Extensions.Logging.Testing.FakeLogCollector`.
- **[`SnapshotAssertions.TUnit`](https://www.nuget.org/packages/SnapshotAssertions.TUnit/):** text-snapshot assertions for API-surface tests and similar deterministic-string scenarios.
- **[`TimeAssertions.TUnit`](https://www.nuget.org/packages/TimeAssertions.TUnit/):** assertion-level timing budgets via `.And.WithinTimeBudget(...)`.
- **[`MathAssertions.TUnit`](https://www.nuget.org/packages/MathAssertions.TUnit/):** tolerance comparisons, sequences, statistics, linear algebra, number theory, 3D geometry.

## Contributing

Issues and pull requests welcome. Before opening a PR:

- Run `dotnet build` and `dotnet test` locally; the CI pipeline enforces the same quality bar (zero warnings as errors, 90% line / 90% branch coverage minimum).
- Match the existing code style (`.editorconfig` is authoritative; `dotnet format` covers formatting).
- For new assertions, include a test for both the happy path and a representative failure case.

For larger ideas, open a [Discussion](https://github.com/JohnVerheij/JsonAssertions.TUnit/discussions) first to align on direction before investing implementation time.

See [CONTRIBUTING.md](CONTRIBUTING.md) for the full PR review checklist, and [CONVENTIONS.md](CONVENTIONS.md) for the family-wide code conventions shared across `LogAssertions.TUnit`, `SnapshotAssertions.TUnit`, `TimeAssertions.TUnit`, `MathAssertions.TUnit`, and this repo.

## License

[MIT](LICENSE). Copyright (c) 2026 John Verheij.
