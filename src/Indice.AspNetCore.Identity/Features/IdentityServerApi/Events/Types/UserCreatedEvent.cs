using System.Text;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// An event that is raised when a new user is created on IdentityServer.
    /// </summary>
    public class UserCreatedEvent : IIdentityServerApiEvent
    {
        /// <summary>
        /// Creates a new instance of <see cref="UserCreatedEvent"/>.
        /// </summary>
        /// <param name="user"></param>
        public UserCreatedEvent(SingleUserInfo user) => User = user;

        /// <summary>
        /// The instance of the user that was created.
        /// </summary>
        public SingleUserInfo User { get; private set; }

        /// <summary>
        /// Creates a detailed string representation of <see cref="UserCreatedEvent"/>.
        /// </summary>
        public override string ToString() {
            var builder = new StringBuilder();
            builder.AppendLine("A user was created in IdentityServer API.")
                   .AppendLine($"Id: {User.Id}, ")
                   .AppendLine($"UserName: {User.UserName}, ")
                   .AppendLine($"Email: {User.Email}, ")
                   .AppendLine($"EmailConfirmed: {User.EmailConfirmed}, ")
                   .AppendLine($"CreateDate: {User.CreateDate}, ")
                   .AppendLine($"LockoutEnabled: {User.LockoutEnabled}")
                   .AppendLine($"LockoutEnd: {User.LockoutEnd}")
                   .AppendLine($"PhoneNumber: {User.PhoneNumber}")
                   .AppendLine($"PhoneNumberConfirmed: {User.PhoneNumberConfirmed}")
                   .AppendLine($"TwoFactorEnabled: {User.TwoFactorEnabled}");
            return builder.ToString();
        }
    }
}
