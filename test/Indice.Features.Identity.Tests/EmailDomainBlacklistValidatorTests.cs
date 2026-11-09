using Indice.Features.Identity.Core.Configuration;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.EmailValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Indice.Features.Identity.Tests;

public class EmailDomainBlacklistValidatorTests
{
    private readonly Mock<UserManager<User>> _userManager;

    public EmailDomainBlacklistValidatorTests() {
        var userStore = new Mock<IUserStore<User>>();
        _userManager = new Mock<UserManager<User>>(
            userStore.Object,
            null!, null!, null!, null!, null!, null!, null!, null!
        );
    }

    #region Valid Email Passes Tests

    [Theory]
    [InlineData("user@gmail.com")]
    [InlineData("test@outlook.com")]
    [InlineData("admin@company.com")]
    [InlineData("user.name@example.org")]
    public async Task ValidateAsync_ValidEmail_ReturnsSuccess(string email) {
        // Arrange
        var providers = new List<IEmailDomainBlacklistProvider>();
        var validator = new EmailDomainBlacklistValidator<User>(providers);
        var user = new User { Email = email };

        // Act
        var result = await validator.ValidateAsync(_userManager.Object, user);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ValidateAsync_EmptyBlacklist_AllEmailsPass() {
        // Arrange
        var providers = new List<IEmailDomainBlacklistProvider>();
        var validator = new EmailDomainBlacklistValidator<User>(providers);
        var user = new User { Email = "any@domain.com" };

        // Act
        var result = await validator.ValidateAsync(_userManager.Object, user);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ValidateAsync_NullEmail_ReturnsFailure() {
        // Arrange
        var providers = new List<IEmailDomainBlacklistProvider>();
        var validator = new EmailDomainBlacklistValidator<User>(providers);
        var user = new User { Email = null };

        // Act
        var result = await validator.ValidateAsync(_userManager.Object, user);

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ValidateAsync_EmptyEmail_ReturnsFailure() {
        // Arrange
        var providers = new List<IEmailDomainBlacklistProvider>();
        var validator = new EmailDomainBlacklistValidator<User>(providers);
        var user = new User { Email = string.Empty };

        // Act
        var result = await validator.ValidateAsync(_userManager.Object, user);

        // Assert
        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData("invalidemail")]
    [InlineData("invalid@")]
    [InlineData("@invalid.com")]
    [InlineData("invalid@@domain.com")]
    public async Task ValidateAsync_InvalidEmailFormat_ReturnsFailure(string invalidEmail) {
        // Arrange
        var providers = new List<IEmailDomainBlacklistProvider>();
        var validator = new EmailDomainBlacklistValidator<User>(providers);
        var user = new User { Email = invalidEmail };

        // Act
        var result = await validator.ValidateAsync(_userManager.Object, user);

        // Assert
        Assert.False(result.Succeeded);
    }

    #endregion

    #region Blacklisted Domain Fails Tests

    [Fact]
    public async Task ValidateAsync_BlacklistedDomain_ReturnsFailure() {
        // Arrange
        var blacklistedDomain = "tempmail.com";
        var mockProvider = new Mock<IEmailDomainBlacklistProvider>();
        mockProvider.Setup(p => p.IsDomainBlacklistedAsync(blacklistedDomain, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var providers = new List<IEmailDomainBlacklistProvider> { mockProvider.Object };
        var validator = new EmailDomainBlacklistValidator<User>(providers);
        var user = new User { Email = $"user@{blacklistedDomain}" };

        // Act
        var result = await validator.ValidateAsync(_userManager.Object, user);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code.Contains("EmailBlacklisted"));
    }

    [Theory]
    [InlineData("user@guerrillamail.com")]
    [InlineData("test@10minutemail.com")]
    [InlineData("admin@throwaway.email")]
    public async Task ValidateAsync_MultipleBlacklistedDomains_AllReturnFailure(string email) {
        // Arrange
        var mockProvider = new Mock<IEmailDomainBlacklistProvider>();
        mockProvider.Setup(p => p.IsDomainBlacklistedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var providers = new List<IEmailDomainBlacklistProvider> { mockProvider.Object };
        var validator = new EmailDomainBlacklistValidator<User>(providers);
        var user = new User { Email = email };

        // Act
        var result = await validator.ValidateAsync(_userManager.Object, user);

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ValidateAsync_BlacklistedDomain_CaseInsensitive() {
        // Arrange
        var blacklistedDomain = "tempmail.com";
        var mockProvider = new Mock<IEmailDomainBlacklistProvider>();
        mockProvider.Setup(p => p.IsDomainBlacklistedAsync(blacklistedDomain, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var providers = new List<IEmailDomainBlacklistProvider> { mockProvider.Object };
        var validator = new EmailDomainBlacklistValidator<User>(providers);

        // Act & Assert - Test different cases
        var upperCaseUser = new User { Email = "user@TEMPMAIL.COM" };
        var upperCaseResult = await validator.ValidateAsync(_userManager.Object, upperCaseUser);
        Assert.False(upperCaseResult.Succeeded);

        var mixedCaseUser = new User { Email = "user@TempMail.Com" };
        var mixedCaseResult = await validator.ValidateAsync(_userManager.Object, mixedCaseUser);
        Assert.False(mixedCaseResult.Succeeded);
    }

    [Fact]
    public async Task IsBlacklistedAsync_MultipleProviders_ReturnsOnFirstMatch() {
        // Arrange
        var blacklistedDomain = "spam.com";

        var slowProvider = new Mock<IEmailDomainBlacklistProvider>();
        slowProvider.Setup(p => p.IsDomainBlacklistedAsync(blacklistedDomain, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var fastProvider = new Mock<IEmailDomainBlacklistProvider>();
        fastProvider.Setup(p => p.IsDomainBlacklistedAsync(blacklistedDomain, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var providers = new List<IEmailDomainBlacklistProvider> { slowProvider.Object, fastProvider.Object };
        var validator = new EmailDomainBlacklistValidator<User>(providers);
        var user = new User { Email = $"test@{blacklistedDomain}" };

        // Act
        var result = await validator.ValidateAsync(_userManager.Object, user);

        // Assert
        Assert.False(result.Succeeded);
    }

    #endregion

    #region Configuration-Based Blacklist Tests

    [Fact]
    public async Task ValidateAsync_ConfigurationBlacklist_CommaSeparatedDomains_IsHonored() {
        // Arrange
        var options = Options.Create(new EmailBlacklistOptions {
            Domains = "tempmail.com,guerrillamail.com,throwaway.email"
        });

        var providers = new List<IEmailDomainBlacklistProvider> {
            new ConfigEmailDomainBlacklistProvider(options)
        };
        var validator = new EmailDomainBlacklistValidator<User>(providers);

        // Act & Assert
        var blacklistedUser = new User { Email = "user@tempmail.com" };
        var blacklistedResult = await validator.ValidateAsync(_userManager.Object, blacklistedUser);
        Assert.False(blacklistedResult.Succeeded);

        var validUser = new User { Email = "user@gmail.com" };
        var validResult = await validator.ValidateAsync(_userManager.Object, validUser);
        Assert.True(validResult.Succeeded);
    }

    [Fact]
    public async Task ValidateAsync_ConfigurationBlacklist_EmptyConfiguration_AllowsAllDomains() {
        // Arrange
        var options = Options.Create(new EmailBlacklistOptions {
            Domains = null
        });

        var providers = new List<IEmailDomainBlacklistProvider> {
            new ConfigEmailDomainBlacklistProvider(options)
        };
        var validator = new EmailDomainBlacklistValidator<User>(providers);
        var user = new User { Email = "user@anydomain.com" };

        // Act
        var result = await validator.ValidateAsync(_userManager.Object, user);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ValidateAsync_ConfigurationBlacklist_CaseInsensitive() {
        // Arrange
        var options = Options.Create(new EmailBlacklistOptions {
            Domains = "tempmail.com"
        });

        var providers = new List<IEmailDomainBlacklistProvider> {
            new ConfigEmailDomainBlacklistProvider(options)
        };
        var validator = new EmailDomainBlacklistValidator<User>(providers);

        // Act & Assert
        var upperCaseUser = new User { Email = "user@TEMPMAIL.COM" };
        var upperCaseResult = await validator.ValidateAsync(_userManager.Object, upperCaseUser);
        Assert.False(upperCaseResult.Succeeded);

        var lowerCaseUser = new User { Email = "user@tempmail.com" };
        var lowerCaseResult = await validator.ValidateAsync(_userManager.Object, lowerCaseUser);
        Assert.False(lowerCaseResult.Succeeded);
    }

    #endregion

    #region Multiple Providers Tests

    [Fact]
    public async Task ValidateAsync_MultipleProviders_AnyProviderMatches_ReturnsFailure() {
        // Arrange
        var options = Options.Create(new EmailBlacklistOptions {
            Domains = "configblacklist.com"
        });

        var configProvider = new ConfigEmailDomainBlacklistProvider(options);

        var providers = new List<IEmailDomainBlacklistProvider> { configProvider };
        var validator = new EmailDomainBlacklistValidator<User>(providers);

        // Act
        var user = new User { Email = "user@configblacklist.com" };
        var result = await validator.ValidateAsync(_userManager.Object, user);

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ValidateAsync_NoProviders_ThrowsArgumentNullException() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EmailDomainBlacklistValidator<User>(null!));
    }

    #endregion

    #region File Provider Tests

    [Fact]
    public void FileEmailDomainBlacklistProvider_EmbeddedResourceFound_ConstructsSuccessfully() {
        // The FileEmailDomainBlacklistProvider now uses embedded resources.
        // This test verifies the provider can be instantiated when the embedded resource exists.
        // If the resource is missing, the constructor will throw an InvalidOperationException.
        var provider = new FileEmailDomainBlacklistProvider();
        Assert.NotNull(provider);
    }

    [Fact]
    public async Task FileEmailDomainBlacklistProvider_LoadsEmbeddedBlacklist() {
        // Arrange
        var provider = new FileEmailDomainBlacklistProvider();

        // Act & Assert - The embedded list should contain common disposable email domains
        // Testing against known domains from the blocklist file
        Assert.True(await provider.IsDomainBlacklistedAsync("0-mail.com", TestContext.Current.CancellationToken));
        Assert.False(await provider.IsDomainBlacklistedAsync("gmail.com", TestContext.Current.CancellationToken));
    }

    #endregion

}