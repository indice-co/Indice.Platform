using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Providers;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Services.Risk;
using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.Steps.Operator;
using Indice.Features.Agents.Core.Workflows.Steps.Risk;
using Microsoft.Agents.AI.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Tests;

/// <summary>Everything a risk investigation test needs: the real workflow graph and checkpoints, with the model, the MCP server and the knowledge base scripted.</summary>
internal sealed class RiskHarness : IAsyncDisposable
{
    public const string Role = "RiskInvestigator";
    public const string KnownTun = "9001234567";
    public const string UserId = "u-bo-1";
    public static readonly DateTimeOffset Now = new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);
    public static readonly Guid AnchorEventId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly ServiceProvider _provider;

    public Guid ConversationId { get; } = Guid.NewGuid();
    public ScriptedModel Model { get; } = new();
    public ScriptedQueries Queries { get; } = new();
    public ScriptedKnowledge Knowledge { get; } = new();
    public IConversationFileStore Files { get; }
    public IChatClient Client { get; }

    private RiskHarness(ClaimsPrincipal? user, Action<RiskInvestigationOptions>? configure) {
        var options = new RiskInvestigationOptions { RequiredRoles = [Role] };
        configure?.Invoke(options);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AgentsDbContext>(builder => builder.UseInMemoryDatabase(Guid.NewGuid().ToString()), ServiceLifetime.Singleton);
        services.AddSingleton<PersistedCheckpointStore>();
        services.AddTransient(sp => CheckpointManager.CreateJson(sp.GetRequiredService<PersistedCheckpointStore>()));
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(Options.Create(new ModelsOptions()));
        services.AddSingleton<IPromptTemplateRenderer, TemplateNames>();
        services.AddSingleton<AgentsClaimsPrincipalSelector>(() => user);
        services.AddSingleton<TimeProvider>(new FixedTime(Now));
        services.AddSingleton(new AgentInfoRegistry([
            new AgentInfo(AgentsConstants.AgentNames.Risk, "risk", ["text/plain"], ["text/markdown"]) { RequiredRoles = [Role] }
        ]));
        services.AddKeyedSingleton<IChatClient>(nameof(AgentsModelRole.Reasoning), Model);
        services.AddSingleton<IDocumentsService>(Knowledge);
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>, ZeroEmbeddings>();
        services.AddSingleton<IRiskQueryClient>(Queries);
        services.AddTransient<IConversationFileStore, ConversationFileStore>();
        services.AddTransient<RiskRequestAnalyzer>();
        services.AddTransient<RiskUnsupportedResponder>();
        services.AddTransient<TransactionAnchorResolver>();
        services.AddTransient<EvidenceCollector>();
        services.AddTransient<RiskReportComposer>();
        services.AddTransient<UserInputCollector>();
        services.AddKeyedTransient(AgentsConstants.AgentNames.Risk, (sp, key) => RiskInvestigationFeatureExtensions.BuildWorkflow(sp));
        services.AddTransient<IChatClient, DrivingClient>();
        _provider = services.BuildServiceProvider();
        Files = _provider.GetRequiredService<IConversationFileStore>();
        Client = _provider.GetRequiredService<IChatClient>();
    }

    public static RiskHarness Create(ClaimsPrincipal? user = null, Action<RiskInvestigationOptions>? configure = null) => new(user ?? Investigator(), configure);

    public static ClaimsPrincipal Investigator(string role = Role) => new(new ClaimsIdentity(
        [new Claim("sub", UserId), new Claim("name", "Maria Papadaki"), new Claim("role", role)], authenticationType: "test"));

    private ChatOptions TurnOptions => new() { Instructions = AgentsConstants.AgentNames.Risk, ConversationId = ConversationId.ToString() };

    /// <summary>A new user turn.</summary>
    public Task<ChatResponse> SendAsync(string text) {
        var message = new ChatMessage(ChatRole.User, text) {
            AdditionalProperties = new AdditionalPropertiesDictionary { [nameof(ConversationId)] = ConversationId.ToString() }
        };
        return Client.GetResponseAsync([message], TurnOptions, TestContext.Current.CancellationToken);
    }

    /// <summary>The user's answer to a question the agent asked through the input port.</summary>
    public Task<ChatResponse> ReplyAsync(ChatResponse previous, string answer) {
        var call = AskedFor(previous) ?? throw new InvalidOperationException("The agent did not ask for anything.");
        var message = new ChatMessage(ChatRole.User, [new TextContent(answer), new FunctionResultContent(call.CallId, new UserInputRequestPort.UserResponse(answer))]);
        return Client.GetResponseAsync([message], TurnOptions, TestContext.Current.CancellationToken);
    }

    public static FunctionCallContent? AskedFor(ChatResponse response) =>
        response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>().FirstOrDefault();

    public static IReadOnlyList<FileReference> FilesIn(ChatResponse response) =>
        [.. response.Messages.SelectMany(m => m.Contents).OfType<DataContent>()
            .Where(d => d.MediaType == AgentsConstants.MediaTypes.File)
            .Select(d => JsonSerializer.Deserialize<FileReference>(d.Data.Span)!)];

    public async Task<string> ReadFileAsync(FileReference file) {
        var stored = await Files.GetAsync(ConversationId, file.Id, UserId, TestContext.Current.CancellationToken);
        return System.Text.Encoding.UTF8.GetString(stored!.Data);
    }

    public ValueTask DisposeAsync() => _provider.DisposeAsync();

    // ---- doubles ----------------------------------------------------------------------------------------------------------

    /// <summary>Drives the workflow like <c>AgentsChatClient</c> does, without the router.</summary>
    private sealed class DrivingClient(IServiceProvider services) : IChatClient
    {
        private readonly WorkflowChatClient _inner = new(services);
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => _inner.GetResponseAsync(messages, options, cancellationToken);
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => _inner.GetStreamingResponseAsync(messages, options, cancellationToken);
        public object? GetService(Type serviceType, object? serviceKey = null) => _inner.GetService(serviceType, serviceKey);
        public void Dispose() { }
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    /// <summary>Renders every template as its own name, so the scripted model can tell the analyzer's call from the narrative's.</summary>
    private sealed class TemplateNames : IPromptTemplateRenderer
    {
        public string Render(string templateName, object? values = null) => $"[{templateName}]";
    }

    private sealed class ZeroEmbeddings : IEmbeddingGenerator<string, Embedding<float>>
    {
        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(values.Select(_ => new Embedding<float>(new float[] { 0f, 0f, 0f })).ToList()));
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }
}

