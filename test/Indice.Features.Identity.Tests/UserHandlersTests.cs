#if NET8_0_OR_GREATER
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Indice.Features.Identity.Core.Data.Models;
using Indice.AspNetCore.Identity.Api.Models;
using Indice.Features.Identity.Core;
using Indice.Events;
using Indice.Features.Identity.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Collections.Specialized;
using System.Text;
using System.Text.Json;
using Indice.Features.Media.AspNetCore.Models;
using Indice.Features.Media.AspNetCore.Models.Requests;
using Indice.Features.Media.Data;
using Indice.Features.Messages.Tests.Security;
using Indice.Serialization;
using Indice.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

public class UserHandlersTests : IAsyncLifetime
{
    // Constants
    private const string BASE_URL = "https://server";
    // Private fields
    private readonly HttpClient _httpClient;
    private readonly ITestOutputHelper _output;
    private ServiceProvider _serviceProvider;

    public UserHandlersTests(ITestOutputHelper output) {
        _output = output;
        var builder = new WebHostBuilder();
        builder.ConfigureAppConfiguration(builder => {
            builder.AddInMemoryCollection(new Dictionary<string, string> {
                ["ConnectionStrings:MessagesDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database=MessagesDb.Test_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true",
                ["ConnectionStrings:StorageConnection"] = "UseDevelopmentStorage=true",
                ["General:Host"] = "https://server"
            });
        });
        builder.ConfigureServices(services => {
            var configuration = services.BuildServiceProvider().GetService<IConfiguration>();
            services.AddRouting();
          
            _serviceProvider = services.BuildServiceProvider();
        });
        builder.Configure(app => {
            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(e => {
                //e.Map();
            });
        });
        var server = new TestServer(builder);
        var handler = server.CreateHandler();
        _httpClient = new HttpClient(handler) {
            BaseAddress = new Uri(BASE_URL)
        };
    }


    [Fact]
    public async Task CreateUser_ShouldReturnCreatedAtRoute_WhenUserIsCreatedSuccessfully() {
        // Arrange
        var mockUserStore = new Mock<IUserStore<User>>();
        var mockOptions = new Mock<IOptionsSnapshot<IdentityOptions>>();
        var mockPasswordHasher = new Mock<IPasswordHasher<User>>();
        var mockUserValidators = new List<IUserValidator<User>>();
        var mockPasswordValidators = new List<IPasswordValidator<User>>();
        var mockKeyNormalizer = new Mock<ILookupNormalizer>();
        var mockErrorDescriber = new Mock<IdentityErrorDescriber>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<ExtendedUserManager<User>>>();
        var mockEventService = new Mock<IPlatformEventService>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockUserStateProvider = new Mock<IUserStateProvider<User>>();

        var mockDbContext = new Mock<ExtendedIdentityDbContext<User, Role>>();

        // Set up IdentityOptions
        var identityOptions = new IdentityOptions();
        mockOptions.Setup(o => o.Value).Returns(identityOptions);

        // Set up roles in the context
        var roles = new List<Role> { new Role { Name = "Admin", NormalizedName = "ADMIN" } };
        mockDbContext.Setup(db => db.Roles.ToListAsync()).ReturnsAsync(roles);

        // Set up user manager
        var userManager = new ExtendedUserManager<User>(
            mockUserStore.Object,
            mockOptions.Object,
            mockPasswordHasher.Object,
            mockUserValidators,
            mockPasswordValidators,
            mockKeyNormalizer.Object,
            mockErrorDescriber.Object,
            mockServiceProvider.Object,
            mockLogger.Object,
            new IdentityMessageDescriber(), // Assuming it's ok to instantiate
            mockEventService.Object,
            mockConfiguration.Object,
            mockUserStateProvider.Object
        );

        var userRequest = new CreateUserRequest {
            Email = "test@test.com",
            Password = "Password123!",
            Roles = new List<string> { "Admin" },
            Claims = new List<BasicClaimInfo> {
                new BasicClaimInfo { Type = JwtClaimTypes.GivenName, Value = "John" },
                new BasicClaimInfo { Type = JwtClaimTypes.FamilyName, Value = "Doe" }
            }
        };

        // Act
        var result = await UserHandlers.CreateUser(userManager, mockDbContext.Object, userRequest);

        // Assert
        var createdAtRouteResult = Assert.IsType<CreatedAtRoute<SingleUserInfo>>(result.Value);
        Assert.Equal("test@test.com", createdAtRouteResult.Value.Email);
    }

    public Task DisposeAsync() {
        throw new NotImplementedException();
    }

    public Task InitializeAsync() {
        throw new NotImplementedException();
    }
}
#endif