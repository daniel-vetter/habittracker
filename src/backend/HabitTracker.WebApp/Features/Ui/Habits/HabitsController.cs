using System.Collections.Immutable;
using HabitTracker.WebApp.Database;
using HabitTracker.WebApp.Features.Core.Days;
using HabitTracker.WebApp.Features.Core.Habits;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.WebApp.Features.Ui.Habits;

[ApiController]
[Route("api/[controller]")]
public class HabitsController : ControllerBase
{
    private readonly Db _db;
    private readonly IDayService _days;

    public HabitsController(Db db, IDayService days)
    {
        _db = db;
        _days = days;
    }

    [HttpGet]
    public async Task<ImmutableArray<HabitResponse>> List()
    {
        var habits = await _db.Habits
            .Where(h => h.EndDate == null)
            .OrderBy(h => h.Name)
            .ToListAsync();
        return habits.Select(ToResponse).ToImmutableArray();
    }

    [HttpPost]
    public async Task<HabitResponse> Create(HabitRequest request)
    {
        var habit = new DbHabit { Start = await _days.GetOpenDay() };
        Apply(habit, request);
        _db.Habits.Add(habit);
        await _db.SaveChangesAsync();
        await _days.ResyncOpenDay();
        return ToResponse(habit);
    }

    [HttpPut("{id:int}")]
    public async Task<HabitResponse> Update(int id, HabitRequest request)
    {
        var habit = await _db.Habits.FirstOrDefaultAsync(h => h.Id == id)
            ?? throw new KeyNotFoundException($"Habit {id} not found.");
        Apply(habit, request);
        await _db.SaveChangesAsync();
        await _days.ResyncOpenDay();
        return ToResponse(habit);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var habit = await _db.Habits.FirstOrDefaultAsync(h => h.Id == id);
        if (habit is not null && habit.EndDate is null)
        {
            habit.EndDate = await _days.GetOpenDay();
            await _db.SaveChangesAsync();
            await _days.ResyncOpenDay();
        }
        return NoContent();
    }

    private static void Apply(DbHabit habit, HabitRequest request)
    {
        habit.Name = request.Name.Trim();
        habit.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        habit.ScheduleKind = request.ScheduleKind;
        habit.IntervalDays = request.ScheduleKind == ScheduleKind.EveryNDays ? request.IntervalDays : null;
        habit.Weekdays = request.ScheduleKind == ScheduleKind.Weekdays ? request.Weekdays.ToArray() : [];
        habit.MissPolicy = request.MissPolicy;
    }

    private static HabitResponse ToResponse(DbHabit h) => new(
        h.Id, h.Name, h.Notes, h.ScheduleKind, h.IntervalDays,
        h.Weekdays.ToImmutableArray(), h.MissPolicy, h.Start);
}

public record HabitRequest(
    string Name,
    string? Notes,
    ScheduleKind ScheduleKind,
    int? IntervalDays,
    ImmutableArray<int> Weekdays,
    MissPolicy MissPolicy);

public record HabitResponse(
    int Id,
    string Name,
    string? Notes,
    ScheduleKind ScheduleKind,
    int? IntervalDays,
    ImmutableArray<int> Weekdays,
    MissPolicy MissPolicy,
    DateOnly Start);
