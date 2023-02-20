namespace Indice.Api.JobHandlers;

public class DownloadWebsiteContentJobHandler
{
    private readonly ILogger<DownloadWebsiteContentJobHandler> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public DownloadWebsiteContentJobHandler(
        ILogger<DownloadWebsiteContentJobHandler> logger,
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment webHostEnvironment
    ) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
    }

    public async Task Process(DownloadWebsiteCommand command) {
        _logger.LogDebug("{CommandName} received to download content from website '{Url}'.", nameof(DownloadWebsiteCommand), command.Url);
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(command.Url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var savePath = Path.Combine(_webHostEnvironment.ContentRootPath, "App_Data", $"{command.Url.Replace("https://", string.Empty)}.html");
        await File.WriteAllTextAsync(savePath, content);
    }
}

public record DownloadWebsiteCommand(string Url);