/// <summary>The reasoning model: extracts the request, and writes the findings summary.</summary>
internal sealed class ScriptedModel : IChatClient
{
    /// <summary>What the model "extracts" from the user's words.</summary>
    public Func<string, RiskRequestAnalysis> Analyze { get; set; } = _ => new RiskRequestAnalysis("P-02", RiskHarness.KnownTun, "2026-11-12", "en", "court filing");

    /// <summary>The findings summary the model "writes".</summary>
    public string Narrative { get; set; } = "The customer logged in with a password and a second factor [E-8]. The transfer was recorded [E-2].";

    public bool FailNarrative { get; set; }
    public List<string> AnalyzerInputs { get; } = [];
    public string? NarrativeInput { get; private set; }
    public string? NarrativeInstructions { get; private set; }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) {
        var text = messages.Last(m => m.Role == ChatRole.User).Text;
        AnalyzerInputs.Add(text);
        var analysis = Analyze(text);
        var json = JsonSerializer.Serialize(new { playbookId = analysis.PlaybookId, tun = analysis.Tun, transactionDate = analysis.TransactionDate, language = analysis.Language, purpose = analysis.Purpose });
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, json)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        NarrativeInput = messages.Last(m => m.Role == ChatRole.User).Text;
        NarrativeInstructions = options?.Instructions;
        if (FailNarrative) {
            throw new InvalidOperationException("The model is down.");
        }
        var half = Narrative.Length / 2;
        yield return new ChatResponseUpdate(ChatRole.Assistant, Narrative[..half]);
        await Task.Yield();
        yield return new ChatResponseUpdate(ChatRole.Assistant, Narrative[half..]);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}

