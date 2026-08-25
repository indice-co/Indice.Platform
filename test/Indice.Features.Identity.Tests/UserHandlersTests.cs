using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Duende.IdentityModel;
using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Server.Manager;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Globalization;
using Indice.Types;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Indice.Features.Identity.Tests;
public class UserHandlersTests : IAsyncLifetime
{
    private readonly ServiceProvider _serviceProvider;

    public UserHandlersTests() {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["ConnectionStrings:TestDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database=Indice.FilterClause.Test_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true",
            ["IdentityOptions:Password:PasswordHistoryLimit"] = "1",
        }).Build();
        var services = new ServiceCollection();
        // configure dependencies
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<ExtendedIdentityDbContext<User, Role>>(builder => builder.UseSqlServer(configuration.GetConnectionString("TestDb")));
        services.AddIdentity<User, Role>()
                   .AddExtendedUserManager()
                   .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>()
                   .AddExtendedSignInManager()
                   .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
                   .AddExtendedPhoneNumberTokenProvider(configuration)
                   .AddIdentityMessageDescriber();
        services.AddLogging();
        services.AddLocalization();
        services.AddDefaultPlatformEventService();
        services.AddPlatformEventHandler<UserCreatedEvent, UserCreatedAssetionHanbdler>();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task CreateUserHandler_ShouldEmit_UserCreatedEvent_WithPopulatedClaims_Test() {

        var userManager = _serviceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var identityDbContext = _serviceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();

        //seed database
        identityDbContext.Roles.Add(new Role("Developer") { NormalizedName = "DEVELOPER" });
        await identityDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // execute
        _ = await UserHandlers.CreateUser(userManager, identityDbContext, new Server.Manager.Models.CreateUserRequest {
            UserName = "john.doe@indice.gr",
            Email = "john.doe@indice.gr",
            Password = "password",
            BypassPasswordValidation = true,
            FirstName = "John",
            LastName = "Doe",
            Claims = [
                new() { Type = "customer_code", Value = "000001" },
                new() { Type = "locale", Value = "el" }
            ],
            Roles = ["Developer"]
        });
        Assert.True(true);
    }

    [Fact]
    public async Task GetUsers_Test() {

        var userManager = _serviceProvider.GetRequiredService<ExtendedUserManager<User>>();
        //seed database
        var identityDbContext = _serviceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();

        //seed database
        identityDbContext.Roles.Add(new Role("Developer") { NormalizedName = "DEVELOPER" });
        await identityDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // execute
        _ = await UserHandlers.CreateUser(userManager, identityDbContext, new Server.Manager.Models.CreateUserRequest {
            UserName = "john.doe@indice.gr",
            Email = "john.doe@indice.gr",
            Password = "password",
            BypassPasswordValidation = true,
            FirstName = "John",
            LastName = "Doe",
            Claims = [
                new() { Type = "customer_code", Value = "000001" },
                new() { Type = "locale", Value = "el" }
            ],
            Roles = ["Developer"]
        });

        ListOptions options = new() {
            Page = 1,
            Size = 10
        };
        UserListFilter filter = new();
        // execute
        _ = await UserHandlers.GetUsers(identityDbContext, options, filter, ["locale", "customer_code"]);
        Assert.True(true);
    }


