using GymSystem.Domain.Entities;
using GymSystem.Infastructure.Extensions;
using GymSystem.Mvc.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Infrastructure servisleri (Database + Identity)
// Project reference sayesinde assembly'ler otomatik yüklenecek
builder.Services.AddInfrastructureServices(builder.Configuration, "appsettings.json");

// HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOrGymOwner", policy =>
        policy.RequireRole("Admin", "GymOwner"));
    
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
    
    options.AddPolicy("GymOwnerPolicy", policy =>
        policy.Requirements.Add(new GymOwnerRequirement()));
});

builder.Services.AddScoped<IAuthorizationHandler, GymOwnerAuthorizationHandler>();

// Claims transformation
builder.Services.AddScoped<IClaimsTransformation, GymLocationClaimsTransformation>();

// Cookie settings (MVC için)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// HttpClient for API calls
builder.Services.AddHttpClient("GymApi", client =>
{
    var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5001";
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Claims Transformation
public class GymLocationClaimsTransformation : IClaimsTransformation
{
    private readonly UserManager<AppUser> _userManager;

    public GymLocationClaimsTransformation(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated == true)
        {
            var claimsIdentity = (ClaimsIdentity)principal.Identity;
            var user = await _userManager.GetUserAsync(principal);

            if (user != null)
            {
                // GymLocationId claim ekle
                if (user.GymLocationId != null && !claimsIdentity.HasClaim(c => c.Type == "GymLocationId"))
                {
                    claimsIdentity.AddClaim(new Claim("GymLocationId", user.GymLocationId.Value.ToString()));
                }

                // MemberId claim ekle
                if (user.MemberId != null && !claimsIdentity.HasClaim(c => c.Type == "MemberId"))
                {
                    claimsIdentity.AddClaim(new Claim("MemberId", user.MemberId.Value.ToString()));
                }
            }
        }

        return principal;
    }
}