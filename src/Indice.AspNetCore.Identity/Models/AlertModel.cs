namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>Tiny model for passing state to view regarding error bubbles and all sorts of alerts.</summary>
    /// <remarks>Usualy combined perfectly with TempData in order to pass the state of success or error between Actions.</remarks>
    public class AlertModel
    {
        /// <summary>The message.</summary>
        public string Message { get; set; }
        /// <summary>The alert type.</summary>
        public AlertType AlertType { get; set; }

        /// <summary>Creates an alert with <see cref="AlertType.Info"/>.</summary>
        /// <param name="message">The message.</param>
        public static AlertModel Info(string message) => new() { AlertType = AlertType.Info, Message = message };
        /// <summary>Creates an alert with <see cref="AlertType.Warning"/>.</summary>
        /// <param name="message">The message.</param>
        public static AlertModel Warn(string message) => new() { AlertType = AlertType.Warning, Message = message };
        /// <summary>Creates an alert with <see cref="AlertType.Danger"/>.</summary>
        /// <param name="message">The message.</param>
        public static AlertModel Error(string message) => new() { AlertType = AlertType.Danger, Message = message };
        /// <summary>Creates an alert with <see cref="AlertType.Success"/></summary>
        /// <param name="message">The message.</param>
        public static AlertModel Success(string message) => new() { AlertType = AlertType.Success, Message = message };
    }

    /// <summary>The type of alert <see cref="AlertModel" />.</summary>
    public enum AlertType
    {
        /// <summary>Success</summary>
        Success,
        /// <summary>Info</summary>
        Info,
        /// <summary>Warning</summary>
        Warning,
        /// <summary>Danger</summary>
        Danger
    }
}
