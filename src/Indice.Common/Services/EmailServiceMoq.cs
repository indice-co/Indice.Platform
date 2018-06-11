using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Services
{
    public class EmailServiceMoq : IEmailService
    {
        public async Task SendAsync(string[] recipients, string subject, string body) => await SendAsync<object>(recipients, subject, body, null, null);

        public Task SendAsync<TModel>(string[] recipients, string subject, string body, string template, TModel data) where TModel : class {
            foreach (var recipient in recipients) {
                Console.WriteLine($"Email:\n\t\t{recipient}/{subject}\n\n\t\t{body}");
            }
#if NET452
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }
    }
}
