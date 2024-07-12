using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Indice.Localization;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Indice.Common.Tests;
public class StringLocalizerTests
{

    [Fact]
    public void Resx_ResourcesShouldExportAsJsonGraph() { 

        var graph = StringLocalizerTestsTranslations.ResourceManager.ToObjectGraph(new CultureInfo("el"), '.');
        var json = JsonSerializer.Serialize(graph, new JsonSerializerOptions {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        var jsonExpected = "{\"Products\":{\"Listing\":{\"Title\":\"Προιόντα\",\"Name\":\"Γιάννης\",\"DatePicker\":{\"Month\":\"Μήνας\"}}},\"Title\":\"Τίτλος\"}";
        Assert.Equal(jsonExpected, json);
    }

    [Fact]
    public void Resx_ResourcesShouldExportAsJsonGraphDefaultCulture() {

        var graph = StringLocalizerTestsTranslations.ResourceManager.ToObjectGraph(pathDelimiter: '.');
        var json = JsonSerializer.Serialize(graph, new JsonSerializerOptions {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        var jsonExpected = "{\"Products\":{\"Listing\":{\"Title\":\"Products\",\"Name\":\"Giannis\",\"DatePicker\":{\"Month\":\"Month\"}}},\"Title\":\"Title\"}";
        Assert.Equal(jsonExpected, json);
    }


    [Fact]
    
    public void Resx_ResourcesShouldExportEnglishAsJsonGraphForNotExistingCulture() {

        var graph = StringLocalizerTestsTranslations.ResourceManager.ToObjectGraph(new CultureInfo("ru"));
        var json = JsonSerializer.Serialize(graph, new JsonSerializerOptions {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        var jsonExpected = "{\"Products\":{\"Listing\":{\"Title\":\"Products\",\"Name\":\"Giannis\",\"DatePicker\":{\"Month\":\"Month\"}}},\"Title\":\"Title\"}";
        Assert.Equal(jsonExpected, json);
    }
}
