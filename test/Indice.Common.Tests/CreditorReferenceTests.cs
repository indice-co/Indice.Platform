using System;
using Indice.Types;
using Xunit;

namespace Indice.Common.Tests
{
    public class CreditorReferenceTests
    {
        [Fact]
        public void FailsWhenReferencesAreInvalid() {
            Assert.Throws<ArgumentException>(() => CreditorReference.Create(null));
            Assert.Throws<ArgumentException>(() => CreditorReference.Create("123456789", "123456789", "123456789"));
        }

        [Theory]
        [InlineData("101", "RF90101")]
        [InlineData("2348236", "RF332348236")]
        [InlineData("KD89LOHDM7837744KI", "RF19KD89LOHDM7837744KI")]
        [InlineData("MMKIDHR737383MLAKSI", "RF78MMKIDHR737383MLAKSI")]
        public void CanGenerateValidCreditorReferenceUsingInput(string reference, string expectedValue) {
            var creditorReference = CreditorReference.Create(reference);
            Assert.True(CreditorReference.IsValid(creditorReference.ElectronicFormat));
            Assert.Equal(expectedValue, creditorReference.ElectronicFormat);
        }

        [Theory]
        [InlineData("RF90 101")]
        [InlineData("RF332348236")]
        [InlineData("RF19KD89LOHDM7837744KI")]
        [InlineData("RF78 MMKI DHR7 3738 3MLA KSI")]
        public void CanIdentifyValidCreditorReferences(string creditorReference) {
            Assert.True(CreditorReference.IsValid(creditorReference));
            var rf = CreditorReference.Parse(creditorReference);
            Assert.True(rf != null);
        }

        [Theory]
        [InlineData("RF91 101")]
        [InlineData("RF342348236")]
        [InlineData("RF20KD89LOHDM7837744KI")]
        [InlineData("RF79 MMKI DHR7 3738 3MLA KSI")]
        [InlineData("RF342%^&348236")]
        public void CanIdentifyInvalidCreditorReferences(string creditorReference) {
            Assert.False(CreditorReference.IsValid(creditorReference));
            Assert.Throws<FormatException>(() => CreditorReference.Parse(creditorReference));
        }
    }
}
