using System;
using System.Threading.Tasks;
using Indice.Hosting.Tasks;

namespace Indice.Api.JobHandlers
{
    public class SendSmsJobHandler
    {
        private readonly IMessageQueue<LogSmsDto> _messageQueue;

        public SendSmsJobHandler(IMessageQueue<LogSmsDto> messageQueue) {
            _messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        }

        public async Task Process(SmsDto message) {
            if (message == null) {
                return;
            }
            var waitTime = new Random().Next(5, 10) * 100;
            await Task.Delay(waitTime);
            await _messageQueue.Enqueue(new LogSmsDto { 
                Id = message.Id,
                Message = message.Message,
                PhoneNumber = message.PhoneNumber,
                UserId = message.UserId
            });
        }
    }
}
