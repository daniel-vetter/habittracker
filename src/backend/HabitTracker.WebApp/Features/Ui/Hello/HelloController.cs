using Microsoft.AspNetCore.Mvc;

namespace HabitTracker.WebApp.Features.Ui.Hello;

[ApiController]
[Route("api/[controller]")]
public class HelloController : ControllerBase
{
    [HttpGet]
    public HelloResponse Get() => new("Hello from HabitTracker!");
}

public record HelloResponse(string Message);
