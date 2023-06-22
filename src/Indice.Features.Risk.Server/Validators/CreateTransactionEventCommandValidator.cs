using FluentValidation;
using Indice.Configuration;
using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Server.Commands;

namespace Indice.Features.Risk.Server.Validators;

internal class CreateTransactionEventCommandValidator<TTransaction> : AbstractValidator<CreateTransactionEventCommand> where TTransaction : Transaction
{
    private readonly ITransactionStore<TTransaction> _transactionStore;

    public CreateTransactionEventCommandValidator(ITransactionStore<TTransaction> transactionStore) {
        _transactionStore = transactionStore ?? throw new ArgumentNullException(nameof(transactionStore));
        RuleFor(x => x.Name).MaximumLength(TextSizePresets.M256);
        RuleFor(x => x.TransactionId).NotEmpty().MustAsync(BeExistingTransactionId);
    }

    private async Task<bool> BeExistingTransactionId(Guid transactionId, CancellationToken cancellationToken) {
        var transaction = await _transactionStore.GetByIdAsync(transactionId);
        return transaction is not null;
    }
}
