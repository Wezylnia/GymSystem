using Microsoft.AspNetCore.Mvc;
using GymSystem.Application.Abstractions.Services;

namespace GymSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;

    public DiagnosticsController(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Servis kayıtlarını test et
    /// </summary>
    [HttpGet("test-services")]
    public IActionResult TestServices()
    {
        var results = new List<object>();

        // IAppointmentService kaydını kontrol et
        try
        {
            var appointmentService = _serviceProvider.GetService<IAppointmentService>();
            results.Add(new { Service = "IAppointmentService", Status = appointmentService != null ? "✓ Registered" : "✗ NOT Registered" });
        }
        catch (Exception ex)
        {
            results.Add(new { Service = "IAppointmentService", Status = "✗ Error", Error = ex.Message });
        }

        // ITrainerService kaydını kontrol et
        try
        {
            var trainerService = _serviceProvider.GetService<ITrainerService>();
            results.Add(new { Service = "ITrainerService", Status = trainerService != null ? "✓ Registered" : "✗ NOT Registered" });
        }
        catch (Exception ex)
        {
            results.Add(new { Service = "ITrainerService", Status = "✗ Error", Error = ex.Message });
        }

        // IMemberService kaydını kontrol et
        try
        {
            var memberService = _serviceProvider.GetService<IMemberService>();
            results.Add(new { Service = "IMemberService", Status = memberService != null ? "✓ Registered" : "✗ NOT Registered" });
        }
        catch (Exception ex)
        {
            results.Add(new { Service = "IMemberService", Status = "✗ Error", Error = ex.Message });
        }

        return Ok(results);
    }
}