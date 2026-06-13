# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.5.0] - 2026-06-13: structural JSON equivalence

Minor release. Adds whole-document structural equivalence assertions. Purely additive.

### Added

- **`Assert.That(actual).IsEquivalentJsonTo(expected)`** asserts that two JSON documents carry the same values at the same paths. Equivalence ignores object-property order and the lexical form of numbers, so `1`, `1.0`, and `1e0` are equal. Both a JSON `string` and a `JsonElement` are accepted as the actual value; the expected document is a JSON `string`. A configure overload sets options: `IgnorePath("items[*].timestamp")` excludes a path from the comparison (the same path grammar as the other assertions, including the `[*]` wildcard), and `IgnoreArrayOrder()` compares arrays as multisets rather than position by position. On failure the message names the first diverging path, the category of difference, and a rendered view of each side, with container values shown as truncated raw JSON.
- **`JsonEquivalence`** (framework-agnostic core) exposes the comparison directly: `Compare(expected, actual, options)` over JSON strings or `JsonElement`s returns the first `JsonDifference`, or `null` when the documents are equivalent. `JsonEquivalenceOptions`, `JsonDifference`, and `JsonDifferenceKind` are public so the engine is usable outside the assertion path.

## [0.4.2] - 2026-06-05: documentation refresh

Documentation and release-tooling release. No API or behavior change.

### Changed

- Refreshed the README (plain-ASCII punctuation) and rewrote the shared `CONVENTIONS.md`: removed the version-history preamble so it reads as a conventions document, not a changelog.
- Documented the JSON path syntax (dot-separated with `[n]` indices plus the `[*]` wildcard) and why it is a navigable subset rather than JSONPath/JMESPath.
- The release workflow now publishes the matching `CHANGELOG.md` section as the GitHub release body (`body_path`), so release notes carry the full hand-written detail instead of GitHub's auto-generated commit summary.

## [0.4.1] - 2026-06-04

Documentation patch. No code, public API, or behavior change.

### Changed

- `[*]` wildcard path syntax: documented the **empty-array footgun** in the root and packed READMEs. `[*].id` passes vacuously on an empty array (correct "for all" semantics) while `[0].id` fails on one, so a naive `[0]` to `[*]` migration silently drops the implicit non-emptiness check; the notes tell readers to pair `[*]` with `HasNonEmptyJsonArray(...)` when emptiness should fail the test.
- `[*]` wildcard path syntax: documented the **index-scoped caveat** in the root and packed READMEs. Wildcards fit existence and genuinely-uniform value checks only; an element-specific value check (for example `HasJsonValue("items[2].id", 2)`) must stay index-scoped rather than become `[*]`.

## [0.4.0] - 2026-06-03: `[*]` wildcard array paths, structural JSON canonicalizer

Feature release. Adds the `[*]` wildcard path segment so array-element assertions check every element (`HasJsonProperty("[*].id")`, `HasJsonValueMatching("[*].isStarted", ...)`) rather than only index `[0]`, turning a weak first-element check into an all-element check. Also adds `JsonCanonicalizer.Canonicalize`, a typeless structural canonicalizer (sorted keys, stable indent, all fields preserved so new fields surface) with JSON-path scrubbing of volatile values, for composing JSON snapshots with a sibling snapshot package's normalizer hook. Also folds in the accumulated CI hardening, the Renovate migration, and the CONVENTIONS v0.7 sync from the unreleased line.

### Added

