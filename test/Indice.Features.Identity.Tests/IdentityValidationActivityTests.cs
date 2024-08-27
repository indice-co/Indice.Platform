using System;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.IdentityValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Indice.Features.Identity.Tests;
//https://andrewlock.net/simplifying-theory-test-data-with-xunit-combinatorial/
public class IdentityValidationActivityTests : IAsyncLifetime
{
    public IdentityValidationActivityTests() {
        var inMemorySettings = new Dictionary<string, string> {
            ["IdentityOptions:SignIn:RequirePostSignInConfirmedEmail"] = "true",
            ["IdentityOptions:SignIn:RequirePostSignInConfirmedPhoneNumber"] = "true",
            ["IdentityOptions:SignIn:Mfa:Policy"] = "Default",
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        var services = new ServiceCollection()
            .AddSingleton(configuration)
            .AddLogging();
        ServiceProvider = services.BuildServiceProvider();
    }
    public ServiceProvider ServiceProvider { get; }

    [Fact]
    public async Task IdentityValidationFlowTest() {

        var httpContext = new DefaultHttpContext {
            RequestServices = ServiceProvider
        };

        var pipeline = new RequiresMfaOnboardingActivity() {
            Next = new RequiresMfaActivity() {
                Next = new RequiresPasswordChangeActivity() {
                    Next = new RequiresEmailVerificationActivity() {
                        Next = new RequiresPhoneNumberVerificationActivity() { 
                        
                        }
                    }
                }
            }
        };

        var user = new Core.Data.Models.User() {
            UserName = "someone@indice.gr",
            Email = "someone@indice.gr",
            EmailConfirmed = false,
            PhoneNumber = "+30 6900000000",
            PhoneNumberConfirmed = false,
            PasswordExpirationPolicy = Core.Data.Models.PasswordExpirationPolicy.Never,
            PasswordExpirationDate = null,
            PasswordExpired = false,
        };

        var request = new IdentityValidationActivityContext(user, httpContext);
        await pipeline.HandleAsync(request);

        Assert.Equal(UserState.RequiresEmailVerification, request.Result.UserState);
    }

    public Task InitializeAsync() {
        return Task.CompletedTask;
    }
    public async Task DisposeAsync() {
        await ServiceProvider.DisposeAsync();
    }
}
