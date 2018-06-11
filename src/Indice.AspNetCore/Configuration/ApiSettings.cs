using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.Configuration
{
    public class ApiSettings
    {
        public string ResourceName { get; set; } = "api1";
        public Dictionary<string, string> Scopes { get; set; } = new Dictionary<string, string>();
        public string FriendlyName { get; set; } = "My Api Name";
        public string DefaultVersion { get; set; } = "1";
    }
}
