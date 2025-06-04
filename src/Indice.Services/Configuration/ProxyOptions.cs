namespace Indice.Configuration;

/// <summary>Proxy options.</summary>
public class ProxyOptions
{
    /// <summary>The name is used to mark the section found inside a configuration file.</summary>
    public static readonly string Name = "Proxy";
    /// <summary>The IP address of the proxy server.</summary>
    public string? Ip { get; set; }
    /// <summary>Indicates whether proxy is enabled.</summary>
    public bool Enabled { get; set; }
}
