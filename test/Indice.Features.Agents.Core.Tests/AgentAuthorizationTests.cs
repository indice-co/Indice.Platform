using System.Security.Claims;
using System.Text.Json;
using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Tests;

internal static class AgentTestUsers
{
    public const string ComplianceRole = "ComplianceOfficer";

    public static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());

    public static ClaimsPrincipal Authenticated(params string[] roles) =>
        new(new ClaimsIdentity(roles.Select(role => new Claim("role", role)), authenticationType: "test"));
}

public class AgentInfoRegistryTests
{
    private static AgentInfo Open(string name) => new(name, $"{name} agent", ["text/plain"], ["text/markdown"]);

    private static AgentInfo Restricted(string name, params string[] roles) => Open(name) with { RequiredRoles = roles };

    private static readonly AgentInfoRegistry Registry = new([
        Open(AgentsConstants.AgentNames.Auto),
        Open(AgentsConstants.AgentNames.Knowledge),
        Restricted("compliance", AgentTestUsers.ComplianceRole)
    ]);

    private static readonly AgentInfo ComplianceAgent = Registry.Find("compliance")!;

    [Fact]
    public void An_Agent_Without_Roles_Is_Open_To_Everyone_Including_Anonymous_Callers() {
        var knowledge = Registry.Find(AgentsConstants.AgentNames.Knowledge)!;

        Assert.True(Registry.IsAllowed(knowledge, null));
        Assert.True(Registry.IsAllowed(knowledge, AgentTestUsers.Anonymous()));
        Assert.True(Registry.IsAllowed(knowledge, AgentTestUsers.Authenticated()));
    }

    [Fact]
    public void A_Role_Restricted_Agent_Requires_An_Authenticated_Caller_With_The_Role() {
        Assert.False(Registry.IsAllowed(ComplianceAgent, null));
        Assert.False(Registry.IsAllowed(ComplianceAgent, AgentTestUsers.Anonymous()));
        Assert.False(Registry.IsAllowed(ComplianceAgent, AgentTestUsers.Authenticated()));
        Assert.False(Registry.IsAllowed(ComplianceAgent, AgentTestUsers.Authenticated("SomethingElse")));
        Assert.True(Registry.IsAllowed(ComplianceAgent, AgentTestUsers.Authenticated(AgentTestUsers.ComplianceRole)));
    }

    [Fact]
    public void The_Role_Is_Also_Recognised_On_The_Standard_Role_Claim_Type() {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, AgentTestUsers.ComplianceRole)], authenticationType: "test");

        Assert.True(Registry.IsAllowed(ComplianceAgent, new ClaimsPrincipal(identity)));
    }

    [Fact]
    public void Administrators_Do_Not_Bypass_A_Role_Restriction() {
        var admin = AgentTestUsers.Authenticated("Administrator", BasicRoleNamesAgentsAdmin);

        Assert.False(Registry.IsAllowed(ComplianceAgent, admin));
    }

    private const string BasicRoleNamesAgentsAdmin = "AgentsAdmin";

    [Fact]
    public void A_Restricted_Agent_Is_Reported_Exactly_Like_One_That_Does_Not_Exist() {
        var ordinary = AgentTestUsers.Authenticated();

        Assert.Null(Registry.Find("compliance", ordinary));
        Assert.Null(Registry.Find("does-not-exist", ordinary));
        Assert.NotNull(Registry.Find("compliance", AgentTestUsers.Authenticated(AgentTestUsers.ComplianceRole)));
        Assert.NotNull(Registry.Find("COMPLIANCE", AgentTestUsers.Authenticated(AgentTestUsers.ComplianceRole))); // names are case-insensitive
    }

    [Fact]
    public void Discovery_And_Routing_Lists_Are_Filtered_Per_Caller() {
        var ordinary = AgentTestUsers.Authenticated();
        var officer = AgentTestUsers.Authenticated(AgentTestUsers.ComplianceRole);

        Assert.Equal(new[] { "auto", "knowledge" }, Registry.All(ordinary).Select(a => a.Name));
        Assert.Equal(new[] { "auto", "knowledge", "compliance" }, Registry.All(officer).Select(a => a.Name));
        // The meta router never routes to itself, for anyone.
        Assert.Equal(new[] { "knowledge" }, Registry.RoutableTargets(ordinary).Select(a => a.Name));
        Assert.Equal(new[] { "knowledge", "compliance" }, Registry.RoutableTargets(officer).Select(a => a.Name));
        // The unfiltered overloads stay unfiltered: they are for startup code and tests.
        Assert.Equal(3, Registry.All().Count);
    }

    [Fact]
    public void Authorization_Metadata_Is_Not_Part_Of_The_Discovery_Payload() {
        var json = JsonSerializer.Serialize(new[] { ComplianceAgent with { KnowledgeCategories = ["internal-kb"] } });

        Assert.DoesNotContain("RequiredRoles", json);
        Assert.DoesNotContain(AgentTestUsers.ComplianceRole, json);
        Assert.DoesNotContain("KnowledgeCategories", json);
        Assert.DoesNotContain("internal-kb", json);
        Assert.Contains("\"Name\":\"compliance\"", json);
    }
}

