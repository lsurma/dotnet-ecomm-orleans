using ECommerce.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// ORLEANS SILO - Serwer Orleans hostujący grainy
/// 
/// W klasycznym podejściu .NET:
/// - Tworzylibyśmy zwykłą aplikację ASP.NET Core
/// - Serwisy byłyby rejestrowne w DI i dostępne przez kontrolery
/// - Każde żądanie HTTP tworzyłoby nową instancję serwisu (Scoped) lub używało Singleton
/// 
/// W Orleans:
/// - Silo to kontener dla grainów
/// - Grainy są automatycznie aktywowane gdy są potrzebne
/// - Orleans zarządza cyklem życia grainów
/// - Może być wiele Silo w klastrze (scale-out)
/// - Persystencja i clustering są skonfigurowane centralnie
/// 
/// IN CLASSIC .NET APPROACH:
/// - We would create a regular ASP.NET Core application
/// - Services would be registered in DI and available through controllers
/// - Each HTTP request would create new service instance (Scoped) or use Singleton
/// 
/// IN ORLEANS:
/// - Silo is a container for grains
/// - Grains are automatically activated when needed
/// - Orleans manages grain lifecycle
/// - There can be many Silos in a cluster (scale-out)
/// - Persistence and clustering are configured centrally
/// </summary>

var builder = WebApplication.CreateBuilder(args);

// Konfiguracja connection string do PostgreSQL
// Configure PostgreSQL connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=ecommerce_orleans;Username=postgres;Password=postgres";

// Dodaj DbContext (opcjonalne dla tej aplikacji, ale pokazuje integrację z EF Core)
// Add DbContext (optional for this app, but shows EF Core integration)
builder.Services.AddDbContext<ECommerceDbContext>(options =>
    options.UseNpgsql(connectionString));

// KONFIGURACJA ORLEANS SILO
// ORLEANS SILO CONFIGURATION
builder.Host.UseOrleans((context, siloBuilder) =>
{
    // W środowisku developerskim używamy lokalnego clustering (bez bazy)
    // In development we use local clustering (without database)
    if (builder.Environment.IsDevelopment())
    {
        // Localhost clustering - wszystko w jednym procesie
        // Świetne do nauki i debugowania
        // Localhost clustering - everything in one process
        // Great for learning and debugging
        siloBuilder.UseLocalhostClustering();
        
        // Persystencja w pamięci - dane tracone po restarcie
        // In-memory persistence - data lost after restart
        siloBuilder.AddMemoryGrainStorage("Default");
    }
    else
    {
        // W produkcji używamy AdoNet clustering (PostgreSQL)
        // In production we use AdoNet clustering (PostgreSQL)
        siloBuilder.UseAdoNetClustering(options =>
        {
            options.Invariant = "Npgsql";
            options.ConnectionString = connectionString;
        });

        // Persystencja w PostgreSQL
        // Persistence in PostgreSQL
        siloBuilder.AddAdoNetGrainStorage("Default", options =>
        {
            options.Invariant = "Npgsql";
            options.ConnectionString = connectionString;
        });
    }

    // Konfiguracja dashboardu Orleans (opcjonalne, ale pomocne)
    // Orleans dashboard configuration (optional but helpful)
    siloBuilder.UseDashboard(options =>
    {
        options.Port = 8080;
    });
});

var app = builder.Build();

// W środowisku developerskim dodajemy endpoint dla informacji
// In development add info endpoint
if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => new
    {
        Message = "Orleans Silo is running",
        Dashboard = "http://localhost:8080",
        Info = "This is the Orleans server hosting the grains. Use the API project to interact with grains."
    });
}

app.Run();

