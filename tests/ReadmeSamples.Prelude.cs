// What the README samples use without declaring it themselves: the placeholders of a test (the response, the body,
// a token, a client) and stand-ins for the consumer's own types. Compiled by CI together with every C# sample of the
// READMEs (readme_samples.py of assertions-ci); never part of a package.
using System.Text.Json.Serialization;

namespace ReadmeSamples;

/// <summary>A user of the consumer's API.</summary>
/// <param name="Id">The user ID.</param>
/// <param name="Name">The user name.</param>
/// <param name="Active">Whether the user is active.</param>
public sealed record UserDto(int Id, string Name, bool Active);

/// <summary>An order of the consumer's API.</summary>
/// <param name="Id">The order ID.</param>
/// <param name="Customer">The customer name.</param>
/// <param name="Total">The order total.</param>
public sealed record OrderDto(int Id, string Customer, decimal Total);

/// <summary>The body of the consumer's health endpoint.</summary>
/// <param name="Status">The overall status.</param>
public sealed record HealthCheckResponse(string Status);

/// <summary>The consumer's source-generated serializer context, registering the DTOs above.</summary>
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(OrderDto))]
[JsonSerializable(typeof(HealthCheckResponse))]
public partial class MyJsonContext : JsonSerializerContext
{
}

/// <summary>The members every README sample can use.</summary>
public partial class Prelude
{
    /// <summary>The response of the request under test.</summary>
    protected HttpResponseMessage response = new();

    /// <summary>The token of the test.</summary>
    protected CancellationToken ct;

    /// <summary>A JSON document as text.</summary>
    protected string json = "{}";

    /// <summary>A response body as text.</summary>
    protected string body = "{}";

    /// <summary>A request body.</summary>
    protected object payload = new();

    /// <summary>The client of the host under test.</summary>
    protected HttpClient _httpClient = new();

    /// <summary>A second name for the client of the host under test.</summary>
    protected HttpClient _client = new();
}