/// <summary>
/// The master router is the authorization boundary for role-restricted agents. A caller without the role must neither be
/// able to select the agent by name, nor be offered it by the models, nor be routed to it by a model answer, nor stay pinned
/// to one of its workflows.
/// </summary>
public class IntentRouterAuthorizationTests : IDisposable
{
    private readonly AgentsDbContext _db = new(new DbContextOptionsBuilder<AgentsDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    public void Dispose() => _db.Dispose();

    private static ChatMessage Message(string text = "I need an evidence pack for a transaction") => new(ChatRole.User, text);

    private (IntentRouterService Router, RecordingPrompts Prompts, ScriptedChatClient Chat) Create(ClaimsPrincipal? user, string modelAnswers = "knowledge", IKnowledgeHintService? hints = null) {
        var chat = new ScriptedChatClient(modelAnswers);
        var prompts = new RecordingPrompts();
        var store = new EmptyHistoryStore();
        var registry = new AgentInfoRegistry([
            new AgentInfo(AgentsConstants.AgentNames.Auto, "router", ["text/plain"], ["text/markdown"]),
            new AgentInfo(AgentsConstants.AgentNames.Knowledge, "knowledge agent", ["text/plain"], ["text/markdown"]),
            new AgentInfo("compliance", "compliance agent", ["text/plain"], ["text/markdown"]) { RequiredRoles = [AgentTestUsers.ComplianceRole] }
        ]);
        AgentsClaimsPrincipalSelector selector = () => user;
        var router = new IntentRouterService(
            chat,
            Options.Create(new AgentsOptions()),
            Options.Create(new ModelsOptions()),
            prompts,
            registry,
            new UserClaimsAIContextProvider(selector, new NullUsersService()),
            new ConversationStoreChatHistoryProvider(store),
            selector,
            hints ?? new ScriptedHints(),
            new UnusedSystemOne(),
            store,
            new PersistedCheckpointStore(_db),
            new AgentMessageLocalizer(),
            NullLogger<IntentRouterService>.Instance);
        return (router, prompts, chat);
    }

    [Fact]
    public async Task Selecting_A_Restricted_Agent_By_Name_Is_Refused_Exactly_Like_An_Unknown_Agent() {
        var (router, _, chat) = Create(AgentTestUsers.Authenticated());

        var restricted = await router.RouteAsync(Message(), new ChatOptions { Instructions = "compliance" }, TestContext.Current.CancellationToken);
        var unknown = await router.RouteAsync(Message(), new ChatOptions { Instructions = "nope" }, TestContext.Current.CancellationToken);

        Assert.True(restricted.HasError);
        Assert.Null(restricted.AgentName);
        Assert.Equal("Agent 'compliance' is not registered.", restricted.Reason);
        Assert.Equal("Agent 'nope' is not registered.", unknown.Reason);
        Assert.Equal(0, chat.Calls);
    }

    [Fact]
    public async Task An_Anonymous_Caller_Cannot_Select_A_Restricted_Agent() {
        var (router, _, _) = Create(AgentTestUsers.Anonymous());

        var decision = await router.RouteAsync(Message(), new ChatOptions { Instructions = "compliance" }, TestContext.Current.CancellationToken);

        Assert.True(decision.HasError);
        Assert.Null(decision.AgentName);
    }

    [Fact]
    public async Task A_Caller_With_The_Role_Can_Select_The_Agent_By_Name() {
        var (router, _, _) = Create(AgentTestUsers.Authenticated(AgentTestUsers.ComplianceRole));

        var decision = await router.RouteAsync(Message(), new ChatOptions { Instructions = "compliance" }, TestContext.Current.CancellationToken);

        Assert.False(decision.HasError);
        Assert.Equal("compliance", decision.AgentName);
        Assert.True(decision.IsInScope);
    }

    [Theory]
    [InlineData(false, new[] { "knowledge" })]
    [InlineData(true, new[] { "knowledge", "compliance" })]
    public async Task Automatic_Routing_Only_Offers_The_Model_The_Agents_The_Caller_May_Use(bool hasRole, string[] expectedOffered) {
        var user = hasRole ? AgentTestUsers.Authenticated(AgentTestUsers.ComplianceRole) : AgentTestUsers.Authenticated();
        var (router, prompts, _) = Create(user);

        await router.RouteAsync(Message(), new ChatOptions { Instructions = "auto" }, TestContext.Current.CancellationToken);

        Assert.Equal(expectedOffered, prompts.OfferedRouterAgents());
    }

    [Fact]
    public async Task An_Administrator_Without_The_Role_Is_Not_Offered_The_Restricted_Agent() {
        var (router, prompts, _) = Create(AgentTestUsers.Authenticated("Administrator", "AgentsAdmin"));

        await router.RouteAsync(Message(), new ChatOptions { Instructions = "auto" }, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "knowledge" }, prompts.OfferedRouterAgents());
    }

