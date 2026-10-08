namespace Indice.Configuration;

/// <summary>Proxy options.</summary>
public class ProxyOptions
{
    /// <summary>The name is used to mark the section found inside a configuration file.</summary>
    public static readonly string Name = "Proxy";

    /// <summary>Indicates whether proxy is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>The IP address of the proxy server.</summary>
    public string? Ip { get; set; }

    /// <summary>The IP addresses of known proxies.</summary>
    public string[]? KnownProxies { get; set; }

    /// <summary>The list of proxy known networks.</summary>
    public string[]? KnownNetworks { get; set; }

    /// <summary>
    /// Specifies the maximum number of forwarded headers to process from proxied requests.
    /// A value of <c>1</c> (default) means only the first proxy in the chain is trusted.
    /// Set to <c>0</c> to allow an unlimited number of forwarded headers.
    /// </summary>
    public int ForwardLimit { get; set; }
}
