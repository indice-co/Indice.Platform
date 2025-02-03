namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>The request model used to update user preferences for an existing template.</summary>
public class UpdateTemplateUserPreferencesRequest
{
    /// <summary>The name of the template.</summary>
    public bool IgnoreUserPreferences { get; set; }
}
