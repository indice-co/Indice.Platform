# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [7.4.0] - 2023-09-19
### Added
- Infrastructure that can detect impossible travel logins

You can add this feature by using the following extension method on `IIdentityServerBuilder`

```csharp
.AddImpossibleTravelDetector(options => {
    options.AcceptableSpeed = 90d;
    options.OnImpossibleTravelFlowType = OnImpossibleTravelFlowType.PromptMfa;
})
```

| Option                       | Type        | Default   | Description                                                                          
| ---------------------------- | ----------- | --------- | -----------------------------------------------------------------------------------------------------------------------------------------------------------------------
| AcceptableSpeed              | double      | 80Km/h    | The maximum allowed speed during 2 consecutive successful logins*                    
| OnImpossibleTravelFlowType   | enum        | PromptMfa | **PromptMfa**: if impossible travel is detected then user is prompted to pass MFA checks<br>**DenyLogin**: if impossible travel is detected then user is denied login

 
***Note:** The algorithm compares the current login's location (based on the IP address) and the current datetime with the corresponding values from the last login. It calculates the speed needed to reach from point B (last login location) to point A (current login location) by using the [Haversine formula](https://en.wikipedia.org/wiki/Haversine_formula) and if the speed is greater than the speed provided in `AcceptableSpeed` property the the action described in the `OnImpossibleTravelFlowType` setting is taken.

### Removed
- Please remove the 2 occurences of the following line in the `POST` method of `Login` action method

```
await Events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName));
```