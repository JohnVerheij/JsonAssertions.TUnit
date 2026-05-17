# JsonAssertions.TUnit

[![CI](https://github.com/JohnVerheij/JsonAssertions.TUnit/actions/workflows/ci.yml/badge.svg)](https://github.com/JohnVerheij/JsonAssertions.TUnit/actions/workflows/ci.yml)
[![CodeQL](https://github.com/JohnVerheij/JsonAssertions.TUnit/actions/workflows/codeql.yml/badge.svg)](https://github.com/JohnVerheij/JsonAssertions.TUnit/actions/workflows/codeql.yml)
[![codecov](https://codecov.io/gh/JohnVerheij/JsonAssertions.TUnit/branch/main/graph/badge.svg)](https://codecov.io/gh/JohnVerheij/JsonAssertions.TUnit)
[![NuGet](https://img.shields.io/nuget/v/JsonAssertions.TUnit.svg)](https://www.nuget.org/packages/JsonAssertions.TUnit/)
[![Downloads](https://img.shields.io/nuget/dt/JsonAssertions.TUnit.svg)](https://www.nuget.org/packages/JsonAssertions.TUnit/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

TUnit-native JSON assertions for .NET. Fluent entry points over TUnit's `Assert.That(...)` pipeline for asserting against `System.Text.Json` documents, HTTP response bodies (including RFC 7807 ProblemDetails), and the registration state of source-generated `JsonSerializerContext` instances. AOT-compatible, trimmable, no runtime reflection in the assertion path.

> **Scope:** Test projects only. Not intended for production code.

---

## Status: v0.3.0

Property existence, value-at-path, value-predicate, value-one-of, value-parsable-as-`T`, shape (kind / array-length / non-empty / boolean / non-empty-string), HTTP-response JSON assertions (status + AOT-clean deserialization + structural equality, RFC 7807 ProblemDetails, ValidationProblemDetails), AOT-context regression assertions (`RoundtripsCleanlyVia` and `HasJsonTypeInfoFor` via the `AsJsonContext` bridge), and a canonicalising-renderer (`JsonRenderers.ReformatJson`) for composition with `SnapshotAssertions.TUnit` at the consumer's call site. Each fluent entry point is available over a JSON `string`, a `System.Text.Json.JsonElement`, and an `HttpResponseMessage` (whose body is read as the JSON document):

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
| `HasJsonResponse<T>(HttpStatusCode, JsonTypeInfo<T>, T expected, ct)` on `HttpResponseMessage` | Asserts status + AOT-clean deserialization + structural equality in one chain. |
| `MatchesProblemDetails(int status, ..., ct)` on `HttpResponseMessage` | Asserts an RFC 7807 `application/problem+json` response with matching fields. |
| `MatchesValidationProblemDetails(int status, IReadOnlyDictionary<string, string[]> errors, ..., ct)` on `HttpResponseMessage` | Like `MatchesProblemDetails` plus the ASP.NET Core `errors` dictionary. |
| `RoundtripsCleanlyVia<T>(JsonTypeInfo<T>)` on any `T` | Asserts serialize → deserialize → re-serialize is byte-identical via the supplied source-generated `JsonTypeInfo<T>`. |
| `AsJsonContext().HasJsonTypeInfoFor<T>()` on a `JsonSerializerContext`-typed source | Asserts the supplied source-generated context registers a `JsonTypeInfo<T>` for `T`. |
| `JsonRenderers.ReformatJson<T>(JsonTypeInfo<T>)` *(static factory)* | Returns `Func<string, string>` that canonicalises a JSON string via the consumer's `JsonSerializerContext`. Composes with `SnapshotAssertions.TUnit`'s `MatchesSnapshot(Func<>)` at the consumer's call site without coupling the packages. |

The point over a hand-rolled `TryGetProperty(...).IsTrue()` helper is the **failure message**: every assertion renders a path-context block saying *where* resolution stopped, not merely that it did.

The AOT-context regression assertions (`HasJsonTypeInfoFor`, `RoundtripsCleanlyVia`) pair together as a CI gate to keep a source-generated `JsonSerializerContext` in sync with the consumer's domain types — see the dedicated section below.

---

## Table of contents

- [Why this package](#why-this-package)
- [Install](#install)
- [Package layout](#package-layout)
- [Namespaces (and a `GlobalUsings.cs` recommendation)](#namespaces-and-a-globalusingscs-recommendation)
- [Quick start](#quick-start)
- [Path syntax](#path-syntax)
- [Entry points](#entry-points)
- [Failure diagnostics](#failure-diagnostics)
- [Cookbook: common patterns](#cookbook-common-patterns)
- [Modern .NET 10+ practices on display](#modern-net-10-practices-on-display)
- [Design notes](#design-notes)
- [Stability intent (pre-1.0)](#stability-intent-pre-10)
- [Roadmap](#roadmap)
- [Out of scope](#out-of-scope)
- [Family](#family)
- [Contributing](#contributing)
- [License](#license)

---

## Why this package

Asserting against a JSON response body or document in tests typically devolves into one of:

- A hand-rolled `TryGetProperty(...).IsTrue()` chain with no failure context:
  ```csharp
  using var doc = JsonDocument.Parse(body);
  await Assert.That(doc.RootElement.TryGetProperty("user", out var user) && user.TryGetProperty("name", out var name) && name.GetString() == "alice")
      .IsTrue().Because("user.name should equal alice");
  ```
  When this fails the test message is *"expected: True; got: False"* — true at which level, and why, is on the caller to investigate manually.
- A bespoke `JsonAssertions` helper class re-implemented in every project, with subtle differences in failure messages, path-syntax, malformed-body handling, and HTTP-receiver wiring between codebases.

This package replaces both with a fluent DSL that auto-imports alongside TUnit's own assertions. Every assertion produces a **path-context block** on failure — the message names *where* on the path resolution stopped, not just that something didn't match — and the same entry points are available on a JSON `string`, a `JsonElement`, and an `HttpResponseMessage` (whose body is read as the JSON document, with the cancellation token flowing to the body read).

For the AOT-shipping audience, v0.3.0 adds two paired regression assertions (`RoundtripsCleanlyVia` + `HasJsonTypeInfoFor`) that catch the "added a new domain type but forgot to add `[JsonSerializable(typeof(NewType))]` to the context" class at CI time, before any runtime serialization touches the unregistered type.

## Install

```bash
dotnet add package JsonAssertions.TUnit
```

**Requirements:** TUnit 1.44.39 or later, .NET 10. `System.Text.Json` is in-box on .NET 10, so the package carries no runtime dependency beyond TUnit. The package is AOT-compatible, trimmable, and uses no runtime reflection in the assertion path.

## Package layout

This repo ships **one** NuGet package with types in two namespaces — the same consumer feel as the rest of the assertion family (a framework-agnostic core plus a TUnit adapter), but in a single assembly to keep zero-overhead packaging at the v0.x stage:

| Package | Purpose | Depends on |
|---|---|---|
| [`JsonAssertions.TUnit`](https://www.nuget.org/packages/JsonAssertions.TUnit/) | `JsonAssertions` framework-agnostic core (`JsonPath`, `JsonValueComparison`, `JsonShape`, `JsonRenderers`, `JsonFailureMessage`) plus `JsonAssertions.TUnit` TUnit adapter (`[GenerateAssertion]` entry points + hand-written extensions over `Assert.That(...)`) | `System.Text.Json` (in-box on net10.0) + `TUnit.Assertions` + `TUnit.Core` |

A future package split (a bare-identifier `JsonAssertions` core package plus this adapter) would fall along the namespace seam if the standalone identifier becomes available; shipping the seam now keeps the option open without committing to it. Adapters for other test frameworks (NUnit, xUnit, MSTest) are *not* shipped today; they would reuse the `JsonAssertions` core. Open a feature request if you need one.

## Namespaces (and a `GlobalUsings.cs` recommendation)

The single package places types in two namespaces with deliberately-different scopes:

| Type / member | Namespace | Auto-imported? |
|---|---|---|
| `JsonPath`, `JsonPathResolution`, `JsonValueComparison`, `JsonShape`, `JsonRenderers`, `JsonFailureMessage` (framework-agnostic core) | `JsonAssertions` | **No** (needs `using JsonAssertions;`) |
| Source-generated assertion entry points (`HasJsonProperty()`, `HasJsonValue()`, `HasJsonResponse()`, `MatchesProblemDetails()`, ...) | `TUnit.Assertions.Extensions` | **Yes** (TUnit auto-imports) |
| `IJsonContextAssertionSource` + `AsJsonContext()` bridge (JSON-context family) | `JsonAssertions.TUnit` | **No** (needs `using JsonAssertions.TUnit;` when chaining `.AsJsonContext()`) |

The fluent assertion entry points auto-import via `TUnit.Assertions.Extensions`; no extra `using` directive is needed if your test project already uses TUnit. For test projects that consume `JsonRenderers.ReformatJson<T>` or the `JsonFailureMessage` factory methods directly, put the namespace into a single `GlobalUsings.cs` so every test file sees it without ceremony:

```csharp
// tests/MyApp.Tests/GlobalUsings.cs
global using JsonAssertions;
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

Wildcard segments (e.g. `[*]`) are not part of the 0.3.0 surface; see the roadmap below.

## Entry points

The full v0.3.0 entry-point catalog is in the Status table at the top of this file. The summary below organizes them by domain.

**Path / value / shape (over JSON `string`, `JsonElement`, and `HttpResponseMessage`):**

- `HasJsonProperty(path)` / `DoesNotHaveJsonProperty(path)`
- `HasJsonValue(path, expected)` (`string` / `bool` / number overloads)
- `HasJsonValueOneOf(path, candidates[])` (`string[]` / `double[]` overloads)
- `HasJsonValueMatching(path, predicate)`
- `HasJsonValueParsableAs<T>(path)` *(where T : IParsable&lt;T&gt;)*
- `HasJsonValueKind(path, kind)` / `HasJsonBoolean(path)` / `HasNonEmptyJsonString(path)`
- `HasJsonArrayLength(path, length)` / `HasNonEmptyJsonArray(path)` / `HasEmptyJsonArray(path)`

**HTTP-response combined assertions (on `HttpResponseMessage`):**

- `HasJsonResponse<T>(status, JsonTypeInfo<T>, expected, ct)` — combined status + AOT-clean deserialization + structural equality
- `MatchesProblemDetails(status, title?, detail?, type?, instance?, ct)` — RFC 7807
- `MatchesValidationProblemDetails(status, errors, ..., ct)` — RFC 7807 + ASP.NET Core `errors`

**AOT-context regression (on a `JsonSerializerContext`-typed source, via `.AsJsonContext()`):**

- `AsJsonContext().HasJsonTypeInfoFor<T>()` — asserts the context registers `JsonTypeInfo<T>`
- `RoundtripsCleanlyVia<T>(JsonTypeInfo<T>)` — serialize → deserialize → re-serialize is byte-identical

**Composition + extension point (in `JsonAssertions` core namespace):**

- `JsonRenderers.ReformatJson<T>(JsonTypeInfo<T>)` — static factory returning `Func<string, string>` that canonicalises a JSON string for snapshot composition
- `JsonFailureMessage` — public path-family factory methods (`ParseFailure`, `PropertyNotFound`, `PropertyShouldNotExist`, `ValueMismatch`, `ShapeMismatch`) for consumer-authored typed JSON assertions

## Failure diagnostics

Every assertion produces a *path-context block* on failure: the message names where on the path navigation stopped, not just that something didn't match.

**Path-resolution failure:**

```text
to have a JSON property at path "user.address.city"
  resolved as far as: user.address
  no property "city" on "user.address"
```

**Array-index failure on a non-array:**

```text
to have a JSON property at path "user[0]"
  resolved as far as: user
  cannot index [0]: "user" is an Object, not an array
```

**Malformed-body failure** (the JSON `string` or HTTP response body wasn't valid JSON):

```text
the asserted value to be parseable JSON
  but parsing failed: '{' is invalid after a property name. Expected a ':'. Path: $.user | LineNumber: 0 | BytePositionInLine: 14.
```

**HTTP combined-assertion failure** (status correct, body deserialized but the resulting value differed from `expected`):

```text
the response to deserialize to a TestDto structurally equal to expected
  expected: TestDto { Id = 42, Name = alice }
  got:      TestDto { Id = 99, Name = bob }
  body:     {"Id":99,"Name":"bob"}
```

**RFC 7807 ProblemDetails field-mismatch failure** (collected into a single message for all non-matching fields):

```text
the response to be RFC 7807 ProblemDetails with the asserted fields
  title: expected "Validation failed" got "Bad request"
  detail: expected "Field X is required" got ""
```

**AOT-context registration failure** (the source-gen context does not register the asserted type):

```text
to register JsonTypeInfo<DateTime>
the JsonSerializerContext to have a JsonTypeInfo registered for DateTime
  but MyJsonContext.GetTypeInfo(typeof(DateTime)) returned null
  hint: add [JsonSerializable(typeof(DateTime))] to MyJsonContext
```

**AOT round-trip drift failure** (the value re-serialized to a different JSON shape):

```text
the value to round-trip cleanly through the supplied JsonTypeInfo
  but the serialized form drifted between trips:
  first:  {"id":42,"name":"alice"}
  second: {"id":42,"name":"alice","drift":true}
```

Failure-message text is not part of the stable public surface; pin behavior against the `JsonPath` / `JsonValueComparison` / `JsonShape` primitives, not against full message-text equality.

## Cookbook: common patterns

### Pattern: assert an HTTP-response JSON body in one fluent call

`HasJsonResponse<T>` collapses the "check status + read body + deserialize + structural compare" pattern into a single fluent call. The deserialization is AOT-clean (uses the source-generated `JsonTypeInfo<T>`); the failure message includes the body (truncated at 256 chars) so non-200 responses or shape mismatches surface their actual content.

```csharp
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

`MatchesProblemDetails` asserts the response is a valid RFC 7807 ProblemDetails (Content-Type `application/problem+json` — case-insensitive per RFC 9110, deserializable shape) and that each specified field matches. Unspecified fields skip (pass `null`). The Apache 2.0 `Microsoft.AspNetCore.Mvc.Abstractions` dependency is *not* required at runtime — the assertion uses an internal mirror type so the production package stays MIT-clean.

```csharp
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

### Pattern: AOT round-trip CI gate (`RoundtripsCleanlyVia` + `HasJsonTypeInfoFor`)

The pair of regression assertions catches the "added a domain type but forgot to update the `JsonSerializerContext`" class at CI time, before any runtime serialization touches the unregistered type. `RoundtripsCleanlyVia` verifies that a populated instance survives serialize → deserialize → re-serialize through a specific `JsonTypeInfo<T>` without drift; `HasJsonTypeInfoFor` verifies the context knows about the type at all.

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

### Pattern: compose with `SnapshotAssertions.TUnit` for canonicalised body snapshots

`JsonRenderers.ReformatJson<T>` returns a `Func<string, string>` that canonicalises a JSON string via the consumer's `JsonSerializerContext` (deserialize then re-serialize through the supplied `JsonTypeInfo<T>`). Composes with [`SnapshotAssertions.TUnit`](https://www.nuget.org/packages/SnapshotAssertions.TUnit/)'s `MatchesSnapshot(Func<>)` overload at the consumer's call site, without coupling the packages (the family's [Cross-package references rule](CONVENTIONS.md) keeps production-side `PackageReference`s separate; composition happens at the test's call site via standard delegates):

```csharp
[Test]
public async Task PostOrder_ResponseBodyMatchesSnapshot(CancellationToken ct)
{
    using var response = await _httpClient.PostAsJsonAsync("/orders", new { ... }, ct);
    var body = await response.Content.ReadAsStringAsync(ct);

    // Canonicalises body via MyJsonContext (deterministic ordering + formatting), then snapshots.
    await Assert.That(body).MatchesSnapshot(
        JsonRenderers.ReformatJson(MyJsonContext.Default.OrderDto));
}
```

The two-step composition (async body-read in the test, sync canonicalise + snapshot) keeps the rule "no sync-over-async" intact while letting the snapshot pipeline operate on a deterministic JSON shape.

### Pattern: extend the same DSL with consumer-authored typed assertions

For domain-specific assertions that this package doesn't ship, the `JsonAssertions.JsonFailureMessage` factory methods (`ParseFailure`, `PropertyNotFound`, `PropertyShouldNotExist`, `ValueMismatch`, `ShapeMismatch`) produce failure messages that match the package's diagnostic style. Compose them with TUnit's `[GenerateAssertion]` to ship a typed assertion alongside your own DTO conventions:

```csharp
public static class OrderAssertions
{
    [GenerateAssertion]
    public static AssertionResult HasOrderStatusOf(this string body, string expectedStatus)
    {
        var doc = JsonDocument.Parse(body);
        var resolution = JsonPath.Resolve(doc.RootElement, "order.status");
        if (!resolution.Found)
        {
            return AssertionResult.Failed(JsonFailureMessage.PropertyNotFound("order.status", resolution));
        }
        return string.Equals(resolution.Element.GetString(), expectedStatus, StringComparison.Ordinal)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ValueMismatch("order.status", resolution, $"\"{expectedStatus}\""));
    }
}

// Call site (auto-import via [GenerateAssertion]):
await Assert.That(body).HasOrderStatusOf("Paid");
```

## Design notes

### Why path-context failure messages

A `Assert.That(body).HasJsonProperty("user.address.city")` failure that says "*True expected; got: False*" is a debugging burden: the caller has to instrument the test to find out *which* segment of the path didn't resolve. Every assertion in this package walks the path, captures the longest prefix that resolved + the segment that blocked + the kind of value at the blocking point, and renders that as a structured "resolved as far as / reason" block in the failure message. This is the load-bearing reason the package exists; if the path-context block were removed the package would degrade to a thin syntactic wrapper over `TryGetProperty(...).IsTrue()` with worse ergonomics.

### Why a single package (not a core + adapter split)

The rest of the family (`LogAssertions`, `SnapshotAssertions`, `TimeAssertions`, `MathAssertions`) ships two NuGet packages (framework-agnostic core + TUnit adapter). JsonAssertions ships one assembly with types in two namespaces — the same consumer feel, but lower packaging overhead at the v0.x stage. The namespace seam (`JsonAssertions` core types in one ns, `JsonAssertions.TUnit` adapter types in another) is kept so a future split along that seam stays a low-cost option if the bare `JsonAssertions` package identifier on nuget.org ever becomes available.

### Why mirror types for ProblemDetails

`MatchesProblemDetails` and `MatchesValidationProblemDetails` deserialize the response body into internal `ProblemDetailsMirror` / `ValidationProblemDetailsMirror` types rather than the BCL `Microsoft.AspNetCore.Mvc.ProblemDetails`. Two reasons:

1. **MIT-clean production package.** `Microsoft.AspNetCore.Mvc.Abstractions` is Apache 2.0. The family's licensing rule keeps consumer-facing dependencies MIT-only (Apache 2.0 is acceptable in test code, e.g. when the test project takes `FrameworkReference="Microsoft.AspNetCore.App"`, but not in the package's shipped surface).
2. **AOT-clean deserialization.** The mirror has a source-generated `JsonSerializerContext` (`ProblemDetailsMirrorJsonContext`); the assertion path uses `JsonSerializer.Deserialize(json, jsonTypeInfo)` with the source-gen entry, so no runtime reflection.

The mirror is intentionally a faithful shape of the RFC 7807 ProblemDetails wire format (plus the `[JsonExtensionData]` extension-members dictionary per §3.2). The test project asserts the mirror's wire shape against the *real* `Microsoft.AspNetCore.Mvc.ProblemDetails` from the framework reference, so if ASP.NET Core ever drifts the format the test catches it at CI time rather than at consumer runtime.

### Why the `.AsJsonContext()` bridge for `HasJsonTypeInfoFor<T>`

`Assert.That(MyJsonContext.Default)` returns an assertion source typed at the concrete subtype (`IAssertionSource<MyJsonContext>`). A natural single-generic `HasJsonTypeInfoFor<MyDto>()` would require the assertion method to also have a `TContext` generic for the receiver (`HasJsonTypeInfoFor<T, TContext>` where `TContext : JsonSerializerContext`), and C# does not allow partial generic-type-argument inference — both type arguments would have to be supplied at every call site (`HasJsonTypeInfoFor<MyDto, MyJsonContext>()`). The `.AsJsonContext()` bridge wraps the source into an `IJsonContextAssertionSource` whose `Context` is statically typed at `JsonSerializerContext`, moving the receiver-type generic out of the leaf assertion's signature and restoring the single-explicit-generic shape. The bridge is a zero-cost upcast through TUnit's existing `Map` pipeline; AOT-clean, no reflection.

The deeper rationale lives at [thomhurst/TUnit#5922](https://github.com/thomhurst/TUnit/issues/5922) (upstream design discussion). The adapter pattern is the in-layer solution that does not depend on the upstream design landing.

## Modern .NET 10+ practices on display

The package is a deliberate showcase of modern .NET conventions:

- **AOT-compatible** (`IsAotCompatible=true`), trimmable (`IsTrimmable=true`), no runtime reflection in the assertion path.
- **Source-generated assertion entries** via TUnit's `[GenerateAssertion]`. No interface implementation required, no reflection at runtime.
- **C# 14 file-scoped namespaces** + `Nullable=enable` + `TreatWarningsAsErrors=true` + five Roslyn analyzer packs at full strength (Meziantou, SonarAnalyzer, Roslynator, Microsoft.VisualStudio.Threading, DotNetProjectFile).
- **`Microsoft.CodeAnalysis.BannedApiAnalyzers`** enforces no-reflection at build time via a shared `BannedSymbols.txt`.

## Stability intent (pre-1.0)

This is a 0.x release and the public API may evolve.

- **Additive changes** (new entry points, new input overloads) ship in any patch without breaking ApiCompat.
- **Breaking changes** to existing signatures bump the minor version (0.X.0) and are called out in the [CHANGELOG](CHANGELOG.md). The 0.0.1 -> 0.1.0 step evolved the `[GenerateAssertion]` source-method return types from `bool` to `AssertionResult` to enable the path-context failure messages; the generated TUnit chain extensions were unaffected at chain-syntax level. The 0.1.0 -> 0.2.0 step was purely additive (new segment forms in the existing path grammar, plus the five new entry points listed above). The 0.2.0 -> 0.3.0 step was purely additive too (HTTP-response JSON, RFC 7807 ProblemDetails / ValidationProblemDetails, AOT-context regression assertions `RoundtripsCleanlyVia` and `HasJsonTypeInfoFor`, the `JsonRenderers` canonicalising-renderer factory, and the promotion of `JsonFailureMessage` from internal to public as a curated extension point for consumer-authored typed assertions).
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
