using System;
using System.Collections.Generic;
using System.Text;
using Indice.Types;
using Xunit;

namespace Indice.Common.Tests
{
    public class Base64HostTests
    {
        [Theory]
        [InlineData("https://www.domain.gr")]
        [InlineData("http://www.domain.gr")]
        [InlineData("http://domain.gr")]
        [InlineData("https://www.domain.gr:34")]
        public void UrlCompressionTest(string hostName) {
            var base64 = new Base64Host(hostName).ToString();
            var deserializedHostName = Base64Host.Parse(base64).Host;
            Assert.Equal(hostName, deserializedHostName);
        }
    }
}
