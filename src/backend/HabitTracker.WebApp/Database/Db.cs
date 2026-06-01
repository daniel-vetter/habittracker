using System.ComponentModel.DataAnnotations;
using HabitTracker.WebApp.Features.Core.Habits;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.WebApp.Database;

public class Db : DbContext
{
    public Db(DbContextOptions<Db> options) : base(options) { }

    public DbSet<DbHabit> Habits => Set<DbHabit>();
    public DbSet<DbDay> Days => Set<DbDay>();
    public DbSet<DbHabitCompletion> Completions => Set<DbHabitCompletion>();
    public DbSet<DbUpdateLog> UpdateLogs => Set<DbUpdateLog>();
    public DbSet<DbConfigEntry> ConfigEntries => Set<DbConfigEntry>();
}

/// <summary>Captured stdout/stderr of an update sidecar run, persisted after the new container booted.</summary>
public class DbUpdateLog
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Log { get; set; } = "";
}

/// <summary>Generic key/value store for small app settings (e.g. the auto-update flag).
/// <see cref="Type"/> is a tag describing how <see cref="Value"/> was serialized.</summary>
public class DbConfigEntry
{
    [Key]
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string Type { get; set; } = "";
}

public class DbHabit
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Notes { get; set; }

    public ScheduleKind ScheduleKind { get; set; }

    /// <summary>Set iff <see cref="ScheduleKind.EveryNDays"/>.</summary>
    public int? IntervalDays { get; set; }

    /// <summary>DayOfWeek values (Sunday = 0). Used iff <see cref="ScheduleKind.Weekdays"/>.</summary>
    public int[] Weekdays { get; set; } = [];

    public MissPolicy MissPolicy { get; set; }

    /// <summary>First logical day the habit is active on.</summary>
    public DateOnly Start { get; set; }

    /// <summary>Soft delete: null = active; once set, the habit is no longer due from this day on.</summary>
    public DateOnly? EndDate { get; set; }
}

/// <summary>Every logical day, contiguous and without gaps. Exactly one row is open
/// (<see cref="Closed"/> == false) — the last one. Days advance only via "end day".</summary>
public class DbDay
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public bool Closed { get; set; }
}

/// <summary>A habit's occurrence on a day: materialized when the habit is due on that (open) day.
/// <see cref="Completed"/> is the user's truth (toggled). <see cref="Overdue"/> is derived at
/// materialization (true = carried/late occurrence, false = freshly scheduled that day).</summary>
public class DbHabitCompletion
{
    public int Id { get; set; }
    public DbHabit Habit { get; set; } = null!;
    public DbDay Day { get; set; } = null!;
    public bool Completed { get; set; }
    public bool Overdue { get; set; }
}
