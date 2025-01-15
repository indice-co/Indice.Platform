namespace Indice.Features.Cases.Workflows.Models;

/// <summary>Custom action blocking activity that will generate the corresponding component.</summary>
public class CustomAction
{
    /// <summary>The Id to trigger the action.</summary>
    public string Id { get; set; } = null!;
    
    /// <summary>The Role allowed to trigger the action.</summary>
    public string? AllowedRole { get; set; }

    /// <summary>The name of the action.</summary>
    public string? Name { get; set; }

    /// <summary>The label of the action.</summary>
    public string? Label { get; set; }

    /// <summary>The class of the action.</summary>
    public string? Class { get; set; }

    /// <summary>Determines whether at the end of the action the user will be redirected to Cases list of Back-office UI.</summary>
    public bool? RedirectToList { get; set; }

    /// <summary>A response message that is returned if the action is completed with success.</summary>
    public SuccessMessage? SuccessMessage { get; set; }

    /// <summary>The description of the action.</summary>
    public string? Description { get; set; }

    /// <summary>The Default Value of action's input.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Determines whether the action will have an input element.</summary>
    public bool? HasInput { get; set; }
}

/// <summary>Success Message Action Model.</summary>
public class SuccessMessage
{
    /// <summary>The message's Title.</summary>
    public string Title { get; set; } = null!;

    /// <summary>The message's Body.</summary>
    public string? Body { get; set; }
}