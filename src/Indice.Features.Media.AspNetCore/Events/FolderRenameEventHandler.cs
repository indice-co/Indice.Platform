using Indice.Events;
using Indice.Features.Media.Data;
using Indice.Services;

namespace Indice.Features.Media.AspNetCore.Events;
internal class FolderRenameEventHandler(IFileServiceFactory fileServiceFactory) : IPlatformEventHandler<FolderRenameEvent>
{
    public async Task Handle(FolderRenameEvent @event, PlatformEventArgs args) {
        
    }
}
