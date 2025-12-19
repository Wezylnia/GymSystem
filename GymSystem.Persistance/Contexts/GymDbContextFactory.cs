using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GymSystem.Persistance.Contexts;

public class GymDbContextFactory : IDesignTimeDbContextFactory<GymDbContext> {
    public GymDbContext CreateDbContext(string[] args) {
        var optionsBuilder = new DbContextOptionsBuilder<GymDbContext>();

        // Migration sırasında kullanılacak connection string
        optionsBuilder.UseNpgsql("Host=localhost;Database=postgres;Username=postgres;Password=123");

        return new GymDbContext(optionsBuilder.Options);
    }
}
