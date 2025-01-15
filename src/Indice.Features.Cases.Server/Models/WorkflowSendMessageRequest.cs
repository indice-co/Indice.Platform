using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Server.Models;

/// <summary>Send message from workflow as an actor.</summary>
public class WorkflowSendMessageRequest
{
    /// <summary>Message</summary>
    public Message Message {get;set;}
    
    /// <summary>Actor</summary>
    public WorkflowActor WorkflowActor {get;set;}
}