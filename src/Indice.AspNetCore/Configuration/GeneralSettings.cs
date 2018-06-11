using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.Configuration
{
    public class GeneralSettings
    {
        public static readonly string Name = "General";
        public string Host { get; set; }
        public string Authority { get; set; }
        public string ApplicationName { get; set; } = "My App name";
        public string ApplicationDescription { get; set; } = "My App does this and that.";
        public ClientSettings Client { get; set; }
        public ApiSettings Api { get; set; }
        public bool EnableSwagger { get; set; }
    }
}
