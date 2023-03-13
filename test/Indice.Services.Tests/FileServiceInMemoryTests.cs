using System.Text;
using Indice.Services;
using Xunit;

namespace Indice.Common.Tests;

public class FileServiceInMemoryTests
{
    
    public FileServiceInMemoryTests() {
        FileService = new FileServiceInMemory();
    }

    public FileServiceInMemory FileService { get; }

    [Fact]
    public async Task FileSearchTest() {
        await FileService.SaveAsync("my/attachments/1/file name.txt", Encoding.ASCII.GetBytes("This is the content"));
        await FileService.SaveAsync("my/attachments/2/file2.pdf", Encoding.ASCII.GetBytes("This is the content"));
        await FileService.SaveAsync("my/other-stuff/3/my-data.html", Encoding.ASCII.GetBytes("This is the content"));
        var files = await FileService.SearchAsync("my/attachments");

        Assert.Equal(2, files.Count());
    }
}
