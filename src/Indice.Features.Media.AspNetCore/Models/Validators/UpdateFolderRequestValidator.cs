using FluentValidation;
using Indice.Configuration;
using Indice.Features.Media.AspNetCore.Models.Requests;
using Indice.Features.Media.AspNetCore.Stores.Abstractions;

namespace Indice.Features.Media.AspNetCore.Models.Validators;

/// <summary>Contains validation logic for <see cref="UpdateFolderRequest"/>.</summary>
public class UpdateFolderRequestValidator : AbstractValidator<UpdateFolderRequest>
{
    /// <summary>Updates a new instance of <see cref="UpdateFolderRequestValidator"/>.</summary>
    public UpdateFolderRequestValidator(IMediaFolderStore folderStore) {
        RuleFor(folder => folder.Name)
            .NotEmpty()
            .WithMessage("Please provide a name for the folder's name.")
            .MaximumLength(TextSizePresets.M128)
            .WithMessage($"Folder name cannot exceed {TextSizePresets.M128} characters.");
        RuleFor(folder => folder.Description)
            .MaximumLength(TextSizePresets.M512)
            .WithMessage($"Folder description cannot exceed {TextSizePresets.M512} characters.");
        RuleFor(folder => folder.ParentId)
            .MustAsync(async (id, token) => await folderStore.GetById(id.Value) is not null)
            .When(folder => folder.ParentId is not null)
            .WithMessage("Parent should be an existing folder.");
    }
}
