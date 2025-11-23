using GymSystem.Common.Helpers;
using GymSystem.Domain.Entities;
using GymSystem.Mvc.Helpers;
using GymSystem.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ApiMemberDto = GymSystem.Application.Abstractions.Contract.Member.MemberDto;

namespace GymSystem.Mvc.Controllers;

public class AccountController : Controller {
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<AccountController> _logger;
    private readonly ApiHelper _apiHelper;

    public AccountController(
        SignInManager<AppUser> signInManager,
        UserManager<AppUser> userManager,
        ILogger<AccountController> logger,
        ApiHelper apiHelper) {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
        _apiHelper = apiHelper;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null) {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null) {
            ModelState.AddModelError(string.Empty, "Email veya şifre hatalı.");
            return View(model);
        }

        if (!user.IsActive) {
            ModelState.AddModelError(string.Empty, "Hesabınız aktif değil. Lütfen yönetici ile iletişime geçin.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded) {
            _logger.LogInformation("User {Email} logged in successfully", model.Email);
            return await RedirectAfterLogin(user, returnUrl);
        }

        if (result.IsLockedOut) {
            _logger.LogWarning("User {Email} account locked out", model.Email);
            ModelState.AddModelError(string.Empty, "Hesabınız kilitlendi. Lütfen daha sonra tekrar deneyin.");
        }
        else {
            ModelState.AddModelError(string.Empty, "Email veya şifre hatalı.");
        }

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register() {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model) {
        if (!ModelState.IsValid)
            return View(model);

        var user = new AppUser {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            PhoneNumber = model.PhoneNumber,
            EmailConfirmed = false,
            IsActive = true,
            CreatedAt = DateTimeHelper.Now  // PostgreSQL-safe DateTime
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded) {
            try {
                // 1. Yeni kullanıcıları Member rolüne ekle
                await _userManager.AddToRoleAsync(user, "Member");
                _logger.LogInformation("Member rolü eklendi. User: {Email}", model.Email);

                // 2. Member kaydı oluştur (API üzerinden - AllowAnonymous endpoint kullanarak)
                var memberData = new {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    MembershipStartDate = DateTimeHelper.Now,
                    IsActive = true
                };

                _logger.LogInformation("Member kaydı oluşturuluyor. User: {Email}, Endpoint: {Endpoint}", 
                    model.Email, ApiEndpoints.MembersRegister);

                var (memberCreated, memberDto, errorMessage) = await _apiHelper.PostWithResponseAsync<object, ApiMemberDto>(
                    ApiEndpoints.MembersRegister, memberData);

                _logger.LogInformation("API Response: Success={Success}, Error={Error}", 
                    memberCreated, errorMessage ?? "null");

                if (memberCreated && memberDto != null) {
                    // Member ID'yi kullanıcıya ata
                    user.MemberId = memberDto.Id;
                    await _userManager.UpdateAsync(user);

                    _logger.LogInformation("✅ Member kaydı başarıyla oluşturuldu. User: {Email}, Member ID: {MemberId}", 
                        model.Email, memberDto.Id);
                }
                else {
                    _logger.LogWarning("❌ Member kaydı oluşturulamadı: {Error}. User: {Email}", 
                        errorMessage ?? "Unknown error", model.Email);
                    
                    // API yanıt vermediyse veya hata varsa, direkt database'e ekleyelim
                    _logger.LogInformation("Alternatif yöntem deneniyor: Direkt database insert...");
                    
                    // Bu durumda başka bir mekanizma kullanabiliriz veya hata mesajı gösterebiliriz
                }

                _logger.LogInformation("User {Email} created a new account", model.Email);

                // Auto login
                await _signInManager.SignInAsync(user, isPersistent: false);

                if (memberCreated) {
                    TempData["SuccessMessage"] = "Kayıt başarılı! Hoş geldiniz.";
                }
                else {
                    TempData["WarningMessage"] = "Kayıt tamamlandı ancak üyelik kaydınız oluşturulurken bir sorun oluştu. Lütfen yöneticiye başvurun.";
                }
                
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "❌ Register sonrası Member oluşturulurken EXCEPTION. User: {Email}, Message: {Message}", 
                    model.Email, ex.Message);
                
                // Kullanıcı oluşturuldu ama Member kaydı başarısız
                // Yine de giriş yapabilir, sonra düzeltilir
                await _signInManager.SignInAsync(user, isPersistent: false);
                
                TempData["ErrorMessage"] = $"Kayıt tamamlandı ancak üyelik kaydınız oluşturulurken bir sorun oluştu: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        foreach (var error in result.Errors) {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() {
        return View();
    }

    #region Helpers

    private async Task<IActionResult> RedirectAfterLogin(AppUser user, string? returnUrl) {
        // Return URL öncelikli
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        // Role'e göre yönlendirme
        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains("Admin"))
            return RedirectToAction("Index", "Home");

        if (roles.Contains("GymOwner"))
            return RedirectToAction("Index", "GymLocations");

        if (roles.Contains("Member"))
            return RedirectToAction("Index", "Appointments");

        return RedirectToAction("Index", "Home");
    }

    #endregion
}