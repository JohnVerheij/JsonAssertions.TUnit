# JsonAssertions.TUnit

[![NuGet](https://img.shields.io/nuget/v/JsonAssertions.TUnit.svg)](https://www.nuget.org/packages/JsonAssertions.TUnit/)
[![Downloads](https://img.shields.io/nuget/dt/JsonAssertions.TUnit.svg)](https://www.nuget.org/packages/JsonAssertions.TUnit/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **Scope:** Test projects only. Not intended for production code.

TUnit-native JSON assertions for .NET. Fluent entry points over TUnit's `Assert.That(...)` pipeline for asserting against `System.Text.Json` documents, HTTP response bodies (including RFC 7807 ProblemDetails), and the registration state of source-generated `JsonSerializerContext` instances. AOT-compatible, trimmable, no runtime reflection in the assertion path.

> **Full documentation, design notes, and roadmap:** [github.com/JohnVerheij/JsonAssertions.TUnit](https://github.com/JohnVerheij/JsonAssertions.TUnit)

## Status

Each path / value / shape entry point is available over a JSON `string`, a `System.Text.Json.JsonElement`, and an `HttpResponseMessage` (whose body is read as the JSON document). HTTP-response and AOT-context assertions target their natural receiver type.

| Entry point | Behaviour |
|---|---|
| `HasJsonProperty(path)` | Asserts a property exists at the path. |
| `DoesNotHaveJsonProperty(path)` | Asserts no property exists at the path. |
| `HasJsonValue(path, expected)` | Asserts the value at `path` equals `expected` (a `string`, `bool`, or number; the `int` / `uint` / `long` / `ulong` overloads match a number or a numeric string, so a string-encoded 64-bit integer such as Protobuf `int64` / `uint64` matches too). |
| `HasJsonValueOneOf(path, T[])` | Asserts the value at `path` is one of the given values (`string[]`, `double[]`, `int[]`, `uint[]`, `long[]`, or `ulong[]`). |
| `HasJsonValueMatching(path, predicate)` | Asserts the value at `path` satisfies `Func<JsonElement, bool>`. |
| `HasJsonValueParsableAs<T>(path)` | Asserts the value at `path` is a JSON string parseable as `T` (where `T : IParsable<T>`). |
| `HasJsonValueKind(path, kind)` | Asserts the value at `path` is of the given `JsonValueKind`. |
| `HasJsonBoolean(path)` | Asserts the value at `path` is a JSON boolean (`true` or `false`). |
| `HasNonEmptyJsonString(path)` | Asserts the value at `path` is a non-empty JSON string. |
| `HasJsonArrayLength(path, length)` | Asserts the value at `path` is a JSON array of the given length. |
| `HasNonEmptyJsonArray(path)` / `HasEmptyJsonArray(path)` | Asserts the value at `path` is a non-empty / empty JSON array. |
| `HasJsonResponse<T>(status, JsonTypeInfo<T>, T expected, ct)` on `HttpResponseMessage` | Asserts status + AOT-clean deserialization + structural equality in one chain. |
| `MatchesProblemDetails(status, ..., ct)` on `HttpResponseMessage` | Asserts an RFC 7807 `application/problem+json` response with matching fields. |
| `MatchesValidationProblemDetails(status, errors, ..., ct)` on `HttpResponseMessage` | Like `MatchesProblemDetails` plus the ASP.NET Core `errors` dictionary. |
| `RoundtripsCleanlyVia<T>(JsonTypeInfo<T>)` on any `T` | Asserts serialize → deserialize → re-serialize is byte-identical via the supplied source-generated `JsonTypeInfo<T>`. |
| `AsJsonContext().HasJsonTypeInfoFor<T>()` on a `JsonSerializerContext`-typed source | Asserts the supplied source-generated context registers a `JsonTypeInfo<T>` for `T`. |
| `JsonRenderers.ReformatJson<T>(JsonTypeInfo<T>)` *(static factory)* | Returns `Func<string, string>` that canonicalises a JSON string via the consumer's `JsonSerializerContext`; composes with `SnapshotAssertions.TUnit`'s `MatchesSnapshot(Func<>)` at the consumer's call site. |
| `JsonCanonicalizer.Canonicalize(json, opts)` *(static, v0.4.0+)* | Typeless structural canonical form (sorted keys, stable indent, all fields preserved) with `[*]`-aware `ScrubPath` scrubbing of volatile values; for pinning a whole response shape as a snapshot. Needs no `JsonSerializerContext` (unlike `ReformatJson<T>`) and keeps unknown fields. |

The path is a dot-separated property navigation with optional `[N]` zero-based bracket indices and an optional leading `$` JSONPath root reference: `user.name`, `items[0].id`, `objects[0].planData[1].pickPlanId`, `$[0]` for a root-array first element. The `[*]` wildcard (v0.4.0+) matches every element of an array on `HasJsonProperty` and `HasJsonValueMatching` (`items[*].id`). See [the path-syntax notes on GitHub](https://github.com/JohnVerheij/JsonAssertions.TUnit#path-syntax) for the full grammar.

Two notes on `[*]`. It is a "for all" quantifier, so `[*].id` passes **vacuously** on an empty array, whereas `[0].id` *fails* on one; a naive `[0]` to `[*]` migration silently drops the implicit non-emptiness check, so pair the wildcard with `HasNonEmptyJsonArray("items")` when emptiness should fail the test. And `[*]` fits existence and genuinely-uniform value checks only: an element-specific check (for example "the element at index 2 has `id` 2", `HasJsonValue("items[2].id", 2)`) must stay index-scoped rather than become `[*]`.

The point over a hand-rolled `TryGetProperty(...).IsTrue()` helper is the **failure message**: every assertion renders a path-context block saying *where* resolution stopped, not merely that it did.

## Install

```bash
dotnet add package JsonAssertions.TUnit
```

**Requirements:** TUnit 1.48.6 or later, .NET 10. `System.Text.Json` is in-box on .NET 10, so the package carries no runtime dependency beyond TUnit.

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

Each integer overload (`int` / `uint` / `long` / `ulong`) matches the value whether the JSON encodes it as a number or a numeric string; `long` / `ulong` exist because protobuf serializes 64-bit ints as JSON strings (to avoid 53-bit precision loss) while System.Text.Json writes them as numbers. `double` matches a JSON number only. Use the `L` / `UL` suffix to assert a 64-bit value (`HasJsonValue("guid.high", 123456789012345L)`).

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
| `JsonPath`, `JsonPathResolution`, `JsonValueComparison`, `JsonShape`, `JsonRenderers`, `JsonFailureMessage` (framework-agnostic core) | `JsonAssertions` | No (needs `using JsonAssertions;`) |
| Source-generated assertion entry points | `TUnit.Assertions.Extensions` | Yes (TUnit auto-imports) |

## Roadmap

- Semantic JSON equality and subset / fragment matching (`IsEquivalentJsonTo`, `ContainsJson`).

## Family

Part of an assertion family for TUnit:

- [LogAssertions.TUnit](https://github.com/JohnVerheij/LogAssertions.TUnit)
- [SnapshotAssertions.TUnit](https://github.com/JohnVerheij/SnapshotAssertions.TUnit)
- [TimeAssertions.TUnit](https://github.com/JohnVerheij/TimeAssertions.TUnit)
- [MathAssertions.TUnit](https://github.com/JohnVerheij/MathAssertions.TUnit)
- [SseAssertions.TUnit](https://github.com/JohnVerheij/SseAssertions.TUnit)
- [GrpcAssertions.TUnit](https://github.com/JohnVerheij/GrpcAssertions.TUnit)

## License

[MIT](https://github.com/JohnVerheij/JsonAssertions.TUnit/blob/main/LICENSE). Copyright (c) 2026 John Verheij.
