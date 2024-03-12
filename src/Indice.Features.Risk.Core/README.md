## Risk Engine

### To use the Risk Engine in your project, follow these steps:

1. Inherit your risk rules from the `RiskRule` base class.
2. Implement a derived class from the `RuleOptions` base class for your rule options.
3. Implement a derived class from `RuleOptionsValidator` for validating your rule options.

### Adding a New Rule

To add a new rule to the risk engine, use the `AddRule` method provided by the `RiskEngineBuilder` class. This method allows you to specify the rule implementation, options, and validator.

### Mapping Admin Endpoints

When specifying your `RuleOptions` base class, ensure that it derives from the framework's base class and includes a discriminator for JSON polymorphism. This enables the manager endpoints to recognize and interact with your custom rule options transparently.

Custom base class example:
```csharp
public class MyRuleOptions : RuleOptions
{
    internal const string TypeDiscriminator = "_type";
}
```

Configuring `SwaggerGenOptions`:
```csharp
options.AddPolymorphism(builder.Services);
```

Configuring `JsonOptions`:
```csharp
options.JsonSerializerOptions.Converters.Add(new JsonPolymorphicConverterFactory<MyRuleOptions>(MyRuleOptions.TypeDiscriminator));
```

Mapping manager endpoints example:
```csharp
app.MapAdminRisk<MyRuleOptions>();
```