# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.0.1] - Initial preview: skeleton release establishing repository, package identifier, and quality bar

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

[Unreleased]: https://github.com/JohnVerheij/JsonAssertions.TUnit/compare/v0.0.1...HEAD
[0.0.1]: https://github.com/JohnVerheij/JsonAssertions.TUnit/releases/tag/v0.0.1
