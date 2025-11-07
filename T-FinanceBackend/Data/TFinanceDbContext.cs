using Microsoft.EntityFrameworkCore;
using TFinanceBackend.Models;

namespace TFinanceBackend.Data
{
    public class TFinanceDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
    }
}
