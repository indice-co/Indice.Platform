using Microsoft.EntityFrameworkCore;

namespace Indice.MultitenantApi.Data
{
    public class ApiDbContext : DbContext
    {
        public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options) { }
    }
}
