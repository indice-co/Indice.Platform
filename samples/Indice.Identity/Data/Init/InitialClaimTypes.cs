using System;
using System.Collections.Generic;
using IdentityModel;
using Features = Indice.AspNetCore.Identity.Features;

namespace Indice.Identity.Data.Init
{
    /// <summary>
    /// Provides functionality to generate test claim types for development purposes.
    /// </summary>
    public class InitialClaimTypes
    {
        private static readonly List<Features.ClaimType> ClaimTypes = new List<Features.ClaimType> {
            new Features.ClaimType { Id = $"{Guid.NewGuid()}", Name = JwtClaimTypes.BirthDate, Reserved = true, Required = false, UserEditable = false, ValueType = Features.ValueType.DateTime, Description = "End-User's birthday, represented as an ISO 8601:2004 [ISO8601‑2004] YYYY-MM-DD format." },
            new Features.ClaimType { Id = $"{Guid.NewGuid()}", Name = JwtClaimTypes.Email, Reserved = true, Required = false, UserEditable = false, ValueType = Features.ValueType.String, Description = "End-User's preferred e-mail address." },
            new Features.ClaimType { Id = $"{Guid.NewGuid()}", Name = JwtClaimTypes.EmailVerified, Reserved = true, Required = false, UserEditable = false, ValueType = Features.ValueType.Boolean, Description = "'true' if the End-User's e-mail address has been verified; otherwise 'false'." },
            new Features.ClaimType { Id = $"{Guid.NewGuid()}", Name = JwtClaimTypes.FamilyName, Reserved = true, Required = true, DisplayName = "Last Name", UserEditable = false, ValueType = Features.ValueType.String, Description = "Surname(s) or last name(s) of the End-User." },
            new Features.ClaimType { Id = $"{Guid.NewGuid()}", Name = JwtClaimTypes.Gender, Reserved = true, Required = false, UserEditable = false, ValueType = Features.ValueType.String, Description = "End-User's gender." },
            new Features.ClaimType { Id = $"{Guid.NewGuid()}", Name = JwtClaimTypes.GivenName, Reserved = true, Required = true, DisplayName = "First Name", UserEditable = false, ValueType = Features.ValueType.String, Description = "Given name(s) or first name(s) of the End-User." },
            new Features.ClaimType { Id = $"{Guid.NewGuid()}", Name = JwtClaimTypes.MiddleName, Reserved = true, Required = false, UserEditable = false, ValueType = Features.ValueType.String, Description = "Middle name(s) of the End-User." },
            new Features.ClaimType { Id = $"{Guid.NewGuid()}", Name = JwtClaimTypes.Name, Reserved = true, Required = false, UserEditable = false, ValueType = Features.ValueType.String, Description = "End-User's full name in displayable form including all name parts, possibly including titles and suffixes, ordered according to the End-User's locale and preferences." },
            new Features.ClaimType { Id = $"{Guid.NewGuid()}", Name = JwtClaimTypes.NickName, Reserved = true, Required = false, UserEditable = false, ValueType = Features.ValueType.String, Description = "Casual name of the End-User that may or may not be the same as the given_name." },
            new Features.ClaimType { Id = $"{Guid.NewGuid()}", Name = JwtClaimTypes.PhoneNumber, Reserved = true, Required = false, UserEditable = false, ValueType = Features.ValueType.String, Description = "End-User's preferred telephone number" },
            new Features.ClaimType { Id = $"{Guid.NewGuid()}", Name = JwtClaimTypes.PhoneNumberVerified, Reserved = true, Required = false, UserEditable = false, ValueType = Features.ValueType.Boolean, Description = "True if the End-User's phone number has been verified; otherwise false" },
            new Features.ClaimType { Id = $"{Guid.NewGuid()}", Name = JwtClaimTypes.Picture, Reserved = true, Required = false, UserEditable = false, ValueType = Features.ValueType.String, Description = "URL of the End-User's profile picture." },
            new Features.ClaimType { Id = $"{Guid.NewGuid()}", Name = JwtClaimTypes.PreferredUserName, Reserved = true, Required = false, UserEditable = false, ValueType = Features.ValueType.String, Description = "Shorthand name by which the End-User wishes to be referred to at the RP, such as janedoe or j.doe." },
            new Features.ClaimType { Id = $"{Guid.NewGuid()}", Name = JwtClaimTypes.Profile, Reserved = true, Required = false, UserEditable = false, ValueType = Features.ValueType.String, Description = "URL of the End-User's profile page. The contents of this Web page SHOULD be about the End-User." },
            new Features.ClaimType { Id = $"{Guid.NewGuid()}", Name = JwtClaimTypes.Role, Reserved = true, Required = false, UserEditable = false, ValueType = Features.ValueType.String, Description = "The role." },
            new Features.ClaimType { Id = $"{Guid.NewGuid()}", Name = JwtClaimTypes.Subject, Reserved = true, Required = false, UserEditable = false, ValueType = Features.ValueType.String, Description = "Unique Identifier for the End-User at the Issuer." },
            new Features.ClaimType { Id = $"{Guid.NewGuid()}", Name = JwtClaimTypes.WebSite, Reserved = true, Required = false, UserEditable = false, ValueType = Features.ValueType.String, Description = "URL of the End-User's Web page or blog." },
            new Features.ClaimType { Id = $"{Guid.NewGuid()}", Name = JwtClaimTypes.ZoneInfo, Reserved = true, Required = false, UserEditable = false, ValueType = Features.ValueType.String, Description = "String from the time zone database (http://www.twinsun.com/tz/tz-link.htm) representing the End-User's time zone. For example, Europe/Paris or America/Los_Angeles." }
        };

        /// <summary>
        /// Gets a collection of test claim types.
        /// </summary>
        public static IReadOnlyCollection<Features.ClaimType> Get() => ClaimTypes;
    }
}
