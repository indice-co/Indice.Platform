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
    /// <summary>
    /// Models the data that are sent in an Azure queue, persisting the principal's context.
    /// </summary>
    public abstract class EnvelopeBase
    {
        /// <summary>
        /// User information.
        /// </summary>
        public string User { get; set; }
        /// <summary>
        /// The user id.
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// The user id.
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// The tenant id.
        /// </summary>
        public string TenantId { get; set; }
        /// <summary>
        /// The fully qualified name of the type sent.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Retrieves the principal's context.
        /// </summary>
        public virtual IPrincipal GetUserContext() {
            var claims = new List<Claim>();
            if (!string.IsNullOrEmpty(UserId)) {
                claims.Add(new Claim("sub", UserId.ToString()));
            }
            if (!string.IsNullOrEmpty(ClientId)) {
                claims.Add(new Claim("client_id", ClientId.ToString()));
            }
            if (!string.IsNullOrEmpty(TenantId)) {
                claims.Add(new Claim(BasicClaimTypes.TenantId, TenantId));
            }
            if (!string.IsNullOrEmpty(User)) {
                claims.Add(new Claim("name", User));
            }
            var delegatedUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "WorkerAuth"));
            return delegatedUser;
        }
    }

    /// <summary>
    /// Models the data that are sent in an Azure queue, persisting the principal's context.
    /// </summary>
    /// <typeparam name="T">The type of the payload.</typeparam>
    public class Envelope<T> : EnvelopeBase
    {
        /// <summary>
        /// Creates a new <see cref="Envelope{T}"/> instance.
        /// </summary>
        public Envelope() { }

        /// <summary>
        /// Creates a new <see cref="Envelope{T}"/> instance.
        /// </summary>
        /// <param name="user">The current principal.</param>
        /// <param name="payload">The payload to transmit.</param>
        public Envelope(IPrincipal user, T payload) => Populate(user, payload);

        /// <summary>
        /// The payload to transmit.
        /// </summary>
        public T Payload { get; set; }

        internal Envelope<T> Populate(IPrincipal user, T payload) {
            User = user?.Identity.Name;
            if (user != null) { 
                var claimsPrincipal = new ClaimsPrincipal(user);
                UserId = claimsPrincipal.FindFirst("sub")?.Value;
                ClientId = claimsPrincipal.FindFirst("client_id")?.Value;
                TenantId = claimsPrincipal.FindFirst(BasicClaimTypes.TenantId)?.Value;
            }
            Type = typeof(T).FullName;
            Payload = payload;
            return this;
        }
    }

    /// <summary>
    /// Models the data that are sent in an Azure queue, persisting the principal's context.
    /// </summary>
    public class Envelope : Envelope<JsonElement>
    {
        /// <summary>
        /// Converts the payload to the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ReadAs<T>() => Payload.ToObject<T>(JsonSerializerOptionDefaults.GetDefaultSettings());

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAnonymous"></typeparam>
        /// <param name="instanceDummy"></param>
        /// <returns></returns>
        public TAnonymous ReadAs<TAnonymous>(TAnonymous instanceDummy) => Payload.ToObject<TAnonymous>();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <param name="user"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public static Envelope<TInstance> Create<TInstance>(IPrincipal user, TInstance payload) => new Envelope<TInstance>(user, payload);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <typeparam name="TInstance"></typeparam>
        /// <param name="user"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public static TMessage Create<TMessage, TInstance>(IPrincipal user, TInstance payload) where TMessage : Envelope<TInstance>, new() =>
            new TMessage().Populate(user, payload) as TMessage;
    }
}
