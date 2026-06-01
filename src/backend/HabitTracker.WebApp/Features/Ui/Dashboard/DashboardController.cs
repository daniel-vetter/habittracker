using System.Collections.Immutable;
using HabitTracker.WebApp.Database;
using HabitTracker.WebApp.Features.Core.Days;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.WebApp.Features.Ui.Dashboard;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly Db _db;
    private readonly IDayService _days;

    public DashboardController(Db db, IDayService days)
    {
        _db = db;
        _days = days;
    }

    [HttpGet]
    public async Task<DashboardResponse> Get()
    {
        // Make sure today's logical day exists so it shows up as the latest bubble.
        await _days.GetOpenDay();

        var recent = await _db.Days
            .OrderByDescending(d => d.Date)
            .Take(7)
            .ToListAsync();
        var dates = recent.Select(d => d.Date).ToList();

        var counts = (await _db.Completions
                .Where(c => dates.Contains(c.Day.Date))
                .Select(c => new { c.Day.Date, c.Completed })
                .ToListAsync())
            .GroupBy(x => x.Date)
            .ToDictionary(g => g.Key, g => (Total: g.Count(), Completed: g.Count(x => x.Completed)));

        var days = recent
            .OrderBy(d => d.Date)
            .Select(d =>
            {
                var c = counts.GetValueOrDefault(d.Date);
                return new DashboardDay(d.Date, c.Total, c.Completed);
            })
            .ToImmutableArray();

        return new DashboardResponse(days);
    }
}

public record DashboardResponse(ImmutableArray<DashboardDay> Days);

public record DashboardDay(DateOnly Date, int Total, int Completed);