    [Fact]
    public async Task A_Model_Answer_Naming_An_Agent_The_Caller_May_Not_Use_Is_Out_Of_Scope() {
        // Prompt injection or a hallucinated name: the model says "compliance" although it was never offered to this caller.
        var (router, _, _) = Create(AgentTestUsers.Authenticated(), modelAnswers: "compliance");

        var decision = await router.RouteAsync(Message(), new ChatOptions { Instructions = "auto" }, TestContext.Current.CancellationToken);

        Assert.Null(decision.AgentName);
        Assert.False(decision.IsInScope);
    }

    [Fact]
    public async Task A_Model_Answer_Naming_The_Restricted_Agent_Is_Honoured_For_A_Caller_With_The_Role() {
        var (router, _, _) = Create(AgentTestUsers.Authenticated(AgentTestUsers.ComplianceRole), modelAnswers: "compliance");

        var decision = await router.RouteAsync(Message(), new ChatOptions { Instructions = "auto" }, TestContext.Current.CancellationToken);

        Assert.Equal("compliance", decision.AgentName);
        Assert.True(decision.IsInScope);
    }

    [Theory]
    [InlineData(true, "compliance")]
    [InlineData(false, "knowledge")]
    public async Task A_Workflow_Waiting_For_Input_Only_Keeps_The_Turn_While_The_Caller_Still_Has_The_Role(bool stillHasRole, string expectedAgent) {
        var conversationId = Guid.NewGuid();
        var checkpoints = new PersistedCheckpointStore(_db);
        var checkpoint = await checkpoints.CreateCheckpointAsync(new AgentSessionId(conversationId, "compliance"), JsonDocument.Parse("""{"step":1}""").RootElement);
        var reply = new ChatMessage(ChatRole.User, [new TextContent("123456"), new FunctionResultContent($"request-1:{checkpoint.CheckpointId}", "123456")]);
        var user = stillHasRole ? AgentTestUsers.Authenticated(AgentTestUsers.ComplianceRole) : AgentTestUsers.Authenticated();
        var (router, _, _) = Create(user, modelAnswers: "knowledge");

        var decision = await router.RouteAsync(reply, new ChatOptions { ConversationId = conversationId.ToString(), Instructions = "auto" }, TestContext.Current.CancellationToken);

        Assert.Equal(expectedAgent, decision.AgentName);
    }

    [Fact]
    public async Task Knowledge_Matches_Are_Appended_To_What_The_Routing_Model_Reads() {
        var hints = new ScriptedHints(new KnowledgeHint("internal-kb", 0.624, ["compliance"]));
        var (router, _, chat) = Create(AgentTestUsers.Authenticated(AgentTestUsers.ComplianceRole), modelAnswers: "compliance", hints: hints);

        await router.RouteAsync(Message("trace TUN 12345"), new ChatOptions { Instructions = "auto" }, TestContext.Current.CancellationToken);

        var prompt = Assert.Single(chat.Prompts);
        Assert.StartsWith("trace TUN 12345", prompt);
        Assert.Contains("KNOWLEDGE MATCHES:", prompt);
        Assert.Contains("- internal-kb (score 0.62) -> agent compliance", prompt);
    }

    [Fact]
    public async Task Without_Hints_The_Routing_Model_Reads_The_Message_Unchanged() {
        var (router, _, chat) = Create(AgentTestUsers.Authenticated());

        await router.RouteAsync(Message("hello there"), new ChatOptions { Instructions = "auto" }, TestContext.Current.CancellationToken);

        Assert.Equal("hello there", Assert.Single(chat.Prompts));
    }

    [Theory]
    [InlineData(false, new[] { "knowledge" })]
    [InlineData(true, new[] { "knowledge", "compliance" })]
    public async Task The_Hint_Lookup_Only_Ever_Sees_The_Agents_The_Caller_May_Use(bool hasRole, string[] expectedAgents) {
        var hints = new ScriptedHints();
        var user = hasRole ? AgentTestUsers.Authenticated(AgentTestUsers.ComplianceRole) : AgentTestUsers.Authenticated();
        var (router, _, _) = Create(user, hints: hints);

        await router.RouteAsync(Message(), new ChatOptions { Instructions = "auto" }, TestContext.Current.CancellationToken);

        Assert.Equal(expectedAgents, hints.AgentsSeen);
    }

