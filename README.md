# JsonAssertions.TUnit

[![CI](https://github.com/JohnVerheij/JsonAssertions.TUnit/actions/workflows/ci.yml/badge.svg)](https://github.com/JohnVerheij/JsonAssertions.TUnit/actions/workflows/ci.yml)
[![CodeQL](https://github.com/JohnVerheij/JsonAssertions.TUnit/actions/workflows/codeql.yml/badge.svg)](https://github.com/JohnVerheij/JsonAssertions.TUnit/actions/workflows/codeql.yml)
[![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/JohnVerheij/JsonAssertions.TUnit/badge)](https://scorecard.dev/viewer/?uri=github.com/JohnVerheij/JsonAssertions.TUnit)
[![OpenSSF Best Practices](https://www.bestpractices.dev/projects/16139/badge)](https://www.bestpractices.dev/projects/16139)
[![codecov](https://codecov.io/gh/JohnVerheij/JsonAssertions.TUnit/branch/main/graph/badge.svg)](https://codecov.io/gh/JohnVerheij/JsonAssertions.TUnit)
[![NuGet](https://img.shields.io/nuget/v/JsonAssertions.TUnit.svg)](https://www.nuget.org/packages/JsonAssertions.TUnit/)
[![Downloads](https://img.shields.io/nuget/dt/JsonAssertions.TUnit.svg)](https://www.nuget.org/packages/JsonAssertions.TUnit/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

TUnit-native JSON assertions for .NET. Fluent entry points over TUnit's `Assert.That(...)` pipeline for asserting against `System.Text.Json` documents, HTTP response bodies (including RFC 7807 ProblemDetails), and the registration state of source-generated `JsonSerializerContext` instances. AOT-compatible, trimmable, no runtime reflection in the assertion path.

> **Scope:** Test projects only. Not intended for production code.

> Part of the **[DotNetAssertions](https://dotnetassertions.dev)** family of assertion extensions for TUnit.

---

## Status

Property existence, value-at-path, value-predicate, value-one-of, value-parsable-as-`T`, shape (kind / array-length / non-empty / boolean / non-empty-string), HTTP-response JSON assertions (status + AOT-clean deserialization + equality under `object.Equals`, RFC 7807 ProblemDetails, ValidationProblemDetails), AOT-context regression assertions (`RoundtripsCleanlyVia` and `HasJsonTypeInfoFor` via the `AsJsonContext` bridge), and a canonicalizing-renderer (`JsonRenderers.ReformatJson`) for composition with `SnapshotAssertions.TUnit` at the consumer's call site. The path, value, and shape entry points and `ContainsJson` are available over a JSON `string`, a `System.Text.Json.JsonElement`, and an `HttpResponseMessage` (whose body is read as the JSON document); the other entry points name their receivers in the table:

| Entry point | Behavior |
|---|---|
| `HasJsonProperty(string path)` | Asserts a property exists at the path. |
| `HasJsonProperties(string[] paths)` *(v0.7.0+)* | Asserts a property exists at each of the paths, against one parse of the document; the failure lists every path that does not resolve. |
| `DoesNotHaveJsonProperty(string path)` | Asserts no property exists at the path. |
| `HasJsonValue(string path, string\|bool\|number expected)` | Asserts the value at `path` equals `expected`. The integer overloads (`int` / `uint` / `long` / `ulong`) match the value whether the JSON encodes it as a number or as a string that holds a JSON integer literal. |
| `HasJsonValueOneOf(string path, string[]\|double[]\|int[]\|uint[]\|long[]\|ulong[] candidates)` | Asserts the value at `path` is one of the given values. The integer overloads match a number or a string that holds a JSON integer literal. |
| `HasJsonValueMatching(string path, Func<JsonElement, bool> predicate)` | Asserts the value at `path` satisfies the predicate; with `[*]`, on every element. |
| `HasJsonValueParsableAs<T>(string path)` *where T : IParsable&lt;T&gt;* | Asserts the value at `path` is a JSON string parseable as `T` (for example `Guid`, `DateTimeOffset`, `TimeSpan`). |
| `HasJsonValueKind(string path, JsonValueKind kind)` | Asserts the value at `path` is of the given kind. |
| `HasJsonBoolean(string path)` | Asserts the value at `path` is a JSON boolean (either `true` or `false`). |
| `HasNonEmptyJsonString(string path)` | Asserts the value at `path` is a non-empty JSON string. |
| `HasJsonArrayLength(string path, int length)` | Asserts the value at `path` is a JSON array of the given length. |
| `HasNonEmptyJsonArray(string path)` / `HasEmptyJsonArray(string path)` | Asserts the value at `path` is a non-empty / empty JSON array. |
| `IsEquivalentJsonTo(string expected)` / `IsEquivalentJsonTo(string expected, Action<JsonEquivalenceOptions> configure)` over a JSON `string` or `JsonElement` | Asserts the whole document is structurally equivalent to `expected`, independent of property order and number form. The configure callback sets `IgnorePath` and `IgnoreArrayOrder`. |
| `ContainsJson(string expectedSubset)` / `ContainsJson(string expectedSubset, Action<JsonEquivalenceOptions> configure)` over a JSON `string`, `JsonElement`, or `HttpResponseMessage` *(v0.6.0+)* | Asserts the document contains the expected subset (extra actual properties ignored); the failure lists every missing or mismatched path. Same options as `IsEquivalentJsonTo`. |
| `GetJsonValue<T>(string path)` / `GetJsonString(string path)` / `GetJsonElement(string path)` over a JSON `string` or `JsonElement` (and `GetJsonValueAsync` / `GetJsonStringAsync` / `GetJsonElementAsync` on `HttpResponseMessage`) *(v0.6.0+)*; `GetJsonValues<T>(string path)` / `GetJsonValuesAsync<T>` *(v0.7.0+)* | **Extraction, not assertion:** reads a typed value out at `path` and returns it, for pulling an id to drive the next request or a count to cross-check an array length. `GetJsonValue<T>` parses any `IParsable<T>` from a JSON string, number, or boolean; `GetJsonElement` returns a detached copy; `GetJsonValues<T>` returns every value a `[*]` path expands to. Throws `JsonExtractionException` with path context when the path does not resolve or a value does not parse, and `ArgumentException` for a malformed path, checked before any navigation. |
| `HasJsonResponse<T>(HttpStatusCode, JsonTypeInfo<T>, T expected, ct)` on `HttpResponseMessage` | Asserts status + AOT-clean deserialization + equality under `object.Equals` in one chain (use a record whose members have value equality, or override `Equals` on `T`). |
| `MatchesProblemDetails(int status, ..., ct)` on `HttpResponseMessage` | Asserts an RFC 7807 `application/problem+json` response with matching fields; `status` must match both the HTTP status code and the body's `status` field (v0.7.0+), as RFC 7807 requires. |
| `MatchesValidationProblemDetails(int status, IReadOnlyDictionary<string, string[]> errors, ..., ct)` on `HttpResponseMessage` | Like `MatchesProblemDetails` plus the ASP.NET Core `errors` dictionary. |
| `RoundtripsCleanlyVia<T>(JsonTypeInfo<T>)` on any `T` | Asserts serialize -> deserialize -> re-serialize is byte-identical via the supplied source-generated `JsonTypeInfo<T>`. |
| `AsJsonContext().HasJsonTypeInfoFor<T>()` on a `JsonSerializerContext`-typed source | Asserts the supplied source-generated context registers a `JsonTypeInfo<T>` for `T`. |
| `JsonRenderers.ReformatJson<T>(JsonTypeInfo<T>)` *(static factory)* | Returns `Func<string, string>` that canonicalizes a JSON string via the consumer's `JsonSerializerContext`. Composes with `SnapshotAssertions.TUnit`'s `MatchesSnapshot(Func<>)` at the consumer's call site without coupling the packages. |
| `JsonCanonicalizer.Canonicalize(string json)` / `Canonicalize(string json, Action<JsonCanonicalizeOptions> configure)` *(static, v0.4.0+)* | Typeless structural canonical form (sorted keys, stable indent, all fields preserved) with `[*]`-aware `ScrubPath` scrubbing of volatile values; for pinning a whole response shape as a snapshot. Needs no `JsonSerializerContext` (unlike `ReformatJson<T>`) and keeps unknown fields. |

The point over a hand-rolled `TryGetProperty(...).IsTrue()` helper is the **failure message**: a path assertion whose path does not resolve says *where* resolution stopped and why (`resolved as far as 'user', which has no property 'email'`), not merely that it did.

The AOT-context regression assertions (`HasJsonTypeInfoFor`, `RoundtripsCleanlyVia`) pair together as a CI gate to keep a source-generated `JsonSerializerContext` in sync with the consumer's domain types - see the dedicated section below.

---

## Table of contents

- [Why this package](#why-this-package)
- [Compared with TUnit's built-in JSON assertions](#compared-with-tunits-built-in-json-assertions)
- [Install](#install)
- [Package layout](#package-layout)
- [Namespaces (and a `GlobalUsings.cs` recommendation)](#namespaces-and-a-globalusingscs-recommendation)
- [Quick start](#quick-start)
- [Path syntax](#path-syntax)
- [Entry points](#entry-points)
- [Failure diagnostics](#failure-diagnostics)
- [Cookbook: common patterns](#cookbook-common-patterns)
- [Known issues](#known-issues)
- [Design notes](#design-notes)
- [Stability intent (pre-1.0)](#stability-intent-pre-10)
- [Roadmap](#roadmap)
- [Out of scope](#out-of-scope)
- [Family compatibility](#family-compatibility)
- [Pair with](#pair-with)
- [Contributing](#contributing)
- [License](#license)

---

## Why this package

Asserting against a JSON response body or document in tests typically devolves into one of:

- A hand-rolled `TryGetProperty(...).IsTrue()` chain with no failure context:
  ```csharp
  using System.Text.Json;

  using var doc = JsonDocument.Parse(body);
  await Assert.That(doc.RootElement.TryGetProperty("user", out var user) && user.TryGetProperty("name", out var name) && name.GetString() == "alice")
      .IsTrue().Because("user.name should equal alice");
  ```
  When this fails, the test message is `Expected to be true, because user.name should equal alice` / `but found False`: false at which level, and why, is on the caller to investigate manually.
- A bespoke `JsonAssertions` helper class re-implemented in every project, with subtle differences in failure messages, path-syntax, malformed-body handling, and HTTP-receiver wiring between codebases.

This package replaces both with a fluent DSL that auto-imports alongside TUnit's own assertions. A path assertion whose path does not resolve names *where* on the path resolution stopped, not just that something didn't match, and the path, value, and shape entry points (and `ContainsJson`) are available on a JSON `string`, a `JsonElement`, and an `HttpResponseMessage` (whose body is read as the JSON document, with the cancellation token flowing to the body read).

For the AOT-shipping audience, v0.3.0 adds two paired regression assertions (`RoundtripsCleanlyVia` + `HasJsonTypeInfoFor`) that catch the "added a new domain type but forgot to add `[JsonSerializable(typeof(NewType))]` to the context" class at CI time, before any runtime serialization touches the unregistered type.

## Compared with TUnit's built-in JSON assertions

TUnit ships JSON assertions of its own. They assert the node in hand or the response headers; this package asserts at a path and reads the response body. Checked against TUnit 1.81.10:

| TUnit | What it does | Here instead |
|---|---|---|
| `IsDeepEqualTo` / `IsNotDeepEqualTo` on `JsonElement` and `JsonNode` | Whole-document equality through `JsonElement.DeepEquals` / `JsonNode.DeepEquals`. Property order and number form are ignored (`{"a":1,"b":2}` equals `{"b":2,"a":1.0}`), and arrays are ordered. The failure names one difference (`differs at $.b: expected 3 but found 2`). The difference finder compares numbers by their text, so with `1e2` against `100` plus an extra property it reports `$.n` instead of the extra property. No ignored paths, no unordered arrays, no subset. | `IsEquivalentJsonTo` with `IgnorePath` (`[*]`-aware) and `IgnoreArrayOrder`; `ContainsJson` for a subset, listing every difference |
| `HasProperty(name)` / `DoesNotHaveProperty(name)` on `JsonElement`; `HasJsonProperty(name)` / `DoesNotHaveJsonProperty(name)` on `JsonNode` | A top-level property **name**, not a path: `HasProperty("user.name")` looks for a key named `user.name`. The `JsonElement` failure prints the whole element. | `HasJsonProperty(path)` on a JSON `string`, a `JsonElement` and an `HttpResponseMessage`, with indices, `[*]` and the path-context failure. This package has no `JsonNode` receiver, so on a `JsonNode`, `HasJsonProperty` is TUnit's and takes a name: `HasJsonProperty("user.name")` there looks for a key named `user.name`. |
| `IsObject`, `IsArray`, `IsString`, `IsNumber`, `IsBoolean`, `IsNull`, `IsNotNull` (`JsonElement`); `IsJsonObject`, `IsJsonArray`, `IsJsonValue`, `IsJsonArrayEmpty`, `IsJsonArrayNotEmpty`, `HasJsonArrayCount(n)` (`JsonNode`) | Kind and length of the asserted node itself | `HasJsonValueKind`, `HasJsonBoolean`, `HasJsonArrayLength`, `HasNonEmptyJsonArray` and `HasEmptyJsonArray` at a path |
| `IsValidJson`, `IsNotValidJson`, `IsValidJsonObject`, `IsValidJsonArray` on `string` | Parse checks | Not needed: every assertion on a `string` or a response fails with an explained message when the text is not JSON |
| `HasJsonContent()`, `HasContentType(x)`, `IsOk()` … `HasStatusCode(c)` on `HttpResponseMessage` | Headers and status only; the body is never read. `HasJsonContent` accepts exactly `application/json` (it fails on `application/problem+json`), and `HasContentType` compares case-sensitively (it fails on `Application/Problem+Json`). | Body assertions on the response. `MatchesProblemDetails` checks the media type case-insensitively (RFC 9110) and, from 0.7.0, the status code. |
| not in TUnit | | Paths with indices and `[*]`; value at a path, with integers as numbers or as strings; one-of; predicate; ProblemDetails; extraction; `HasJsonTypeInfoFor` and `RoundtripsCleanlyVia` |

## Install

```bash
dotnet add package JsonAssertions.TUnit
```

**Requirements:** TUnit 1.83.0 or later, .NET 10 or .NET 11. `System.Text.Json` is in-box on both, so the package carries no runtime dependency beyond TUnit. The package is AOT-compatible, trimmable, and uses no runtime reflection in the assertion path.

## Package layout

This repo ships **one** NuGet package with types in two namespaces - the same consumer feel as the rest of the assertion family (a framework-agnostic core plus a TUnit adapter), but in a single assembly to keep zero-overhead packaging at the v0.x stage:

| Package | Purpose | Depends on |
|---|---|---|
| [`JsonAssertions.TUnit`](https://www.nuget.org/packages/JsonAssertions.TUnit/) | `JsonAssertions` framework-agnostic core (for example `JsonPath`, `JsonValueComparison`, `JsonShape`, `JsonRenderers`, `JsonFailureMessage`, `JsonEquivalence`, `JsonCanonicalizer`) plus `JsonAssertions.TUnit` TUnit adapter (`[GenerateAssertion]` entry points + hand-written extensions over `Assert.That(...)`) | `System.Text.Json` (in-box on net10.0 and net11.0) + `TUnit.Assertions` + `TUnit.Core` |

A future package split (a bare-identifier `JsonAssertions` core package plus this adapter) would fall along the namespace seam if the standalone identifier becomes available; shipping the seam now keeps the option open without committing to it. Adapters for other test frameworks (NUnit, xUnit, MSTest) are not planned; the family targets TUnit.

## Namespaces (and a `GlobalUsings.cs` recommendation)

The single package places its own types in two namespaces with deliberately-different scopes, and the source generator emits the fluent entry points into a third that belongs to TUnit:

| Type / member | Namespace | Auto-imported? |
|---|---|---|
| `JsonPath`, `JsonPathResolution`, `JsonValueComparison`, `JsonShape`, `JsonRenderers`, `JsonFailureMessage`, `JsonEquivalence`, `JsonEquivalenceOptions`, `JsonDifference`, `JsonDifferenceKind`, `JsonCanonicalizer`, `JsonCanonicalizeOptions` (framework-agnostic core) | `JsonAssertions` | **No** (needs `using JsonAssertions;`) |
| **Typed extraction** (`GetJsonValue<T>()`, `GetJsonValues<T>()`, `GetJsonString()`, `GetJsonElement()` and the `...Async` forms, plus `JsonExtractionException`) | `JsonAssertions` | **No** (needs `using JsonAssertions;`) |
| Source-generated assertion entry points (`HasJsonProperty()`, `HasJsonValue()`, `HasJsonResponse()`, `MatchesProblemDetails()`, ...) | `TUnit.Assertions.Extensions` | **Yes** (TUnit auto-imports) |
| `IJsonContextAssertionSource` + `AsJsonContext()` bridge (JSON-context family) | `JsonAssertions.TUnit` | **No** (needs `using JsonAssertions.TUnit;` when chaining `.AsJsonContext()`) |

**The extraction methods are the only fluent extension methods in this package that extend the receiver instead of the assertion chain.** They are extensions on the *receiver* (the `HttpResponseMessage`, `string`, or `JsonElement` itself), because they return a value rather than asserting one. The rest of the package trains you to reach for the chain, so the wrong spelling is the natural first guess, and it fails with a bare `CS1061` that never mentions the namespace you are missing:

```csharp
using JsonAssertions;   // the extraction methods live here, and do not auto-import

// RIGHT: the receiver, not the assertion
var orderId = await response.GetJsonValueAsync<int>("orderId", ct);

// WRONG: ValueAssertion<HttpResponseMessage> has no such method, and the
// resulting CS1061 does not mention the namespace you are missing
// await Assert.That(response).GetJsonValueAsync<int>("orderId", ct);
```

Only the fluent assertion entry points auto-import (via `TUnit.Assertions.Extensions`), so a test project that already uses TUnit needs no `using` for them. The other three rows need **two** `using` directives between them: `using JsonAssertions;` covers both the typed extraction and the core types, and `using JsonAssertions.TUnit;` covers `.AsJsonContext()`. A single `GlobalUsings.cs` puts both in front of every test file without ceremony:

```csharp
// tests/MyApp.Tests/GlobalUsings.cs
global using JsonAssertions;       // for GetJsonValue<T>() and the core types
global using JsonAssertions.TUnit; // for .AsJsonContext()
```

`JsonPath.Resolve(JsonElement, string)` is the framework-agnostic primitive: it returns a `JsonPathResolution` carrying the resolved element on success and the failure-point context (how far it got, which segment failed, what kind of value blocked it) on failure. `JsonValueComparison.Matches` compares a resolved element against an expected value, and `JsonShape` provides the array-length / value-kind predicates. The TUnit entry points are thin `[GenerateAssertion]` wrappers over them.

## Quick start

```csharp
using System.Text.Json;

[Test]
public async Task ResponseBodyHasExpectedShape(CancellationToken ct)
{
    string json = """{"user":{"name":"alice","age":30,"active":true,"id":"550e8400-e29b-41d4-a716-446655440000"},"items":[{"name":"x"}],"status":"Healthy"}""";

    await Assert.That(json).HasJsonProperty("user.name");
    await Assert.That(json).HasJsonProperties(["user.age", "user.id", "items[*].name"]);
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
using System.Text.Json;

using var document = JsonDocument.Parse(json);
await Assert.That(document.RootElement).HasJsonValue("user.name", "alice");

// Directly on an HTTP response - the body is read and asserted against:
await Assert.That(response).HasJsonProperty("user.name", ct);
await Assert.That(response).HasJsonArrayLength("items", 3, ct);
```

A response body or string that is not valid JSON fails the assertion with an explained message rather than throwing a raw `JsonException`; a body that cannot be parsed does not vacuously satisfy `DoesNotHaveJsonProperty`. A JSON string that starts with a byte order mark (U+FEFF, which `Encoding.UTF8.GetString` keeps) is read without it.

### Response bodies

The body is read with `ReadAsStringAsync`, which decodes it from its charset or byte order mark, and each assertion reads it again. That works because `HttpClient` buffers the body by default. Two cases need one `await response.Content.LoadIntoBufferAsync()` first:

- **A body read through its stream.** A response from `HttpCompletionOption.ResponseHeadersRead`, or one built on a `StreamContent`, can be read once. A test that reads it first (`ReadAsStreamAsync`, `ReadFromJsonAsync`, a log line) leaves nothing to assert, and the assertion fails with `the response body is empty; a body read through its stream before the assertion is gone, unless the content was buffered first with LoadIntoBufferAsync`.
- **A response shared by tests that run in parallel.** `HttpContent` does not support concurrent first reads of an unbuffered body (`InvalidOperationException: The stream was already consumed`).

An empty body (a 204, or a response built without content) fails with the same `the response body is empty` text, in every response assertion and extraction method. The `CancellationToken` cancels the call: a token that is already canceled throws `OperationCanceledException` also when the body is in memory, and a read that the token cancels throws `OperationCanceledException` that carries the token and says so: `The response body read was canceled before the JSON assertion ran.` (`... before the JSON value was read.` for an extraction method).

### Integer overloads: number or string

The integer overloads (`int` / `uint` / `long` / `ulong`) match the value whether the JSON encodes it as a number or as a string, because the encoding depends on the serializer. `long` / `ulong` exist because protobuf serializes 64-bit ints as JSON strings (to avoid 53-bit precision loss), while System.Text.Json writes them as numbers, and both match. A passing value is an integer literal in range (no fraction or exponent, so `30.0` and `3e1` do not match `HasJsonValue(path, 30)`), or a JSON string that holds a JSON integer literal in range: an optional `-`, then `0` or digits that do not start with `0`. So `" 42 "`, `"+42"` and `"042"` do not match, and the failure says the string is not a JSON integer literal; `-0`, as a number or a string, matches `0` for every integer overload. The `double` overload matches a JSON number only, and accepts `30.0` and `3e1` as does `IsEquivalentJsonTo`. JSON has no NaN or infinity, so a non-finite expected `double` throws `ArgumentOutOfRangeException`, and a literal beyond `double` range (`1e400`) never matches.

`HasJsonValueParsableAs<T>` and `GetJsonValue<T>` are generic over any `IParsable<T>`, so they parse with `T.TryParse` under the invariant culture instead: the value is parsed by the type's own rules, so `"+42"` is 42 for `int`, and `"1,5"` is 15 for `double` (it allows thousands separators).

Use the `L` / `UL` suffix to assert a 64-bit value:

```csharp
// number-encoded (System.Text.Json): {"guid":{"high":123456789012345,"low":42}}
await Assert.That(json).HasJsonValue("guid.high", 123456789012345L);

// string-encoded (protobuf JsonFormatter): {"guid":{"high":"123456789012345","low":"18446744073709551615"}}
await Assert.That(json).HasJsonValue("guid.high", 123456789012345L);
await Assert.That(json).HasJsonValue("guid.low", 18446744073709551615UL);

await Assert.That(json).HasJsonValueOneOf("message.sequence", [100L, 200L, 300L]);
```

When an assertion fails, the message names the failure point rather than just reporting a `false`:

```text
Expected to have a JSON property at path 'user.email'
but resolved as far as 'user', which has no property 'email'

at Assert.That(Json).HasJsonProperty("user.email")
```

## Path syntax

A path is a sequence of dot-separated property names and zero-based bracket indices, navigated from the asserted element (or, for the `string` overload, from the parsed document's root):

- `user.address.city` resolves `user`, then `address`, then `city`.
- `items[0].name` resolves the first element of `items`, then `name` on it. Indices are zero-based, non-negative integers. Property and index segments compose freely (`objects[0].entries[1].id`).

**Why dot + `[n]`, not JSONPath or JMESPath?** The path is a deliberately small, navigable subset, not a query language. A full JSONPath/JMESPath engine would add a runtime dependency (several are reflection-based) or a hand-rolled query parser, both at odds with this package's BCL-and-TUnit-only, AOT-first, no-reflection rules. The subset covers the common assertion case: navigate to a known location and assert its value or shape. Beyond the `[*]` wildcard it does not support filter expressions or recursive descent; for those, resolve the element yourself and assert on the result.
- `items[*].name` uses the `[*]` wildcard (since v0.4.0): it matches every element of the array, so the assertion holds only if it holds for all of them. Nested and multiple wildcards compose (`cycles[*].cycleId`, `[*].tags[*]`). Supported by `HasJsonProperty`, `HasJsonProperties`, `HasJsonValueMatching` and `GetJsonValues<T>`, and by the `IgnorePath` and `ScrubPath` patterns; an empty array passes vacuously (a "for all" over an empty set), and a failure names the first failing element by its concrete index.
  - **Empty-array footgun.** `[*]` is a "for all" quantifier, so `[*].id` passes vacuously on an empty array (there is nothing to violate the predicate), whereas `[0].id` *fails* on an empty array (there is no first element). A naive `[0]` to `[*]` migration therefore silently drops the implicit non-emptiness check that `[0]` carried. When an empty array should fail the test, pair the wildcard with an explicit non-emptiness assertion, for example `HasNonEmptyJsonArray("items")` alongside `HasJsonProperty("items[*].id")`.
  - **Wildcards are for existence and uniform checks, not element-specific ones.** `[*]` fits property-existence checks and value checks that genuinely hold for every element. A check that depends on a specific element's position (for example "the element at index 2 has `id` 2", `HasJsonValue("items[2].id", 2)`) must stay index-scoped; rewriting it as `[*]` would assert the same value for every element and change the meaning of the test.
- `$` is the JSONPath root reference: `$` alone resolves to the asserted element itself; `$.user.name` is equivalent to `user.name`; `$[0]` (and bare `[0]`) is equivalent against a root array. Any other leading `$` starts a name: `$type` is the property `$type`, as System.Text.Json writes it for polymorphic types.
- A path that traverses a non-object value where a property is expected (or a non-array where an index is expected) resolves to "not found" rather than throwing; the failure message names which segment blocked the resolution.
- A name runs up to the next `.` or `[`, so it cannot contain either. A name that appears twice in one object resolves to its last occurrence, as System.Text.Json reads it: `HasJsonValue("a", 2)` passes on `{"a":1,"a":2}`, and `IsEquivalentJsonTo` cannot see the duplicate.
- **A malformed path throws `ArgumentException` before any navigation** (v0.7.0+), naming the path and the character where it goes wrong: an empty or whitespace path; a leading, doubled or trailing dot (`..` is JSONPath's recursive descent, which is not supported); `$.` with nothing or an index after it; an empty, unclosed, negative, non-numeric or too-large index; a name directly after `]` without a `.`. `IgnorePath` and `ScrubPath` check their path when it is registered. A single-path assertion checks its path once it has the JSON, before navigating, so a body that is not valid JSON, an empty body or a `default` element is reported first; `HasJsonProperties` and `GetJsonValues<T>` check their paths before reading anything. Either way a malformed path never passes.

The single-location assertions (`HasJsonValue`, `HasJsonValueOneOf`, `HasJsonValueParsableAs<T>`, `HasJsonArrayLength`, `DoesNotHaveJsonProperty`, ...) and the single-value extraction methods target one location and reject `[*]` with `ArgumentException`, wherever it sits in the path. The wildcard is for the all-element assertions above, and for `GetJsonValues<T>`. For pinning a whole response shape as a snapshot, `JsonCanonicalizer.Canonicalize(json, o => o.ScrubPath("..."))` (since v0.4.0) produces a deterministic structural form (object keys sorted, stable two-space indent, LF, every field preserved) with `[*]`-aware `ScrubPath` scrubbing of volatile values; compose it with a snapshot library's normalizer hook. Unlike the typed `JsonRenderers.ReformatJson<T>`, it needs no `JsonSerializerContext` and keeps unknown fields, so an added or removed field surfaces as a diff.

## Entry points

The full entry-point catalog is in the Status table at the top of this file. The summary below organizes them by domain.

**Path / value / shape (over JSON `string`, `JsonElement`, and `HttpResponseMessage`):**

- `HasJsonProperty(path)` / `DoesNotHaveJsonProperty(path)`
- `HasJsonProperties([path, ...])` *(v0.7.0+)*: one path is `HasJsonProperty`; several are `HasJsonProperties(["user.name", "user.age"])`, a collection expression, with the token after it on a response. The paths are checked before the JSON is read: an empty list, a null or blank path, a repeated path or a malformed one throws `ArgumentException` naming its index (`paths[1]`).
- `HasJsonValue(path, expected)` (`string` / `bool` / `double` overloads, plus `int` / `uint` / `long` / `ulong` integer overloads that match a number or a string that holds a JSON integer literal)
- `HasJsonValueOneOf(path, candidates[])` (`string[]` / `double[]` / `int[]` / `uint[]` / `long[]` / `ulong[]` overloads)
- `HasJsonValueMatching(path, predicate)`
- `HasJsonValueParsableAs<T>(path)` *(where T : IParsable&lt;T&gt;)*
- `HasJsonValueKind(path, kind)` / `HasJsonBoolean(path)` / `HasNonEmptyJsonString(path)`
- `HasJsonArrayLength(path, length)` / `HasNonEmptyJsonArray(path)` / `HasEmptyJsonArray(path)`

> **Only the `HttpResponseMessage` forms take a `CancellationToken`** - the token flows to the response-body read, and there is nothing to cancel when the source is already a `string` or a `JsonElement`. Passing one anyway does not produce a helpful "no such overload": the compiler picks the `HttpResponseMessage` overload as the closest match and reports `CS1929` (*"... requires a receiver of type `System.Net.Http.HttpResponseMessage`"*), which points at the receiver when the fix is to drop the token.
>
> ```csharp
> await Assert.That(element).HasJsonArrayLength("items", 2, ct);  // CS1929, misleading
> await Assert.That(element).HasJsonArrayLength("items", 2);      // correct: nothing to cancel
> ```

**Whole-document equivalence and subset (`IsEquivalentJsonTo` over JSON `string` and `JsonElement`; `ContainsJson` also over `HttpResponseMessage`):**

- `IsEquivalentJsonTo(expected)` / `IsEquivalentJsonTo(expected, configure)` (over `string` / `JsonElement`) - structural equality, independent of property order and number form (`1` == `1.0` == `1e0`). The configure callback sets `IgnorePath(path)` (excludes a path, `[*]`-aware) and `IgnoreArrayOrder()` (multiset array comparison).
- `ContainsJson(expectedSubset)` / `ContainsJson(expectedSubset, configure)` *(v0.6.0+; over `string`, `JsonElement`, and `HttpResponseMessage`)* - subset match: every property in the expected document must be present and equal in the actual document, which may carry additional properties. Same order/number-form independence and `IgnorePath` / `IgnoreArrayOrder` options as equivalence; an expected array asserts a positional prefix. The failure lists **every** missing or mismatched path, not just the first (under `IgnoreArrayOrder()` an unmatched array element is reported as a whole, at its position in the expected array, with no inner path). Use it to replace a block of per-property `HasJsonProperty` / `HasJsonValue` checks against one response.

**Typed extraction, not assertion (over JSON `string` and `JsonElement`; `...Async` on `HttpResponseMessage`):** *(v0.6.0+)*

- `GetJsonValue<T>(path)` where `T : IParsable<T>` - reads and returns the value at `path`, parsed as `T` from a JSON string, number, or boolean (`true` / `false`) literal (invariant culture), for driving a later request or a cross-check. `GetJsonString(path)` returns a JSON string value; `GetJsonElement(path)` returns a detached (`Clone`d) subtree that survives the source document's disposal. On a failure to resolve the path, convert the value, or parse the source, throws `JsonExtractionException` with the same path context the assertions report (`No JSON property at path 'user.email': resolved as far as 'user', which has no property 'email'`); a malformed path, or a `[*]` in it, throws `ArgumentException` instead, before any navigation. These return values (they are not awaited assertions); the `HttpResponseMessage` forms are `GetJsonValueAsync` / `GetJsonStringAsync` / `GetJsonElementAsync`.
- `GetJsonValues<T>(path)` *(v0.7.0+)* - reads every value a `[*]` path expands to, in document order, each parsed as `GetJsonValue<T>` parses one value; `GetJsonValuesAsync<T>` on a response. A path without `[*]` throws `ArgumentException` (use `GetJsonValue<T>` for one value). An empty array gives an empty list. Every element must resolve and parse: the exception lists each one that does not, by its concrete path, so no value is dropped silently.
- These extend the **receiver**, not the assertion chain: write `response.GetJsonValueAsync<int>(...)`, not `Assert.That(response).GetJsonValueAsync<int>(...)`. They live in the `JsonAssertions` namespace, so they need `using JsonAssertions;` - see [Namespaces](#namespaces-and-a-globalusingscs-recommendation).

**HTTP-response combined assertions (on `HttpResponseMessage`):**

- `HasJsonResponse<T>(status, JsonTypeInfo<T>, expected, ct)` - combined status + AOT-clean deserialization + equality under `object.Equals` (use a record whose members have value equality, or override `Equals` on `T`)
- `MatchesProblemDetails(status, title?, detail?, type?, instance?, ct)` - RFC 7807
- `MatchesValidationProblemDetails(status, errors, ..., ct)` - RFC 7807 + ASP.NET Core `errors`

**AOT-context regression (on a `JsonSerializerContext`-typed source, via `.AsJsonContext()`):**

- `AsJsonContext().HasJsonTypeInfoFor<T>()` - asserts the context registers `JsonTypeInfo<T>`
- `RoundtripsCleanlyVia<T>(JsonTypeInfo<T>)` - serialize -> deserialize -> re-serialize is byte-identical

**Composition + extension point (in `JsonAssertions` core namespace):**

- `JsonRenderers.ReformatJson<T>(JsonTypeInfo<T>)` - static factory returning `Func<string, string>` that canonicalizes a JSON string for snapshot composition
- `JsonFailureMessage` - five public path-family factory methods (`ParseFailure`, `PropertyNotFound`, `PropertyShouldNotExist`, `ValueMismatch`, `ShapeMismatch`) for consumer-authored typed JSON assertions, plus `ExpectedParseFailure`, `EquivalenceMismatch`, and `ContainsMismatch` for the equivalence and subset assertions. Each returns the text after `but` (what was found); the assertion's `ExpectationMessage` states the expectation
- `JsonEquivalence.Compare(expected, actual, options)` - structural comparison over JSON strings or `JsonElement`s, returning the first `JsonDifference` or `null` when equivalent
- `JsonEquivalence.ContainsAll(expected, actual, options)` *(v0.6.0+)* - subset comparison returning **every** `JsonDifference` (empty when the actual document contains the expected subset); `JsonFailureMessage.ContainsMismatch(differences)` renders the multi-difference failure text

## Failure diagnostics

Every failure has three parts: the expectation, what was found instead (after `but`), and the call. A path that does not resolve says where navigation stopped and why; the other failures (value, shape, equivalence, subset, HTTP, ProblemDetails, AOT-context) say what differed. Names are in single quotes and values are JSON literals, and a value the expectation cannot show (the `double` or `bool` you passed, the one-of candidates) follows in parentheses. The texts below are copied from the package's full-message tests, which run each one under the invariant culture, `nl-NL` and `tr-TR`. Numbers the package renders use the invariant culture. Two parts follow the current culture: what TUnit prints into the expectation line itself (a negative integer in `HasJsonValue`, the expected value of `HasJsonResponse`), and the value `HasJsonResponse` deserialized, which is printed with its own `ToString` (a `decimal` reads `2,5` under `nl-NL`).

**Path-resolution failure:**

```text
Expected to have a JSON property at path 'user.email'
but resolved as far as 'user', which has no property 'email'

at Assert.That(Json).HasJsonProperty("user.email")
```

**Index out of range:**

```text
Expected to have a JSON property at path 'user.roles[5]'
but resolved as far as 'user.roles', which has 2 elements, so no index [5]

at Assert.That(Json).HasJsonProperty("user.roles[5]")
```

**Several paths at once** (every path that did not resolve, in order; a `[*]` path names its first failing element and, when it expands to more than one element, how many failed):

```text
Expected to have a JSON property at each of the given paths
but 2 of 3 paths did not resolve:
  'user.email': resolved as far as 'user', which has no property 'email'
  'items[*].id': resolved as far as 'items[1]', which has no property 'id' (1 of 2 elements)

at Assert.That(Json).HasJsonProperties(["user.name", "user.email", "items[*].id"])
```

**Value failure** (a `double` or `bool` expectation shows the value after the found one):

```text
Expected to have the given JSON number at path 'user.age'
but found 31 (expected 30.5)

at Assert.That(Json).HasJsonValue("user.age", 30.5)
```

**Malformed-body failure** (the JSON `string` or HTTP response body wasn't valid JSON):

```text
Expected to have a JSON property at path 'user'
but the value is not valid JSON: Expected depth to be zero at the end of the JSON payload. There is an open JSON object or array that should be closed. LineNumber: 0 | BytePositionInLine: 8.

at Assert.That(malformed).HasJsonProperty("user", ct)
```

**Subset failure** (every path that differs):

```text
Expected to contain the expected JSON
but 2 paths differ:
  at 'a' the value differs
    expected: 2
    actual:   1
  at 'c' the property is missing from the actual document
    expected: 3
    actual:   absent

at Assert.That(Small).ContainsJson("""{"a":2,"c":3}""")
```

**HTTP combined-assertion failure** (status correct, body deserialized but the resulting value was not equal to `expected` under `object.Equals`):

```text
Expected to have status OK and a JSON body equal to TestDto { Id = 1, Name = a }
but the body deserialized to TestDto { Id = 2, Name = b }

at Assert.That(dto).HasJsonResponse(HttpStatusCode.OK, HasJsonResponseTests.TestJsonContext.Default.TestDto, new HasJsonResponseTests.TestDto(1, "a"), ct)
```

**RFC 7807 ProblemDetails failure** (every entry that differs, the HTTP status code included):

```text
Expected to be an RFC 7807 problem response with status 404
but 3 fields differed:
  HTTP status: 400 BadRequest (expected 404)
  status: 400 (expected 404)
  title: "Bad" (expected "Not found")

at Assert.That(problem).MatchesProblemDetails(404, "Not found", , , , ct)
```

TUnit's generator leaves an empty slot in the call line for each optional argument you did not pass (`404, "Not found", , , , ct`); the message is otherwise exact.

**AOT-context registration failure** (the source-gen context does not register the asserted type):

```text
Expected to register JsonTypeInfo<DateTime>
but ContextRegistrationJsonContext.GetTypeInfo(typeof(DateTime)) returned null; add [JsonSerializable(typeof(DateTime))] to ContextRegistrationJsonContext

at Assert.That(HasJsonTypeInfoForTests.ContextRegistrationJsonContext.Default).AsJsonContext().HasJsonTypeInfoFor<DateTime>()
```

**AOT round-trip drift failure** (the value re-serialized to a different JSON shape):

```text
Expected to serialize the same after a round trip through its JsonTypeInfo
but the second serialization differed:
  first:  {"v":7}
  second: {"v":7,"drift":true}

at Assert.That(new PinDrifter(7)).RoundtripsCleanlyVia(PinDrifterContext.Default.PinDrifter)
```

What to pin instead of the message text: see [Stability intent (pre-1.0)](#stability-intent-pre-10). 0.7.0 rewrote every failure text to the rule above, so a test that pinned a 0.6 message needs its pin updated.

## Cookbook: common patterns

### Pattern: pull a value out to drive the next step (`GetJsonValue`, v0.6.0+)

Acceptance and end-to-end flows often read a value from one response to build the next request, or read a count to cross-check an array length. Instead of a hand-rolled `JsonDocument.Parse(...).RootElement.GetProperty("orderId").GetInt32()` (which throws a bare `KeyNotFoundException` / `InvalidOperationException` with no path context), extract it with a typed getter:

```csharp
using System.Net.Http.Json;

// parse the body once, then read what the test needs
var order = await response.GetJsonElementAsync("order", ct);
var orderId = order.GetJsonValue<int>("orderId");         // drives the next request
await _client.PostAsJsonAsync($"/orders/{orderId}/submit", payload, ct);

// cross-field consistency
var itemCount = order.GetJsonValue<int>("itemCount");
var items = order.GetJsonElement("items");
await Assert.That(itemCount).IsEqualTo(items.GetArrayLength());
```

Note the receiver: these hang off the `response` / `JsonElement` itself, **not** off `Assert.That(...)`, and they need `using JsonAssertions;`. They return a value instead of asserting one, so they are not part of the assertion chain - see [Namespaces](#namespaces-and-a-globalusingscs-recommendation) for the spelling that does not compile.

`GetJsonValue<T>` parses any `IParsable<T>` (`int`, `long`, `bool`, `Guid`, `DateTimeOffset`, ...) from a JSON string, number, or boolean literal; a missing path or unparseable value throws `JsonExtractionException` naming where the path stopped, and a malformed path throws `ArgumentException` before any navigation. `GetJsonElement` returns a detached copy, so it stays valid after the parsed document is gone.

**Extract before `Assert.Multiple`.** The extraction methods are not assertions, so a `JsonExtractionException` ends a `using (Assert.Multiple())` block like any exception. When an assertion in the block has already failed, C# disposes the scope while the extraction error propagates, and the scope reports the collected failure in its place: the report never mentions the extraction. Read the values first, then assert in the scope.

### Pattern: read every value of a list (`GetJsonValues<T>`, v0.7.0+)

When a test needs the names of every entry, `GetJsonValues<T>` replaces a loop over `EnumerateArray()`: it reads each value a `[*]` path expands to, in document order, parsed as `GetJsonValue<T>` parses one.

```csharp
var names = await response.GetJsonValuesAsync<string>("entries[*].name", ct);
await Assert.That(names).IsEquivalentTo(["db", "cache", "queue"]);
```

An empty array gives an empty list, which a collection assertion then reports. An element that does not resolve or parse is never dropped: the exception lists each one by its concrete path.

```text
Could not read 2 of 2 values at 'user.roles[*]':
  'user.roles[0]': found "admin", which does not parse as Guid
  'user.roles[1]': found "dev", which does not parse as Guid
```

### Pattern: assert a response contains an expected shape (`ContainsJson`, v0.6.0+)

When a test asserts several fields of one response, a block of per-property checks re-reads the body per line and restates each path in a `.Because`. `ContainsJson` collapses the block into one declarative subset assertion: the expected fields must be present and equal, and the response may carry any additional fields (durations, timestamps, versions) the test does not want to pin. The failure lists **every** missing or mismatched field in one report, so a single run tells you everything that is wrong (under `IgnoreArrayOrder()` an unmatched array element is reported as a whole, at its position in the expected array, with no inner path).

```csharp
// Before: one assert per field, each re-reads the body, each restates the path
await Assert.That(response).HasJsonProperty("status", ct).Because("aggregate health has status");
await Assert.That(response).HasJsonProperty("totalDurationMs", ct).Because("has totalDurationMs");
await Assert.That(response).HasNonEmptyJsonArray("entries", ct).Because("at least one entry");
await Assert.That(response).HasJsonProperty("entries[0].name", ct);
await Assert.That(response).HasJsonProperty("entries[0].status", ct);

// After: one subset assertion; extra response fields are ignored; failure names every wrong path
await Assert.That(response).ContainsJson("""
    { "status": "Healthy",
      "entries": [ { "name": "db", "status": "Healthy" } ] }
    """, ct);
```

Use `IsEquivalentJsonTo` instead when the test must reject unexpected fields (full equality). `ContainsJson` also works over a JSON `string` and a `JsonElement`; when a test asserts status, shape, and extracts a value, parse the body once to a `JsonElement` and assert against that. An expected array asserts a positional prefix (the actual array may be longer); pass `o => o.IgnoreArrayOrder()` to match elements as a multiset subset, or `o => o.IgnorePath("...")` to exclude a volatile field.

> **Do not fold a volatile value into the expected subset.** `ContainsJson` asserts *equality* for every field you put in it - "subset" constrains which fields are checked, not how strictly they are compared. A field whose value legitimately varies between runs (an elapsed-milliseconds or duration reading, a server-generated identifier, a health status that can be `Healthy` or `Degraded`) is pinned to whatever the response happened to say the day the test was written, which asserts **more** than the contract does and fails on the first run that legitimately differs.
>
> This is the failure mode a migration onto `ContainsJson` walks into: collapsing a block of per-property checks feels mechanical, so a volatile field is easy to sweep in with the stable ones, and a presence check (`HasJsonProperty("totalDurationMs")`) silently becomes an equality check. The test is now flaky - and the usual cure for a flaky assertion is to delete it, which is how the field ends up checked by nothing at all. Keep volatile fields on their own presence or predicate assertion, or exclude them with `o => o.IgnorePath("...")`:
>
> ```csharp
> // status and the entry shape are the contract; totalDurationMs is not
> await Assert.That(response).ContainsJson("""
>     { "status": "Healthy",
>       "entries": [ { "name": "db" } ] }
>     """, ct);
> await Assert.That(response).HasJsonProperty("totalDurationMs", ct);   // present, not pinned
> ```

### Pattern: the list includes these elements, among others (`ContainsJson` + `IgnoreArrayOrder`)

Health, status, and listing endpoints return more entries than one test cares about, often in no guaranteed order. Name only the entries the test needs, with only the fields it checks; `IgnoreArrayOrder()` matches each expected element against some actual element (a multiset subset), so extra entries and any order pass:

```csharp
// Passes when "entries" holds a healthy "db" and a healthy "cache" entry, wherever they are,
// whatever else the endpoint lists and whatever other fields each entry carries.
await Assert.That(response).ContainsJson("""
    { "entries": [
        { "name": "db",    "status": "Healthy" },
        { "name": "cache", "status": "Healthy" } ] }
    """, o => o.IgnoreArrayOrder(), ct);
```

This replaces a hand-written loop over `EnumerateArray()` that searches for each name. Each actual element is used for one expected element at most, so two expected entries need two matching actual entries. Without `IgnoreArrayOrder()`, an expected array is a positional prefix: the first expected element must match the first actual element. On failure the message lists every expected element that found no match.

### Pattern: assert an HTTP-response JSON body in one fluent call

`HasJsonResponse<T>` collapses the "check status + read body + deserialize + structural compare" pattern into a single fluent call. The deserialization is AOT-clean (uses the source-generated `JsonTypeInfo<T>`); the failure message includes the body (truncated at 256 chars) for an unexpected status (for example a non-200 response, when the body is non-empty) and for an undeserializable body, so those cases surface their actual content. A body that deserializes to a different value reports that value; the expectation line shows the expected one.

```csharp
using System.Net;

[Test]
public async Task GetOrder_ReturnsExpectedOrder(CancellationToken ct)
{
    using var response = await _httpClient.GetAsync("/orders/42", ct);

    await Assert.That(response).HasJsonResponse(
        HttpStatusCode.OK,
        MyJsonContext.Default.OrderDto,
        expected: new OrderDto(Id: 42, Customer: "alice", Total: 19.99m),
        cancellationToken: ct);
}
```

### Pattern: assert an RFC 7807 ProblemDetails response

`MatchesProblemDetails` asserts the response is a valid RFC 7807 ProblemDetails (Content-Type `application/problem+json` - case-insensitive per RFC 9110, deserializable shape) and that each specified field matches. Unspecified fields skip (pass `null`). No ASP.NET Core dependency (such as `Microsoft.AspNetCore.Mvc.Abstractions`) is required at runtime - the assertion uses an internal mirror type, so the package takes no ASP.NET Core reference.

```csharp
using System.Net.Http.Json;

[Test]
public async Task PostOrder_OnValidationFailure_ReturnsProblemDetails(CancellationToken ct)
{
    using var response = await _httpClient.PostAsJsonAsync("/orders", new { Quantity = -1 }, ct);

    await Assert.That(response).MatchesProblemDetails(
        status: 400,
        title: "Validation failed",
        type: "https://example.com/probs/validation",
        cancellationToken: ct);
}
```

For ASP.NET Core's `ValidationProblemDetails` (which carries an `errors` dictionary keyed by field name), use `MatchesValidationProblemDetails` instead.

**`status` is both the HTTP status code and the body's field** (v0.7.0+). RFC 7807 section 3.1 requires the two to be the same, so both ProblemDetails assertions check `response.StatusCode` as well as the `status` field of the body: a 200 response whose body has `"status": 400` fails `MatchesProblemDetails(status: 400)` with `HTTP status: 200 OK (expected 400)`. A separate status-code assertion next to it is no longer needed. The failure lists every entry that differs in one list; for `MatchesValidationProblemDetails` that includes each expected `errors` key that is absent, null or holds other messages, and only those:

```text
Expected to be an RFC 7807 validation problem response with status 422 and the expected errors
but 4 entries differed:
  HTTP status: 400 BadRequest (expected 422)
  status: 400 (expected 422)
  errors.Age: ["too low"] (expected ["too high"])
  errors.Email: absent (expected ["invalid"])

at Assert.That(validation).MatchesValidationProblemDetails(422, errors, , , , , ct)
```

**`MatchesProblemDetails` asserts the media type as well as the fields.** A test that existed only to check for `application/problem+json` is therefore already covered by it, and migrating both that test and a separate status/field test onto this one assertion leaves the two with identical bodies - which a strict analyzer set flags (SonarAnalyzer `S4144` fails the build on duplicate method implementations). Drop the now-redundant content-type test. If you would rather keep a focused one for its narrower failure message, assert the media type directly with TUnit's `HasContentType` instead of restating the ProblemDetails check; note that it compares case-sensitively, so it fails on `Application/Problem+Json`, which `MatchesProblemDetails` accepts.

### Pattern: AOT round-trip CI gate (`RoundtripsCleanlyVia` + `HasJsonTypeInfoFor`)

**`HasJsonTypeInfoFor<T>` is the only assertion here that guards a failure class the rest of your suite can miss entirely.** A domain type missing from the `JsonSerializerContext` is not a compile error, and nothing fails until something actually serializes that exact type. Whether it surfaces before deployment depends on the resolver chain: with a reflection fallback in the chain (the common default), a JIT test run serializes the unregistered type happily, while the published Native-AOT binary - which has no reflection to fall back to - throws at runtime. That is the worst shape a bug can have: green in CI, broken in production, on a code path a test never thought to cover. One assertion per registered type moves the check to CI, where it belongs.

The pair of regression assertions catches the "added a domain type but forgot to update the `JsonSerializerContext`" class at CI time, before any runtime serialization touches the unregistered type. `RoundtripsCleanlyVia` verifies that a populated instance survives serialize -> deserialize -> re-serialize through a specific `JsonTypeInfo<T>` without drift; `HasJsonTypeInfoFor` verifies the context knows about the type at all.

```csharp
[Test]
public async Task SerializerContextStaysInSyncWithDomainTypes()
{
    // The context registers every domain type the assertion expects.
    await Assert.That(MyJsonContext.Default).AsJsonContext().HasJsonTypeInfoFor<UserDto>();
    await Assert.That(MyJsonContext.Default).AsJsonContext().HasJsonTypeInfoFor<OrderDto>();
    await Assert.That(MyJsonContext.Default).AsJsonContext().HasJsonTypeInfoFor<HealthCheckResponse>();

    // Each registered type round-trips cleanly through that registration.
    var user = new UserDto(Id: 1, Name: "alice", Active: true);
    var order = new OrderDto(Id: 42, Customer: "alice", Total: 19.99m);
    await Assert.That(user).RoundtripsCleanlyVia(MyJsonContext.Default.UserDto);
    await Assert.That(order).RoundtripsCleanlyVia(MyJsonContext.Default.OrderDto);
}
```

`.AsJsonContext()` is a one-method bridge that produces an `IJsonContextAssertionSource` whose `Context` is statically typed at `JsonSerializerContext`. The bridge exists because `Assert.That(MyJsonContext.Default)` returns an assertion source typed at the concrete subtype (`IAssertionSource<MyJsonContext>`), and C# does not allow partial generic-type-argument inference. The bridge moves the receiver type out of the leaf assertion's signature, restoring the single-explicit-generic shape (`HasJsonTypeInfoFor<MyDto>()`) consumers expect. The pattern is AOT-clean (one O(1) lookup against the context's type registry; the upcast goes through TUnit's existing `Map` pipeline, no reflection).

`RoundtripsCleanlyVia` compares the JSON the serializer writes, and the serializer writes what the contract it uses describes: the properties and converters of a registered type. When that contract belongs to a base type or interface, the properties that only the value's own type declares are left out of the JSON, so differences in them are never detected. That happens to a derived value held through a base type without `[JsonDerivedType]`, and to an unregistered type in an `object` property that has a registered base type or interface. To cover those properties, register the type with `[JsonSerializable]` and round-trip it through its own `JsonTypeInfo<T>`.

Two cases throw `NotSupportedException`, so the test errors instead of failing. An unregistered type in an `object` property with no registered ancestor besides `object` has no contract at all. And an `object` property whose JSON is not `null` deserializes to a `JsonElement` by default, so the second serialization throws unless the context registers `JsonElement` (`[JsonSerializable(typeof(JsonElement))]`).

### Pattern: compose with `SnapshotAssertions.TUnit` for canonicalized body snapshots

`JsonRenderers.ReformatJson<T>` returns a `Func<string, string>` that canonicalizes a JSON string via the consumer's `JsonSerializerContext` (deserialize then re-serialize through the supplied `JsonTypeInfo<T>`). Composes with [`SnapshotAssertions.TUnit`](https://www.nuget.org/packages/SnapshotAssertions.TUnit/)'s `MatchesSnapshot(Func<>)` overload at the consumer's call site, without coupling the packages (the family's [Cross-package references rule](CONVENTIONS.md) keeps production-side `PackageReference`s separate; composition happens at the test's call site via standard delegates):

```csharp no-compile
[Test]
public async Task PostOrder_ResponseBodyMatchesSnapshot(CancellationToken ct)
{
    using var response = await _httpClient.PostAsJsonAsync("/orders", new { Sku = "A-1", Quantity = 3 }, ct);
    var body = await response.Content.ReadAsStringAsync(ct);

    // Canonicalizes body via MyJsonContext (deterministic ordering + formatting), then snapshots.
    await Assert.That(body).MatchesSnapshot(
        JsonRenderers.ReformatJson(MyJsonContext.Default.OrderDto));
}
```

The two-step composition (async body-read in the test, sync canonicalize + snapshot) keeps the rule "no sync-over-async" intact while letting the snapshot pipeline operate on a deterministic JSON shape.

### Pattern: extend the same DSL with consumer-authored typed assertions

For domain-specific assertions that this package doesn't ship, the `JsonAssertions.JsonFailureMessage` factory methods (`ParseFailure`, `PropertyNotFound`, `PropertyShouldNotExist`, `ValueMismatch`, `ShapeMismatch`) produce failure messages that match the package's diagnostic style. Each returns the text after `but`, so state the expectation in `ExpectationMessage`, in single quotes (TUnit's generator pastes it into an interpolated string, which cannot hold a double quote). Compose them with TUnit's `[GenerateAssertion]` to ship a typed assertion alongside your own DTO conventions:

```csharp
using System.Text.Json;
using JsonAssertions;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

public static class OrderAssertions
{
    [GenerateAssertion(ExpectationMessage = "to have order status '{expectedStatus}'")]
    public static AssertionResult HasOrderStatusOf(this string body, string expectedStatus)
    {
        using var doc = JsonDocument.Parse(body);
        var resolution = JsonPath.Resolve(doc.RootElement, "order.status");
        if (!resolution.Found)
        {
            return AssertionResult.Failed(JsonFailureMessage.PropertyNotFound("order.status", resolution));
        }

        return JsonValueComparison.Matches(resolution.Element, expectedStatus)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ValueMismatch("order.status", resolution, expectedDescription: string.Empty));
    }
}

// Call site (auto-import via [GenerateAssertion]):
await Assert.That(body).HasOrderStatusOf("Paid");
```

`ValueMismatch` and `ShapeMismatch` still take an `expectedDescription`, for compatibility, but no longer render it: the expectation line shows the expected value. A failure then reads like the package's own:

```text
Expected to have order status 'Paid'
but found "Pending"

at Assert.That(body).HasOrderStatusOf("Paid")
```

## Known issues

**`.Or` acts as `.And` on an `HttpResponseMessage`** (seen on TUnit 1.80.0 and 1.81.10). On a response, a chain fails whenever any link fails: `Assert.That(response).HasJsonProperty("missing", ct).Or.HasJsonProperty("user", ct)` fails, and so does TUnit's own `IsNotFound().Or.IsOk()` on a 200 response. The same chains pass on a `string` and a `JsonElement`. The cause is in TUnit: an assertion on a non-sealed receiver is generated as a covariant extension, which builds on `AssertionContext.Map`, and `Map` drops the `.Or` combiner. The failure is loud (a test that should pass fails), never a silent pass. Until TUnit fixes it, assert on the body string, or assert the status code separately instead of chaining it with `.Or`:

```csharp
var body = await response.Content.ReadAsStringAsync(ct);
await Assert.That(body).HasJsonProperty("a").Or.HasJsonProperty("b");
```

## Design notes

- **Path-context failure messages.** A hand-rolled check that fails with `Expected to be true` / `but found False` forces you to instrument the test to find which segment failed. Every path assertion here walks the path and reports the longest prefix that resolved, the segment that blocked, and the value kind at the blocking point. This is the load-bearing reason the package exists; without it, it would be a thin wrapper over `TryGetProperty(...).IsTrue()`.
- **Single package, not core + adapter.** The bare `JsonAssertions` ID is taken on nuget.org, so a separate core could not use the matching name; the JSON primitives are also thin enough that a split would be near-empty. Types still live in two namespaces (`JsonAssertions` core, `JsonAssertions.TUnit` adapter), so a split stays low-cost if the ID ever frees up.
- **Mirror types for ProblemDetails.** `MatchesProblemDetails` deserializes into internal `ProblemDetailsMirror` types rather than `Microsoft.AspNetCore.Mvc.ProblemDetails`: that type lives in the ASP.NET Core shared framework, and referencing it would tie a package that otherwise depends only on `System.Text.Json` and TUnit to ASP.NET Core. The mirror's source-generated `JsonSerializerContext` makes deserialization AOT-clean. The test project asserts the mirror's shape against the real ASP.NET Core type, catching wire-format drift at CI time.
- **The `.AsJsonContext()` bridge for `HasJsonTypeInfoFor<T>`.** A single-generic `HasJsonTypeInfoFor<MyDto>()` would otherwise need a second receiver-type generic at every call site (C# cannot partially infer type arguments). The bridge upcasts the source to a statically-`JsonSerializerContext`-typed wrapper, restoring the single-generic shape. Zero-cost, AOT-clean. Background: [thomhurst/TUnit#5922](https://github.com/thomhurst/TUnit/issues/5922).

## Stability intent (pre-1.0)

This is a 0.x release and the public API may evolve.

- **Additive changes** (new entry points, new input overloads) ship in a minor version (0.X.0) without breaking ApiCompat; a patch carries fixes and docs only.
- **Breaking changes** to existing signatures or behavior bump the minor version (0.X.0) and are called out under BREAKING in the [CHANGELOG](CHANGELOG.md).
- `PackageValidationBaselineVersion` pins to the previous shipped version so ApiCompat breakage is caught at pack time; `CompatibilitySuppressions.xml` records accepted differences.
- Failure-message text is not part of the stable public surface; pin behavior against the `JsonPath` / `JsonValueComparison` / `JsonShape` primitives, not against full message-text equality. 0.7.0 rewrote every text to one rule (see [Failure diagnostics](#failure-diagnostics)).

The 1.0 milestone signals API stability.

## Roadmap

The next increments, each as a reviewed pull request:

- Semantic JSON equality already ships as `IsEquivalentJsonTo` (0.5.0); subset matching (`ContainsJson`) and typed extraction (`GetJsonValue<T>` / `GetJsonString` / `GetJsonElement`) in 0.6.0; several paths at once (`HasJsonProperties`) and every value of a list (`GetJsonValues<T>`) in 0.7.0. Further increments are demand-driven.

### Out of scope

- **Production-code use** (per the scope blockquote above).
- **JSON schema validation** out of scope for this package: it needs a JSON Schema engine (a runtime dependency `System.Text.Json` does not provide) and is a different mental model from path / value / shape assertions. A future opt-in adapter package could add it if demand appears, keeping the schema-engine dependency out of this zero-dependency core.
- **A JSON serializer or parser.** The package builds on `System.Text.Json`; it does not replace it.

## Family compatibility

The nine assertion-family packages: `LogAssertions.TUnit`, `TimeAssertions.TUnit`, `SnapshotAssertions.TUnit`, `MathAssertions.TUnit`, `JsonAssertions.TUnit`, `SseAssertions.TUnit`, `GrpcAssertions.TUnit`, `TracingAssertions.TUnit`, and `MetricsAssertions.TUnit`: release independently and target the same .NET TFM at any moment (LTS-anchored, multi-target during STS support windows; see the [TFM policy in CONVENTIONS.md](CONVENTIONS.md#tfm-policy) for the rotation schedule). **Mix versions freely.** Each package ships under SemVer with `EnablePackageValidation` strict-mode ApiCompat against its previous baseline, so binary breaks within a version line are caught at pack time.

For per-package release notes:
- [LogAssertions.TUnit CHANGELOG](https://github.com/JohnVerheij/LogAssertions.TUnit/blob/main/CHANGELOG.md)
- [TimeAssertions.TUnit CHANGELOG](https://github.com/JohnVerheij/TimeAssertions.TUnit/blob/main/CHANGELOG.md)
- [SnapshotAssertions.TUnit CHANGELOG](https://github.com/JohnVerheij/SnapshotAssertions.TUnit/blob/main/CHANGELOG.md)
- [MathAssertions.TUnit CHANGELOG](https://github.com/JohnVerheij/MathAssertions.TUnit/blob/main/CHANGELOG.md)
- [JsonAssertions.TUnit CHANGELOG](https://github.com/JohnVerheij/JsonAssertions.TUnit/blob/main/CHANGELOG.md)
- [SseAssertions.TUnit CHANGELOG](https://github.com/JohnVerheij/SseAssertions.TUnit/blob/main/CHANGELOG.md)
- [GrpcAssertions.TUnit CHANGELOG](https://github.com/JohnVerheij/GrpcAssertions.TUnit/blob/main/CHANGELOG.md)
- [TracingAssertions.TUnit CHANGELOG](https://github.com/JohnVerheij/TracingAssertions.TUnit/blob/main/CHANGELOG.md)
- [MetricsAssertions.TUnit CHANGELOG](https://github.com/JohnVerheij/MetricsAssertions.TUnit/blob/main/CHANGELOG.md)

## Pair with

- **[`LogAssertions.TUnit`](https://www.nuget.org/packages/LogAssertions.TUnit/)**: fluent log assertions over `Microsoft.Extensions.Logging.Testing.FakeLogCollector`.
- **[`TimeAssertions.TUnit`](https://www.nuget.org/packages/TimeAssertions.TUnit/)**: `TimeProvider`-aware time assertions and cross-cutting `.WithinTimeBudget(...)` chain methods.
- **[`SnapshotAssertions.TUnit`](https://www.nuget.org/packages/SnapshotAssertions.TUnit/)**: text-snapshot assertions for API-surface tests and similar deterministic-string scenarios. Coexists with Verify; covers the 80% case without coverage friction.
- **[`MathAssertions.TUnit`](https://www.nuget.org/packages/MathAssertions.TUnit/)**: tolerance-aware fluent assertions over numeric and geometric types (vectors, quaternions, matrices, planes, complex numbers, arrays).
- **[`SseAssertions.TUnit`](https://www.nuget.org/packages/SseAssertions.TUnit/)**: Server-Sent Events (SSE) wire-format assertions: event-count, field shape (`event:`, `data:`, `id:`, `retry:`), and stream content validation.
- **[`GrpcAssertions.TUnit`](https://www.nuget.org/packages/GrpcAssertions.TUnit/)**: fluent gRPC outcome assertions (`ThrowsGrpcException` with `StatusCode` shorthands and detail refinements) plus the `GrpcCallBuilder` test-double helper.
- **[`TracingAssertions.TUnit`](https://www.nuget.org/packages/TracingAssertions.TUnit/)**: fluent OpenTelemetry distributed-tracing (`Activity` / span) assertions: operation name, tags, status, kind, events, and parent/child, root and same-trace relationships, captured via a raw `ActivityListener` with no OpenTelemetry SDK dependency.
- **[`MetricsAssertions.TUnit`](https://www.nuget.org/packages/MetricsAssertions.TUnit/)**: fluent assertions over `System.Diagnostics.Metrics` instruments (counters, histograms, gauges), built on `MetricCollector`.

## Contributing

Issues and pull requests welcome. Before opening a PR:

- Run `dotnet build` and `dotnet test` locally; the CI pipeline enforces the same quality bar (zero warnings as errors, 90% line / 90% branch coverage minimum).
- Match the existing code style (`.editorconfig` is authoritative; `dotnet format` covers formatting).
- For new assertions, include a test for both the happy path and a representative failure case.

For larger ideas, open a [Discussion](https://github.com/JohnVerheij/JsonAssertions.TUnit/discussions) first to align on direction before investing implementation time.

See [CONTRIBUTING.md](CONTRIBUTING.md) for the full PR review checklist, and [CONVENTIONS.md](CONVENTIONS.md) for the family-wide code conventions shared across the nine packages: `LogAssertions.TUnit`, `TimeAssertions.TUnit`, `SnapshotAssertions.TUnit`, `MathAssertions.TUnit`, `JsonAssertions.TUnit`, `SseAssertions.TUnit`, `GrpcAssertions.TUnit`, `TracingAssertions.TUnit`, and `MetricsAssertions.TUnit`.

## License

[MIT](LICENSE). Copyright (c) 2026 John Verheij.
