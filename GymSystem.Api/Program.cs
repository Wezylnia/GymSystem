using GymSystem.Infastructure.Extensions;

// Application service imports
using GymSystem.Application.Abstractions.Services;
using GymSystem.Application.Services.Appointments;
using GymSystem.Application.Services.GymLocations;
using GymSystem.Application.Services.Members;
using GymSystem.Application.Services.Reports;
using GymSystem.Application.Services.Services;
using GymSystem.Application.Services.Trainers;

// Common services
using GymSystem.Common.Helpers;

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

// Infrastructure servisleri ekle (Database, Persistence, Identity)
builder.Services.AddInfrastructureServices(builder.Configuration, "appsettings.json");

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