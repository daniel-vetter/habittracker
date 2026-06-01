using System.Collections.Immutable;
using HabitTracker.WebApp.Database;
using HabitTracker.WebApp.Features.Core.Days;
using HabitTracker.WebApp.Features.Core.Habits;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.WebApp.Features.Ui.Review;

[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    private readonly Db _db;
    private readonly IDayService _days;

    public ReviewController(Db db, IDayService days)
    {
        _db = db;
        _days = days;
    }

    [HttpGet]
    public async Task<ReviewResponse> Get()
    {
        var date = await _days.GetOpenDay();

        var items = (await _db.Completions
                .Include(c => c.Habit)
                .Where(c => c.Day.Date == date)
                .OrderBy(c => c.Habit.Name)
                .ToListAsync())
            .Select(c => new ReviewItem(
                c.Habit.Id, c.Habit.Name, c.Habit.Notes, c.Completed, c.Overdue, c.Habit.ScheduleKind))
            .ToImmutableArray();

        var pastDays = (await _db.Completions
                .Include(c => c.Habit)
                .Include(c => c.Day)
                .Where(c => c.Day.Closed)
                .ToListAsync())
            .GroupBy(c => c.Day.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new PastDay(
                g.Key,
                g.OrderBy(c => c.Habit.Name)
                    .Select(c => new PastDayItem(c.Habit.Name, c.Completed, c.Overdue))
                    .ToImmutableArray()))
            .ToImmutableArray();

        return new ReviewResponse(date, items, pastDays);
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle(ToggleRequest request)
    {
        var date = await _days.GetOpenDay();
        var occurrence = await _db.Completions
            .FirstOrDefaultAsync(c => c.Day.Date == date && c.Habit.Id == request.HabitId);
        if (occurrence is null) return NotFound();

        occurrence.Completed = request.Completed;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("end-day")]
    public async Task<EndDayResponse> EndDay() => new(await _days.CloseDay());
}

public record ReviewItem(
    int HabitId,
    string Name,
    string? Notes,
    bool Completed,
    bool Overdue,
    ScheduleKind ScheduleKind);

public record ReviewResponse(
    DateOnly LogicalDate,
    ImmutableArray<ReviewItem> Items,
    ImmutableArray<PastDay> PastDays);

public record PastDay(DateOnly Date, ImmutableArray<PastDayItem> Items);

public record PastDayItem(string Name, bool Completed, bool Overdue);

public record ToggleRequest(int HabitId, bool Completed);

public record EndDayResponse(DateOnly NewLogicalDate);
