using Xunit;
using Moq;
using MediatR;
using Producto.Application.Commands;
using Producto.Application.Handlers;
using Producto.Domain.Repositories;
using Producto.Domain.Aggregates;
using Producto.Domain.Events;
using Producto.Domain.VO;
using System;
using System.Threading;
using System.Threading.Tasks;
using Producto.Application.DTO;

namespace Producto.Tests;
public class CommandHandlerCreate
{
    private readonly Mock<IProductoRepository> _productoRepositoryMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly CreateProductoCommandHandler _handler;

    public CommandHandlerCreate()
    {
        _productoRepositoryMock = new Mock<IProductoRepository>();
        _mediatorMock = new Mock<IMediator>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _handler = new CreateProductoCommandHandler(_productoRepositoryMock.Object, _mediatorMock.Object, _eventPublisherMock.Object);
    }

    [Fact]
    public async Task Handle_Returns_Valid_Guid_When_Successful()
    {
        // Arrange
        var dto = new CreateProductoDTO
        {
            Nombre = "Producto Test",
            PrecioBase = 100,
            Categoria = "Electrónica",
            ImagenUrl = "https://example.com/image.jpg" // URL válida
        }; 
        var command = new CreateProductCommand(dto);

        _productoRepositoryMock.Setup(repo => repo.AgregarAsync(It.IsAny<Product>()))
                               .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
    }

    [Fact]
    public async Task Handle_Throws_ArgumentException_When_Invalid_ImageUrl()
    {
        // Arrange
        var dto = new CreateProductoDTO
        {
            Nombre = "Producto Test",
            PrecioBase = 100,
            Categoria = "Electrónica",
            ImagenUrl = "invalid-url" // Forzamos el fallo aquí
        };

        var command = new CreateProductCommand(dto);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Publishes_Event_When_Product_Is_Created()
    {
        // Arrange
        var dto = new CreateProductoDTO
        {
            Nombre = "Producto Test",
            PrecioBase = 100,
            Categoria = "Electrónica",
            ImagenUrl = "https://example.com/image.jpg" // URL válida
        };
        var command = new CreateProductCommand(dto);

        _productoRepositoryMock.Setup(repo => repo.AgregarAsync(It.IsAny<Product>()))
                               .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _eventPublisherMock.Verify(pub => pub.Publish(It.IsAny<ProductoCreadoEvent>(), "productos_exchange", ""), Times.Once);
    }

    [Fact]
    public async Task Handle_Publishes_Event_Using_MediatR()
    {
        // Arrange
        var dto = new CreateProductoDTO
        {
            Nombre = "Producto Test",
            PrecioBase = 100,
            Categoria = "Electrónica",
            ImagenUrl = "https://valid-image-url.com/image.jpg" // Asegurar que sea una URL válida
        };

        var command = new CreateProductCommand(dto);

        _productoRepositoryMock.Setup(repo => repo.AgregarAsync(It.IsAny<Product>()))
                               .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(m => m.Publish(It.IsAny<ProductoCreadoEvent>(), default), Times.Once);
    }
}