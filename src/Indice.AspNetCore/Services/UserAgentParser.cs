namespace Indice.AspNetCore.Services;

/// <summary></summary>
public class UserAgentParser
{

}

/// <summary></summary>
public class UserAgentInfo
{
    private UserAgentInfo(string os, string userAgent, string rawUserAgent) {
        Os = os;
        UserAgent = userAgent;
        RawUserAgent = rawUserAgent;
    }

    /// <summary></summary>
    public string Os { get; }
    /// <summary></summary>
    public string UserAgent { get; }
    /// <summary></summary>
    public string RawUserAgent { get; }
}
