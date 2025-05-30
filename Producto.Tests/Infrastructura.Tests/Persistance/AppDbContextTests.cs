using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Producto.Infrastructure.Persistance;
using Producto.Domain.Aggregates;
using Producto.Domain.VO;

namespace Producto.Tests.Infrastructura.Tests.Persistance;

public class AppDbContextTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void Can_Create_Database_And_Add_Product()
    {
        var dbContext = GetInMemoryDbContext();

        var producto = new Product(
            Guid.NewGuid(),
            new NombreProductoVO("Laptop"),
            new PrecioBaseVO(999.99m),
            new CategoriaVO("Electrónica"),
            new ImagenUrlVo("https://example.com/laptop.jpg")
        );

        dbContext.Productos.Add(producto);
        dbContext.SaveChanges();

        var retrievedProduct = dbContext.Productos.Find(producto.Id);

        Assert.NotNull(retrievedProduct);
        Assert.Equal("Laptop", retrievedProduct.Nombre.Valor);
    }

    [Fact]
    public void ValueObjects_Are_Correctly_Mapped()
    {
        var dbContext = GetInMemoryDbContext();

        var producto = new Product(
            Guid.NewGuid(),
            new NombreProductoVO("Smartphone"),
            new PrecioBaseVO(499.99m),
            new CategoriaVO("Tecnología"),
            new ImagenUrlVo("https://example.com/smartphone.jpg")
        );

        dbContext.Productos.Add(producto);
        dbContext.SaveChanges();

        var retrievedProduct = dbContext.Productos.Find(producto.Id);

        Assert.NotNull(retrievedProduct);
        Assert.Equal("Smartphone", retrievedProduct.Nombre.Valor);
        Assert.Equal(499.99m, retrievedProduct.PrecioBase.Valor);
        Assert.Equal("Tecnología", retrievedProduct.Categoria.Valor);
        Assert.Equal("https://example.com/smartphone.jpg", retrievedProduct.ImagenUrl.Valor);
    }
}