/// <summary>The risk MCP server: answers each catalog query from a script, and remembers what was asked.</summary>
internal sealed class ScriptedQueries : IRiskQueryClient
{
    public List<(string QueryId, IReadOnlyDictionary<string, object?> Parameters)> Calls { get; } = [];
    public Dictionary<string, Func<IReadOnlyDictionary<string, object?>, RiskQueryResult>> Handlers { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string? CorrelationId { get; private set; }
    public int Sessions { get; private set; }

    public IEnumerable<string> QueryIds => Calls.Select(call => call.QueryId);

    public Task<IRiskQuerySession> OpenSessionAsync(string correlationId, CancellationToken cancellationToken = default) {
        CorrelationId = correlationId;
        Sessions++;
        return Task.FromResult<IRiskQuerySession>(new Session(this));
    }

    public static Dictionary<string, JsonElement> Row(object values) =>
        JsonSerializer.SerializeToElement(values).EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());

    public static RiskQueryResult Result(string queryId, params object[] rows) => new() {
        QueryId = queryId,
        Database = queryId.StartsWith("Q-RISK") ? "RiskDb" : queryId.StartsWith("Q-SIGN") ? "SignInLogsDb" : queryId.StartsWith("Q-ACT") ? "ActivityLogsDb" : "IdentityDb",
        ExecutedAt = RiskHarness.Now,
        SqlSha256 = new string('a', 64),
        RowCount = rows.Length,
        Rows = [.. rows.Select(Row)],
        DurationMs = 5
    };

    public static RiskQueryResult Failure(string queryId, string error) => new() { QueryId = queryId, Error = error };

