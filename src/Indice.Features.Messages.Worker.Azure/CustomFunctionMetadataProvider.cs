using System.Collections.Immutable;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Azure.Functions.Worker.Core.FunctionMetadata;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Messages.Worker.Azure;

/// <summary>
/// Custom Function Metadata Provider that allows to enable/disable Functions based on configuration settings.
/// </summary>
public class CustomFunctionMetadataProvider : IFunctionMetadataProvider
{
    private readonly IServiceProvider sp;
    private readonly IConfiguration _config;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sp"></param>
    /// <param name="configuration"></param>
    public CustomFunctionMetadataProvider(IServiceProvider sp, IConfiguration configuration) {
        this.sp = sp;
        _config = configuration;
    }

    public Task<ImmutableArray<IFunctionMetadata>> GetFunctionMetadataAsync(string directory) {
        var service = this.sp.GetServices<IFunctionMetadataProvider>().ToList();
        var functionMetadataProvider = service.Last(x => x.GetType() != typeof(CustomFunctionMetadataProvider));

        var metadataList = new List<IFunctionMetadata>();
        Task<ImmutableArray<IFunctionMetadata>> list = functionMetadataProvider.GetFunctionMetadataAsync(directory);

        HashSet<string> disabledFunctions = new HashSet<string>();
        var scriptFiles = list.Result.Select(fn => fn.ScriptFile).ToHashSet();
        foreach (var item in scriptFiles) {
            Type[] types = Assembly.LoadFrom(item).GetTypes();
            var methods = types.SelectMany(t => t.GetMethods()).Where(m => m.GetCustomAttributes(typeof(EnableFunctionAttribute)).Any() && m.GetCustomAttributes(typeof(FunctionAttribute)).Any());
            foreach (var method in methods) {
                var setting = method.GetCustomAttribute<EnableFunctionAttribute>();
                if (IsSettingEnabled(setting)) {
                    disabledFunctions.Add(method.GetCustomAttribute<FunctionAttribute>().Name);
                }
            }
        }

        foreach (var item in list.Result) {
            if (!disabledFunctions.Contains(item.Name)) {
                metadataList.Add(item);
            }
        }

        return Task.FromResult(metadataList.ToImmutableArray());
    }

    private bool IsSettingEnabled(EnableFunctionAttribute? attribute) {
        if (attribute == null) return false;

        // check the target setting and return false (disabled) if the value exists and is "falsey"
        string? value = _config.GetValue<string>(attribute.SettingName);

        if (string.IsNullOrEmpty(value) && !attribute.IsDefault) {
            return true;
        }

        if (!string.IsNullOrEmpty(value) && string.Compare(value, attribute.ActivationValue, StringComparison.OrdinalIgnoreCase) == 0) {
            return true;
        }

        return false;
    }
}

/// <summary>
/// Attribute to enable/disable a Function based on a configuration setting
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class EnableFunctionAttribute : Attribute, IFilterMetadata
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
}