    [Fact]
    public void CreateUserDevice_ConvertDbPublicKeyToJsonWebKey_Test() {
        const string publicRsaKey = """
            -----BEGIN PUBLIC KEY-----            
            MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEA49JI4i5PBbl02coZnWao
            jBa5ToQTZEXthKBMmuSXCpDhaNZEBnkbmF0J0xOTZ9BOQlen2TVAdC8inK8DqZC5
            GH+TuIjVnZf92XqIXxjCP4LmNaAQtolmW5VnYUYuJ4XDunand2cney0YiQ3uDpEW
            OWDzg3NiMgMDcdvdy7lFFQ9ajD1HtX+11EVvyafK5yZD0evwJ83T91seSHgpEWM/
            5riD5KxsrVW4Jwjz4XDge5GKuS7B12I7OpLl/pW2cRUtsQa9T7j3vrr3S2GJU52w
            ypKymT1r2VafxNpXFzSC3n2MRVh6ubmyZGpbCux6h/4GmvYcU6nE9jL1g23kU/Vi
            gcn1jyf7m+5oNnmaWw0MgT57/QbSf+RnLn/TN+y+Isdm+gGydedLKvZ01IgZe02f
            /X0cFMjSb+whhoXGPz2bOZtrai2IJmHnLzbVHrz4CnCzbMws6fJhJJC88DNvLd54
            8v6foGI2ZjizLEdBYlJEi03eaiVCf0I6J8hUyhXCiLHTBL/kYg0PbUaMlRJE2fny
            KYDBiQa6Iin7HbpccSi3834hjvpe4XyZYp6HEH6uBccydQov54LquhjA9XJJKAr/
            419p1S/ycxFJtTIMCdZHs/6/Tc3AEw9qho4bqeNrzon7Ooq2LY05AkfI8J95u/eo
            RtVSF5JEQj+t+21jPrv0W9cCAwEAAQ==
            -----END PUBLIC KEY-----
            """;

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicRsaKey.ToCharArray());

        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(new RsaSecurityKey(rsa) {
            KeyId = CryptoRandom.CreateUniqueId(16, CryptoRandom.OutputFormat.Hex)
        });

