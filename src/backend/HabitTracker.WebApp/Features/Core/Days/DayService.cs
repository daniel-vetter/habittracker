using HabitTracker.WebApp.Database;
using HabitTracker.WebApp.Features.Core.Habits;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.WebApp.Features.Core.Days;

public interface IDayService
{
    /// <summary>The current open logical day.</summary>
    Task<DateOnly> GetOpenDay();

    /// <summary>Closes the open day and opens the next one (materialized). Returns the new open day.</summary>
    Task<DateOnly> CloseDay();

    /// <summary>Brings the open day's completions in line with the currently-due active habits:
    /// adds missing due occurrences, refreshes Overdue, removes uncompleted ones no longer due.
    /// Only ever touches the open day — closed days are immutable history.</summary>
    Task ResyncOpenDay();
}

[ScopedService<IDayService>]
public class DayService : IDayService
{
    private readonly Db _db;

    public DayService(Db db) => _db = db;

    public async Task<DateOnly> GetOpenDay() => (await OpenDayEntity()).Date;

    public async Task<DateOnly> CloseDay()
    {
        var open = await OpenDayEntity();
        open.Closed = true;
        var next = new DbDay { Date = open.Date.AddDays(1) };
        _db.Days.Add(next);
        await _db.SaveChangesAsync();

        await ResyncOpenDay();
        return next.Date;
    }

    public async Task ResyncOpenDay()
    {
        var open = await OpenDayEntity();
        var d = open.Date;

        var activeHabits = await _db.Habits
            .Where(h => h.Start <= d && (h.EndDate == null || d < h.EndDate))
            .ToListAsync();
        var activeIds = activeHabits.Select(h => h.Id).ToHashSet();

        var completedByHabit = (await _db.Completions
                .Where(c => c.Completed)
                .Select(c => new { HabitId = c.Habit.Id, c.Day.Date })
                .ToListAsync())
            .GroupBy(x => x.HabitId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Date).ToHashSet());

        var openOccurrences = await _db.Completions
            .Include(c => c.Habit)
            .Where(c => c.Day.Id == open.Id)
            .ToListAsync();
        var occurrenceByHabit = openOccurrences.ToDictionary(c => c.Habit.Id);

        // Drop uncompleted occurrences whose habit is no longer active or no longer due.
        foreach (var occurrence in openOccurrences)
        {
            if (occurrence.Completed) continue;
            var dates = completedByHabit.GetValueOrDefault(occurrence.Habit.Id) ?? new();
            var stillDue = activeIds.Contains(occurrence.Habit.Id) && IsDue(occurrence.Habit, dates, d);
            if (!stillDue) _db.Completions.Remove(occurrence);
        }

        // Add/refresh occurrences for every due active habit.
        foreach (var habit in activeHabits)
        {
            var dates = completedByHabit.GetValueOrDefault(habit.Id) ?? new();
            if (!IsDue(habit, dates, d)) continue;
            var overdue = IsOverdue(habit, dates, d);

            if (occurrenceByHabit.TryGetValue(habit.Id, out var occurrence))
            {
                if (!occurrence.Completed) occurrence.Overdue = overdue;
            }
            else
            {
                _db.Completions.Add(new DbHabitCompletion
                {
                    Habit = habit, Day = open, Completed = false, Overdue = overdue
                });
            }
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>The single open day; created as today on first ever access (anchors the logical calendar).</summary>
    private async Task<DbDay> OpenDayEntity()
    {
        var open = await _db.Days.FirstOrDefaultAsync(d => !d.Closed);
        if (open is null)
        {
            open = new DbDay { Date = DateOnly.FromDateTime(DateTime.Now) };
            _db.Days.Add(open);
            await _db.SaveChangesAsync();
        }
        return open;
    }

    // ---- Due / overdue calculation (pure, derived from schedule + completed dates) ----

    private static bool IsDue(DbHabit habit, HashSet<DateOnly> completedDates, DateOnly on)
    {
        if (on < habit.Start) return false;

        switch (habit.ScheduleKind)
        {
            case ScheduleKind.Daily:
                return !completedDates.Contains(on);

            case ScheduleKind.EveryNDays:
                return on >= EveryNDaysBaseline(habit, completedDates);

            case ScheduleKind.Weekdays:
                if (habit.MissPolicy == MissPolicy.Lapse)
                    return IsScheduledWeekday(habit, on) && !completedDates.Contains(on);

                // CarryOver: due from the latest scheduled occurrence until completed.
                var s = LatestScheduledOccurrenceOnOrBefore(habit, on);
                return s is { } occ && occ >= habit.Start
                    && !AnyCompletionInRange(completedDates, occ, on);

            default:
                return false;
        }
    }

    private static bool IsOverdue(DbHabit habit, HashSet<DateOnly> completedDates, DateOnly on)
    {
        if (!IsDue(habit, completedDates, on)) return false;

        switch (habit.ScheduleKind)
        {
            case ScheduleKind.EveryNDays:
                return on > EveryNDaysBaseline(habit, completedDates);

            case ScheduleKind.Weekdays when habit.MissPolicy == MissPolicy.CarryOver:
                var s = LatestScheduledOccurrenceOnOrBefore(habit, on);
                return s is { } occ && occ < on;

            default:
                return false;
        }
    }

    private static DateOnly EveryNDaysBaseline(DbHabit habit, HashSet<DateOnly> completedDates)
    {
        var n = habit.IntervalDays.GetValueOrDefault(1);
        if (n < 1) n = 1;
        return completedDates.Count > 0 ? completedDates.Max().AddDays(n) : habit.Start;
    }

    private static bool IsScheduledWeekday(DbHabit habit, DateOnly on) =>
        Array.IndexOf(habit.Weekdays, (int)on.DayOfWeek) >= 0;

    private static bool AnyCompletionInRange(HashSet<DateOnly> completedDates, DateOnly from, DateOnly to)
    {
        foreach (var date in completedDates)
            if (date >= from && date <= to) return true;
        return false;
    }

    /// <summary>Latest date with a scheduled weekday occurrence, at most 7 days back from <paramref name="on"/>.</summary>
    private static DateOnly? LatestScheduledOccurrenceOnOrBefore(DbHabit habit, DateOnly on)
    {
        if (habit.Weekdays.Length == 0) return null;
        for (var i = 0; i < 7; i++)
        {
            var candidate = on.AddDays(-i);
            if (IsScheduledWeekday(habit, candidate)) return candidate;
        }
        return null;
    }
}
