using Automata.Application.Monitoring.Services;
using Automata.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// В web-модуле оставляем только мониторинг, поэтому регистрируем
// ровно то, что требуется для Razor Pages + shared MonitoringService.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<IMonitoringService>(_ =>
    new MonitoringService(BuildConnectionString(builder.Configuration)));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

// Корневой URL сразу ведет на единственную рабочую страницу web-модуля.
app.MapGet("/", context =>
{
    context.Response.Redirect("/Monitor/Index");
    return Task.CompletedTask;
});

app.MapRazorPages()
    .WithStaticAssets();

app.Run();

static string BuildConnectionString(IConfiguration configuration)
{
    var envConnection = Environment.GetEnvironmentVariable("AUTOMATA_CONNECTION_STRING");
    if (!string.IsNullOrWhiteSpace(envConnection))
    {
        return envConnection;
    }

    var configConnection = configuration.GetConnectionString("Automata");
    if (!string.IsNullOrWhiteSpace(configConnection))
    {
        return configConnection;
    }

    return "Host=edu.ngknn.ru;Port=5442;Database=Belov;Username=21P;Password=123;Search Path=automata";
}
