namespace CodeFlowInlineFrame.Settings;

public class ClientSettings
{
    public const string Name = "Client";

    public string Id { get; set; }
    public string Secret { get; set; }
    public IEnumerable<string> Scopes { get; set; }
}
