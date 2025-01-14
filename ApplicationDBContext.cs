using Media_microservice.Entity;
using Microsoft.EntityFrameworkCore;

namespace Media_microservice
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
            : base(options)
        {
        }

        public DbSet<MediaRequest> MediaRequests { get; set; }
        public DbSet<MediaResponse> MediaResponses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
