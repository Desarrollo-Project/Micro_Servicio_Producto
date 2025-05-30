
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Producto.Presentation.Controllers;
using Producto.Application.Commands;
using Producto.Application.DTO;
using MediatR;
using System;
using System.Threading.Tasks;

namespace Producto.Tests;
public class ControllerCreate
{
    private readonly Mock<IMediator> _mediatorMock;

    private readonly ProductosController _controller;

    public ControllerCreate()
    {
        _mediatorMock = new Mock<IMediator>();

        _controller = new ProductosController(_mediatorMock.Object);
    }

    [Fact]
    public async Task CreateProduct_Returns_CreatedAtAction()
    {
        // Arrange
        var dto = new CreateProductoDTO { Nombre = "Producto Test" };
        var expectedId = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateProductCommand>(), default))
                     .ReturnsAsync(expectedId);

        // Act
        var result = await _controller.CreateProduct(dto) as CreatedAtActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(nameof(ProductosController.CreateProduct), result.ActionName);

        // Acceso correcto al ID en el objeto anónimo
        var returnedValue = result.Value;
        Assert.NotNull(returnedValue);

        var idProperty = returnedValue.GetType().GetProperty("id");
        Assert.NotNull(idProperty);

        var actualId = (Guid)idProperty.GetValue(returnedValue);
        Assert.Equal(expectedId, actualId);
    }




    [Fact]
    public async Task CreateProduct_Returns_BadRequest_When_Exception_Occurs()
    {
        // Arrange
        var dto = new CreateProductoDTO { Nombre = "Producto Test" };
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateProductCommand>(), default))
                     .ThrowsAsync(new Exception("Error interno"));

        // Act
        var result = await _controller.CreateProduct(dto) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("Error interno", result.Value.ToString());
    }
}