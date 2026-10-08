using FluentValidation.TestHelper;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Features.Messages.Core.Services.Validators;
using Indice.Services;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Indice.Features.Messages.Tests;

public class CampaignRequestValidatorTests
{
    private readonly CreateCampaignRequestValidator _validator;

    public CampaignRequestValidatorTests()
    {
        var serviceProvider = CreateServiceProvider();
        _validator = new CreateCampaignRequestValidator(serviceProvider);
    }

    private static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services
            .AddDbContext<CampaignsDbContext>(builder => builder.UseInMemoryDatabase(databaseName: "CampaignValidatorTestDb"), ServiceLifetime.Singleton)
            .AddSingleton(configuration)
            .AddTransient<IMessageTypeService, MessageTypeService>()
            .AddTransient<IDistributionListService, DistributionListService>()
            .AddTransient<ITemplateService, TemplateService>()
            .AddTransient<IUserNameAccessor, UserNameAccessorNoOp>()
            .AddTransient<UserNameAccessorAggregate>()
            .AddTransient(serviceProvider => new DatabaseSchemaNameResolver("cmp"));

        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("https://example.com/path")]
    [InlineData("https://example.com:8080")]
    [InlineData("https://example.com:8080/path")]
    [InlineData("http://example.com")]
    [InlineData("https://subdomain.example.com")]
    [InlineData("https://tes-mysite.gr")]  // URL with hyphen in domain
    [InlineData("https://my-test-site.com")]  // Multiple hyphens
    [InlineData("https://sub-domain.example.com")]  // Hyphen in subdomain
    [InlineData("https://example.com/path-with-hyphens")]  // Hyphen in path
    [InlineData("https://example.com/path?param=value")]  // Query string
    [InlineData("https://example.com/path?param=value&other=test")]  // Multiple query params
    [InlineData("https://example.com/path#section")]  // Fragment
    [InlineData("https://example.com:443/path?query=value#fragment")]  // Complex URL
    public void ActionLink_Href_ShouldAcceptValidUrls(string url)
    {
        var request = new CreateCampaignRequest
        {
            Title = "Test Campaign",
            ActionLink = new Hyperlink
            {
                Href = url,
                Text = "Click here"
            }
        };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.ActionLink!.Href);
    }

    [Theory]
    [InlineData("ftp://example.com")]  // Wrong protocol
    [InlineData("example.com")]  // Missing protocol
    [InlineData("//example.com")]  // Protocol-relative URL (not absolute)
    [InlineData("not a url")]  // Not a URL
    [InlineData("javascript:alert('xss')")]  // JavaScript protocol (security)
    public void ActionLink_Href_ShouldRejectInvalidUrls(string url)
    {
        var request = new CreateCampaignRequest
        {
            Title = "Test Campaign",
            ActionLink = new Hyperlink
            {
                Href = url,
                Text = "Click here"
            }
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.ActionLink!.Href);
    }

    [Fact]
    public void ActionLink_Href_WhenNull_ShouldNotValidate()
    {
        var request = new CreateCampaignRequest
        {
            Title = "Test Campaign",
            ActionLink = null
        };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.ActionLink!.Href);
    }

    [Fact]
    public void ActionLink_Href_WhenEmpty_ShouldNotValidate()
    {
        var request = new CreateCampaignRequest
        {
            Title = "Test Campaign",
            ActionLink = new Hyperlink
            {
                Href = "",
                Text = "Click here"
            }
        };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.ActionLink!.Href);
    }

    [Fact]
    public void ActionLink_Href_WhenWhitespace_ShouldNotValidate()
    {
        var request = new CreateCampaignRequest
        {
            Title = "Test Campaign",
            ActionLink = new Hyperlink
            {
                Href = "   ",
                Text = "Click here"
            }
        };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.ActionLink!.Href);
    }
}

internal class UserNameAccessorNoOp : IUserNameAccessor
{
    public int Priority => 0;
    public string Resolve() => "static";
}
