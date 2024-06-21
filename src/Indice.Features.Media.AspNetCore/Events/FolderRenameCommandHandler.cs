using Indice.Events;
using Indice.Services;

namespace Indice.Features.Media.AspNetCore.Events;
internal class FolderRenameCommandHandler(IFileServiceFactory fileServiceFactory) : IPlatformEventHandler<FolderRenameCommand>
{
    private readonly IFileService _fileService = fileServiceFactory.Create(KeyedServiceNames.FileServiceKey) ?? throw new ArgumentNullException(nameof(fileServiceFactory));
    public async Task Handle(FolderRenameCommand @event, PlatformEventArgs args) {
        await _fileService.MoveAsync(@event.OldName, @event.NewName);
    }
}
