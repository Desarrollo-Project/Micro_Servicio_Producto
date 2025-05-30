using Xunit;
using Moq;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Producto.Presentation.Controllers;
using Producto.Application.Commands;
using Producto.Domain.Aggregates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Producto.Domain.VO;


namespace Producto.Tests;


public class ControllerGet
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ProductosController _controller;

    public ControllerGet()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new ProductosController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetAllProducts_Returns_Ok_With_Product_List()
    {
        var productos = new List<Product>
        {
            new Product(Guid.NewGuid(), new NombreProductoVO("Producto 1"), new PrecioBaseVO(299.99m), new CategoriaVO("Electrónica"), new ImagenUrlVo("https://example.com/product1.jpg")),
            new Product(Guid.NewGuid(), new NombreProductoVO("Producto 2"), new PrecioBaseVO(499.99m), new CategoriaVO("Tecnología"), new ImagenUrlVo("https://example.com/product2.jpg"))
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetProductsCommand>(), default))
                     .ReturnsAsync(productos);

        var result = await _controller.GetAllProducts() as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        var returnedProductos = Assert.IsType<List<Product>>(result.Value);
        Assert.Equal(2, returnedProductos.Count);
        Assert.Equal("Producto 1", returnedProductos.First().Nombre.Valor);
    }

    [Fact]
    public async Task GetAllProducts_Returns_Ok_With_Empty_List_When_No_Products()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetProductsCommand>(), default))
                     .ReturnsAsync(new List<Product>());

        var result = await _controller.GetAllProducts() as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        var returnedProductos = Assert.IsType<List<Product>>(result.Value);
        Assert.Empty(returnedProductos);
    }

    [Fact]
    public async Task GetAllProducts_Returns_InternalServerError_When_Exception_Occurs()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetProductsCommand>(), default))
            .ThrowsAsync(new Exception("Error interno"));

        var result = await _controller.GetAllProducts() as ObjectResult;

        Assert.NotNull(result);
        Assert.Equal(500, result.StatusCode);
        Assert.Contains("Ocurrió un error al procesar la solicitud", result.Value.ToString());
    }
}