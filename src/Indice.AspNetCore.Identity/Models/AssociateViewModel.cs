namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// This view model backs the ExternalProvider association views.
    /// </summary>
    public class AssociateViewModel
    {
        /// <summary>
        /// The username
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// The Email
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// The first name 
        /// </summary>
        public string FirstName { get; set; }
        /// <summary>
        /// The last name 
        /// </summary>
        public string LastName { get; set; }
        /// <summary>
        /// phonenumber 
        /// </summary>
        public string PhoneNumber { get; set; }
        /// <summary>
        /// User Has accepted the terms and conditons
        /// </summary>
        public bool HasAcceptedTerms { get; set; }
        /// <summary>
        /// User has accepted the privacy policy
        /// </summary>
        public bool HasReadPrivacyPolicy { get; set; }
        /// <summary>
        /// The external id provider
        /// </summary>
        public string Provider { get; set; }
        /// <summary>
        /// The return url
        /// </summary>
        public string ReturnUrl { get; set; }
    }
}
