using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.PhoneNumberValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Indice.Features.Identity.Tests;

public sealed class PhoneNumberBlacklistProviderTests
{
    private readonly Mock<UserManager<User>> _userManager;

    public PhoneNumberBlacklistProviderTests() {
        var userStore = new Mock<IUserStore<User>>();
        _userManager = new Mock<UserManager<User>>(
            userStore.Object,
            null!, null!, null!, null!, null!, null!, null!, null!
        );
    }

    [Fact]
    public async Task AddPhoneNumberBlacklistValidator_RegistersBothProviders() {
        var services = new ServiceCollection();

        var identityBuilder = services.AddIdentityCore<User>();

        identityBuilder.AddPhoneNumberBlacklistValidator<User>(
            new ConfigurationBuilder().Build(),
            options => {
                options.Enabled = true;
                options.Numbers = "+306912345678";
            });

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var providers = scope.ServiceProvider
            .GetRequiredService<IEnumerable<IPhoneNumberBlacklistProvider>>()
            .ToList();

        Assert.Equal(2, providers.Count);
        Assert.Contains(providers, x => x is ConfigurationPhoneNumberBlacklistProvider);
        Assert.Contains(providers, x => x is CastlePhoneNumberBlacklistProvider);

        var validators = scope.ServiceProvider
            .GetRequiredService<IEnumerable<IUserValidator<User>>>()
            .ToList();

        var validator = validators
            .OfType<PhoneNumberBlacklistValidator<User>>()
            .Single();

        var configuredNumberResult = await validator.ValidateAsync(
            _userManager.Object,
            new User {
                PhoneNumber = "+306912345678"
            });

        Assert.False(configuredNumberResult.Succeeded);

        var castleNumberResult = await validator.ValidateAsync(
            _userManager.Object,
            new User {
                PhoneNumber = "+33753707041"
            });

        Assert.False(castleNumberResult.Succeeded);

        var validNumberResult = await validator.ValidateAsync(
            _userManager.Object,
            new User {
                PhoneNumber = "+306999999999"
            });

        Assert.True(validNumberResult.Succeeded);
    }

    [Fact]
    public void ConfigProvider_ReturnsTrue_WhenNumberIsConfigured() {
        var provider = CreateConfigProvider("+306912345678,+447700900123");

        Assert.True(provider.IsPhoneNumberBlacklisted("+306912345678"));
        Assert.True(provider.IsPhoneNumberBlacklisted("+447700900123"));
    }

    [Fact]
    public void ConfigProvider_ReturnsFalse_WhenNumberIsNotConfigured() {
        var provider = CreateConfigProvider("+306912345678");

        Assert.False(provider.IsPhoneNumberBlacklisted("+306999999999"));
    }

    [Fact]
    public void ConfigProvider_NormalizesConfiguredNumbers() {
        var provider = CreateConfigProvider("00306912345678");

        Assert.True(provider.IsPhoneNumberBlacklisted("+306912345678"));
    }

    [Fact]
    public void ConfigProvider_IgnoresInvalidConfiguredNumbers() {
        var provider = CreateConfigProvider("invalid,+306912345678");

        Assert.True(provider.IsPhoneNumberBlacklisted("+306912345678"));
        Assert.False(provider.IsPhoneNumberBlacklisted("+306999999999"));
    }

    [Fact]
    public void ConfigProvider_HandlesEmptyConfiguration() {
        var provider = CreateConfigProvider(null);

        Assert.False(provider.IsPhoneNumberBlacklisted("+306912345678"));
    }

    [Fact]
    public void CastleProvider_ReturnsTrue_ForNumbersInBlacklist() {
        var provider = new CastlePhoneNumberBlacklistProvider();

        Assert.True(provider.IsPhoneNumberBlacklisted("+33753707041"));
        Assert.True(provider.IsPhoneNumberBlacklisted("+4915905615645"));
        Assert.True(provider.IsPhoneNumberBlacklisted("+447985618978"));
        Assert.True(provider.IsPhoneNumberBlacklisted("+919876543210"));
    }

    [Fact]
    public void CastleProvider_ReturnsFalse_ForNumberNotInBlacklist() {
        var provider = new CastlePhoneNumberBlacklistProvider();

        Assert.False(provider.IsPhoneNumberBlacklisted("+306999999999"));
    }

    [Fact]
    public void CastleProvider_NormalizesInput() {
        var provider = new CastlePhoneNumberBlacklistProvider();

        Assert.False(provider.IsPhoneNumberBlacklisted("0033753707041"));
    }

    private static ConfigurationPhoneNumberBlacklistProvider CreateConfigProvider(string? numbers) {
        return new ConfigurationPhoneNumberBlacklistProvider(
            Options.Create(new PhoneNumberBlacklistOptions {
                Numbers = numbers
            }));
    }
}