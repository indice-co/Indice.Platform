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
    [InlineData("Screenshot 2024-06-13 103846.png", "screenshot-2024-06-13-103846.png")]
    [Theory]
    public void GreekOrUnicodeMustConvertToMeaningfull_ASCII_Test(string input, string expected) {
        
        var output = string.Join('/', input.Split('/', '\\').Select(x => Greeklish.Translate(x).Unidecode().ToKebabCase()));

        Assert.Equal(expected, output);
    }
}
