using Xunit;
using Moq;
using MongoDB.Driver;
using Producto.Infrastructure.Persistance.DataBase;
using Producto.Domain.Aggregates;

namespace Producto.Tests.Infrastructura.Tests.DataBase;
public class MongoInitializerTests
{
    [Fact]
    public void Initialize_Should_Connect_To_Database_Without_Creating_Or_Deleting()
    {
        // Arrange
        var mongoClientMock = new Mock<IMongoClient>();
        var databaseMock = new Mock<IMongoDatabase>();
        var collectionMock = new Mock<IMongoCollection<ProductMongo>>();

        mongoClientMock.Setup(m => m.GetDatabase("productos_db", null)).Returns(databaseMock.Object);
        databaseMock.Setup(db => db.GetCollection<ProductMongo>("productos", null)).Returns(collectionMock.Object);

        var initializer = new MongoInitializer(mongoClientMock.Object);

        // Act
        initializer.Initialize();

        // Assert
        mongoClientMock.Verify(m => m.GetDatabase("productos_db", null), Times.Once);
        databaseMock.Verify(db => db.GetCollection<ProductMongo>("productos", null), Times.Once);
        collectionMock.VerifyNoOtherCalls(); // Confirma que no se insertó/eliminó nada
    }
}