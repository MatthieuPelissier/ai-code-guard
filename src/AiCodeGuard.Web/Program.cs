using AiCodeGuard.Core;
using AiCodeGuard.Web.Data;
using AiCodeGuard.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Persistance de l'historique des analyses (SQLite : aucune installation requise).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=aicodeguard.db"));

// Moteur de règles : sans état, partagé par toute l'application.
builder.Services.AddSingleton(_ => DefaultRules.CreateEngine());

// Relecture LLM optionnelle : activée seulement si une clé est fournie
// (dotnet user-secrets set "Llm:ApiKey" "..."), sinon pattern Null Object.
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection(LlmOptions.SectionName));
builder.Services.AddHttpClient<AnthropicLlmReviewer>();
builder.Services.AddScoped<ILlmReviewer>(sp =>
{
    var apiKey = builder.Configuration[$"{LlmOptions.SectionName}:ApiKey"];
    return string.IsNullOrWhiteSpace(apiKey)
        ? new NullLlmReviewer()
        : sp.GetRequiredService<AnthropicLlmReviewer>();
});

builder.Services.AddScoped<AnalysisService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Rend la classe Program accessible aux tests d'intégration éventuels.
public partial class Program { }
