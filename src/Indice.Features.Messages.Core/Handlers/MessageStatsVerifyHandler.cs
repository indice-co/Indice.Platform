using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Services.Abstractions;

namespace Indice.Features.Messages.Core.Handlers;

/// <summary>
/// Handles send statistics timer events by triggering the verification of the send statistics.
/// </summary>
public sealed class MessageStatsVerifyHandler : ICampaignJobHandler<MessageStatsVerifyTimerEvent>
{
    private readonly IMessageStatsVerificationService _verificationService;

    /// <summary>
    /// The constructor for our handler.
    /// </summary>
    /// <param name="verificationService"></param>
    public MessageStatsVerifyHandler(IMessageStatsVerificationService verificationService) {
        _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
    }

    /// <summary>
    /// Calls the verification of the send statistics.
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public async Task Process(MessageStatsVerifyTimerEvent @event) {
        await _verificationService.VerifyAsync();
    }
}
