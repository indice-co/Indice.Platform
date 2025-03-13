using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Messages.Worker.Azure;

/// <summary>
/// Predicate that determines if a function should be enabled or not.
/// </summary>
/// <param name="functionMetadata">the function to check for enablement</param>
/// <param name="configuration">the configuration</param>
/// <returns></returns>
public delegate bool ExtendedFunctionMetadataProviderDisablePredicate(IFunctionMetadata functionMetadata, IConfiguration configuration);

/// <summary>Decorates the default function implementation.</summary>
internal class ExtendedFunctionMetadataProvider : IFunctionMetadataProvider
{
    private readonly IFunctionMetadataProvider _inner;
    private readonly IConfiguration _configuration;
    private readonly ExtendedFunctionMetadataProviderDisablePredicate _functionDisablePredicate;

    /// <summary>
    /// construct the extended function decorator
    /// </summary>
    /// <param name="inner"></param>
    /// <param name="configuration"></param>
    /// <param name="functionEnabledPredicate"></param>
    public ExtendedFunctionMetadataProvider(IFunctionMetadataProvider inner, IConfiguration configuration, ExtendedFunctionMetadataProviderDisablePredicate functionEnabledPredicate) {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _functionDisablePredicate = functionEnabledPredicate ?? throw new ArgumentNullException(nameof(functionEnabledPredicate));
    }


    /// <inheritdoc/>
    public async Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory) {
        var allFunctions = await _inner.GetFunctionMetadataAsync(directory);
        return allFunctions.Where(fn => !_functionDisablePredicate(fn, _configuration)).ToImmutableArray();
    }
}


/// <summary>
/// Custom Function Metadata Provider that allows to enable/disable Functions based on configuration settings.
/// </summary>
internal class AttributeBasedFunctionMetadataProvider : IFunctionMetadataProvider
{
    private readonly IFunctionMetadataProvider _inner;
    private readonly IConfiguration _configuration;
    /// <summary>
    /// construct decorator
    /// </summary>
    /// <param name="inner"></param>
    /// <param name="configuration"></param>
    public AttributeBasedFunctionMetadataProvider(IFunctionMetadataProvider inner, IConfiguration configuration) {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc/>
    public async Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory) {
        var list = await _inner.GetFunctionMetadataAsync(directory);
        var disabledFunctions = new HashSet<string>();

        var scriptFiles = list.Select(fn => fn.ScriptFile).ToHashSet();
        foreach (var item in scriptFiles) {
            var methods = Assembly.Load(Path.GetFileNameWithoutExtension(item!))
                                   .GetTypes()
                                   .SelectMany(t => t.GetMethods())
                                   .Where(m => m.GetCustomAttributes(typeof(EnableFunctionAttribute)).Any() && 
                                           m.GetCustomAttributes(typeof(FunctionAttribute)).Any()).ToArray();
            foreach (var method in methods) {
                var setting = method.GetCustomAttribute<EnableFunctionAttribute>()!;
                var functionName = method.GetCustomAttribute<FunctionAttribute>()!.Name;
                if (setting.CheckDisabled(_configuration)) {
                    disabledFunctions.Add(functionName!);
                }
            }
        }

        return list.Where(fn => !disabledFunctions.Contains(fn.Name!)).ToImmutableArray();
    }
}

/// <summary>
/// Attribute to enable/disable a Function based on a configuration setting
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class EnableFunctionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnableFunctionAttribute"/> class.
    /// </summary>
    /// <param name="settingName">The appsetting key to check</param>
    /// <param name="activationValue">The value that enable the Function</param>
    /// <param name="isDefault">Indicate if the Function should be enabled if setting is not present</param>
    public EnableFunctionAttribute(string settingName, string activationValue, bool isDefault) {
        this.SettingName = settingName;
        ActivationValue = activationValue;
        IsDefault = isDefault;
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="EnableFunctionAttribute"/> class.
    /// </summary>
    /// <param name="settingName">The appsetting key to check</param>
    /// <param name="activationValue">The value that enable the Function</param>
    public EnableFunctionAttribute(string settingName, string activationValue) {
        this.SettingName = settingName;
        ActivationValue = activationValue;
    }

    internal string SettingName { get; }
    internal string ActivationValue { get; }
    internal bool IsDefault { get; } = false;

    /// <summary>
    /// Check if the Function should be enabled or not
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public bool CheckDisabled(IConfiguration configuration) {
        // check the target setting and return false (disabled) if the value exists and is "falsey"
        var value = configuration[SettingName];

        if (string.IsNullOrEmpty(value) && !IsDefault) {
            return true;
        }

        if (!string.IsNullOrEmpty(value) &&
            !ActivationValue.Equals(value, StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        return false;
    }
}

