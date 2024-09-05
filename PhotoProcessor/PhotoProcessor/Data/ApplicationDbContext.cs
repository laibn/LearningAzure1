using Microsoft.EntityFrameworkCore;
using PhotoProcessor.Models;

namespace PhotoProcessor.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

        public DbSet<PhotoMetadata> Photos { get; set; }
    }
}
