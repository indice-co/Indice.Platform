using Indice.Security;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases
{
    /// <summary>
    /// The options for the initialization of the Case Api.
    /// </summary>
    public class CasesApiOptions
    {
        /// <summary>
        /// Configuration <see cref="Action"/> for internal <see cref="DbContext"/>. 
        /// If not provided the underlying store defaults to SQL Server expecting the setting <i>ConnectionStrings:DefaultConnection</i> to be present.
        /// </summary>
        public Action<DbContextOptionsBuilder> ConfigureDbContext { get; set; }
       
        /// <summary>
        /// The default scope name to be used for Cases API. Defaults to <see cref="CasesApiConstants.Scope"/>.
        /// </summary>
        public string ExpectedScope { get; set; } = CasesApiConstants.Scope;

        /// <summary>
        /// Specifies a prefix for the API endpoints. Defaults to <i>api</i>.
        /// </summary>
        public string ApiPrefix { get; set; } = "api";
        
        /// <summary>
        /// The claim type used to identify the user. Defaults to <i>sub</i>.
        /// </summary>
        public string UserClaimType { get; set; } = BasicClaimTypes.Subject;
        
        /// <summary>
        /// Schema name used for tables. Defaults to <i>case</i>.
        /// </summary>
        public string DatabaseSchema { get; set; } = CasesApiConstants.DatabaseSchema;

        /// <summary>
        /// The claim type groupid name
        /// </summary>
        public string GroupIdClaimType { get; set; } = CasesApiConstants.DefaultGroupIdClaimType;
    }
}