    private sealed class Session(ScriptedQueries owner) : IRiskQuerySession
    {
        public Task<RiskQueryResult> RunAsync(string queryId, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken = default) {
            owner.Calls.Add((queryId, parameters));
            var result = owner.Handlers.TryGetValue(queryId, out var handler) ? handler(parameters) : Result(queryId);
            if (result.Parameters.Count == 0) {
                result.Parameters = parameters.ToDictionary(p => p.Key, p => JsonSerializer.SerializeToElement(p.Value));
            }
            return Task.FromResult(result);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>A complete, realistic set of answers for a transfer made from a registered device with a second factor.</summary>
    public void ScriptTransfer(Action<Dictionary<string, object>>? tweakAnchor = null) {
        var anchor = new Dictionary<string, object> {
            ["Id"] = RiskHarness.AnchorEventId.ToString(),
            ["CreatedAt"] = "2026-11-12T09:30:15.0000000Z",
            ["SubjectId"] = "subject-1",
            ["SessionId"] = "pwd.8f1c",
            ["Name"] = "TransferRequest",
            ["Type"] = "Transfer",
            ["Amount"] = 1500.00,
            ["IpAddress"] = "203.0.113.9",
            ["SourceId"] = "ebanking-api",
            ["SourceTransId"] = RiskHarness.KnownTun,
            ["Location"] = "Athens, Attica, Greece, Europe",
            ["CountryIsoCode"] = "GR",
            ["Data"] = """
                {"Request":{"CreditorIban":"GR1601101250000000012300695","Amount":1500,"OtpVerificationCode":"482913","FactorToken":"abc.def"},
                 "Balance":9000,"DebitProductResourceId":"acc-77","IsProductOriginationOwner":false,"tokenDeviceId":"dev-1",
                 "User":{"UserName":"kpapadopoulos","Claims":[{"Type":"tin","Value":"123456789"}]}}
                """
        };
        tweakAnchor?.Invoke(anchor);

        Handlers["Q-RISK-07"] = p => Equals(p["Tun"], RiskHarness.KnownTun) ? Result("Q-RISK-07", new { Id = anchor["Id"], CreatedAt = anchor["CreatedAt"], SubjectId = anchor["SubjectId"], SessionId = anchor["SessionId"], Name = anchor["Name"], Type = anchor["Type"], Amount = anchor["Amount"], IpAddress = anchor["IpAddress"] }) : Result("Q-RISK-07");
        Handlers["Q-RISK-01"] = _ => Result("Q-RISK-01", anchor);
        Handlers["Q-RISK-06"] = _ => Result("Q-RISK-06",
            new { Id = "22222222-2222-2222-2222-222222222222", CreatedAt = "2026-11-12T09:28:40.0000000Z", Name = "TransferRequestSecondFactorSent", Amount = 1500.0, SourceTransId = (string?)null, ResultId = (string?)null, RiskLevel = (string?)null, RiskScore = (int?)null },
            new { Id = "33333333-3333-3333-3333-333333333333", CreatedAt = "2026-11-12T09:29:30.0000000Z", Name = "TransferRequestRequiresThirdFactor", Amount = 1500.0, SourceTransId = (string?)null, ResultId = "r-1", RiskLevel = "Medium", RiskScore = 40 },
            new { Id = RiskHarness.AnchorEventId.ToString(), CreatedAt = "2026-11-12T09:30:15.0000000Z", Name = "TransferRequest", Amount = 1500.0, SourceTransId = RiskHarness.KnownTun, ResultId = "r-2", RiskLevel = "High", RiskScore = 85 });
        Handlers["Q-RISK-02"] = p => Result("Q-RISK-02",
            new { ResultId = "r-2", CreatedAt = "2026-11-12T09:30:14.0000000Z", RiskLevel = "High", RiskScore = 85, NumberOfRulesExecuted = 7, ruleName = "TransactionOverMinimumAmount", RuleLevel = "High", RuleScore = 60 },
            new { ResultId = "r-2", CreatedAt = "2026-11-12T09:30:14.0000000Z", RiskLevel = "High", RiskScore = 85, NumberOfRulesExecuted = 7, ruleName = "UnusedAccount", RuleLevel = "Medium", RuleScore = 25 });
        Handlers["Q-SIGN-02"] = _ => Result("Q-SIGN-02", new {
            Id = "s-1", CreatedAt = "2026-11-12T09:27:50.0000000Z", Succeeded = true, ActionName = "User Login Success", SubjectId = "subject-1", SubjectName = "kpapadopoulos",
            ApplicationId = "e-banking-mobile", IpAddress = "203.0.113.9", CountryIsoCode = "GR", DeviceId = "dev-1",
            ExtraData = """{"authenticationMethods":["pwd","mfa"],"userDevice":{"id":"d-1","name":"Maria's iPhone"},"device":{"os":"iOS"}}"""
        });
        Handlers["Q-ID-04"] = _ => Result("Q-ID-04", new { Id = "d-1", DeviceId = "dev-1", Platform = 1, Name = "Maria's iPhone", Model = "iPhone 14", OsVersion = "17.2", DateCreated = "2026-05-01T08:00:00Z", LastSignInDate = "2026-11-12T09:27:50Z", IsTrusted = true, Blocked = false });
        Handlers["Q-RISK-08"] = _ => Result("Q-RISK-08", new {
            CreatedAt = "2026-11-12T09:28:41.0000000Z", Name = "SmsSentEvent", SessionId = "pwd.8f1c", IpAddress = "203.0.113.9", PhoneNumber = "+306912345678", Email = (string?)null,
            TrackingId = "trk-9", DeliveryChannel = "Sms", SmsBody = "Your code is 482913 for the transfer of 1500.00 EUR", EmailBody = (string?)null, PushNotificationBody = (string?)null
        });
        Handlers["Q-RISK-10"] = _ => Result("Q-RISK-10");
        Handlers["Q-ACT-01"] = _ => Result("Q-ACT-01");
        Handlers["Q-RISK-19"] = _ => Result("Q-RISK-19", new { IpAddress = "203.0.113.9", CountryIsoCode = "GR", Location = "Athens, Attica, Greece, Europe", Events = 42, FirstSeen = "2026-08-20T07:00:00Z", LastSeen = "2026-11-11T18:00:00Z" });
        Handlers["Q-SIGN-06"] = _ => Result("Q-SIGN-06", new { CountryIsoCode = "GR", Location = "Athens", IpAddress = "203.0.113.9", Logins = 30, FirstSeen = "2026-08-20T07:00:00Z", LastSeen = "2026-11-12T09:27:50Z" });
        Handlers["Q-ID-01"] = _ => Result("Q-ID-01", new {
            Id = "subject-1", UserName = "kpapadopoulos", Email = "kpapadopoulos@example.com", EmailConfirmed = true, PhoneNumber = "+306912345678", PhoneNumberConfirmed = true,
            TwoFactorEnabled = true, Blocked = false, AccessFailedCount = 0, CreateDate = "2025-01-10T10:00:00Z", LastSignInDate = "2026-11-12T09:27:50Z", LastPasswordChangeDate = "2026-02-01T10:00:00Z"
        });
        Handlers["Q-ID-03"] = _ => Result("Q-ID-03",
            new { ClaimType = "customer_code", ClaimValue = "C-1001" }, new { ClaimType = "given_name", ClaimValue = "Konstantinos" },
            new { ClaimType = "family_name", ClaimValue = "Papadopoulos" }, new { ClaimType = "tin", ClaimValue = "123456789" }, new { ClaimType = "profile_note", ClaimValue = "internal" });
        Handlers["Q-RISK-17"] = _ => Result("Q-RISK-17", new { SourceId = "ebanking-api", Events = 1200, FirstEvent = "2026-08-01T00:00:00Z", LastEvent = "2026-11-21T09:00:00Z" });
        Handlers["Q-SIGN-07"] = _ => Result("Q-SIGN-07", new { TableName = "live", Oldest = "2026-08-23T00:00:00Z", Newest = "2026-11-21T09:00:00Z" }, new { TableName = "archive", Oldest = "2025-01-01T00:00:00Z", Newest = "2026-11-21T09:00:00Z" });
        Handlers["Q-ACT-05"] = _ => Result("Q-ACT-05", new { Oldest = "2025-01-01T00:00:00Z", Newest = "2026-11-21T09:00:00Z" });
    }
}

/// <summary>The antifraud knowledge base: only the limitation sections the report quotes.</summary>
internal sealed class ScriptedKnowledge : IDocumentsService
{
    public bool Fail { get; set; }
    public int SearchCalls { get; private set; }
    public string? LastCategory { get; private set; }
    public bool? LastIncludeRestricted { get; private set; }
    public IReadOnlyCollection<string>? LastPrefixes { get; private set; }
    public bool LastHeadingIncludeRestricted { get; private set; }

