# SignalR Proxy feature

In case this is used in an ASP.NET Core application, this feature forwards offloads SignalR socket connections to an external SignalR server.
Also this enables the use of management endpoints to broadcast messages to specific users or to specific groups.

## Usage

### Custom User ID Resolution and Group Validation

You can customize how user IDs are resolved and validate group names by implementing the respective interfaces:

```csharp
builder.AddSignalRProxy(options => {
    options.AddUserIdResolver<CustomUserIdResolver>();
    options.AddGroupNameValidator<TenantGroupValidator>();
});

public class TenantGroupValidator : ISignalRProxyGroupNameValidator
{
    public Task<bool> ValidateAsync(string groupName)
    {
        if (!IsValidTenantGroup(groupName))
            return Task.FromResult(false);
        return Task.FromResult(true);
    }
}
```

## Authentication (optional)

In case the api clients make use of an old SignalR SDK then there is a known issue with the authentication handshake. 
If this is the case then when the client negotiates the connection with a present `Authorize` header then 
the connection instead of establishing the socket to the signalR server using the negotiate response token
it instead makes use of the same value used while negotiating.

To overcome this issue you can set the following option to true:
```csharp
        // Configure options.
        builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options => {
            options.Events ??= new JwtBearerEvents();
            options.Events.OnMessageReceived = (context) => {
                var fromCustomHeaderOrDefault = SignalRProxyAuthentication.SignalRNegotiateTokenRetriever(
                    defaultTokenRetriever: TokenRetrieval.FromAuthorizationHeader()
                );
                context.Token = fromCustomHeaderOrDefault(context.Request);
                return Task.CompletedTask;
            };
        });
        builder.Services.Configure<OAuth2IntrospectionOptions>("Introspection", options => {
            options.TokenRetriever = SignalRProxyAuthentication.SignalRNegotiateTokenRetriever(
                    defaultTokenRetriever: TokenRetrieval.FromAuthorizationHeader()
                );
        });
```

This will make sure that when the negotiate request is made the token is retrieved from the `X-Negotiate-Authorize` header parameter.

### Remarks

The above code snippet assumes that you have already configured the authentication schemes `AddJwtBearer()` and `AddIntrospection()`.

Finaly the above code snippet **is not needed when using the latest SignalR SDK clients**.