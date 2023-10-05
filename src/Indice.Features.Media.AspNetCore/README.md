## Media.AspNetCore

This project contains functionality for creating a Media Library.
It is built as a separate project in order to be reusable in any project referencing it.

## Getting Started

This is an example of how you may give instructions on setting up your project locally.
To get a local copy up and running follow these simple example steps.

### Installation

_Below is an example of how you can install and set up your app._

1. Add a reference to **Indice.Features.Media.AspNetCore**
2. Add the required services by using the following extension and configure the corresponding options if you want to override the defaults
   ```cs
   builder.Services.AddMediaLibrary(Action<MediaApiOptions>? configureAction = null)
   ```
3. Configure the swagger UI
   ```cs
   options.SwaggerEndpoint($"/swagger/{Indice.Features.Media.AspNetCore.MediaLibraryApi.Scope}/swagger.json", Indice.Features.Media.AspNetCore.MediaLibraryApi.Scope);
   ```
4. Configure the swagger docs
   ```cs
   options.AddDocIndice.Features.Media.AspNetCore.MediaLibraryApi.Scope, "Media Library API", "API for managing media library in the backoffice tool.");
   ```
5. Map the Endpoints
   ```cs
   app.MapMediaLibrary();
   ```
6. Configure the Authorization Options by adding a policy named **Indice.Features.Media.AspNetCore.MediaLibraryApi.Policies.BeMediaLibraryManager**

## Configuration

- **ApiPrefix**: Specifies a prefix for the media API endpoints. Defaults to _/api_.
- **AuthenticationScheme**: The authentication scheme to be used for media API. Defaults to _Bearer_.
- **AcceptableFileExtensions**: The acceptable file extensions. Defaults to _.png, .jpg, .gif_.
- **ApiScope**: The scope name to be used for media API. Defaults to _media_.
- **DatabaseSchema**: The database schema for media db. Defaults to _media_.
- **MaxFileSize**: The maximum acceptable size of the files to be uploaded. Defaults to _10MB_.
- **UseSoftDelete**: The File deletion policy. If true the files are only marked as deleted but they are not actually deleted from the storage. Defaults to _true_.
_ **ConfigureDbContext**: The configuration Action to setup the Database context. Defaults to _ConnectionStrings:MediaLibraryDbConnection_.