- **`[*]` wildcard path segment** on `HasJsonProperty` and `HasJsonValueMatching` (backed by the new `JsonPath.ResolveAll` core): `[*]` matches every element of the array it targets, so `HasJsonProperty("[*].id")` requires every element to carry `id`, and `HasJsonValueMatching("[*].isStarted", v => ...)` runs the predicate on each. Nested and multiple wildcards compose (`cycles[*].cycleId`, `[*].tags[*]`). An empty array passes vacuously (a "for all" over an empty set; pair with a non-empty-array assertion when emptiness must fail); a failure names which element failed by its concrete index. `JsonPath.ContainsWildcard(path)` reports whether a path uses the wildcard.
- **`JsonCanonicalizer.Canonicalize(string json, Action<JsonCanonicalizeOptions> configure)`** produces a deterministic structural canonical form: object keys sorted ordinally, two-space indentation, LF line endings, relaxed escaping, and every value preserved (so an added or removed field surfaces as a text diff). `JsonCanonicalizeOptions.ScrubPath(path)` replaces the value at a JSON path (wildcards supported) with a stable token (default `<scrubbed>`, overridable via `WithScrubToken`). Unlike the typed `JsonRenderers.ReformatJson<T>`, this needs no `JsonSerializerContext` and keeps unknown properties, which is what makes it suitable for pinning a whole response shape as a snapshot baseline. Composes with a snapshot library's normalizer hook at the consumer's call site, so neither package depends on the other.
- **`int` / `uint` / `long` / `ulong` overloads of `HasJsonValue` and `HasJsonValueOneOf`** on a JSON `string`, a `JsonElement`, and an `HttpResponseMessage`. Each integer overload matches the value whether the JSON encodes it as a *number* or as a numeric *string* (parsed with `CultureInfo.InvariantCulture`), because the encoding depends on the serializer: System.Text.Json writes integers as JSON numbers, while Protobuf's `JsonFormatter` can emit them as JSON strings and serializes `int64` / `uint64` as strings to avoid the 53-bit precision loss a JSON number would incur. A consumer whose payload carries 64-bit fields (for example a 128-bit id split into `high` / `low` halves) as JSON strings could not match them with the `double` overload and had to fall back to the `string` overload; the typed overloads remove that trap by accepting both encodings. A bare `int` literal binds to the `int` overload, so `HasJsonValue("user.age", 30)` matches the JSON number exactly; use the `L` / `UL` suffix (`HasJsonValue("guid.high", 123456789012345L)`) for 64-bit values, and a collection-expression literal for the one-of form (`HasJsonValueOneOf("message.sequence", [100L, 200L])`). A passing value is an exact integer in range; a fractional number, an out-of-range number, or a non-numeric string fails. The `double` overload is unchanged and matches a JSON number only.
- **`JsonValueComparison.Matches` / `MatchesAny` primitives for `int`, `uint`, `long`, and `ulong`** in the `JsonAssertions` core. Each reads the element as an exact integer from either a JSON number (via `TryGetInt32` / `TryGetUInt32` / `TryGetInt64` / `TryGetUInt64`, so a fractional or out-of-range number returns `false`) or a numeric JSON string parsed with `CultureInfo.InvariantCulture`. A kind or parse mismatch returns `false` rather than throwing, matching the existing `Matches` / `MatchesAny` contract.

### Changed

- Removed `paths-ignore` from `.github/workflows/ci.yml` so the `Build, test & pack` required check always reports a status. Without the fix, docs-only PRs stuck in `Expected - Waiting for status to be reported` and could not satisfy branch protection.
- Dropped drift-prone own-version anchors from the root README and packed adapter README: `## Status: v0.3.0` is now `## Status`. Historical "added in vX.Y" prose markers are unchanged. The CHANGELOG remains the single source of truth for what shipped when.
- Migrated CI dependency automation from Dependabot to Renovate (`.github/renovate.json`), matching `SseAssertions.TUnit` and `TimeAssertions.TUnit`. Daily schedule (before 4am Europe/Amsterdam), `customManagers` keep TUnit version literals in the root README, package README, smoketest csproj, and bug-report Issue Form in lockstep with the central `Directory.Packages.props` pin. `platformAutomerge` replaces the separate `dependabot-auto-merge.yml` workflow. Dependency dashboard issue enabled. Explicit semantic commit scopes: `deps(nuget)`, `ci(github-actions)`, `ci(dotnet-sdk)`. Auto-merge covers `digest`, `pin`, `pinDigest`, and `lockFileMaintenance` updateTypes alongside `minor` and `patch`. The three TUnit packages (`TUnit`, `TUnit.Assertions`, `TUnit.Core`) are grouped into a single PR per release.
- Replaced the single `## Family` section in the root README with `## Family compatibility` (release / ApiCompat prose + per-package CHANGELOG cross-links) and `## Pair with` (sibling package descriptions, excluding self), matching the structure already used by the five sibling family-repo READMEs. TOC updated alongside.
- Updated `CONVENTIONS.md` to v0.7 (from v0.6).
- Added `SseAssertions.TUnit` (the sixth family package, Server-Sent Events wire-format assertions) to the `CONVENTIONS.md` family roster.
- Added a per-package strict-scope policy section to `CONVENTIONS.md` with explicit scope statements for all six packages.
- Added a core+adapter packaging rule section to `CONVENTIONS.md`: five of six family packages ship core+adapter; this repo is the sole single-package member.
- Synchronized `CONVENTIONS.md` across all six family repos (the file is copied identically).
- Expanded the `README.md` Family section to six packages, adding `SseAssertions.TUnit` and updating the "shared across" line in Contributing.
- Added GitHub Actions workflow security scanning. `.github/workflows/zizmor.yml` runs `zizmor` (blocking, with findings shown as inline annotations) on every workflow change; `.github/workflows/codeql.yml` now analyzes the `actions` language alongside `csharp`; `.github/workflows/scorecard.yml` (OpenSSF Scorecard) and `.github/workflows/dependency-review.yml` (fails a PR that adds a high-severity-vulnerable dependency) are new. Added the Renovate `helpers:pinGitHubActionDigestsToSemver` preset so any newly-introduced action is auto-pinned to a commit SHA. CI-only; no effect on shipped packages.

