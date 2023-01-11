using System.ComponentModel.DataAnnotations;

namespace Indice.Identity.Models
{
    public class MfaLoginPushNotificationRequest
    {
        [Required]
        public string ConnectionId { get; set; }
    }
}
