using System.Text.Json;
using System.Text.Json.Serialization;

namespace OcsNet.Core.Bridge;

/// <summary>
/// Message sent from React frontend to .NET backend.
/// JS: window.external.sendMessage(JSON.stringify({ action, requestId, payload }))
/// </summary>
public sealed record AppRequest(
    [property: JsonPropertyName("action")]    string Action,
    [property: JsonPropertyName("requestId")] string? RequestId,
    [property: JsonPropertyName("payload")]   JsonElement? Payload
);

/// <summary>
/// Message sent from .NET backend to React frontend.
/// C#: window.SendWebMessage(JsonSerializer.Serialize(response))
/// </summary>
public sealed record AppResponse(
    [property: JsonPropertyName("event")]     string Event,
    [property: JsonPropertyName("requestId")] string? RequestId,
    [property: JsonPropertyName("data")]      object? Data,
    [property: JsonPropertyName("error")]     string? Error = null
)
{
    public static AppResponse Ok(string @event, object? data = null, string? requestId = null)
        => new(@event, requestId, data);

    public static AppResponse Fail(string @event, string error, string? requestId = null)
        => new(@event, requestId, null, error);
}