### Security

- Hardened GitHub Actions token handling: set `persist-credentials: false` on every `actions/checkout` so the repository token is not written into `.git/config`; moved the inline coverage-report expression in `ci.yml` into an `env:` variable to remove a template-injection vector; and scoped workflow write permissions (`security-events` on `codeql`; `contents`/`id-token`/`packages`/`attestations` on `release`) to the job level with a read-only workflow-level default. CI-only; no released package is affected.

## [0.3.0] - 2026-05-17: AOT-context regression assertions, HTTP-response JSON and RFC 7807 ProblemDetails, a canonicalizing JSON renderer for snapshot composition, plus a public failure-message extension point

### Added

- Added **`RoundtripsCleanlyVia<T>(this T value, JsonTypeInfo<T> jsonTypeInfo)`** as an extension method on any value `T`, asserting the value serializes via the supplied `JsonTypeInfo<T>`, deserializes back, and re-serializes to a byte-identical JSON string. Catches the common "added a property and forgot to update the `JsonSerializerContext`" regression class at CI time. AOT-clean; failure messages render both serialized strings side-by-side for diagnosis.
- Added **`HasJsonTypeInfoFor<T>()` and the `AsJsonContext()` bridge** on `IAssertionSource<TContext>` where `TContext : JsonSerializerContext`. The leaf assertion asserts the underlying context registers a `JsonTypeInfo<T>` for `T`, catching the "added a domain type but forgot the `[JsonSerializable(typeof(NewType))]`" regression class. The educational-demand AOT-moat companion to `RoundtripsCleanlyVia`: where that assertion verifies a value round-trips cleanly through a typed context, `HasJsonTypeInfoFor` verifies the context knows about the type at all. The bridge extension `AsJsonContext()` produces an `IJsonContextAssertionSource` with the context viewed at the `JsonSerializerContext` base, keeping the call site to a single explicit type argument despite the receiver's concrete subtype (`await Assert.That(MyContext.Default).AsJsonContext().HasJsonTypeInfoFor<MyDto>()`). The pattern works around the C# partial-generic-inference limit via an internal upcast adapter; AOT-clean (one O(1) lookup against the context's type registry; no reflection).
- Added **`HasJsonResponse<T>(HttpStatusCode, JsonTypeInfo<T>, T expected, CancellationToken)`** on `HttpResponseMessage`, combining HTTP status + AOT-clean deserialization + structural-equality in one chain. Collapses the common `response.EnsureSuccessStatusCode() + body-read + Deserialize + AreEqual` 4-6-line pattern into a single fluent call. The supplied `JsonTypeInfo<T>` is the source-generated entry from the consumer's `JsonSerializerContext`; no runtime reflection. Failure messages include the response body (truncated at 256 chars) so the diagnostic surfaces the structured-error shape for non-200 responses and the actual JSON shape for deserialization failures. Status-only and predicate overloads deferred pending consumer demand.
- Added **`MatchesProblemDetails(int status, string? title, string? detail, string? type, string? instance, CancellationToken)`** on `HttpResponseMessage`, asserting the response is a valid RFC 7807 ProblemDetails (Content-Type `application/problem+json`, deserializable shape) and that each specified field matches. Unspecified fields skip (pass `null`). Deserializes via an internal `ProblemDetailsMirror` so the production package stays MIT-clean (no `Microsoft.AspNetCore.Mvc.Abstractions` Apache 2.0 dep) and AOT-clean via STJ source-gen. Mismatched fields report in a single failure message with expected-vs-got pairs. RFC 7807 §3.2 extension members are captured by the mirror's `[JsonExtensionData]` dictionary so they survive deserialization; a future `WithExtension(name, value)` chain method may expose the assertion surface.
- Added **`MatchesValidationProblemDetails(int status, IReadOnlyDictionary<string, string[]> errors, string? title, string? detail, string? type, string? instance, CancellationToken)`** on `HttpResponseMessage`. Same shape as `MatchesProblemDetails` plus the ASP.NET Core ValidationProblemDetails `errors` dictionary mapping field names to validation messages. Every key in the supplied `errors` dictionary must appear in the response with matching values.
- Added **`JsonRenderers.ReformatJson<T>(JsonTypeInfo<T>)`** in the `JsonAssertions` core namespace as a static factory returning `Func<string, string>` that canonicalizes a JSON string via the consumer's `JsonSerializerContext` (deserialize then re-serialize through the supplied `JsonTypeInfo<T>`). Composes with `SnapshotAssertions.TUnit`'s `MatchesSnapshot(Func<>)` overload at the consumer's call site without coupling the packages (per CONVENTIONS.md v0.6 cross-package references rule). Two-step composition for HTTP responses: async read body in test, then sync canonicalize + snapshot. AOT-clean by construction.
- Promoted **`JsonFailureMessage` static class** in the `JsonAssertions` core namespace from `internal` to `public`. Exposes a curated subset of failure-message factory methods as the v0.3.0+ extension point for consumer-authored typed JSON assertions: `ParseFailure(JsonException)`, `PropertyNotFound(string path, JsonPathResolution)`, `PropertyShouldNotExist(string path, JsonPathResolution)`, `ValueMismatch(string path, JsonPathResolution, string expectedDescription)`, `ShapeMismatch(string path, JsonPathResolution, string expectedDescription)`. Consumers writing their own typed assertions can compose these factories to produce failure messages that match the package's diagnostic style. Mirrors the `MathAssertions.MathFailureMessage` pattern in the sibling package. HTTP-response, ProblemDetails, and roundtrip-specific factories remain `internal` — context-bound to their specific assertions, no extension value.

