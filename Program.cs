using Microsoft.EntityFrameworkCore;
using NotificationsService.Src.Modules.Notifications.Data;
using NotificationsService.Src.Modules.Notifications.Repository;
using NotificationsService.Src.Modules.Notifications.Services;
using NotificationsService.Infrastructure.Kafka;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

// I added CORS here 
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=notifications.db";

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();

var kafkaSettings = new KafkaSettings();
builder.Configuration.GetSection("Kafka").Bind(kafkaSettings);

if (!string.IsNullOrEmpty(kafkaSettings.BootstrapServers))
{
    try
    {
        builder.Services.AddSingleton(kafkaSettings);
        builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
        builder.Services.AddHostedService<NotificationEventConsumer>();
        
        builder.Logging.AddConsole();
        Console.WriteLine($"Kafka integration enabled: {kafkaSettings.BootstrapServers}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to configure Kafka: {ex.Message}. Running in synchronous mode.");
    }
}
else
{
    Console.WriteLine("Running in synchronous mode (Kafka not configured)");
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    context.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");

Console.WriteLine("Notification Service started successfully!");
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"Database: {connectionString}");

try
{
    Console.WriteLine("Starting web server...");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to start web server: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    throw;
}