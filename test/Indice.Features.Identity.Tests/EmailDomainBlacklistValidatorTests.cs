using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.EmailValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using static Indice.Features.Identity.Core.EmailValidation.EmailDomainBlacklistValidator<Indice.Features.Identity.Core.Data.Models.User>;

namespace Indice.Features.Identity.Tests;

public class EmailDomainBlacklistValidatorTests
{
    private readonly Mock<UserManager<User>> _userManager;

    public EmailDomainBlacklistValidatorTests() {
        var userStore = new Mock<IUserStore<User>>();
        _userManager = new Mock<UserManager<User>>(
            userStore.Object,
            null, null, null, null, null, null, null, null
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
        var providers = new List<IEmailDomainBlacklistProvider> {
            new DefaultEmailDomainBlacklistProvider()
        };
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
        var providers = new List<IEmailDomainBlacklistProvider> {
            new DefaultEmailDomainBlacklistProvider()
        };
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
        var providers = new List<IEmailDomainBlacklistProvider> {
            new DefaultEmailDomainBlacklistProvider()
        };
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
        var providers = new List<IEmailDomainBlacklistProvider> {
            new DefaultEmailDomainBlacklistProvider()
        };
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
        var providers = new List<IEmailDomainBlacklistProvider> {
            new DefaultEmailDomainBlacklistProvider()
        };
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
        Assert.Contains(result.Errors, e => e.Code.Contains("InvalidEmail"));
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
            .ReturnsAsync(async () => {
                await Task.Delay(1000);
                return false;
            });

        var fastProvider = new Mock<IEmailDomainBlacklistProvider>();
        fastProvider.Setup(p => p.IsDomainBlacklistedAsync(blacklistedDomain, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var providers = new List<IEmailDomainBlacklistProvider> { slowProvider.Object, fastProvider.Object };
        var validator = new EmailDomainBlacklistValidator<User>(providers);

        // Act
        var result = await validator.IsBlacklistedAsync(blacklistedDomain);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Configuration-Based Blacklist Tests

    [Fact]
    public async Task ValidateAsync_ConfigurationBlacklist_IdentityOptionsPath_IsHonored() {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> {
                { "IdentityOptions:Email:DomainBlacklist:0", "tempmail.com" },
                { "IdentityOptions:Email:DomainBlacklist:1", "guerrillamail.com" },
                { "IdentityOptions:Email:DomainBlacklist:2", "throwaway.email" }
            })
            .Build();

        var providers = new List<IEmailDomainBlacklistProvider> {
            new ConfigEmailDomainBlacklistProvider(configuration)
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
    public async Task ValidateAsync_ConfigurationBlacklist_AlternativePath_IsHonored() {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> {
                { "EmailDomainBlacklist:0", "spam.com" },
                { "EmailDomainBlacklist:1", "fake.org" }
            })
            .Build();

        var providers = new List<IEmailDomainBlacklistProvider> {
            new ConfigEmailDomainBlacklistProvider(configuration)
        };
        var validator = new EmailDomainBlacklistValidator<User>(providers);

        // Act & Assert
        var blacklistedUser = new User { Email = "test@spam.com" };
        var blacklistedResult = await validator.ValidateAsync(_userManager.Object, blacklistedUser);
        Assert.False(blacklistedResult.Succeeded);

        var validUser = new User { Email = "test@real.com" };
        var validResult = await validator.ValidateAsync(_userManager.Object, validUser);
        Assert.True(validResult.Succeeded);
    }

    [Fact]
    public async Task ValidateAsync_ConfigurationBlacklist_EmptyConfiguration_AllowsAllDomains() {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>())
            .Build();

        var providers = new List<IEmailDomainBlacklistProvider> {
            new ConfigEmailDomainBlacklistProvider(configuration)
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
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> {
                { "EmailDomainBlacklist:0", "tempmail.com" }
            })
            .Build();

        var providers = new List<IEmailDomainBlacklistProvider> {
            new ConfigEmailDomainBlacklistProvider(configuration)
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
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> {
                { "EmailDomainBlacklist:0", "configblacklist.com" }
            })
            .Build();

        var defaultProvider = new DefaultEmailDomainBlacklistProvider();
        var configProvider = new ConfigEmailDomainBlacklistProvider(configuration);

        var providers = new List<IEmailDomainBlacklistProvider> { defaultProvider, configProvider };
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
        Assert.Throws<ArgumentNullException>(() => new EmailDomainBlacklistValidator<User>(null));
    }

    #endregion

    #region File Provider Tests

    [Fact]
    public void FileEmailDomainBlacklistProvider_FileNotFound_ThrowsException() {
        // Act & Assert
        Assert.Throws<FileNotFoundException>(() =>
            new FileEmailDomainBlacklistProvider("nonexistent.txt")
        );
    }

    [Fact]
    public async Task FileEmailDomainBlacklistProvider_ValidFile_LoadsBlacklist() {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try {
            File.WriteAllLines(tempFile, new[] {
                "# Comment line",
                "tempmail.com",
                "",
                "guerrillamail.com",
                "  throwaway.email  "
            });

            var provider = new FileEmailDomainBlacklistProvider(tempFile);

            // Act & Assert
            Assert.True(await provider.IsDomainBlacklistedAsync("tempmail.com"));
            Assert.True(await provider.IsDomainBlacklistedAsync("guerrillamail.com"));
            Assert.True(await provider.IsDomainBlacklistedAsync("throwaway.email"));
            Assert.False(await provider.IsDomainBlacklistedAsync("gmail.com"));
        } finally {
            if (File.Exists(tempFile)) {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task FileEmailDomainBlacklistProvider_IgnoresComments() {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try {
            File.WriteAllLines(tempFile, new[] {
                "# This is a comment",
                "tempmail.com",
                "#guerrillamail.com"
            });

            var provider = new FileEmailDomainBlacklistProvider(tempFile);

            // Act & Assert
            Assert.True(await provider.IsDomainBlacklistedAsync("tempmail.com"));
            Assert.False(await provider.IsDomainBlacklistedAsync("guerrillamail.com"));
        } finally {
            if (File.Exists(tempFile)) {
                File.Delete(tempFile);
            }
        }
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task IsBlacklistedAsync_CancellationRequested_ReturnsFalse() {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var mockProvider = new Mock<IEmailDomainBlacklistProvider>();
        mockProvider.Setup(p => p.IsDomainBlacklistedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(async () => {
                await Task.Delay(1000);
                return true;
            });

        var providers = new List<IEmailDomainBlacklistProvider> { mockProvider.Object };
        var validator = new EmailDomainBlacklistValidator<User>(providers);

        // Act
        var result = await validator.IsBlacklistedAsync("test.com", cts.Token);

        // Assert
        Assert.False(result);
    }

    #endregion
}