using Xunit;
using Moq;
using MediatR;
using Producto.Application.Commands;
using Producto.Application.Handlers;
using Producto.Domain.Repositories;
using Producto.Domain.Aggregates;
using Producto.Domain.Events;
using System;
using System.Threading;
using System.Threading.Tasks;
using Producto.Application.DTO;
using Producto.Domain.VO;

namespace Producto.Tests;

public class CommandHandlerUpdate
{
    private readonly Mock<IProductoRepository> _productoRepositoryMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly UpdateProductCommandHandler _handler;

    public CommandHandlerUpdate()
    {
        _productoRepositoryMock = new Mock<IProductoRepository>();
        _mediatorMock = new Mock<IMediator>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _handler = new UpdateProductCommandHandler(_productoRepositoryMock.Object, _mediatorMock.Object, _eventPublisherMock.Object);
    }

    [Fact]
    public async Task Handle_Updates_Product_When_Exists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var productoOriginal = new Product(id, new NombreProductoVO("Producto Viejo"), new PrecioBaseVO(100), new CategoriaVO("Electrónica"), new ImagenUrlVo("https://old-url.com/image.jpg"));

        var dto = new UpdateProductoDTO { Id = id, Nombre = "Producto Actualizado", PrecioBase = 150, Categoria = "Nueva Categoría", ImagenUrl = "https://example.com/image.jpg" };
        var command = new UpdateProductCommand(dto);

        _productoRepositoryMock.Setup(repo => repo.ObtenerPorIdAsync(id))
                               .ReturnsAsync(productoOriginal);

        _productoRepositoryMock.Setup(repo => repo.ActualizarAsync(It.IsAny<Product>()))
                               .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _productoRepositoryMock.Verify(repo => repo.ActualizarAsync(It.Is<Product>(p =>
            p.Id == id &&
            p.Nombre.Valor == dto.Nombre &&
            p.PrecioBase.Valor == dto.PrecioBase &&
            p.Categoria.Valor == dto.Categoria &&
            p.ImagenUrl.Valor == dto.ImagenUrl
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_Throws_KeyNotFoundException_When_Product_Does_Not_Exist()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new UpdateProductoDTO { Id = id, Nombre = "Producto Test", PrecioBase = 100, Categoria = "Electrónica", ImagenUrl = "https://example.com/image.jpg" };
        var command = new UpdateProductCommand(dto);

        _productoRepositoryMock.Setup(repo => repo.ObtenerPorIdAsync(id))
                               .ReturnsAsync((Product)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Publishes_Event_Using_MediatR()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new UpdateProductoDTO { Id = id, Nombre = "Producto Actualizado", PrecioBase = 150, Categoria = "Electrónica", ImagenUrl = "https://example.com/image.jpg" };
        var command = new UpdateProductCommand(dto);

        var producto = new Product(id, new NombreProductoVO(dto.Nombre), new PrecioBaseVO(dto.PrecioBase), new CategoriaVO(dto.Categoria), new ImagenUrlVo(dto.ImagenUrl));

        _productoRepositoryMock.Setup(repo => repo.ObtenerPorIdAsync(id))
                               .ReturnsAsync(producto);

        _productoRepositoryMock.Setup(repo => repo.ActualizarAsync(It.IsAny<Product>()))
                               .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(m => m.Publish(It.IsAny<ProductoActualizadoEvent>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_Publishes_Event_When_Product_Is_Updated()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new UpdateProductoDTO { Id = id, Nombre = "Producto Actualizado", PrecioBase = 150, Categoria = "Electrónica", ImagenUrl = "https://example.com/image.jpg" };
        var command = new UpdateProductCommand(dto);

        var producto = new Product(id, new NombreProductoVO(dto.Nombre), new PrecioBaseVO(dto.PrecioBase), new CategoriaVO(dto.Categoria), new ImagenUrlVo(dto.ImagenUrl));

        _productoRepositoryMock.Setup(repo => repo.ObtenerPorIdAsync(id))
                               .ReturnsAsync(producto);

        _productoRepositoryMock.Setup(repo => repo.ActualizarAsync(It.IsAny<Product>()))
                               .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _eventPublisherMock.Verify(pub => pub.Publish(It.IsAny<ProductoActualizadoEvent>(), "productos_exchange", "producto.actualizado"), Times.Once);
    }
}