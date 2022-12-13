using System;

namespace Indice.Features.Cases.Models.Responses
{
    /// <summary>
    /// The comment entry for a case.
    /// </summary>
    public class Comment
    {
        /// <summary>
        /// The Id of the comment.
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// The comment text.
        /// </summary>
        public string Text { get; set; }
        
        /// <summary>
        /// Indicates if the comment is made by customer
        /// </summary>
        public bool? IsCustomer { get; set; } // todo Implement properly!!!
        
        /// <summary>
        /// Indicates if the comment is private, which means not visible to the customer.
        /// </summary>
        public bool? Private { get; set; }
        
        /// <summary>
        /// The optional attachment Id of the comment.
        /// </summary>
        public CasesAttachmentLink Attachment { get; set; }

        /// <summary>
        /// If the comment is a reply to another comment, this object will include the original comment.
        /// </summary>
        public Comment ReplyToComment { get; set; }
    }
}