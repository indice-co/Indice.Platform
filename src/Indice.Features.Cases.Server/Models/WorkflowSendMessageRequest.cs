using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Server.Models;

public class WorkflowSendMessageRequest
{
    public Message Message {get;set;}
    
    public CasesActor CasesActor {get;set;}
}