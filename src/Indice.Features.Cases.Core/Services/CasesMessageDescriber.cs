namespace Indice.Features.Cases.Core.Services;

/// <summary>Cases resources describer. Extend this class for customization.</summary>
public class CasesMessageDescriber
{
    /// <summary>The comment to show at timeline when an agent edits a case.</summary>
    /// <param name="userName">The username of the actor.</param>
    /// <param name="email">The email of the actor.</param>
    /// <returns></returns>
    public virtual string EditCaseComment(string userName, string email)
        => string.Format(CasesResources.Culture, CasesResources.EditCaseComment, userName, email);

    /// <summary>The comment to show at timeline when an agent tries to bypass four-eyes principle.</summary>
    public virtual string BlockPreviousApproverComment
        => string.Format(CasesResources.Culture, CasesResources.BlockPreviousApproverComment);
}
