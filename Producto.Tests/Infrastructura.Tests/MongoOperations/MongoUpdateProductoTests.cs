using Xunit;
using Moq;
using MongoDB.Driver;
using Producto.Domain.Aggregates;
using Producto.Domain.Events;
using Producto.Infrastructure.Persistence.MongoOperations;
using Producto.Domain.VO; // Para los Value Objects
using System;
using System.Threading.Tasks;


namespace Producto.Tests.Infrastructura.Tests.MongoOperations;

public class MongoUpdateProductoTests
{
    private readonly Mock<IMongoDatabase> _mongoDatabaseMock;
    private readonly Mock<IMongoCollection<ProductMongo>> _productosCollectionMock;
    private readonly MongoUpdateProducto _mongoUpdateProducto;

    public MongoUpdateProductoTests()
    {
        _mongoDatabaseMock = new Mock<IMongoDatabase>();
        _productosCollectionMock = new Mock<IMongoCollection<ProductMongo>>();

        _mongoDatabaseMock.Setup(db => db.GetCollection<ProductMongo>("productos", null))
                          .Returns(_productosCollectionMock.Object);

        _mongoUpdateProducto = new MongoUpdateProducto(_mongoDatabaseMock.Object);
    }

    /// ✅ **Caso base: Producto actualizado correctamente**
    [Fact]
    public async Task ActualizarAsync_DeberiaActualizarProducto_Exitosamente()
    {
        var evento = new ProductoActualizadoEvent(Guid.NewGuid(), "Nuevo Nombre", 99.99m, "Electrónica", "https://example.com/imagen.jpg");

        var replaceResult = new ReplaceOneResult.Acknowledged(1, 1, null);

        _productosCollectionMock.Setup(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<ProductMongo>>(),
                                                              It.IsAny<ProductMongo>(),
                                                              It.IsAny<ReplaceOptions>(), default))
                                .ReturnsAsync(replaceResult);

        await _mongoUpdateProducto.ActualizarAsync(evento);

        _productosCollectionMock.Verify(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<ProductMongo>>(),
                                                               It.IsAny<ProductMongo>(),
                                                               It.IsAny<ReplaceOptions>(), default), Times.Once);
    }

    /// ✅ **Caso base: Producto insertado (Upsert)**
    [Fact]
    public async Task ActualizarAsync_DeberiaInsertarProducto_SiNoExiste()
    {
        var evento = new ProductoActualizadoEvent(Guid.NewGuid(), "Nuevo Nombre", 99.99m, "Electrónica", "https://example.com/imagen.jpg");

        var replaceResult = new ReplaceOneResult.Acknowledged(0, 0, evento.Id.ToString()); // Simula una inserción (Upsert)

        _productosCollectionMock.Setup(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<ProductMongo>>(),
                                                              It.IsAny<ProductMongo>(),
                                                              It.IsAny<ReplaceOptions>(), default))
                                .ReturnsAsync(replaceResult);

        await _mongoUpdateProducto.ActualizarAsync(evento);

        _productosCollectionMock.Verify(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<ProductMongo>>(),
                                                               It.IsAny<ProductMongo>(),
                                                               It.IsAny<ReplaceOptions>(), default), Times.Once);
    }

    /// ❌ **Caso: No se requiere modificación**
    [Fact]
    public async Task ActualizarAsync_DeberiaDetectarProductoSinCambios()
    {
        var evento = new ProductoActualizadoEvent(Guid.NewGuid(), "Nuevo Nombre", 99.99m, "Electrónica", "https://example.com/imagen.jpg");

        var replaceResult = new ReplaceOneResult.Acknowledged(1, 0, null); // Simula que el producto ya estaba igual

        _productosCollectionMock.Setup(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<ProductMongo>>(),
                                                              It.IsAny<ProductMongo>(),
                                                              It.IsAny<ReplaceOptions>(), default))
                                .ReturnsAsync(replaceResult);

        await _mongoUpdateProducto.ActualizarAsync(evento);

        _productosCollectionMock.Verify(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<ProductMongo>>(),
                                                               It.IsAny<ProductMongo>(),
                                                               It.IsAny<ReplaceOptions>(), default), Times.Once);
    }

    /// ❌ **Caso: Producto no encontrado para actualizar**
    [Fact]
    public async Task ActualizarAsync_DeberiaManejarProductoNoEncontrado_Correctamente()
    {
        var evento = new ProductoActualizadoEvent(Guid.NewGuid(), "Nuevo Nombre", 99.99m, "Electrónica", "https://example.com/imagen.jpg");

        var replaceResult = new ReplaceOneResult.Acknowledged(0, 0, null); // Simula que no se encontró y no se insertó

        _productosCollectionMock.Setup(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<ProductMongo>>(),
                                                              It.IsAny<ProductMongo>(),
                                                              It.IsAny<ReplaceOptions>(), default))
                                .ReturnsAsync(replaceResult);

        await _mongoUpdateProducto.ActualizarAsync(evento);

        _productosCollectionMock.Verify(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<ProductMongo>>(),
                                                               It.IsAny<ProductMongo>(),
                                                               It.IsAny<ReplaceOptions>(), default), Times.Once);
    }

    /// ❌ **Caso: Excepción en MongoDB**
    [Fact]
    public async Task ActualizarAsync_DeberiaLanzarExcepcion_SiMongoFalla()
    {
        var evento = new ProductoActualizadoEvent(Guid.NewGuid(), "Nuevo Nombre", 99.99m, "Electrónica", "https://example.com/imagen.jpg");

        _productosCollectionMock.Setup(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<ProductMongo>>(),
                                                              It.IsAny<ProductMongo>(),
                                                              It.IsAny<ReplaceOptions>(), default))
                                .ThrowsAsync(new MongoException("Error en MongoDB"));

        await Assert.ThrowsAsync<MongoException>(() => _mongoUpdateProducto.ActualizarAsync(evento));
    }

    /// ❌ **Caso: Evento nulo**
    [Fact]
    public async Task ActualizarAsync_DeberiaManejarEventoNulo_SinError()
    {
        await _mongoUpdateProducto.ActualizarAsync(null);

        _productosCollectionMock.Verify(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<ProductMongo>>(),
                                                               It.IsAny<ProductMongo>(),
                                                               It.IsAny<ReplaceOptions>(), default), Times.Never);
    }

}