        Assert.NotNull(jwk);
    }

    [Fact]
    public void CreateUserDevice_ConvertInstallationIdToJsonWebKey_Test() {
        var installationId = "47a9cc78-d5ce-405b-af10-57c7efe45e97";        

        var jwk = JsonWebKeyConverter.ConvertFromSymmetricSecurityKey(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(installationId)) {
            KeyId = CryptoRandom.CreateUniqueId(16, CryptoRandom.OutputFormat.Hex) + " symmetric"
        });

        Assert.NotNull(jwk);
    }

    public async ValueTask InitializeAsync() {
        var dbContext = _serviceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync() {
        var dbContext = _serviceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();
        await dbContext.Database.EnsureDeletedAsync();
        await _serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task UpdateUser_WithNullPhoneNumber_ShouldClearPhoneNumber() {
        var userManager = _serviceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var identityDbContext = _serviceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();

        // Create a user first
        await UserHandlers.CreateUser(userManager, identityDbContext, new CreateUserRequest {
            UserName = "test.user@indice.gr",
            Email = "test.user@indice.gr",
            Password = "password",
            BypassPasswordValidation = true,
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+306912345678",
            Claims = [
                new() { Type = "customer_code", Value = "000001" },
                new() { Type = "locale", Value = "el" }
            ],
        });

        var createdUser = await identityDbContext.Users.FirstOrDefaultAsync(u => u.Email == "test.user@indice.gr", cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(createdUser);
        Assert.NotNull(createdUser.PhoneNumber); // Ensure it starts with a phone number

        // Update user with null phone number
        var result = await UserHandlers.UpdateUser(identityDbContext, userManager, createdUser.Id, new UpdateUserRequest {
            UserName = "test.user@indice.gr",
            Email = "test.user@indice.gr",
            PhoneNumber = null,
            PhoneNumberConfirmed = false,
            Claims = [
                new() { Type = "customer_code", Value = "000001" },
                new() { Type = "locale", Value = "el" }
            ],
        });
        Assert.IsType<Ok<SingleUserInfo>>(result.Result);

        // Verify phone number was cleared
        var updatedUser = await identityDbContext.Users.FirstOrDefaultAsync(u => u.Id == createdUser.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(updatedUser);
        Assert.Null(updatedUser.PhoneNumber);
    }

    [Fact]
    public async Task UpdateUser_WithWhitespaceOnlyPhoneNumber_ShouldClearPhoneNumber() {
        var userManager = _serviceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var identityDbContext = _serviceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();

        // Create a user first
        await UserHandlers.CreateUser(userManager, identityDbContext, new CreateUserRequest {
            UserName = "test.user2@indice.gr",
            Email = "test.user2@indice.gr",
            Password = "password",
            BypassPasswordValidation = true,
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+306912345678",
            Claims = [
                new() { Type = "customer_code", Value = "000001" },
                new() { Type = "locale", Value = "el" }
            ],
        });

        var createdUser = await identityDbContext.Users.FirstOrDefaultAsync(u => u.Email == "test.user2@indice.gr", cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(createdUser);
        Assert.NotNull(createdUser.PhoneNumber); // Ensure it starts with a phone number

        // Update user with whitespace-only phone number
        var result = await UserHandlers.UpdateUser(identityDbContext, userManager, createdUser.Id, new UpdateUserRequest {
            UserName = "test.user2@indice.gr",
            Email = "test.user2@indice.gr",
            PhoneNumber = "   ",
            PhoneNumberConfirmed = false
        });
        Assert.IsType<Ok<SingleUserInfo>>(result.Result);

        // Verify phone number was cleared
        var updatedUser = await identityDbContext.Users.FirstOrDefaultAsync(u => u.Id == createdUser.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(updatedUser);
        Assert.Null(updatedUser.PhoneNumber);
    }

    [Fact]
    public async Task UpdateUser_WithInvalidPhoneNumber_ShouldReturnValidationProblem() {
        var userManager = _serviceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var identityDbContext = _serviceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();

        // Create a user first
        await UserHandlers.CreateUser(userManager, identityDbContext, new CreateUserRequest {
            UserName = "test.user3@indice.gr",
            Email = "test.user3@indice.gr",
            Password = "password",
            BypassPasswordValidation = true,
            FirstName = "Test",
            LastName = "User",
            Claims = [
                new() { Type = "customer_code", Value = "000001" },
                new() { Type = "locale", Value = "el" }
            ],
        });

        var createdUser = await identityDbContext.Users.FirstOrDefaultAsync(u => u.Email == "test.user3@indice.gr", cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(createdUser);

        // Update user with invalid phone number
        var result = await UserHandlers.UpdateUser(identityDbContext, userManager, createdUser.Id, new UpdateUserRequest {
            UserName = "test.user3@indice.gr",
            Email = "test.user3@indice.gr",
            PhoneNumber = "invalid-phone"
        });

        // Verify validation error
        var validationProblem = result.Result as Microsoft.AspNetCore.Http.HttpResults.ValidationProblem;
        Assert.NotNull(validationProblem);
    }

    [Fact]
    public async Task UpdateUser_WithValidPhoneNumberAndWhitespace_ShouldPersistFormattedValue() {
        var userManager = _serviceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var identityDbContext = _serviceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();

        // Create a user first
        await UserHandlers.CreateUser(userManager, identityDbContext, new CreateUserRequest {
            UserName = "test.user4@indice.gr",
            Email = "test.user4@indice.gr",
            Password = "password",
            BypassPasswordValidation = true,
            FirstName = "Test",
            LastName = "User",
            Claims = [
                new() { Type = "customer_code", Value = "000001" },
                new() { Type = "locale", Value = "el" }
            ],
        });

        var createdUser = await identityDbContext.Users.FirstOrDefaultAsync(u => u.Email == "test.user4@indice.gr", cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(createdUser);

        // Update user with valid phone number that has surrounding whitespace
        var result = await UserHandlers.UpdateUser(identityDbContext, userManager, createdUser.Id, new UpdateUserRequest {
            UserName = "test.user4@indice.gr",
            Email = "test.user4@indice.gr",
            PhoneNumber = "  +306912345678  "
        });
        Assert.IsType<Ok<SingleUserInfo>>(result.Result);

        // Verify phone number was formatted and stored
        var updatedUser = await identityDbContext.Users.FirstOrDefaultAsync(u => u.Id == createdUser.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(updatedUser);
        Assert.NotNull(updatedUser.PhoneNumber);
        Assert.Equal(PhoneNumber.Parse("+306912345678").ToString(), updatedUser.PhoneNumber); // Should be trimmed/formatted
        Assert.StartsWith("+", updatedUser.PhoneNumber); // Should be a valid formatted phone number
    }

    [Fact]
    public async Task RemoveTempUserPassword() {
        var userManager = _serviceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var identityDbContext = _serviceProvider.GetRequiredService<ExtendedIdentityDbContext<User, Role>>();
        var configuration = _serviceProvider.GetRequiredService<IConfiguration>();
        var currentUser = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new ("sub", Guid.NewGuid().ToString()), 
                new ("client_id", "test-client-id"),
                new ("admin", "true")
            ], "TestAuthentication"));
        // Create a user first
        await UserHandlers.CreateUser(userManager, identityDbContext, new CreateUserRequest {
            UserName = "test.user5@indice.gr",
            Email = "test.user5@indice.gr",
            Password = "password",
            BypassPasswordValidation = true,
            FirstName = "Test",
            LastName = "User",
            Claims = [
                new() { Type = "customer_code", Value = "000001" },
                new() { Type = "locale", Value = "el" }
            ],
        });
        var createdUser = await identityDbContext.Users.FirstOrDefaultAsync(u => u.Email == "test.user5@indice.gr", cancellationToken: TestContext.Current.CancellationToken);
        var orgiginalPasswordHash = createdUser?.PasswordHash;
        Assert.NotNull(createdUser);

        // Remove users password via the api
        var result = await UserHandlers.RemovePassword(userManager, currentUser, createdUser.Id);
        Assert.IsType<NoContent>(result.Result);

        // Verify the user has null password hash stored in database
        var updatedUser = await identityDbContext.Users.FirstOrDefaultAsync(u => u.Id == createdUser.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(updatedUser);
        Assert.Null(updatedUser.PasswordHash);

        // check password history after removal
        if (configuration.GetIdentityOption<int?>("Password", "PasswordHistoryLimit").GetValueOrDefault() > 0) {
            bool isOriginalPasswordInHistory = await identityDbContext.UserPasswordHistory.AnyAsync(u => u.UserId == createdUser.Id && u.PasswordHash == orgiginalPasswordHash, cancellationToken: TestContext.Current.CancellationToken);
            Assert.True(isOriginalPasswordInHistory, "Password removed should be in userPassword history");
        }

        // Verify user cannot login with an empty/null password and he is essentially passwordless
        var validPassword = await userManager.CheckPasswordAsync(updatedUser, null!);
        Assert.False(validPassword);

        // Verify that trying to remove password again does not cause any issues and returns no content
        var removeSecondTimeResult = await UserHandlers.RemovePassword(userManager, currentUser, updatedUser.Id);
        Assert.IsType<NoContent>(removeSecondTimeResult.Result);

        var self = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new ("sub", createdUser.Id),
                new ("client_id", "test-client-id"),
            ], "TestAuthentication"));

        // Verify that trying to remove password for self returns bad request since user cannot remove his own password.
        var selfRemovePasswordResult = await UserHandlers.RemovePassword(userManager, self, updatedUser.Id);
        Assert.IsType<ValidationProblem>(selfRemovePasswordResult.Result);
    }

    public class UserCreatedAssetionHanbdler : IPlatformEventHandler<UserCreatedEvent>
    {
        public Task Handle(UserCreatedEvent @event, PlatformEventArgs args) {
            args.ThrowOnError = true;
            Assert.Equal(4, @event.User.Claims.Count);
            return Task.CompletedTask;
        }
    }
}