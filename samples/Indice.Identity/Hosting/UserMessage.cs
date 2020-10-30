using System;
using Indice.Hosting;

namespace Indice.Identity.Hosting
{
    public class UserMessage
    {
        public UserMessage() {

        }
        public UserMessage(string userId, string phoneNumber, string message) {
            Id = Guid.NewGuid();
            UserId = userId;
            PhoneNumber = phoneNumber;
            Message = message;
        }

        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string PhoneNumber { get; set; }
        public string Message { get; set; }

        public override string ToString() => $"Id: {Id}, UserId: {UserId}, Phone Number: {PhoneNumber}, Message: {Message}";
    }
}
