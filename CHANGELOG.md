# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.0] - 2026-05-15: Array-indexed paths, root-self, boolean / non-empty-string / matching / one-of / parsable-as<T> assertions, plus the pack-time Release Notes pipeline

### Added

- **Array-indexed path segments.** `JsonPath.Resolve(element, "items[0].name")` navigates `items` (object property) then index 0 (array element) then `name` (object property). Indices are zero-based, non-negative integers in `[N]` brackets. Mixed property + index segments compose freely (`objects[0].planData[1].pickPlanId`). Closes the #1 friction surfaced by the v0.1.0 adoption survey.
- **`$` JSONPath root-self.** `JsonPath.Resolve(element, "$")` resolves to the supplied element itself. `$.user.name` is equivalent to `user.name`; `$[0]` is equivalent to `[0]`. A bare `[0]` against a root array also works without the `$` prefix. Closes the "no path to assert root-array shape" gap surfaced by the v0.1.0 adoption survey.
- **`HasNonEmptyJsonString(path)`** on `string` and `JsonElement` (and `HttpResponseMessage` for the async HTTP entry point). Asserts the value at `path` is a JSON string of non-zero length. A non-string kind or an empty `""` string fails.
- **`HasJsonBoolean(path)`** on `string`, `JsonElement`, and `HttpResponseMessage`. Asserts the value at `path` is a JSON boolean (either `true` or `false`). `JsonValueKind.True` and `.False` are distinct kinds, so this is the discoverable form of "this field is a bool, either value" that `HasJsonValueKind` alone cannot express.
- **`HasJsonValueMatching(path, Func<JsonElement, bool> predicate)`** on `string`, `JsonElement`, and `HttpResponseMessage`. Asserts the value at `path` satisfies a consumer-supplied predicate. Covers the ~¼ of value assertions that are not exact-equality (numeric inequalities, complex shape checks).
- **`HasJsonValueOneOf(path, string[])`** and **`HasJsonValueOneOf(path, double[])`** on `string`, `JsonElement`, and `HttpResponseMessage`. The discoverable form for "value is one of {Healthy, Degraded, Unhealthy}" or "code is one of {200, 503, 504}". Callers pass a C# 12 collection-expression literal: `HasJsonValueOneOf("status", ["Healthy", "Degraded"])`.
- **`HasJsonValueParsableAs<T>(path) where T : IParsable<T>`** on `string`, `JsonElement`, and `HttpResponseMessage`. Asserts the value at `path` is a JSON string whose text parses as `T` via `T.TryParse(value, CultureInfo.InvariantCulture, out _)`. Covers the "value parses as `Guid` / `DateTimeOffset` / `Uri`" pattern without committing to a particular parser per call site.
- **`JsonShape.IsNonEmptyString(JsonElement)`** and **`JsonShape.IsBoolean(JsonElement)`** framework-agnostic predicates in the `JsonAssertions` core, matching the family pattern (core predicate + TUnit-adapter assertion).
- **`JsonValueComparison.MatchesAny(JsonElement, string[])`** and **`JsonValueComparison.MatchesAny(JsonElement, double[])`** framework-agnostic comparison primitives in the `JsonAssertions` core.
- **`Directory.Build.targets` auto-extracts the per-version section from `CHANGELOG.md` at pack time** and feeds it into `<PackageReleaseNotes>`, so the Release Notes tab on the nuget.org package page shows the per-version body verbatim rather than a literal placeholder. Affects releases from this version onward; nupkgs already on nuget.org are immutable.
- **Prepended `View the rendered release notes: <url>` line** on the extracted body, pointing at the matching GitHub Release. nuget.org renders the Release Notes tab as plaintext with preserved line breaks rather than rendered markdown ([NuGet/NuGetGallery#8889](https://github.com/NuGet/NuGetGallery/issues/8889) is the open feature request); the prepended URL gives consumers a one-click route to the rendered-markdown version on GitHub.

### Changed

- **`JsonPath.Resolve` failure-point context for array failures.** An out-of-range index on an array, or an index access on a non-array, now surfaces in `FailedSegment` as `[N]` (matching the resolved-prefix syntax) and renders a tailored reason line in the failure message: `no element at index [N] on "items"` for an array out-of-range; `cannot index [N]: "user" is an Object, not an array` for an index access on a non-array.
- **`<PackageReleaseNotes>` fallback** in `JsonAssertions.TUnit.csproj` is now `$(RepositoryUrl)/releases/tag/v$(Version)` rather than the literal text "See CHANGELOG.md". The URL is auto-linked by nuget.org, so the no-match case still gives consumers a one-click route to the matching GitHub Release notes.
- **`CONVENTIONS.md` updated to v0.5.** Adds a `CHANGELOG conventions` section (Keep a Changelog 1.1.0 standard headers, user-facing-only content, header order, stylistic rules) and a `PackageReleaseNotes` auto-extract convention. Supersedes the v0.4 bump that added `JsonAssertions.TUnit` to the family roster; the v0.5 file remains copied identically across all five family repos.
- **`CODE_OF_CONDUCT.md` upgraded to Contributor Covenant v3.0** from v2.1. The maintainer contact link is now the GitHub profile URL (https://github.com/JohnVerheij) rather than a private email address, since GitHub keeps the primary email private.
- **`PackageValidationBaselineVersion` set to `0.1.0`.** ApiCompat now validates the additive v0.2.0 surface against the v0.1.0 baseline; `CompatibilitySuppressions.xml` is regenerated to capture the accepted additive differences from v0.1.0.
- **Package description** revised to drop the v0.0.1 / v0.1.0 sequencing narrative and describe the current shipped surface verbatim.

## [0.1.0] - 2026-05-14: Value-at-path and shape assertions, an HTTP entry point, and path-context diagnostics

The first feature release. 0.1.0 turns the 0.0.1 skeleton into a real assertion surface: value-at-path assertions, shape assertions (array length, non-empty / empty array, value kind), and a first-class `HttpResponseMessage` entry point. Every assertion's failure message is rebuilt to say *where* on the path resolution stopped. That path-context diagnostic is the load-bearing reason this is a package rather than a hand-rolled `TryGetProperty(...).IsTrue()` helper.

### Added (`JsonAssertions`, framework-agnostic core)

- **`JsonPath.Resolve(JsonElement, string)`** returns a `JsonPathResolution` that carries the resolved element on success, and the failure-point context on failure: how far the path resolved (`ResolvedPrefix`), which segment could not be resolved (`FailedSegment`), and the `JsonValueKind` of the element that blocked it (`ContainerKind`, which distinguishes "the object has no such property" from "the path tried to read a property off a non-object").
- **`JsonPathResolution`** readonly record struct carrying that outcome.
- **`JsonValueComparison.Matches`** overloads for `string`, `bool`, and `double` (numeric) expected values. A kind mismatch returns `false` rather than throwing, so a caller can render a "found a String, expected a Number" diagnostic. Named `JsonValueComparison` rather than `JsonValue` to avoid colliding with `System.Text.Json.Nodes.JsonValue`.
- **`JsonShape`** shape predicates: `IsKind`, `IsArrayOfLength`, `IsNonEmptyArray`, `IsEmptyArray`. Kind mismatches return `false` rather than throwing.

### Added (`JsonAssertions.TUnit`, TUnit adapter)

- **`HasJsonValue(path, expected)`** asserts the value at a dot-separated path. Overloads accept a `string`, a `bool`, or a number (`double`; `int` and `long` literals widen at the call site), over both a JSON `string` and a `JsonElement`.
- **Shape assertions** `HasJsonArrayLength(path, length)`, `HasNonEmptyJsonArray(path)`, `HasEmptyJsonArray(path)`, and `HasJsonValueKind(path, kind)`, over both a JSON `string` and a `JsonElement`. On failure the message shows the found shape (an array reports its length; any other kind reports its kind).
- **`HttpResponseMessage` entry point.** Every property / value / shape assertion is also available directly on an `HttpResponseMessage`: the response body is read as text and the assertion runs against it, so a test can write `await Assert.That(response).HasJsonProperty("user.name", ct)` without a separate read-and-parse step. The methods are asynchronous and take an optional `CancellationToken` (default `CancellationToken.None`) that flows to the body read; the body covers only the JSON payload (status-code assertions are out of scope).
- **Malformed JSON now fails the assertion** with an explained message ("the asserted value to be parseable JSON / but parsing failed: ...") rather than escaping as a raw `JsonException`. This applies to every JSON-`string` and `HttpResponseMessage` overload. A body that cannot be parsed does not vacuously satisfy `DoesNotHaveJsonProperty`.

### Changed

- **`HasJsonProperty` / `DoesNotHaveJsonProperty` now return `AssertionResult` instead of `bool`.** On failure they render a path-context block: `resolved as far as:` (the longest prefix that resolved) followed by a reason line. The generated TUnit chain extensions (`Assert.That(json).HasJsonProperty(path)`) are unaffected at the chain-syntax level. `JsonPath.Exists` (the `bool`-returning core shorthand) is unchanged.
- `PackageValidationBaselineVersion` set to `0.0.1` and `Proj0241` removed from `<NoWarn>`. The auto-generated `CompatibilitySuppressions.xml` captures the additive surface and the `bool` -> `AssertionResult` source-method return-type change as accepted differences from the 0.0.1 baseline.
- **TUnit dependency bumped `1.44.0` -> `1.44.39`.** The 1.44.39 release fixes the `[GenerateAssertion]` source generator emitting an invalid `= null` default for value-type optional parameters, which lets the `HttpResponseMessage` overloads take an optional `CancellationToken ct = default` in line with the family `CancellationToken`-plumbing convention.

### Notes

- Failure-message text is not part of the stable public surface; pin behaviour against the `JsonPath` / `JsonValueComparison` / `JsonShape` primitives, not against full message-text equality.
- The numeric `HasJsonValue` overload reads the element as a `double`; values beyond `double` precision are out of scope for this release.

## [0.0.1] - 2026-05-14: Initial preview, skeleton release establishing repository, package identifier, and quality bar

First public release. One package: `JsonAssertions.TUnit`, a TUnit-native JSON assertion library built on `System.Text.Json`. .NET 10, AOT-compatible, trimmable, no runtime reflection in the assertion path.

The 0.0.1 scope is intentionally narrow. The release exists to establish the repository, claim the `JsonAssertions.TUnit` package identifier on nuget.org, and lock the API style and quality bar before the wider catalog ships at 0.1.0. Consumers needing the full v0.1.0 surface can install 0.0.1 to lock the dependency relationship and watch the CHANGELOG.

### Added (`JsonAssertions`, framework-agnostic core)

- `JsonPath.Exists(JsonElement, string)`: navigates a dot-separated property path from a `JsonElement` and reports whether a property exists at it. A leading `$.` JSONPath-style root prefix is accepted and ignored. A path that traverses a non-object value resolves to "not found" rather than throwing. An empty path, a whitespace path, or a path with an empty segment throws `ArgumentException`.

### Added (`JsonAssertions.TUnit`, TUnit adapter)

- `HasJsonProperty(path)`: fluent entry point asserting a property exists at the dot-separated `path`, generated via TUnit's `[GenerateAssertion]`. Available on a JSON `string` and on a `System.Text.Json.JsonElement`.
- `DoesNotHaveJsonProperty(path)`: the negative form, asserting no property exists at the path. Available on the same two input types.

Both namespaces ship in the single `JsonAssertions.TUnit` assembly. The two-namespace split keeps the same consumer feel as the rest of the assertion family (a framework-agnostic core plus a TUnit adapter) and future-proofs a package split if the bare `JsonAssertions` identifier ever becomes available.

### Roadmap to v0.1.0

The wider surface lands at 0.1.0 as a reviewed pull request:

- Value-at-path assertions (assert the value at a path equals an expected value)
- Shape assertions (key set, array length, value kinds)
- `HttpResponseMessage` as a first-class entry point
- Failure messages that surface the resolved path context

Semantic JSON equality and subset / fragment matching are candidate work for v0.2.0.

### Quality bar (locked at 0.0.1)

- AOT-compatible (`IsAotCompatible=true`), trimmable (`IsTrimmable=true`), no runtime reflection in the assertion path.
- C# 14, `Nullable=enable`, `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`.
- Five Roslyn analyzer packs at full strength (Meziantou, SonarAnalyzer, Roslynator, Microsoft.VisualStudio.Threading, DotNetProjectFile.Analyzers).
- `Microsoft.CodeAnalysis.BannedApiAnalyzers` enforces no-reflection at build time.
- ApiCompat strict mode wired; `PackageValidationBaselineVersion` will pin to 0.0.1 starting from 0.0.2.
- 90% line / 90% branch coverage CI gates.
- Public API surface pinned via snapshot tests using `SnapshotAssertions.TUnit` plus `PublicApiGenerator`; cross-package dogfooding against the family.
- External-consumer smoke test (deliberately different namespace, deliberately different package-resolution path) plus AOT-publish gate on `linux-x64`.
- Trusted Publishing (OIDC) to nuget.org; no long-lived secrets.
- SLSA v1.0 build provenance plus CycloneDX 1.6 SBOM plus SPDX 3.0 SBOM plus OpenVEX v0.2.0 plus Sigstore-signed attestations on every release.
- Source Link, deterministic builds, embedded PDB.
- TUnit dependency pinned to **1.44.0**.

[Unreleased]: https://github.com/JohnVerheij/JsonAssertions.TUnit/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/JohnVerheij/JsonAssertions.TUnit/releases/tag/v0.2.0
[0.1.0]: https://github.com/JohnVerheij/JsonAssertions.TUnit/releases/tag/v0.1.0
[0.0.1]: https://github.com/JohnVerheij/JsonAssertions.TUnit/releases/tag/v0.0.1
