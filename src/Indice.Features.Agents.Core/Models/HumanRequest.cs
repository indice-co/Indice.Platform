using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.Features.Agents.Core.Models;

public class HumanRequest
{
    public string Text { get; set; }
    public string RequestId { get; set; }
    public Dictionary<string, string> Properties { get; set; } = [];
}
public class HumanResponse
{
    public string Text { get; set; }
    public string RequestId { get; set; }
    public Dictionary<string, string> Properties { get; set; } = [];
}
