using Microsoft.EntityFrameworkCore;

namespace DbSecurityDemo.DataAccess
{
    public class DbAccessContext : DbContext
    {
        public DbAccessContext(DbContextOptions<DbAccessContext> options)
           : base(options)
        { }

        public DbSet<User> Users { get; set; }
    }
}
