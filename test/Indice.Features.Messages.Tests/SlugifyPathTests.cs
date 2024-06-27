using Indice.Extensions;
using Indice.Text;
using Xunit;

namespace Indice.Features.Messages.Tests;
public class SlugifyPathTests
{
    [InlineData("Λάχανο Ελληνικό Ποιότητα Α΄ Τιμή Κιλού", "lachano-elliniko-poiotita-a-timi-kilou")]
    [InlineData("Κρεμμύδια Ξερά Χοντρά Τιμή Κιλού", "kremmydia-xera-chontra-timi-kilou")]
    [InlineData("Μαναβικη ", "manaviki")]
    [InlineData("Μαναβικη/Λαχανικά", "manaviki/lachanika")]
    [InlineData("Μαναβικη/Λαχανικά/Καρότα, Ραπανάκια, Τζίντζερ", "manaviki/lachanika/karota-rapanakia-tzintzer")]
    [Theory]
    public void GreekOrUnicodeMustConvertToMeaningfull_ASCII_Test(string input, string expected) {
        
        var output = Greeklish.Translate(input).Unidecode().ToKebabCase();

        Assert.Equal(expected, output);
    }
}