    public List<KnowledgeSnippet> Limitations { get; } = [
        .. new[] { ("L-01", "The Risk API may not have been collecting"), ("L-02", "Sign-in log retention"), ("L-03", "Only login completions are stored"), ("L-04", "Activity log scope"),
                   ("L-05", "Event emitters can fail silently"), ("L-06", "Scores are not a fraud verdict"), ("L-09", "Misleading event names"), ("L-11", "Geo data describes the IP"), ("L-13", "Sensitive content in Data") }
            .Select(item => new KnowledgeSnippet($"Limitations and data quality > Section > {item.Item1} {item.Item2}", $"Verbatim text of {item.Item1}: {item.Item2}.", "antifraud-kb"))
    ];

    public Task<IReadOnlyList<RetrievedChunk>> SearchAsync(ReadOnlyMemory<float> queryVector, RetrievalFilters filters, int topK, double minScore, CancellationToken cancellationToken) {
        SearchCalls++;
        LastCategory = filters.Category;
        LastIncludeRestricted = filters.IncludeRestricted;
        return Task.FromResult<IReadOnlyList<RetrievedChunk>>([]);
    }

    public Task<IReadOnlyList<KnowledgeSnippet>> FindByHeadingAsync(string category, IReadOnlyCollection<string> headingPrefixes, string? language, bool includeRestricted, CancellationToken cancellationToken) {
        LastPrefixes = headingPrefixes;
        LastHeadingIncludeRestricted = includeRestricted;
        return Fail ? throw new InvalidOperationException("The knowledge base is down.") : Task.FromResult<IReadOnlyList<KnowledgeSnippet>>(Limitations);
    }

    public Task<SourceDocument?> FindBySourceAsync(string source, bool includeData, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<Guid> ReplaceAsync(Guid? existingDocumentId, IngestedDocument document, IReadOnlyList<Indice.Features.Agents.Core.Workflows.Ingestion.EmbeddedChunk> chunks, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task ClearAsync() => throw new NotSupportedException();
}
