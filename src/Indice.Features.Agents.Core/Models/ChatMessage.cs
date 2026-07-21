namespace Indice.Features.Agents.Core.Models;

///// <summary>A single turn (user or assistant) in a chat session. DTO exposed at the service boundary; mirrors <see cref="Data.DbMessage"/>.</summary>
//public class ChatMessage
//{
//    /// <summary>Identifier of the session this turn belongs to.</summary>
//    public Guid ConversationId { get; init; }

//    /// <summary>Identifier of the assistant message persisted for this turn.</summary>
//    public Guid MessageId { get; init; }

//    /// <summary>Author role of this message. Serializes as the role's lowercase value (e.g. <c>user</c>).</summary>
//    public ChatMessageRole Role { get; init; } = ChatMessageRole.User;

//    /// <summary>Message body.</summary>
//    public ChatMessageContent Content { get; init; } = new();

//    /// <summary>Creation timestamp.</summary>
//    public DateTimeOffset CreatedAt { get; init; }

//    /// <summary>References to chunks.</summary>
//    public List<Citation> Citations { get; set; } = [];
    
//    /// <summary>References to source documents.</summary>
//    public List<SourceDocumentLink> Sources { get; set; } = [];

//    /// <summary>True when a pipeline step threw and the workflow halted. Out-of-scope is NOT a failure — its refusal text flows through <see cref="Answer"/>.</summary>
//    public bool Failed { get; init; }

//    /// <summary>True when the turn was blocked by a session usage limit — <see cref="Answer"/> carries the predefined limit message, nothing was persisted, and <see cref="MessageId"/> is empty.</summary>
//    public bool LimitReached { get; init; }

//    /// <summary>Questions used in this session so far, for a <c>used/total</c> display. <c>null</c> when the message limit is disabled.</summary>
//    public int? QuestionsUsed { get; init; }

//    /// <summary>Total questions allowed per session, for a <c>used/total</c> display. <c>null</c> when the message limit is disabled.</summary>
//    public int? QuestionsTotal { get; init; }
//}
