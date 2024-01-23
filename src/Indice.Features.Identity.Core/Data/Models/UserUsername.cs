using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Features.Identity.Core.Data.Models;

/// <summary>
/// Represents a username changelog entry for auditing purposes
/// </summary>
public class UserUsername
{
    /// <summary>Constructs a new instance of <see cref="UserUsername"/> with a new Guid Id.</summary>
    public UserUsername() {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Primary key
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// The Id of the related user
    /// </summary>
    public string UserId { get; set; }
    /// <summary>
    /// Previous username used by the related user
    /// </summary>
    public string PreviousUsername { get; set; }
    /// <summary>
    /// The date of the username change event
    /// </summary>
    public DateTimeOffset DateCreated { get; set; }
}
