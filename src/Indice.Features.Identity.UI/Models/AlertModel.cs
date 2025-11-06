using System.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Indice.Features.Identity.UI.Models;

/// <summary>Tiny model for passing state to view regarding error bubbles and all sorts of alerts.</summary>
/// <remarks>Usually combined perfectly with <see cref="TempDataDictionary"/> in order to pass the state of success or error between pages.</remarks>
public class AlertModel
{
    /// <summary>The message.</summary>
    public HtmlString Message { get; set; } = new HtmlString("");
    /// <summary>The alert type.</summary>
    public AlertType AlertType { get; set; }

    /// <summary>Creates an alert with <see cref="AlertType.Info"/>.</summary>
    /// <param name="message">The message.</param>
    public static AlertModel Info(string message) => new() { 
        AlertType = AlertType.Info, 
        Message = new HtmlString(message) 
    };
    
    /// <summary>Creates an alert with <see cref="AlertType.Warning"/>.</summary>
    /// <param name="message">The message.</param>
    public static AlertModel Warn(string message) => new() { 
        AlertType = AlertType.Warning, 
        Message = new HtmlString(message )
    };
    
    /// <summary>Creates an alert with <see cref="AlertType.Danger"/>.</summary>
    /// <param name="message">The message.</param>
    public static AlertModel Error(string message) => new() { 
        AlertType = AlertType.Danger, 
        Message = new HtmlString(message )
    };
    
    /// <summary>Creates an alert with <see cref="AlertType.Success"/></summary>
    /// <param name="message">The message.</param>
    public static AlertModel Success(string message) => new() { 
        AlertType = AlertType.Success, 
        Message = new HtmlString(message )
    };



    /// <summary>Creates an alert with <see cref="AlertType.Info"/>.</summary>
    /// <param name="message">The message.</param>
    public static AlertModel Info(HtmlString message) => new() {
        AlertType = AlertType.Info,
        Message = message
    };

    /// <summary>Creates an alert with <see cref="AlertType.Warning"/>.</summary>
    /// <param name="message">The message.</param>
    public static AlertModel Warn(HtmlString message) => new() {
        AlertType = AlertType.Warning,
        Message = message
    };

    /// <summary>Creates an alert with <see cref="AlertType.Danger"/>.</summary>
    /// <param name="message">The message.</param>
    public static AlertModel Error(HtmlString message) => new() {
        AlertType = AlertType.Danger,
        Message = message
    };

    /// <summary>Creates an alert with <see cref="AlertType.Success"/></summary>
    /// <param name="message">The message.</param>
    public static AlertModel Success(HtmlString message) => new() {
        AlertType = AlertType.Success,
        Message = message
    };
}
