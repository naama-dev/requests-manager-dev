using Microsoft.EntityFrameworkCore;
using RequestsManager.Api.BLL.Interfaces;
using RequestsManager.Api.BLL.Services;
using RequestsManager.Api.DAL.Interfaces;
using RequestsManager.Api.DAL.Repositories;
using RequestsManager.Api.Data;
using RequestsManager.Api.Middleware;
var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.CommandTimeout(120) // הגדלת זמן ההמתנה ל-120 שניות
    ));
builder.Services.AddMemoryCache();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularClient", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
// רישום שכבת ה-DAL (גישה לנתונים)
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
// רישום שכבת ה-BLL (לוגיקה עסקית)
builder.Services.AddScoped<ITicketsService, TicketsService>();
var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsEnvironment("Testing"))
{
    app.MapControllers();
    app.Run();
    return;
}
// יצירת ה-DB (Migrations) ומילוי נתוני בדיקה
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AngularClient");
app.MapControllers();

app.Run();
public partial class Program { }