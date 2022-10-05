using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Json;
using Indice.Extensions;
using Indice.Security;
using Indice.Serialization;

namespace Indice.Types
{
    /// <summary>Models the data that are sent in an Azure queue, persisting the principal's context.</summary>
    public abstract class EnvelopeBase
    {
        /// <summary>User information.</summary>
        public string User { get; set; }
        /// <summary>The user id.</summary>
        public string UserId { get; set; }
        /// <summary>The client id.</summary>
        public string ClientId { get; set; }
        /// <summary>The tenant id.</summary>
        public string TenantId { get; set; }
        /// <summary>The fully qualified name of the type sent.</summary>
        public string Type { get; set; }

        /// <summary>Retrieves the principal's context.</summary>
        public virtual IPrincipal GetUserContext() {
            var claims = new List<Claim>();
            if (!string.IsNullOrEmpty(UserId)) {
                claims.Add(new Claim("sub", UserId.ToString()));
            }
            if (!string.IsNullOrEmpty(ClientId)) {
                claims.Add(new Claim("client_id", ClientId.ToString()));
            }
            if (!string.IsNullOrWhiteSpace(TenantId)) {
                claims.Add(new Claim(BasicClaimTypes.TenantId, TenantId));
            }
            if (!string.IsNullOrEmpty(User)) {
                claims.Add(new Claim("name", User));
            }
            var delegatedUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "WorkerAuth"));
            return delegatedUser;
        }
    }

    /// <summary>Models the data that are sent in an Azure queue, persisting the principal's context.</summary>
    /// <typeparam name="T">The type of the payload.</typeparam>
    public class Envelope<T> : EnvelopeBase
    {
        /// <summary>Creates a new <see cref="Envelope{T}"/> instance.</summary>
        public Envelope() { }

        /// <summary>Creates a new <see cref="Envelope{T}"/> instance.</summary>
        /// <param name="user">The current principal.</param>
        /// <param name="payload">The payload to transmit.</param>
        /// <param name="tenantId">The tenant id (optional).</param>
        public Envelope(IPrincipal user, T payload, string tenantId = null) => Populate(user, payload, tenantId);

        /// <summary>The payload to transmit.</summary>
        public T Payload { get; set; }

        internal Envelope<T> Populate(IPrincipal user, T payload, string tenantId) {
            User = user?.Identity.Name;
            if (user != null) { 
                var claimsPrincipal = new ClaimsPrincipal(user);
                UserId = claimsPrincipal.FindFirst(BasicClaimTypes.Subject)?.Value;
                ClientId = claimsPrincipal.FindFirst(BasicClaimTypes.ClientId)?.Value;
                TenantId = claimsPrincipal.FindFirst(BasicClaimTypes.TenantId)?.Value;
            }
            TenantId = tenantId;
            Type = typeof(T).FullName;
            Payload = payload;
            return this;
        }
    }

    /// <summary>Models the data that are sent in an Azure queue, persisting the principal's context.</summary>
    public class Envelope : Envelope<JsonElement>
    {
        /// <summary>Reads the payload to the specified type, using the provided <paramref name="jsonSerializerOptions"/>.</summary>
        /// <param name="jsonSerializerOptions">Provides options to be used with <see cref="JsonSerializer"/>.</param>
        /// <typeparam name="T">The type of payload.</typeparam>
        public T ReadAs<T>(JsonSerializerOptions jsonSerializerOptions) => Payload.ToObject<T>(jsonSerializerOptions);

        /// <summary>Reads the payload to the specified type, using the <see cref="JsonSerializerOptionDefaults.GetDefaultSettings()"/> serializer options.</summary>
        /// <typeparam name="T">The type of payload.</typeparam>
        public T ReadAs<T>() => ReadAs<T>(JsonSerializerOptionDefaults.GetDefaultSettings());

        /// <summary>Creates a new <see cref="Envelope{T}"/> given the parameters.</summary>
        /// <typeparam name="TPayload">The type of payload.</typeparam>
        /// <param name="user">The current principal.</param>
        /// <param name="payload">The payload to transmit.</param>
        /// <param name="tenantId">The tenant id (optional).</param>
        public static Envelope<TPayload> Create<TPayload>(IPrincipal user, TPayload payload, string tenantId = null) => new(user, payload, tenantId);

        /// <summary>Creates a new <see cref="Envelope{T}"/> given the parameters.</summary>
        /// <typeparam name="TEnvelope">The type of envelope.</typeparam>
        /// <typeparam name="TPayload">The type of payload.</typeparam>
        /// <param name="user">The current principal.</param>
        /// <param name="payload">The payload to transmit.</param>
        /// <param name="tenantId">The tenant id (optional).</param>
        public static TEnvelope Create<TEnvelope, TPayload>(IPrincipal user, TPayload payload, string tenantId = null) where TEnvelope : Envelope<TPayload>, new() =>
            new TEnvelope().Populate(user, payload, tenantId) as TEnvelope;
    }
}
