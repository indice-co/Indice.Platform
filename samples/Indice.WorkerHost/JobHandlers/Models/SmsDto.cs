namespace Indice.WorkerHost.JobHandlers
{
    public class SmsDto
    {
        public SmsDto() { }

        public SmsDto(string userId, string phoneNumber, string message) {
            Id = Guid.NewGuid();
            UserId = userId;
            PhoneNumber = phoneNumber;
            Message = message;
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; }
        public string PhoneNumber { get; set; }
        public string Message { get; set; }

        public override string ToString() => $"Id: {Id}, UserId: {UserId}, Phone Number: {PhoneNumber}, Message: {Message}";
    }
}
