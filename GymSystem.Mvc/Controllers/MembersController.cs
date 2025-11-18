using AutoMapper;
using GymSystem.Mvc.Helpers;
using GymSystem.Mvc.Models;
using GymSystem.Mvc.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Mvc.Controllers;

[Authorize(Policy = "AdminOrGymOwner")]
public class MembersController : Controller {
    private readonly ApiHelper _apiHelper;
    private readonly IMapper _mapper;
    private readonly ILogger<MembersController> _logger;

    public MembersController(ApiHelper apiHelper, IMapper mapper, ILogger<MembersController> logger) {
        _apiHelper = apiHelper;
        _mapper = mapper;
        _logger = logger;
    }

    // GET: Members
    public async Task<IActionResult> Index() {
        try {
            var apiMembers = await _apiHelper.GetListAsync<ApiMemberDto>(ApiEndpoints.Members);

            // AutoMapper ile ViewModel'e map et
            var members = _mapper.Map<List<MemberViewModel>>(apiMembers);

            return View(members);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Üyeler listelenirken hata oluştu");
            ViewBag.ErrorMessage = "Bir hata oluştu: " + ex.Message;
            return View(new List<MemberViewModel>());
        }
    }

    // GET: Members/Create
    public IActionResult Create() {
        return View();
    }

    // POST: Members/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateMemberViewModel model) {
        if (!ModelState.IsValid) {
            return View(model);
        }

        try {
            var (success, errorMessage) = await _apiHelper.PostAsync(ApiEndpoints.Members, model);

            if (success) {
                TempData["SuccessMessage"] = "Üye başarıyla eklendi!";
                return RedirectToAction(nameof(Index));
            }
            else {
                ModelState.AddModelError("", $"Üye eklenirken bir hata oluştu. {errorMessage}");
                return View(model);
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Üye eklenirken hata oluştu");
            ModelState.AddModelError("", "Bir hata oluştu: " + ex.Message);
            return View(model);
        }
    }
}