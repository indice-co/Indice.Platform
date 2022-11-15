namespace Indice.Features.Cases.Models
{
    /// <summary>
    /// Custom action blocking activity that will generate the corresponding component.
    /// </summary>
    public class CustomCaseAction
    {
        /// <summary>
        /// The Id to trigger the action.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// The name of the action.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The label of the action.
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// The description of the action.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The Default Value of action's input.
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Determines whether the action will have an input element.
        /// </summary>
        public bool? HasInput { get; set; }
    }
}