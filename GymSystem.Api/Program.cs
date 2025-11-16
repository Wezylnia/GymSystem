using GymSystem.Infastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS policy for MVC application - sabit portlar
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvc", policy =>
    {
        policy.WithOrigins("http://localhost:5000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Infrastructure servisleri ekle (Database, Persistence vb.)
builder.Services.AddInfrastructureServices(builder.Configuration, "appsettings.json");

var app = builder.Build();

// Configure the HTTP request pipeline - Swagger sadece Development'ta, ama otomatik açılmaz
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowMvc");

app.UseAuthorization();

app.MapControllers();

app.Run();
