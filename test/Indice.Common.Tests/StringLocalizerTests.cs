using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Indice.Localization;
using Xunit;

namespace Indice.Common.Tests;
public class StringLocalizerTests
{

    [Fact(Skip = "not ready")]
    public void Resx_ResourcesShouldExportAsJsonGraph() { 

        var graph = StringLocalizerTestsTranslations.ResourceManager.ToObjectGraph(new CultureInfo("el"), '.');
        var json = JsonSerializer.Serialize(graph);
    }
}
