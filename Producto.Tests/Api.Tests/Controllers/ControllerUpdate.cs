using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Producto.Application.Commands;
using Producto.Application.DTO;
using Producto.Presentation.Controllers;

namespace Producto.Tests;

public class ControllerUpdate
{
    private readonly Mock<IMediator> _mediatorMock;

    private readonly ProductosController _controller;

    public ControllerUpdate()
    {
        _mediatorMock = new Mock<IMediator>();

        _controller = new ProductosController(_mediatorMock.Object);
    }

    [Fact]
    public async Task UpdateProduct_Returns_NoContent_When_Successful()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new UpdateProductoDTO { Id = id, Nombre = "Producto Actualizado" };

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateProductCommand>(), default))
                     .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateProduct(id, dto) as NoContentResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(204, result.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_Returns_NotFound_When_Product_Does_Not_Exist()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new UpdateProductoDTO { Id = id, Nombre = "Producto No Existente" };

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateProductCommand>(), default))
                     .ThrowsAsync(new KeyNotFoundException("Producto no encontrado"));

        // Act
        var result = await _controller.UpdateProduct(id, dto) as NotFoundObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
        Assert.Contains("Producto no encontrado", result.Value.ToString());
    }

    [Fact]
    public async Task UpdateProduct_Returns_BadRequest_When_Ids_Are_Inconsistent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new UpdateProductoDTO { Id = Guid.NewGuid(), Nombre = "Producto Incorrecto" };

        // Act
        var result = await _controller.UpdateProduct(id, dto) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("El ID del producto en la URL no coincide", result.Value.ToString());
    }

    [Fact]
    public async Task UpdateProduct_Returns_BadRequest_When_Exception_Occurs()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new UpdateProductoDTO { Id = id, Nombre = "Producto Test" };

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateProductCommand>(), default))
                     .ThrowsAsync(new Exception("Error interno"));

        // Act
        var result = await _controller.UpdateProduct(id, dto) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("Error interno", result.Value.ToString());
    }
}