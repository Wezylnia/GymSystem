using GymSystem.Infastructure.Extensions;

// Application service imports
using GymSystem.Application.Abstractions.Services;
using GymSystem.Application.Services.Appointments;
using GymSystem.Application.Services.GymLocations;
using GymSystem.Application.Services.Members;
using GymSystem.Application.Services.Reports;
using GymSystem.Application.Services.Services;
using GymSystem.Application.Services.Trainers;
using GymSystem.Application.Services.AI;
using GymSystem.Application.Services.Membership;

// Common services
using GymSystem.Common.Helpers;
using GymSystem.Application.Abstractions.Services.IAIWorkoutPlan;
using GymSystem.Application.Abstractions.Services.IGemini;
using Microsoft.AspNetCore.DataProtection;
using GymSystem.Application.Services.GymLocations.Profile;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS policy for MVC application
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvc", policy =>
    {
        policy.WithOrigins("http://localhost:5000", "https://localhost:5001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Data Protection for cookie sharing (MVC ve API arasında)
builder.Services.AddDataProtection()
    .SetApplicationName("GymSystem")
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\proje\GymSystem\shared-auth-keys"));

// Infrastructure servisleri ekle (Database, Persistence, Identity)
builder.Services.AddInfrastructureServices(builder.Configuration, "appsettings.json");

// AutoMapper Configuration - Scan assembly containing Profile classes
builder.Services.AddAutoMapper(typeof(GymLocationProfile).Assembly);
Console.WriteLine("[ServiceRegistration] ✓ AutoMapper configured with all profiles");

// Cookie Authentication için yapılandırma (MVC ile paylaşımlı)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "GymSystem.Auth"; // MVC ile aynı cookie name
    options.Cookie.Domain = null; // Same domain (localhost)
    options.Cookie.Path = "/";
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = false; // JavaScript'ten erişilebilir olması için
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // HTTP'de çalışsın (development)
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401; // API için redirect yerine 401 döndür
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = 403; // API için redirect yerine 403 döndür
        return Task.CompletedTask;
    };
});

// HttpClient for Gemini API
builder.Services.AddHttpClient("GeminiApi", client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Common Helpers - MANUEL KAYIT
Console.WriteLine("[ServiceRegistration] Registering common helpers...");

builder.Services.AddScoped<IServiceResponseHelper, ServiceResponseHelper>();
Console.WriteLine("[ServiceRegistration] ✓ IServiceResponseHelper -> ServiceResponseHelper");

// Application Servisleri - MANUEL KAYIT (Hard Coded)
Console.WriteLine("[ServiceRegistration] Registering application services...");

builder.Services.AddScoped<IAppointmentService, AppointmentService>();
Console.WriteLine("[ServiceRegistration] ✓ IAppointmentService -> AppointmentService");

builder.Services.AddScoped<IGymLocationService, GymLocationService>();
Console.WriteLine("[ServiceRegistration] ✓ IGymLocationService -> GymLocationService");

builder.Services.AddScoped<IMemberService, MemberService>();
Console.WriteLine("[ServiceRegistration] ✓ IMemberService -> MemberService");

builder.Services.AddScoped<IReportService, ReportService>();
Console.WriteLine("[ServiceRegistration] ✓ IReportService -> ReportService");

builder.Services.AddScoped<IServiceService, ServiceService>();
Console.WriteLine("[ServiceRegistration] ✓ IServiceService -> ServiceService");

builder.Services.AddScoped<ITrainerService, TrainerService>();
Console.WriteLine("[ServiceRegistration] ✓ ITrainerService -> TrainerService");

// AI Services
builder.Services.AddScoped<IGeminiApiService, GeminiApiService>();
Console.WriteLine("[ServiceRegistration] ✓ IGeminiApiService -> GeminiApiService");

builder.Services.AddScoped<IAIWorkoutPlanService, AIWorkoutPlanService>();
Console.WriteLine("[ServiceRegistration] ✓ IAIWorkoutPlanService -> AIWorkoutPlanService");

builder.Services.AddScoped<IMembershipRequestService, MembershipRequestService>();
Console.WriteLine("[ServiceRegistration] ✓ IMembershipRequestService -> MembershipRequestService");

Console.WriteLine("[ServiceRegistration] All application services registered!");

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowMvc");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();