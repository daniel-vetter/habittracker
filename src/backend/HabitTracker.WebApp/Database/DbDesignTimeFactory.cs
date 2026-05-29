using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HabitTracker.WebApp.Database;

public class DbDesignTimeFactory : IDesignTimeDbContextFactory<Db>
{
    public Db CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<Db>()
            .UseNpgsql("Host=_;Database=_")
            .Options);
}
