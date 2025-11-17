using GymSystem.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace GymSystem.Mvc.Controllers;

[Authorize(Policy = "AdminOrGymOwner")]
public class MembersController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MembersController> _logger;

    public MembersController(IHttpClientFactory httpClientFactory, ILogger<MembersController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // GET: Members
    public async Task<IActionResult> Index()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var response = await client.GetAsync("/api/members");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // API'den gelen response'u deserialize et
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var apiMembers = JsonSerializer.Deserialize<List<ApiMemberDto>>(content, options) 
                    ?? new List<ApiMemberDto>();

                // ViewModel'e map et
                var members = apiMembers.Select(m => new MemberViewModel
                {
                    Id = m.Id,
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    Email = m.Email,
                    PhoneNumber = m.PhoneNumber,
                    MembershipStartDate = m.MembershipStartDate,
                    MembershipEndDate = m.MembershipEndDate,
                    CurrentGymLocationId = m.CurrentGymLocationId,
                    CurrentGymLocationName = m.CurrentGymLocation?.Name, // API'den gelen navigation property
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                }).ToList();

                return View(members);
            }
            else
            {
                _logger.LogError("API'den üyeler alınamadı. Status Code: {StatusCode}", response.StatusCode);
                ViewBag.ErrorMessage = "Üyeler yüklenirken bir hata oluştu.";
                return View(new List<MemberViewModel>());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Üyeler listelenirken hata oluştu");
            ViewBag.ErrorMessage = "Bir hata oluştu: " + ex.Message;
            return View(new List<MemberViewModel>());
        }
    }

    // GET: Members/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Members/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateMemberViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync("/api/members", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Üye başarıyla eklendi!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Üye eklenirken hata oluştu. Status Code: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                
                ModelState.AddModelError("", "Üye eklenirken bir hata oluştu.");
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Üye eklenirken hata oluştu");
            ModelState.AddModelError("", "Bir hata oluştu: " + ex.Message);
            return View(model);
        }
    }
}

// API'den dönen Member DTO
public class ApiMemberDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime MembershipStartDate { get; set; }
    public DateTime? MembershipEndDate { get; set; }
    public int? CurrentGymLocationId { get; set; }
    public ApiGymLocationDto? CurrentGymLocation { get; set; } // Navigation property
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ApiGymLocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}