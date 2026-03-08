using System.Text.Json;
using Microsoft.Extensions.Logging;
using InfiniFrame;

namespace OcsNet.Core.Bridge;

public sealed class MessageRouter
{
    private readonly ILogger<MessageRouter> _logger;
    private readonly Dictionary<string, Func<JsonElement?, IInfiniFrameWindow, string?, Task>> _handlers = new();

    public MessageRouter(ILogger<MessageRouter> logger)
    {
        _logger = logger;
    }

    public void Register(string action, Func<JsonElement?, IInfiniFrameWindow, string?, Task> handler)
    {
        _handlers[action] = handler;
    }

    public async void Handle(IInfiniFrameWindow window, string? rawMessage)
    {
        if (string.IsNullOrEmpty(rawMessage))
        {
            _logger.LogWarning("Received empty message");
            return;
        }

        AppRequest? request = null;
        try
        {
            request = JsonSerializer.Deserialize<AppRequest>(rawMessage);
            if (request is null)
            {
                _logger.LogWarning("Received null/unparseable message: {Raw}", rawMessage);
                return;
            }

            if (!_handlers.TryGetValue(request.Action, out var handler))
            {
                _logger.LogWarning("No handler registered for action: {Action}", request.Action);
                window.Send(AppResponse.Fail(request.Action, $"Unknown action: {request.Action}", request.RequestId));
                return;
            }

            await handler(request.Payload, window, request.RequestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling action: {Action}", request?.Action ?? "unknown");
            if (request is not null)
                window.Send(AppResponse.Fail(request.Action, ex.Message, request.RequestId));
        }
    }
}

public static class WindowExtensions
{
    private static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static void Send(this IInfiniFrameWindow window, AppResponse response)
    {
        var json = JsonSerializer.Serialize(response, _opts);
        window.SendWebMessage(json);
    }

    public static void SendEvent(this IInfiniFrameWindow window, string @event, object? data = null)
    {
        window.Send(AppResponse.Ok(@event, data));
    }
}