### Changed

- Updated **`CONVENTIONS.md` to v0.6**, adding the family-wide **Cross-package references rule** (no sibling family package may appear as a `PackageReference` in another sibling's production `.csproj`; composition happens at the consumer's call site via standard delegates) and the **Naming invariant** (no sibling-package-name prefix — `Snapshot*`, `Log*`, `Math*`, `Time*`, `Json*` — may appear in another sibling's public API surface, including typenames, method names, and extension method names). Both invariants are pack-time-enforced from v0.3.0 onward; `JsonAssertions.TUnit` ships the enforcement infrastructure (NuGet dependency-list scan + PublicAPI prefix scan) in this version. The 4 sibling repos adopt the same `CONVENTIONS.md` v0.6 in separate PRs after v0.3.0 merges.
- Set **`PackageValidationBaselineVersion`** to `0.2.0`. ApiCompat now validates the additive v0.3.0 surface against the v0.2.0 baseline; `CompatibilitySuppressions.xml` is regenerated to capture the accepted additive differences (the new HTTP-response, AOT-context, renderer, and bridge surfaces, plus `JsonFailureMessage`'s internal-to-public promotion).
- Changed **`RoundtripsCleanlyVia<T>`** to accept `null` payloads. A null value legitimately survives the round-trip through STJ (`null` → `"null"` → `null`); the previous early-rejection branch was removed. A non-null value that deserializes back to null (serializer corruption) still fails via the dedicated diagnostic.

### Fixed

- Fixed **`MatchesProblemDetails`** and **`MatchesValidationProblemDetails`** content-type comparison to be case-insensitive (RFC 9110 §8.3.2 media-type tokens). A response with `Application/Problem+Json` or any other case variant now correctly satisfies the RFC 7807 content-type check, where the previous ordinal comparison required exact `application/problem+json`.

## [0.2.0] - 2026-05-15: Array-indexed paths, root-self, boolean / non-empty-string / matching / one-of / parsable-as<T> assertions, plus the pack-time Release Notes pipeline

### Added

- **Array-indexed path segments.** `JsonPath.Resolve(element, "items[0].name")` navigates `items` (object property) then index 0 (array element) then `name` (object property). Indices are zero-based, non-negative integers in `[N]` brackets. Mixed property + index segments compose freely (`objects[0].entries[1].id`). Closes the #1 friction surfaced by the v0.1.0 adoption survey.
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

- Failure-message text is not part of the stable public surface; pin behavior against the `JsonPath` / `JsonValueComparison` / `JsonShape` primitives, not against full message-text equality.
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

[unreleased]: https://github.com/JohnVerheij/JsonAssertions.TUnit/compare/v0.5.0...HEAD
[0.5.0]: https://github.com/JohnVerheij/JsonAssertions.TUnit/compare/v0.4.2...v0.5.0
[0.4.2]: https://github.com/JohnVerheij/JsonAssertions.TUnit/compare/v0.4.1...v0.4.2
[0.4.1]: https://github.com/JohnVerheij/JsonAssertions.TUnit/compare/v0.4.0...v0.4.1
[0.4.0]: https://github.com/JohnVerheij/JsonAssertions.TUnit/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/JohnVerheij/JsonAssertions.TUnit/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/JohnVerheij/JsonAssertions.TUnit/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/JohnVerheij/JsonAssertions.TUnit/compare/v0.0.1...v0.1.0
[0.0.1]: https://github.com/JohnVerheij/JsonAssertions.TUnit/releases/tag/v0.0.1
