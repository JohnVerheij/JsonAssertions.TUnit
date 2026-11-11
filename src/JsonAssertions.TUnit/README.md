# JsonAssertions.TUnit

[![NuGet](https://img.shields.io/nuget/v/JsonAssertions.TUnit.svg)](https://www.nuget.org/packages/JsonAssertions.TUnit/)
[![Downloads](https://img.shields.io/nuget/dt/JsonAssertions.TUnit.svg)](https://www.nuget.org/packages/JsonAssertions.TUnit/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **Scope:** Test projects only. Not intended for production code.

> Part of the **[DotNetAssertions](https://dotnetassertions.dev)** family of assertion extensions for TUnit.

TUnit-native JSON assertions for .NET. Fluent entry points over TUnit's `Assert.That(...)` pipeline for asserting against `System.Text.Json` documents, HTTP response bodies (including RFC 7807 ProblemDetails), and the registration state of source-generated `JsonSerializerContext` instances. AOT-compatible, trimmable, no runtime reflection in the assertion path.

> **Full documentation, design notes, and roadmap:** [github.com/JohnVerheij/JsonAssertions.TUnit](https://github.com/JohnVerheij/JsonAssertions.TUnit)

## Status

Each path / value / shape entry point is available over a JSON `string`, a `System.Text.Json.JsonElement`, and an `HttpResponseMessage` (whose body is read as the JSON document). HTTP-response and AOT-context assertions target their natural receiver type.

| Entry point | Behavior |
|---|---|
| `HasJsonProperty(path)` | Asserts a property exists at the path. |
| `HasJsonProperties([path, ...])` *(v0.7.0+)* | Asserts a property exists at each of the paths, against one parse; the failure lists every path that does not resolve. |
| `DoesNotHaveJsonProperty(path)` | Asserts no property exists at the path. |
| `HasJsonValue(path, expected)` | Asserts the value at `path` equals `expected` (a `string`, `bool`, or number; the `int` / `uint` / `long` / `ulong` overloads match a number or a string that holds a JSON integer literal, so a string-encoded 64-bit integer such as Protobuf `int64` / `uint64` matches too). |
| `HasJsonValueOneOf(path, T[])` | Asserts the value at `path` is one of the given values (`string[]`, `double[]`, `int[]`, `uint[]`, `long[]`, or `ulong[]`). |
| `HasJsonValueMatching(path, predicate)` | Asserts the value at `path` satisfies `Func<JsonElement, bool>`. |
| `HasJsonValueParsableAs<T>(path)` | Asserts the value at `path` is a JSON string parseable as `T` (where `T : IParsable<T>`). |
| `HasJsonValueKind(path, kind)` | Asserts the value at `path` is of the given `JsonValueKind`. |
| `HasJsonBoolean(path)` | Asserts the value at `path` is a JSON boolean (`true` or `false`). |
| `HasNonEmptyJsonString(path)` | Asserts the value at `path` is a non-empty JSON string. |
| `HasJsonArrayLength(path, length)` | Asserts the value at `path` is a JSON array of the given length. |
| `HasNonEmptyJsonArray(path)` / `HasEmptyJsonArray(path)` | Asserts the value at `path` is a non-empty / empty JSON array. |
| `IsEquivalentJsonTo(expected)` / `IsEquivalentJsonTo(expected, configure)` over a JSON `string` or `JsonElement` | Asserts the whole document is structurally equivalent to `expected`, independent of property order and number form (`1` == `1.0`). The configure callback sets `IgnorePath` and `IgnoreArrayOrder`. |
| `ContainsJson(expectedSubset)` / `ContainsJson(expectedSubset, configure)` over a JSON `string`, `JsonElement`, or `HttpResponseMessage` *(v0.6.0+)* | Asserts the document contains the expected subset (extra actual properties ignored); the failure lists every missing or mismatched path. Same options as `IsEquivalentJsonTo`. |
| `GetJsonValue<T>(path)` / `GetJsonString(path)` / `GetJsonElement(path)` over a JSON `string` or `JsonElement` (and `...Async` on `HttpResponseMessage`) *(v0.6.0+)*; `GetJsonValues<T>(path)` *(v0.7.0+)* | Reads a typed value out at `path` and returns it (not an assertion), for driving later requests or cross-checks; `GetJsonValue<T>` parses a JSON string, number, or boolean, and `GetJsonValues<T>` reads every value a `[*]` path expands to. Throws `JsonExtractionException` with path context when the path does not resolve or a value does not parse, and `ArgumentException` for a malformed path, before any navigation. |
| `HasJsonResponse<T>(status, JsonTypeInfo<T>, T expected, ct)` on `HttpResponseMessage` | Asserts status + AOT-clean deserialization + equality under `object.Equals` in one chain (use a record whose members have value equality, or override `Equals` on `T`). |
| `MatchesProblemDetails(status, ..., ct)` on `HttpResponseMessage` | Asserts an RFC 7807 `application/problem+json` response with matching fields; `status` must match both the HTTP status code and the body's `status` field (v0.7.0+), as RFC 7807 requires. |
| `MatchesValidationProblemDetails(status, errors, ..., ct)` on `HttpResponseMessage` | Like `MatchesProblemDetails` plus the ASP.NET Core `errors` dictionary. |
| `RoundtripsCleanlyVia<T>(JsonTypeInfo<T>)` on any `T` | Asserts serialize -> deserialize -> re-serialize is byte-identical via the supplied source-generated `JsonTypeInfo<T>`. |
| `AsJsonContext().HasJsonTypeInfoFor<T>()` on a `JsonSerializerContext`-typed source | Asserts the supplied source-generated context registers a `JsonTypeInfo<T>` for `T`. |
| `JsonRenderers.ReformatJson<T>(JsonTypeInfo<T>)` *(static factory)* | Returns `Func<string, string>` that canonicalizes a JSON string via the consumer's `JsonSerializerContext`; composes with `SnapshotAssertions.TUnit`'s `MatchesSnapshot(Func<>)` at the consumer's call site. |
| `JsonCanonicalizer.Canonicalize(json)` / `Canonicalize(json, configure)` *(static, v0.4.0+)* | Typeless structural canonical form (sorted keys, stable indent, all fields preserved) with `[*]`-aware `ScrubPath` scrubbing of volatile values; for pinning a whole response shape as a snapshot. Needs no `JsonSerializerContext` (unlike `ReformatJson<T>`) and keeps unknown fields. |

The path is a dot-separated property navigation with optional `[N]` zero-based bracket indices and an optional leading `$` JSONPath root reference: `user.name`, `items[0].id`, `objects[0].entries[1].id`, `$[0]` for a root-array first element. The `[*]` wildcard (v0.4.0+) matches every element of an array on `HasJsonProperty`, `HasJsonProperties`, `HasJsonValueMatching` and `GetJsonValues<T>` (`items[*].id`); the single-location assertions reject it. A malformed path throws `ArgumentException` before any navigation. See [the path-syntax notes on GitHub](https://github.com/JohnVerheij/JsonAssertions.TUnit#path-syntax) for the full grammar.

Two notes on `[*]`. It is a "for all" quantifier, so `[*].id` passes **vacuously** on an empty array, whereas `[0].id` *fails* on one; a naive `[0]` to `[*]` migration silently drops the implicit non-emptiness check, so pair the wildcard with `HasNonEmptyJsonArray("items")` when emptiness should fail the test. And `[*]` fits existence and genuinely-uniform value checks only: an element-specific check (for example "the element at index 2 has `id` 2", `HasJsonValue("items[2].id", 2)`) must stay index-scoped rather than become `[*]`.

The point over a hand-rolled `TryGetProperty(...).IsTrue()` helper is the **failure message**: a path assertion whose path does not resolve says *where* resolution stopped and why (`resolved as far as 'user', which has no property 'email'`), not merely that it did.

## Install

```bash
dotnet add package JsonAssertions.TUnit
```

**Requirements:** TUnit 1.83.0 or later, .NET 10. `System.Text.Json` is in-box on .NET 10, so the package carries no runtime dependency beyond TUnit.

## Quick start

```csharp
using System.Text.Json;

[Test]
public async Task ResponseBodyHasExpectedShape(CancellationToken ct)
{
    string json = """{"user":{"name":"alice","age":30},"roles":["admin"]}""";

    await Assert.That(json).HasJsonProperty("user.name");
    await Assert.That(json).HasJsonProperties(["user.age", "roles[0]"]);
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

Each integer overload (`int` / `uint` / `long` / `ulong`) matches the value whether the JSON encodes it as a number or as a string that holds a JSON integer literal (no `+`, white space or leading zero); `long` / `ulong` exist because protobuf serializes 64-bit ints as JSON strings (to avoid 53-bit precision loss) while System.Text.Json writes them as numbers. `double` matches a JSON number only. An integer overload needs an integer literal (`30.0` and `3e1` do not match `30`); `double` and `IsEquivalentJsonTo` accept them. A non-finite expected `double` throws `ArgumentOutOfRangeException`: JSON has no NaN or infinity. Use the `L` / `UL` suffix to assert a 64-bit value (`HasJsonValue("guid.high", 123456789012345L)`).

When an assertion fails, the message names the failure point:

```text
Expected to have a JSON property at path 'user.email'
but resolved as far as 'user', which has no property 'email'

at Assert.That(Json).HasJsonProperty("user.email")
```

A response body or string that is not valid JSON fails the assertion with an explained message rather than throwing a raw `JsonException`, and an empty response body says `the response body is empty`. A body read through its stream before the assertion is gone unless the content was buffered first with `LoadIntoBufferAsync`.

## Subset matching (`ContainsJson`, 0.6.0+)

One declarative assertion in place of a block of per-property checks. The expected fields must be present and equal; the response may carry any additional fields the test does not want to pin. The failure lists every wrong path, not just the first (under `IgnoreArrayOrder()` an unmatched array element is reported as a whole, at its position in the expected array, with no inner path).

```csharp
await Assert.That(response).ContainsJson("""
    { "status": "Healthy",
      "entries": [ { "name": "db", "status": "Healthy" } ] }
    """, ct);
```

**Do not fold a volatile value into the expected subset.** `ContainsJson` asserts *equality* for every field you put in it - "subset" constrains which fields are checked, not how strictly they are compared. A duration reading, a server-generated identifier, or a status that legitimately varies gets pinned to whatever the response said the day the test was written, which asserts more than the contract does and fails on the first run that differs. Collapsing a block of per-property checks feels mechanical, so a volatile field is easy to sweep in with the stable ones, turning a presence check into an equality check. Keep those fields on their own presence or predicate assertion, or exclude them with `o => o.IgnorePath("...")`.

## Namespaces

The package places its own types in two namespaces, the same shape as the rest of the assertion family, and the source generator emits the fluent entry points into TUnit's:

| Type | Namespace | Auto-imported? |
|---|---|---|
| `JsonPath`, `JsonPathResolution`, `JsonValueComparison`, `JsonShape`, `JsonRenderers`, `JsonFailureMessage`, `JsonEquivalence`, `JsonEquivalenceOptions`, `JsonDifference`, `JsonDifferenceKind`, `JsonCanonicalizer`, `JsonCanonicalizeOptions` (framework-agnostic core) | `JsonAssertions` | No (needs `using JsonAssertions;`) |
| Typed extraction (`GetJsonValue<T>()`, `GetJsonValues<T>()`, `GetJsonString()`, `GetJsonElement()` and the `...Async` forms) | `JsonAssertions` | No (needs `using JsonAssertions;`) |
| `IJsonContextAssertionSource` + the `AsJsonContext()` bridge (the AOT-context assertions) | `JsonAssertions.TUnit` | No (needs `using JsonAssertions.TUnit;` when chaining `.AsJsonContext()`) |
| Source-generated assertion entry points | `TUnit.Assertions.Extensions` | Yes (TUnit auto-imports) |

Only the source-generated entry points auto-import. The other three rows need **two** `using` directives between them (`using JsonAssertions;` covers both of its rows), which a single `GlobalUsings.cs` puts in front of every test file:

```csharp
global using JsonAssertions;       // core types + GetJsonValue<T>()
global using JsonAssertions.TUnit; // .AsJsonContext()
```

The extraction methods are the only fluent extension methods in the package that extend the **receiver** instead of the assertion chain, because they return a value rather than asserting one.

```csharp
using JsonAssertions;   // the extraction methods live here, and do not auto-import

// RIGHT: the receiver, not the assertion
var orderId = await response.GetJsonValueAsync<int>("orderId", ct);

// WRONG: ValueAssertion<HttpResponseMessage> has no such method
// await Assert.That(response).GetJsonValueAsync<int>("orderId", ct);
```

## Roadmap

- Semantic JSON equality already ships as `IsEquivalentJsonTo` (0.5.0); subset matching (`ContainsJson`) and typed extraction (`GetJsonValue<T>` / `GetJsonString` / `GetJsonElement`) in 0.6.0; `HasJsonProperties` and `GetJsonValues<T>` in 0.7.0. Further increments are demand-driven.

## Family

Part of an assertion family for TUnit:

- [LogAssertions.TUnit](https://github.com/JohnVerheij/LogAssertions.TUnit)
- [SnapshotAssertions.TUnit](https://github.com/JohnVerheij/SnapshotAssertions.TUnit)
- [TimeAssertions.TUnit](https://github.com/JohnVerheij/TimeAssertions.TUnit)
- [MathAssertions.TUnit](https://github.com/JohnVerheij/MathAssertions.TUnit)
- [SseAssertions.TUnit](https://github.com/JohnVerheij/SseAssertions.TUnit)
- [GrpcAssertions.TUnit](https://github.com/JohnVerheij/GrpcAssertions.TUnit)
- [TracingAssertions.TUnit](https://github.com/JohnVerheij/TracingAssertions.TUnit)
- [MetricsAssertions.TUnit](https://github.com/JohnVerheij/MetricsAssertions.TUnit)

## License

[MIT](https://github.com/JohnVerheij/JsonAssertions.TUnit/blob/main/LICENSE). Copyright (c) 2026 John Verheij.
