using FluentValidation;
using Indice.Configuration;
using Indice.Features.Media.AspNetCore.Models.Requests;
using Indice.Features.Media.AspNetCore.Stores.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Media.AspNetCore.Models.Validators;

/// <summary>Contains validation logic for <see cref="UpdateFolderRequest"/>.</summary>
internal class UpdateFolderRequestValidator : AbstractValidator<UpdateFolderRequest>, IDisposable
{
    private readonly IFolderStore _folderStore;
    private readonly IServiceScope _scope;
    /// <summary>Updates a new instance of <see cref="UpdateFolderRequestValidator"/>.</summary>
    public UpdateFolderRequestValidator(IServiceProvider serviceProvider) {
        _scope = serviceProvider.CreateScope();
        _folderStore = _scope.ServiceProvider.GetRequiredService<IFolderStore>();
        RuleFor(folder => folder.Name)
            .NotEmpty()
            .WithMessage("Please provide a name for the folder's name.")
            .MaximumLength(TextSizePresets.M128)
            .WithMessage($"Folder name cannot exceed {TextSizePresets.M128} characters.");
        RuleFor(folder => folder.Description)
            .MaximumLength(TextSizePresets.M512)
            .WithMessage($"Folder description cannot exceed {TextSizePresets.M512} characters.");
        RuleFor(folder => folder.ParentId)
            .MustAsync(async (id, token) => await _folderStore.GetById(id.Value) is not null)
            .When(folder => folder.ParentId is not null)
            .WithMessage("Parent should be an existing folder.");
    }

    public void Dispose() {
        _scope.Dispose();
    }
}
