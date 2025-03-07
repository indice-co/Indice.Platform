namespace Indice.Features.Messages.Core.Models;

/// <summary>Models a hyperlink.</summary>
public class Hyperlink
{
    /// <summary>Creates a new instance of <see cref="Hyperlink"/>.</summary>
    public Hyperlink() { }

    /// <summary>Creates a new instance of <see cref="Hyperlink"/>.</summary>
    /// <param name="text">Defines the hyperlink text.</param>
    /// <param name="href">Defines the hyperlink URL.</param>
    public Hyperlink(string text, string href) {
        Text = text;
        Href = href;
    }

    /// <summary>Defines the hyperlink text.</summary>
    public string? Text { get; set; }
    /// <summary>Defines the hyperlink URL.</summary>
    public string? Href { get; set; }
}
