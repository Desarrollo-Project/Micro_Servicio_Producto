using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Producto.Application.Commands;
using Producto.Presentation.Controllers;

namespace Producto.Tests;

public class ControllerDelete
{
    private readonly Mock<IMediator> _mediatorMock;

    private readonly ProductosController _controller;

    public ControllerDelete()
    {
        _mediatorMock = new Mock<IMediator>();

        _controller = new ProductosController(_mediatorMock.Object);
    }

    [Fact]
    public async Task DeleteProduct_Returns_NoContent_When_Successful()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteProductCommand>(), default))
                     .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteProduct(id) as NoContentResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(204, result.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_Returns_NotFound_When_Product_Does_Not_Exist()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteProductCommand>(), default))
                     .ThrowsAsync(new KeyNotFoundException("Producto no encontrado"));

        // Act
        var result = await _controller.DeleteProduct(id) as NotFoundObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
        Assert.Contains("Producto no encontrado", result.Value.ToString());
    }

    [Fact]
    public async Task DeleteProduct_Returns_BadRequest_When_Id_Is_Empty()
    {
        // Arrange
        var id = Guid.Empty;

        // Act
        var result = await _controller.DeleteProduct(id) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("El ID del producto proporcionado no es válido", result.Value.ToString());
    }

    [Fact]
    public async Task DeleteProduct_Returns_BadRequest_When_Exception_Occurs()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteProductCommand>(), default))
                     .ThrowsAsync(new Exception("Error interno"));

        // Act
        var result = await _controller.DeleteProduct(id) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("Ocurrió un error al intentar eliminar el producto", result.Value.ToString());
    }
}