using GymSystem.Common.Helpers;
using GymSystem.Domain.Entities;
using GymSystem.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Mvc.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<AppUser> signInManager,
        UserManager<AppUser> userManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Email veya şifre hatalı.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Hesabınız aktif değil. Lütfen yönetici ile iletişime geçin.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in.", model.Email);
            
            // Role'e göre yönlendirme
            var roles = await _userManager.GetRolesAsync(user);
            
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            
            // Default yönlendirmeler
            if (roles.Contains("Admin"))
            {
                return RedirectToAction("Index", "Home");
            }
            else if (roles.Contains("GymOwner"))
            {
                return RedirectToAction("Index", "GymLocations");
            }
            else if (roles.Contains("Member"))
            {
                return RedirectToAction("Index", "Appointments");
            }
            
            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} account locked out.", model.Email);
            ModelState.AddModelError(string.Empty, "Hesabınız kilitlendi. Lütfen daha sonra tekrar deneyin.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Email veya şifre hatalı.");
        }

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new AppUser
        {
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

        if (result.Succeeded)
        {
            // Yeni kullanıcıları Member rolüne ekle
            await _userManager.AddToRoleAsync(user, "Member");
            
            // Member tablosuna kaydet ve MemberId'yi al
            // (Şimdilik basit tutalım - gerçek projede Member entity'si oluşturulmalı)
            
            _logger.LogInformation("User {Email} created a new account.", model.Email);

            // Auto login
            await _signInManager.SignInAsync(user, isPersistent: false);
            
            TempData["SuccessMessage"] = "Kayıt başarılı! Hoş geldiniz.";
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}