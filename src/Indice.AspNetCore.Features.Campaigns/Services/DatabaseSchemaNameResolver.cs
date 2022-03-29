using System;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal class DatabaseSchemaNameResolver
    {
        private readonly string _schemaName;

        public DatabaseSchemaNameResolver(string schemaName) {
            _schemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        }

        public string GetSchemaName() => _schemaName;
    }
}
