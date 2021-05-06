using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Indice.Serialization;
using Indice.Types;
using Xunit;

namespace Indice.Services.Tests
{
    public class EnvelopeTests
    {
        [Fact]
        public void EnvelopeShouldBe_Serializable_Test() {
            var payload = new DummyPayload {
                MyText = "This is a test"
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", Guid.NewGuid().ToString()) }, "DummyAuth"));
            var message = Envelope.Create(user, payload);
            var json = JsonSerializer.Serialize(message);
            var messageResult = JsonSerializer.Deserialize<Envelope>(json);
            var data = messageResult.ReadAs<DummyPayload>();
        }

        public class DummyPayload
        {
            public string MyText { get; set; }
        }
    }

}
