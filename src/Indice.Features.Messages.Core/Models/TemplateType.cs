using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Features.Messages.Core.Models;

/// <summary>
/// Represents the type of a template.
/// </summary>
public enum TemplateType : byte
{
    /// <summary>
    /// The template is a full template.
    /// </summary>
    Full = 0,
    /// <summary>
    /// The template is partial.
    /// </summary>
    Partial = 1
}
