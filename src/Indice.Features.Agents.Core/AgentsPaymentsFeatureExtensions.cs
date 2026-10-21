using Indice.Features.Agents.Core.Payments;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Stripe;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions to register the chat payments (Stripe) services.</summary>
public static class AgentsPaymentsFeatureExtensions
{
    /// <summary>Registers <see cref="IPaymentsService"/> with <see cref="StripeOptions"/> bound from the <c>Stripe</c> configuration section.</summary>
    /// <remarks>Register a custom <see cref="IPaymentEventHandler"/> before calling this to run business logic once a payment is authorized.</remarks>
    public static IServiceCollection AddAgentsPayments(this IServiceCollection services, Action<StripeOptions>? configureAction = null) {
        var optionsBuilder = services.AddOptions<StripeOptions>().BindConfiguration(StripeOptions.Name);
        if (configureAction is not null) {
            optionsBuilder.Configure(configureAction);
        }
        optionsBuilder.Validate(x => !string.IsNullOrWhiteSpace(x.PublishableKey), "Stripe:PublishableKey is required.")
                      .Validate(x => !string.IsNullOrWhiteSpace(x.SecretKey), "Stripe:SecretKey is required.")
                      .Validate(x => !string.IsNullOrWhiteSpace(x.WebhookSecret), "Stripe:WebhookSecret is required.")
                      .ValidateOnStart();
        services.TryAddSingleton<IStripeClient>(sp => new StripeClient(sp.GetRequiredService<IOptions<StripeOptions>>().Value.SecretKey));
        services.TryAddTransient<IPaymentEventHandler, NoOpPaymentEventHandler>();
        services.TryAddTransient<IPaymentsService, StripePaymentsService>();
        return services;
    }
}
