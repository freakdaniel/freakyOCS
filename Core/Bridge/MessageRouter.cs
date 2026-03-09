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

    public void Handle(IInfiniFrameWindow window, string? rawMessage)
    {
        if (string.IsNullOrEmpty(rawMessage))
        {
            _logger.LogWarning("Received empty message");
            return;
        }

        AppRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<AppRequest>(rawMessage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize message: {Raw}", rawMessage);
            return;
        }

        if (request is null)
        {
            _logger.LogWarning("Received null/unparseable message: {Raw}", rawMessage);
            return;
        }

        if (!_handlers.TryGetValue(request.Action, out var handler))
        {
            _logger.LogWarning("No handler registered for action: {Action}", request.Action);
            // Fire-and-forget error response from thread pool — keeps GTK thread free
            _ = Task.Run(async () =>
                await window.SendAsync(AppResponse.Fail(request.Action, $"Unknown action: {request.Action}", request.RequestId)));
            return;
        }

        // Run handler on a thread-pool thread so the GTK main thread is freed
        // immediately.  SendWebMessageAsync marshals back to GTK via Invoke, so
        // all replies still arrive on the correct thread.
        _ = Task.Run(async () =>
        {
            try
            {
                await handler(request.Payload, window, request.RequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling action: {Action}", request.Action);
                try { await window.SendAsync(AppResponse.Fail(request.Action, ex.Message, request.RequestId)); }
                catch { /* best effort */ }
            }
        });
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

    /// <summary>
    /// Thread-safe send: uses SendWebMessageAsync which dispatches to the GTK/WebKit
    /// main thread on Linux, avoiding silent failures when called from a thread-pool
    /// continuation after an async/await.
    /// </summary>
    public static async Task SendAsync(this IInfiniFrameWindow window, AppResponse response)
    {
        var json = JsonSerializer.Serialize(response, _opts);
        await window.SendWebMessageAsync(json);
    }

    public static async Task SendEventAsync(this IInfiniFrameWindow window, string @event, object? data = null)
    {
        await window.SendAsync(AppResponse.Ok(@event, data));
    }
}
