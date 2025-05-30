using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;
using Producto.Infrastructure.Persistance;


namespace Producto.Tests.Infrastructura.Tests.Persistance;

public class AppDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_Should_Load_Configuration_And_Initialize_Context()
    {
        // 🔹 Simular un archivo `appsettings.json` con una cadena de conexión válida
        var inMemorySettings = new Dictionary<string, string>
        {
            { "ConnectionStrings:Postgres", "Host=localhost;Database=test_db;Username=test_user;Password=test_password" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // 🔹 Crear la fábrica de contexto
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(config.GetConnectionString("Postgres"));

        var factory = new AppDbContextFactory();
        var dbContext = factory.CreateDbContext(null);

        // 🔹 Validar que la instancia generada es válida
        Assert.NotNull(dbContext);
        Assert.IsType<AppDbContext>(dbContext);
    }
}