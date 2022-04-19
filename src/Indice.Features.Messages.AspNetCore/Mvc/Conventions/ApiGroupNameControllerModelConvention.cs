using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Indice.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// Allows to dynamically set the group name of a controller.
    /// </summary>
    public class ApiGroupNameControllerModelConvention : IControllerModelConvention
    {
        private readonly string _groupNamePlaceholder;
        private readonly string _replacementValue;

        /// <summary>
        /// Creates a new instance of <see cref="ApiGroupNameControllerModelConvention"/>.
        /// </summary>
        /// <param name="groupNamePlaceholder">The placeholder used in the controller metadata of the <see cref="ApiExplorerSettingsAttribute"/>.</param>
        /// <param name="replacementValue">The value to replace.</param>
        public ApiGroupNameControllerModelConvention(string groupNamePlaceholder, string replacementValue) {
            _groupNamePlaceholder = groupNamePlaceholder ?? throw new ArgumentNullException(nameof(groupNamePlaceholder));
            _replacementValue = replacementValue;
        }

        /// <inheritdoc />
        public void Apply(ControllerModel controller) {
            var selector = controller.Selectors[0];
            if (selector.EndpointMetadata is null) {
                return;
            }
            var apiExplorerSettingsObj = selector.EndpointMetadata.Where(x => x.GetType() == typeof(ApiExplorerSettingsAttribute)).FirstOrDefault();
            if (apiExplorerSettingsObj is null) {
                return;
            }
            var index = selector.EndpointMetadata.IndexOf(apiExplorerSettingsObj);
            var apiExplorerSettings = (ApiExplorerSettingsAttribute)selector.EndpointMetadata[index];
            var currentGroupName = apiExplorerSettings.GroupName;
            if (!currentGroupName.Contains(_groupNamePlaceholder)) {
                return;
            }
            var newGroupName = string.IsNullOrWhiteSpace(_replacementValue) ? null : currentGroupName.Replace(_groupNamePlaceholder, _replacementValue);
            if (controller.ApiExplorer is not null) {
                controller.ApiExplorer.GroupName = newGroupName;
            }
            apiExplorerSettings.GroupName = newGroupName;
        }
    }
}
