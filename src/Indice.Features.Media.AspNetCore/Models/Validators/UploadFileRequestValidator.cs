using FluentValidation;
using Indice.Features.Media.AspNetCore.Models.Requests;
using Indice.Features.Media.AspNetCore.Stores.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Media.AspNetCore.Models.Validators;

/// <summary>Contains validation logic for <see cref="UploadFileRequest"/>.</summary>
internal class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>, IDisposable
{
    private readonly IFolderStore _folderStore;
    private readonly IServiceScope _scope;
    /// <summary>Updates a new instance of <see cref="UploadFileRequestValidator"/>.</summary>
    public UploadFileRequestValidator(IServiceProvider serviceProvider) {
        _scope = serviceProvider.CreateScope();
        _folderStore = _scope.ServiceProvider.GetRequiredService<IFolderStore>();
        RuleFor(folder => folder.FolderId)
            .MustAsync(async (id, token) => await _folderStore.GetById(id.Value) is not null)
            .When(folder => folder.FolderId is not null)
            .WithMessage("Parent should be an existing folder.");
    }

    public void Dispose() {
        _scope.Dispose();
    }
}
