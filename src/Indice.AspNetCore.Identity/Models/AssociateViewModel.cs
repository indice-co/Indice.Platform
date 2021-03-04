namespace Indice.AspNetCore.Identity.Models
{
    public class AssociateViewModel
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public bool HasAcceptedTerms { get; set; }
        public bool HasReadPrivacyPolicy { get; set; }
        public string Provider { get; set; }
        public string ReturnUrl { get; set; }
    }
}
