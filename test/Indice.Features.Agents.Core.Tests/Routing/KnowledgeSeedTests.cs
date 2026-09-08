using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Steps;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Tests.Routing;

public class KnowledgeSeedTests
{
    [Fact]
    public void Seeds_state_from_the_last_stamped_user_message() {
        var conversationId = Guid.NewGuid();
        var question = new ChatMessage(ChatRole.User, "How do I enable MFA?").SetConversationId(conversationId);
        var messages = new List<ChatMessage> {
            new(ChatRole.User, "earlier question"),
            new(ChatRole.Assistant, "earlier answer"),
            question,
            // Handoff context sync re-labels other participants' remarks as user messages; they carry no conversation id.
            new(ChatRole.User, "Routing you to the knowledge agent."),
        };

        var state = KnowledgeSeed.CreateConversationState(messages);

        Assert.Same(question, state.Message);
        Assert.Equal(conversationId.ToString(), state.ConversationId);
    }

    [Fact]
    public void Falls_back_to_the_last_user_message_and_a_fresh_id_when_nothing_is_stamped() {
        var messages = new List<ChatMessage> {
            new(ChatRole.User, "first"),
            new(ChatRole.Assistant, "reply"),
            new(ChatRole.User, "second"),
        };

        var state = KnowledgeSeed.CreateConversationState(messages);

        Assert.Equal("second", state.Message.Text);
        Assert.True(Guid.TryParse(state.ConversationId, out var id) && id != Guid.Empty);
    }

    [Fact]
    public void Uses_an_id_stamped_on_an_earlier_message_when_the_question_has_none() {
        var conversationId = Guid.NewGuid();
        var messages = new List<ChatMessage> {
            new ChatMessage(ChatRole.System, "context").SetConversationId(conversationId),
            new(ChatRole.User, "first"),
            new(ChatRole.Assistant, "reply"),
        };

        var state = KnowledgeSeed.CreateConversationState(messages);

        Assert.Equal("first", state.Message.Text);
        Assert.Equal(conversationId.ToString(), state.ConversationId);
    }

    [Fact]
    public void Throws_without_a_user_message() {
        Assert.Throws<InvalidOperationException>(() => KnowledgeSeed.CreateConversationState([new ChatMessage(ChatRole.Assistant, "hi")]));
    }
}
