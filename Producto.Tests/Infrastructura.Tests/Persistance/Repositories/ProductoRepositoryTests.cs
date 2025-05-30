using Xunit;
using Microsoft.EntityFrameworkCore;
using Producto.Infrastructure.Persistance;
using Producto.Infrastructure.Persistance.Repositories;
using Producto.Domain.Aggregates;
using System;
using System.Threading.Tasks;
using Producto.Domain.VO;

namespace Producto.Tests.Infrastructura.Tests.Persistance.Repositories;

public class ProductoRepositoryTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task AgregarAsync_Should_Add_Product_To_Database()
    {
        var dbContext = GetInMemoryDbContext();
        var repository = new ProductoRepository(dbContext);

        var producto = new Product(Guid.NewGuid(), new NombreProductoVO("Laptop"), new PrecioBaseVO(999.99m), new CategoriaVO("Electrónica"), new ImagenUrlVo("https://example.com/laptop.jpg"));

        await repository.AgregarAsync(producto);

        var retrievedProduct = await repository.ObtenerPorIdAsync(producto.Id);

        Assert.NotNull(retrievedProduct);
        Assert.Equal("Laptop", retrievedProduct.Nombre.Valor);
    }


    [Fact]
    public async Task ActualizarAsync_Should_Update_Product()
    {
        var dbContext = GetInMemoryDbContext();
        var repository = new ProductoRepository(dbContext);

        var producto = new Product(Guid.NewGuid(), new NombreProductoVO("Tablet"), new PrecioBaseVO(299.99m), new CategoriaVO("Tecnología"), new ImagenUrlVo("https://example.com/tablet.jpg"));

        await repository.AgregarAsync(producto);

        // 🔹 Corrección: No crear una nueva instancia, modificar la existente
        producto = new Product(producto.Id, new NombreProductoVO("Tablet Pro"), producto.PrecioBase, producto.Categoria, producto.ImagenUrl);

        await repository.ActualizarAsync(producto);

        var retrievedProduct = await repository.ObtenerPorIdAsync(producto.Id);

        Assert.Equal("Tablet Pro", retrievedProduct.Nombre.Valor);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_Should_Return_Null_If_Product_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext();
        var repository = new ProductoRepository(dbContext);

        var retrievedProduct = await repository.ObtenerPorIdAsync(Guid.NewGuid());

        Assert.Null(retrievedProduct);
    }

    [Fact]
    public async Task EliminarAsync_Should_Remove_Product_From_Database()
    {
        var dbContext = GetInMemoryDbContext();
        var repository = new ProductoRepository(dbContext);

        var producto = new Product(Guid.NewGuid(), new NombreProductoVO("Monitor"), new PrecioBaseVO(199.99m), new CategoriaVO("Computación"), new ImagenUrlVo("https://example.com/monitor.jpg"));

        await repository.AgregarAsync(producto);
        await repository.EliminarAsync(producto.Id);

        var retrievedProduct = await repository.ObtenerPorIdAsync(producto.Id);

        Assert.Null(retrievedProduct);
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_If_Context_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new ProductoRepository(null!));
    }

    [Fact]
    public async Task AgregarAsync_Should_Throw_ArgumentNullException_If_Product_Is_Null()
    {
        var dbContext = GetInMemoryDbContext();
        var repository = new ProductoRepository(dbContext);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.AgregarAsync(null!));
    }

    [Fact]
    public async Task ActualizarAsync_Should_Throw_ArgumentNullException_If_Product_Is_Null()
    {
        var dbContext = GetInMemoryDbContext();
        var repository = new ProductoRepository(dbContext);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.ActualizarAsync(null!));
    }

    [Fact]
    public async Task ObtenerTodosAsync_Should_Throw_NotImplementedException()
    {
        var dbContext = GetInMemoryDbContext();
        var repository = new ProductoRepository(dbContext);

        await Assert.ThrowsAsync<NotImplementedException>(() => repository.ObtenerTodosAsync());
    }

}