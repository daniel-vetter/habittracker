using Microsoft.EntityFrameworkCore;

namespace HabitTracker.WebApp.Database;

public class Db : DbContext
{
    public Db(DbContextOptions<Db> options) : base(options) { }

    // DbSets go here as the project grows
}
