using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using HabitTracker.WebApp.Database;
using HabitTracker.WebApp.Features.Core.SelfUpdate;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.WebApp.Features.Ui.System;

[ApiController]
[Route("api/[controller]")]
public class SystemController : Controller
{
    private readonly Db _db;
    private readonly SelfUpdateFacade _selfUpdateFacade;

    public SystemController(Db db, SelfUpdateFacade selfUpdateFacade)
    {
        _db = db;
        _selfUpdateFacade = selfUpdateFacade;
    }

    [HttpGet("GetAppDetails")]
    public AppDetailsResponse GetAppDetails()
    {
        return new AppDetailsResponse(
            Environment.GetEnvironmentVariable("BUILD_TIME") ?? "unknown",
            Environment.GetEnvironmentVariable("BUILD_COMMIT") ?? "unknown",
            Environment.Version.ToString(),
            RuntimeInformation.OSDescription);
    }

    [HttpGet("GetUpdateStatus")]
    public async Task<UpdateStatusResponse> GetUpdateStatus()
    {
        var status = await _selfUpdateFacade.GetStatus();
        return new UpdateStatusResponse(
            status.IsUpdateFeatureAvailable,
            status.IsUpdateAvailable,
            status.LastCheck,
            status.AutoUpdate);
    }

    [HttpPost("CheckForUpdate")]
    public async Task CheckForUpdate()
    {
        await _selfUpdateFacade.CheckNow();
    }

    [HttpPost("ApplyUpdate")]
    public async Task ApplyUpdate()
    {
        await _selfUpdateFacade.ApplyUpdate();
    }

    [HttpPost("SetAutoUpdate")]
    public async Task SetAutoUpdate(SetAutoUpdateRequest request)
    {
        await _selfUpdateFacade.SetAutoUpdate(request.Enabled);
    }

    [HttpGet("GetUpdateLogs")]
    public async Task<ImmutableArray<UpdateLogResponse>> GetUpdateLogs()
    {
        var logs = await _db.UpdateLogs
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new UpdateLogResponse(x.Id, x.CreatedAt, x.Log))
            .ToListAsync();
        return logs.ToImmutableArray();
    }
}

public record AppDetailsResponse(
    string BuildTime,
    string BuildCommit,
    string DotNetVersion,
    string OSDescription);

public record UpdateStatusResponse(
    bool IsUpdateFeatureAvailable,
    bool IsUpdateAvailable,
    DateTimeOffset? LastCheck,
    bool AutoUpdate);

public record UpdateLogResponse(int Id, DateTimeOffset CreatedAt, string Log);

public record SetAutoUpdateRequest
{
    [Required] public required bool Enabled { get; init; }
}
