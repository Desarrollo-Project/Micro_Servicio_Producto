using Xunit;
using Moq;
using MediatR;
using Producto.Application.Commands;
using Producto.Application.Handlers;
using Producto.Domain.Repositories;
using Producto.Domain.Events;
using System;
using System.Threading;
using System.Threading.Tasks;
using Producto.Domain.Aggregates;

namespace Producto.Tests;
public class CommandHandlerDelete
{
    private readonly Mock<IProductoRepository> _productoRepositoryMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly DeleteProductCommandHandler _handler;

    public CommandHandlerDelete()
    {
        _productoRepositoryMock = new Mock<IProductoRepository>();
        _mediatorMock = new Mock<IMediator>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _handler = new DeleteProductCommandHandler(_productoRepositoryMock.Object, _mediatorMock.Object, _eventPublisherMock.Object);
    }

    [Fact]
    public async Task Handle_Deletes_Product_When_Exists()
    {
        // Arrange
        var id = Guid.NewGuid();
        _productoRepositoryMock.Setup(repo => repo.ObtenerPorIdAsync(id))
                               .ReturnsAsync(new Product(id, null, null, null, null));

        _productoRepositoryMock.Setup(repo => repo.EliminarAsync(id))
                               .Returns(Task.CompletedTask);

        var command = new DeleteProductCommand(id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _productoRepositoryMock.Verify(repo => repo.EliminarAsync(id), Times.Once);
    }

    [Fact]
    public async Task Handle_Throws_KeyNotFoundException_When_Product_Does_Not_Exist()
    {
        // Arrange
        var id = Guid.NewGuid();
        _productoRepositoryMock.Setup(repo => repo.ObtenerPorIdAsync(id))
                               .ReturnsAsync((Product)null);

        var command = new DeleteProductCommand(id);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Publishes_Event_When_Product_Is_Deleted()
    {
        // Arrange
        var id = Guid.NewGuid();
        _productoRepositoryMock.Setup(repo => repo.ObtenerPorIdAsync(id))
                               .ReturnsAsync(new Product(id, null, null, null, null));

        _productoRepositoryMock.Setup(repo => repo.EliminarAsync(id))
                               .Returns(Task.CompletedTask);

        var command = new DeleteProductCommand(id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _eventPublisherMock.Verify(pub => pub.Publish(It.IsAny<ProductoEliminadoEvent>(), "productos_exchange", "producto.eliminado"), Times.Once);
    }

    [Fact]
    public async Task Handle_Publishes_Event_Using_MediatR()
    {
        // Arrange
        var id = Guid.NewGuid();
        _productoRepositoryMock.Setup(repo => repo.ObtenerPorIdAsync(id))
                               .ReturnsAsync(new Product(id, null, null, null, null));

        _productoRepositoryMock.Setup(repo => repo.EliminarAsync(id))
                               .Returns(Task.CompletedTask);

        var command = new DeleteProductCommand(id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(m => m.Publish(It.IsAny<ProductoEliminadoEvent>(), default), Times.Once);
    }
}