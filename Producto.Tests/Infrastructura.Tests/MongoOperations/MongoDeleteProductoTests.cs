using Xunit;
using Moq;
using MongoDB.Driver;
using Producto.Domain.Aggregates;
using Producto.Domain.Events;
using Producto.Infrastructure.Persistence.MongoOperations;
using System;
using System.Threading.Tasks;

namespace Producto.Tests.Infrastructura.Tests.MongoOperations;

public class MongoDeleteProductoTests
{
    private readonly Mock<IMongoDatabase> _mongoDatabaseMock;
    private readonly Mock<IMongoCollection<ProductMongo>> _productosCollectionMock;
    private readonly MongoDeleteProducto _mongoDeleteProducto;

    public MongoDeleteProductoTests()
    {
        _mongoDatabaseMock = new Mock<IMongoDatabase>();
        _productosCollectionMock = new Mock<IMongoCollection<ProductMongo>>();

        _mongoDatabaseMock.Setup(db => db.GetCollection<ProductMongo>("productos", null))
                          .Returns(_productosCollectionMock.Object);

        _mongoDeleteProducto = new MongoDeleteProducto(_mongoDatabaseMock.Object);
    }

    /// ✅ **Caso base: Producto eliminado exitosamente**
    [Fact]
    public async Task EliminarAsync_DeberiaEliminarProducto_Exitosamente()
    {
        var evento = new ProductoEliminadoEvent(Guid.NewGuid());
        var deleteResult = new DeleteResult.Acknowledged(1);

        _productosCollectionMock.Setup(c => c.DeleteOneAsync(It.IsAny<FilterDefinition<ProductMongo>>(), default))
                                .ReturnsAsync(deleteResult);

        await _mongoDeleteProducto.EliminarAsync(evento);

        _productosCollectionMock.Verify(c => c.DeleteOneAsync(It.IsAny<FilterDefinition<ProductMongo>>(), default), Times.Once);
    }

    /// ❌ **Caso: Producto no encontrado**
    [Fact]
    public async Task EliminarAsync_DeberiaManejarProductoNoEncontrado_Correctamente()
    {
        var evento = new ProductoEliminadoEvent(Guid.NewGuid());
        var deleteResult = new DeleteResult.Acknowledged(0);

        _productosCollectionMock.Setup(c => c.DeleteOneAsync(It.IsAny<FilterDefinition<ProductMongo>>(), default))
                                .ReturnsAsync(deleteResult);

        await _mongoDeleteProducto.EliminarAsync(evento);

        _productosCollectionMock.Verify(c => c.DeleteOneAsync(It.IsAny<FilterDefinition<ProductMongo>>(), default), Times.Once);
    }

    /// ❌ **Caso: Excepción en MongoDB**
    [Fact]
    public async Task EliminarAsync_DeberiaLanzarExcepcion_SiMongoFalla()
    {
        var evento = new ProductoEliminadoEvent(Guid.NewGuid());

        _productosCollectionMock.Setup(c => c.DeleteOneAsync(It.IsAny<FilterDefinition<ProductMongo>>(), default))
                                .ThrowsAsync(new MongoException("Error en MongoDB"));

        await Assert.ThrowsAsync<MongoException>(() => _mongoDeleteProducto.EliminarAsync(evento));
    }

    /// ❌ **Caso: Evento nulo**
    [Fact]
    public async Task EliminarAsync_DeberiaManejarEventoNulo_SinError()
    {
        await _mongoDeleteProducto.EliminarAsync(null);

        _productosCollectionMock.Verify(c => c.DeleteOneAsync(It.IsAny<FilterDefinition<ProductMongo>>(), default), Times.Never);
    }
}