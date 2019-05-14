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
        [InlineData("https://localhost:44356")]
        public void UrlCompressionTest(string hostName) {
            var base64 = new Base64Host(hostName).ToString();
            var deserializedHostName = Base64Host.Parse(base64).Host;
            Assert.Equal(hostName, deserializedHostName);
        }

        [Theory]
        [InlineData("Aa0ibG9jYWxob3N0", "https://localhost:44322")]
        public void UrlDecompressTest(string compressed, string hostName) {
            var result = Base64Host.Parse(compressed);
            Assert.Equal(hostName, result.Host);
        }
    }
}
