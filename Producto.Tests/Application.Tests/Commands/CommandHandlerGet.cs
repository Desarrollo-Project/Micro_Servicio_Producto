using Xunit;
using Moq;
using MediatR;
using Producto.Application.Commands;
using Producto.Application.Contracts.Persistence;
using Producto.Application.Handlers;
using Producto.Domain.Aggregates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Producto.Domain.VO;


namespace Producto.Tests;
public class GetProductsCommandHandlerTests
{
    private readonly Mock<IProductReadRepository> _productReadRepositoryMock;
    private readonly GetProductsCommandHandler _handler;

    public GetProductsCommandHandlerTests()
    {
        _productReadRepositoryMock = new Mock<IProductReadRepository>();
        _handler = new GetProductsCommandHandler(_productReadRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_Products_List()
    {
        var productos = new List<Product>
        {
            new Product(Guid.NewGuid(), new NombreProductoVO("Laptop"), new PrecioBaseVO(999.99m), new CategoriaVO("Electrónica"), new ImagenUrlVo("https://example.com/laptop.jpg")),
            new Product(Guid.NewGuid(), new NombreProductoVO("Smartphone"), new PrecioBaseVO(499.99m), new CategoriaVO("Tecnología"), new ImagenUrlVo("https://example.com/smartphone.jpg"))
        };

        _productReadRepositoryMock.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(productos);

        var result = await _handler.Handle(new GetProductsCommand(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Equal("Laptop", result.First().Nombre.Valor);
    }

    [Fact]
    public async Task Handle_Should_Return_Empty_List_If_No_Products()
    {
        _productReadRepositoryMock.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var result = await _handler.Handle(new GetProductsCommand(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}