using Indice.Hosting;

namespace Indice.Identity.Hosting
{
    public class UserMessage : WorkItem
    {
        public UserMessage(string phoneNumber, string message) {
            PhoneNumber = phoneNumber;
            Message = message;
        }

        public string PhoneNumber { get; set; }
        public string Message { get; set; }

        public override string ToString() => $"Phone Number: {PhoneNumber}, Message: {Message}";
    }
}
