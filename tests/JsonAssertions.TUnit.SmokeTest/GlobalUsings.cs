// The smoke-test project deliberately uses <ImplicitUsings>disable</ImplicitUsings> so a
// failure to wire up these usings, or a future change that breaks the auto-discovery of
// JsonAssertions.TUnit's [GenerateAssertion]-emitted entry points, surfaces as a build
// failure here rather than silently passing in our own test project (which lives in the
// JsonAssertions.TUnit.Tests namespace and gets parent-namespace visibility for free).
//
// Note there is deliberately NO `global using JsonAssertions.TUnit;`: the fluent entry
// points auto-import via TUnit.Assertions.Extensions, and the smoke test exists to prove
// exactly that. A regression that hid them behind an explicit namespace would break this
// build.

global using System.Text.Json;                      // JsonDocument
global using System.Threading;                       // CancellationToken
global using System.Threading.Tasks;                 // Task