    [Fact]
    public async Task A_Failing_Hint_Lookup_Does_Not_Stop_Routing() {
        var (router, _, _) = Create(AgentTestUsers.Authenticated(), modelAnswers: "knowledge", hints: new ScriptedHints { Throw = true });

        var decision = await router.RouteAsync(Message(), new ChatOptions { Instructions = "auto" }, TestContext.Current.CancellationToken);

        Assert.Equal("knowledge", decision.AgentName);
        Assert.False(decision.HasError);
    }

    [Fact]
    public async Task An_Explicit_Selection_Does_Not_Look_Up_Hints() {
        var hints = new ScriptedHints();
        var (router, _, _) = Create(AgentTestUsers.Authenticated(), hints: hints);

        await router.RouteAsync(Message(), new ChatOptions { Instructions = "knowledge" }, TestContext.Current.CancellationToken);

        Assert.Equal(0, hints.Lookups);
    }

    // --- test doubles -------------------------------------------------------------------------------------------------

    private sealed class ScriptedHints(params KnowledgeHint[] hints) : IKnowledgeHintService
    {
        public bool Throw { get; init; }
        public int Lookups { get; private set; }
        public string[] AgentsSeen { get; private set; } = [];

        public Task<IReadOnlyList<KnowledgeHint>> GetHintsAsync(string message, IReadOnlyList<AgentInfo> allowedAgents, CancellationToken cancellationToken = default) {
            Lookups++;
            AgentsSeen = [.. allowedAgents.Select(agent => agent.Name)];
            return Throw ? throw new InvalidOperationException("embedding service is down") : Task.FromResult<IReadOnlyList<KnowledgeHint>>(hints);
        }
    }

    /// <summary>Answers the router's structured-output call with a fixed agent choice.</summary>
    private sealed class ScriptedChatClient(string agent) : IChatClient
    {
        public int Calls { get; private set; }
        public List<string> Prompts { get; } = [];

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) {
            Calls++;
            Prompts.Add(messages.Last(message => message.Role == ChatRole.User).Text);
            var json = JsonSerializer.Serialize(new { agentName = agent, reason = "scripted", isInScope = true });
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, json)));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }

    /// <summary>Records what the router renders into its model instructions, so a test can see which agents the model is told about.</summary>
    private sealed class RecordingPrompts : IPromptTemplateRenderer
    {
        private readonly List<(string Name, object? Values)> _calls = [];

        public string Render(string templateName, object? values = null) {
            _calls.Add((templateName, values));
            return $"[{templateName}]";
        }

        public string[] OfferedRouterAgents() {
            var last = _calls.Last(call => call.Name == "IntentRouter");
            var element = JsonSerializer.SerializeToElement(last.Values);
            return [.. element.GetProperty("agents").EnumerateArray().Select(agent => agent.GetProperty("name").GetString()!)];
        }
    }

    private sealed class UnusedSystemOne : ISystemOneClient
    {
        public Task<SystemOneDecision> DecideAsync(SystemOneDecisionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class NullUsersService : IUsersService
    {
        public Task<Profile?> GetAsync(string userId, CancellationToken cancellationToken) => Task.FromResult<Profile?>(null);
        public Task<string?> GetDisplayNameAsync(string userId, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
        public Task<Profile> UpsertFromClaimsAsync(string userId, string? displayName, string? email, string? locale, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Profile> UpdatePreferencesAsync(string userId, string? preferredLanguage, string? responseStyle, List<string>? preferredCategories, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class EmptyHistoryStore : IConversationStore
    {
        public Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(Guid conversationId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ChatMessage>>([]);
        public Task<Conversation?> LoadOrCreateAsync(string userId, string? authorName, Guid? conversationId, ChatTopic? topic, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Conversation?> GetAsync(Guid conversationId, string userId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ResultSet<ConversationListItem>> ListAsync(string userId, ListOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ChatMessage> AppendTurnAsync(Guid conversationId, ChatMessage userMessage, ChatResponse response, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AppendFailedTurnAsync(Guid conversationId, ChatMessage userMessage, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<int> DeleteAsync(Guid conversationId, string userId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<int> CountSessionsAsync(string userId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<long> GetUsageTokensAsync(string userId, DateTimeOffset since, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> SetLikeAsync(string userId, Guid conversationId, Guid messageId, bool? liked, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> SetReadOnlyAsync(string userId, Guid conversationId, bool isReadOnly, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> SetVisibilityAsync(string userId, Guid conversationId, bool hidden, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
