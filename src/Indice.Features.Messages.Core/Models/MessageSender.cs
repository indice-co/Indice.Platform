using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.Features.Messages.Core.Models
{
    /// <summary>
    /// The representation of a sender id visible in the recipients phone, email address etc.
    /// </summary>
    public class MessageSender
    {
        /// <summary>Sender id</summary>
        public string Id { get; set; }
        /// <summary>Sender Name</summary>
        public string DisplayName { get; set; }
        /// <summary>Checks for id existence</summary>
        public bool IsEmpty => string.IsNullOrWhiteSpace(Id);
        /// <inheritdoc/>
        public override string ToString() => IsEmpty ? base.ToString() : $"{DisplayName} <{Id}>";
    }
}
