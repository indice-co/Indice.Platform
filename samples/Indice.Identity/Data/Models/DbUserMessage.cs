using System;
using Indice.Hosting;

namespace Indice.Identity.Data
{
    public class DbUserMessage : WorkItemBase
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string PhoneNumber { get; set; }
        public string Message { get; set; }

        public override string ToString() => $"UserId: {UserId}, Phone Number: {PhoneNumber}, Message: {Message}";
    }
}
