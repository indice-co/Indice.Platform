In order to configure the `LimitUploadOptions` and override the default values of `DefaultMaxFileSizeBytes` and `DefaultAllowedFileExtensions` you may use one of the following methods:

- Default `IConfiguration` binding method:
```csharp
builder.Services.AddLimitUpload(builder.Configuration);
```
Then, add to `appsettings.json`:
```json
{
    "LimitUploadOptions": {
        "DefaultMaxFileSizeBytes": 6291456,
        "DefaultAllowedFileExtensions": [ ".pdf" ]
    }
}
```

- Configure the options in your own way:
```csharp
builder.Services.AddLimitUpload(x => {
    x.DefaultAllowedFileExtensions = ...
    x.DefaultMaxFileSizeBytes = ...
});
```