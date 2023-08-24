using FluentValidation;
using Indice.Features.Media.AspNetCore.Models.Requests;
using Indice.Features.Media.AspNetCore.Stores.Abstractions;

namespace Indice.Features.Media.AspNetCore.Models.Validators;

/// <summary>Contains validation logic for <see cref="UploadFileRequest"/>.</summary>
public class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
{
    /// <summary>Updates a new instance of <see cref="UploadFileRequestValidator"/>.</summary>
    public UploadFileRequestValidator(IFolderStore folderStore) {
        RuleFor(folder => folder.FolderId)
            .MustAsync(async (id, token) => await folderStore.GetById(id.Value) is not null)
            .When(folder => folder.FolderId is not null)
            .WithMessage("Parent should be an existing folder.");
    }
}
