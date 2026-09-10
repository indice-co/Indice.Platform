using System.Text.Json;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows.Verification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Default <see cref="ICustomerDataResolver"/>: calls the configured MCP tool
/// (<see cref="AgentsOptions.CustomerDataOptions.DataRetrievalTool"/>) with the external reference and reads
/// the JSON payload back off the tool result.
/// </summary>
public sealed class McpCustomerDataResolver : ICustomerDataResolver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AgentsOptions.CustomerDataOptions _options;
    private readonly ILogger<McpCustomerDataResolver> _logger;

    /// <summary>Creates a new <see cref="McpCustomerDataResolver"/>.</summary>
    public McpCustomerDataResolver(IServiceProvider serviceProvider, IOptions<AgentsOptions> options, ILogger<McpCustomerDataResolver> logger) {
        _serviceProvider = serviceProvider;
        _options = options.Value.CustomerData;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<CustomerDataRecord?> ResolveAsync(ExternalReference reference, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(reference);
        var result = await McpToolInvoker.CallAsync(_serviceProvider, _options.McpClientName, _options.DataRetrievalTool, new Dictionary<string, object?> {
            ["referenceId"] = reference.Id,
            ["referenceType"] = reference.Type
        }, cancellationToken);
        if (result is null || result.IsError == true) {
            _logger.LogWarning("MCP tool {Tool} returned no customer data for reference {ReferenceType}.", _options.DataRetrievalTool, reference.Type);
            return null;
        }
        var payload = McpToolInvoker.ReadJson(result);
        if (payload is not { ValueKind: JsonValueKind.Object or JsonValueKind.Array }) {
            return null;
        }
        return new CustomerDataRecord {
            Reference = reference,
            DataType = reference.Type,
            Data = payload.Value
        };
    }
}

/// <summary>
/// Default <see cref="IVerificationCodeService"/>: delegates delivery and validation of the one-time password
/// to the configured MCP tools.
/// </summary>
public sealed class McpVerificationCodeService : IVerificationCodeService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AgentsOptions.CustomerDataOptions _options;
    private readonly ILogger<McpVerificationCodeService> _logger;

    /// <summary>Creates a new <see cref="McpVerificationCodeService"/>.</summary>
    public McpVerificationCodeService(IServiceProvider serviceProvider, IOptions<AgentsOptions> options, ILogger<McpVerificationCodeService> logger) {
        _serviceProvider = serviceProvider;
        _options = options.Value.CustomerData;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> SendAsync(VerifiableField channel, string conversationId, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(channel);
        var result = await McpToolInvoker.CallAsync(_serviceProvider, _options.McpClientName, _options.OtpSendTool, new Dictionary<string, object?> {
            ["channel"] = channel.Kind.ToString().ToLowerInvariant(),
            ["recipient"] = channel.Value,
            ["conversationId"] = conversationId
        }, cancellationToken);
        if (result is null || result.IsError == true) {
            // The recipient is never logged: it is undisclosed customer data.
            _logger.LogWarning("MCP tool {Tool} failed to deliver a one-time password over {Channel}.", _options.OtpSendTool, channel.Kind);
            return false;
        }
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyAsync(VerifiableField channel, string code, string conversationId, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(channel);
        if (string.IsNullOrWhiteSpace(code)) {
            return false;
        }
        var result = await McpToolInvoker.CallAsync(_serviceProvider, _options.McpClientName, _options.OtpVerifyTool, new Dictionary<string, object?> {
            ["channel"] = channel.Kind.ToString().ToLowerInvariant(),
            ["recipient"] = channel.Value,
            ["code"] = code.Trim(),
            ["conversationId"] = conversationId
        }, cancellationToken);
        if (result is null || result.IsError == true) {
            return false;
        }
        // Tools answer either with a boolean-ish payload or with a { "verified": true } envelope.
        var payload = McpToolInvoker.ReadJson(result);
        return payload switch {
            { ValueKind: JsonValueKind.True } => true,
            { ValueKind: JsonValueKind.False } => false,
            { ValueKind: JsonValueKind.Object } element => element.TryGetProperty("verified", out var verified) && verified.ValueKind == JsonValueKind.True,
            _ => false
        };
    }
}

/// <summary>Shared plumbing for the MCP-backed customer data services.</summary>
internal static class McpToolInvoker
{
    /// <summary>Calls <paramref name="toolName"/> on the MCP client registered under <paramref name="clientName"/>.</summary>
    public static async Task<CallToolResult?> CallAsync(
        IServiceProvider serviceProvider, string clientName, string toolName,
        IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken) {
        var factory = serviceProvider.GetKeyedService<IMcpClientFactory>(clientName)
            ?? throw new InvalidOperationException($"No MCP client is registered under the name '{clientName}'. Call services.AddMcpClient(\"{clientName}\", ...) or configure Dex:CustomerData:McpClientName.");
        var client = await factory.CreateAsync(cancellationToken);
        return await client.CallToolAsync(toolName, arguments, cancellationToken: cancellationToken);
    }

    /// <summary>Reads the tool result as JSON — the structured content when present, otherwise the first text block.</summary>
    public static JsonElement? ReadJson(CallToolResult result) {
        if (result.StructuredContent is { } structured) {
            return structured.Deserialize<JsonElement>();
        }
        var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(text)) {
            return null;
        }
        try {
            using var document = JsonDocument.Parse(text);
            return document.RootElement.Clone();
        } catch (JsonException) {
            return null;
        }
    }
}
