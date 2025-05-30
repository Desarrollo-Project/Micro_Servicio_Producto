using Xunit;
using Moq;
using FluentAssertions;
using MongoDB.Driver;
using Producto.Domain.Events;
using Producto.Domain.Aggregates; // Para ProductMongo
using Producto.Domain.VO;
using Producto.Infrastructure.Persistance.MongoOperations; // Para MongoCreateProducto
using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Connections;
using Microsoft.Extensions.Logging;

namespace Producto.Tests.Infrastructura.Tests.MongoOperations;

    public class MongoCreateProductoTests
    {
        private readonly Mock<IMongoDatabase> _mockMongoDatabase;
        private readonly Mock<IMongoCollection<ProductMongo>> _mockMongoCollection;

        public MongoCreateProductoTests()
        {
            _mockMongoCollection = new Mock<IMongoCollection<ProductMongo>>();
            _mockMongoDatabase = new Mock<IMongoDatabase>();

            // Configurar el mock de IMongoDatabase para que devuelva el mock de IMongoCollection
            _mockMongoDatabase.Setup(db => db.GetCollection<ProductMongo>(
                    "productos", // Nombre exacto de la colección usado en la clase
                    It.IsAny<MongoCollectionSettings>()))
                .Returns(_mockMongoCollection.Object);
        }

        [Fact]
        public void Constructor_ConMongoDatabaseNull_DebeLanzarArgumentNullException()
        {
            // Arrange
            IMongoDatabase? nullDatabase = null;

            // Act
            Action action = () => new MongoCreateProducto(nullDatabase!);

            // Assert
            action.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("mongoDatabase");
        }

        [Fact]
        public void Constructor_ConMongoDatabaseValido_DebeObtenerColeccionCorrectamente()
        {
            // Arrange & Act
            // La creación del objeto en el setup de la clase de prueba ya invoca el constructor.
            // Si no hay excepción, y el mock se configuró bien, este test verifica que GetCollection fue llamado.
            var creator = new MongoCreateProducto(_mockMongoDatabase.Object);

            // Assert
            // Verifica que GetCollection fue llamado exactamente una vez con el nombre "productos".
            _mockMongoDatabase.Verify(db => db.GetCollection<ProductMongo>(
                    "productos", It.IsAny<MongoCollectionSettings>()),
                Times.Once);
        }

        [Fact]
        public async Task CrearAsync_ConEventoNull_NoDebeLlamarAInsertOneAsync()
        {
            // Arrange
            var creator = new MongoCreateProducto(_mockMongoDatabase.Object);
            ProductoCreadoEvent? eventoNull = null;

            // Act
            await creator.CrearAsync(eventoNull!);

            // Assert
            _mockMongoCollection.Verify(c => c.InsertOneAsync(
                    It.IsAny<ProductMongo>(),
                    It.IsAny<InsertOneOptions>(), // O null si no usas opciones explícitas
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CrearAsync_ConEventoValido_DebeLlamarAInsertOneAsyncUnaVez()
        {
            // Arrange
            var creator = new MongoCreateProducto(_mockMongoDatabase.Object);
            var evento = new ProductoCreadoEvent(Guid.NewGuid(), "Test", 10m, "Cat", "http://example.com/valid_image.png");

            _mockMongoCollection.Setup(c => c.InsertOneAsync(It.IsAny<ProductMongo>(), null, default))
                .Returns(Task.CompletedTask); // Simula una inserción exitosa

            // Act
            await creator.CrearAsync(evento);

            // Assert
            _mockMongoCollection.Verify(c => c.InsertOneAsync(
                    It.IsAny<ProductMongo>(), null, default),
                Times.Once);
        }


        [Fact]
        public async Task CrearAsync_ConEventoValido_DebeMapearIdCorrectamenteAProductoMongo()
        {
            // Arrange
            var creator = new MongoCreateProducto(_mockMongoDatabase.Object);
            var eventoId = Guid.NewGuid();
            var evento = new ProductoCreadoEvent(eventoId, "Test", 10m, "Cat", "http://example.com/valid_image.png");
            ProductMongo? productoMongoCapturado = null;

            _mockMongoCollection.Setup(c => c.InsertOneAsync(It.IsAny<ProductMongo>(), null, default))
                .Callback<ProductMongo, InsertOneOptions, CancellationToken>((p, _, __) => productoMongoCapturado = p)
                .Returns(Task.CompletedTask);

            // Act
            await creator.CrearAsync(evento);

            // Assert
            productoMongoCapturado?.Id.Should().Be(eventoId);
        }

        [Fact]
        public async Task CrearAsync_ConEventoValido_DebeMapearNombreCorrectamenteAProductoMongo()
        {
            // Arrange
            var creator = new MongoCreateProducto(_mockMongoDatabase.Object);
            var eventoNombre = "Nombre Correcto";
            var evento = new ProductoCreadoEvent(Guid.NewGuid(), eventoNombre, 10m, "Cat", "http://example.com/valid_image.png");
            ProductMongo? productoMongoCapturado = null;

            _mockMongoCollection.Setup(c => c.InsertOneAsync(It.IsAny<ProductMongo>(), null, default))
                .Callback<ProductMongo, InsertOneOptions, CancellationToken>((p, _, __) => productoMongoCapturado = p)
                .Returns(Task.CompletedTask);

            // Act
            await creator.CrearAsync(evento);

            // Assert
            productoMongoCapturado?.Nombre?.Valor.Should().Be(eventoNombre);
        }

        [Fact]
        public async Task CrearAsync_ConEventoValido_DebeMapearPrecioBaseCorrectamenteAProductoMongo()
        {
            // Arrange
            var creator = new MongoCreateProducto(_mockMongoDatabase.Object);
            var eventoPrecio = 123.45m;
            var evento = new ProductoCreadoEvent(Guid.NewGuid(), "Test", eventoPrecio, "Cat", "http://example.com/valid_image.png");
            ProductMongo? productoMongoCapturado = null;

            _mockMongoCollection.Setup(c => c.InsertOneAsync(It.IsAny<ProductMongo>(), null, default))
                .Callback<ProductMongo, InsertOneOptions, CancellationToken>((p, _, __) => productoMongoCapturado = p)
                .Returns(Task.CompletedTask);

            // Act
            await creator.CrearAsync(evento);

            // Assert
            productoMongoCapturado?.PrecioBase?.Valor.Should().Be(eventoPrecio);
        }

        [Fact]
        public async Task CrearAsync_ConEventoValido_DebeMapearCategoriaCorrectamenteAProductoMongo()
        {
            // Arrange
            var creator = new MongoCreateProducto(_mockMongoDatabase.Object);
            var eventoCategoria = "Categoria Test";
            var evento = new ProductoCreadoEvent(Guid.NewGuid(), "Aveo Vinotinto", 10321, eventoCategoria, "http://example.com/valid_image.png");
            ProductMongo? productoMongoCapturado = null;

            _mockMongoCollection.Setup(c => c.InsertOneAsync(It.IsAny<ProductMongo>(), null, default))
                .Callback<ProductMongo, InsertOneOptions, CancellationToken>((p, _, __) => productoMongoCapturado = p)
                .Returns(Task.CompletedTask);

            // Act
            await creator.CrearAsync(evento);

            // Assert
            productoMongoCapturado?.Categoria?.Valor.Should().Be(eventoCategoria);
        }

        [Fact]
        public async Task CrearAsync_ConEventoValido_DebeMapearImagenUrlCorrectamenteAProductoMongo()
        {
            // Arrange
            var creator = new MongoCreateProducto(_mockMongoDatabase.Object);
            var eventoImagenUrl = "http://test.com/imagen.png";
            var evento = new ProductoCreadoEvent(Guid.NewGuid(), "Aveo Vinotinto", 10m, "Carros nuevos", eventoImagenUrl);
            ProductMongo? productoMongoCapturado = null;

            _mockMongoCollection.Setup(c => c.InsertOneAsync(It.IsAny<ProductMongo>(), null, default))
                .Callback<ProductMongo, InsertOneOptions, CancellationToken>((p, _, __) => productoMongoCapturado = p)
                .Returns(Task.CompletedTask);

            // Act
            await creator.CrearAsync(evento);

            // Assert
            productoMongoCapturado?.ImagenUrl?.Valor.Should().Be(eventoImagenUrl);
        }



        [Fact]
        public async Task CrearAsync_CuandoMongoDBFalla_DebeLanzarMongoException()
        {
            var creator = new MongoCreateProducto(_mockMongoDatabase.Object);
            var evento = new ProductoCreadoEvent(Guid.NewGuid(), "Test", 10m, "Cat", "http://example.com/valid_image.png");

            _mockMongoCollection.Setup(c => c.InsertOneAsync(It.IsAny<ProductMongo>(), null, default))
                .ThrowsAsync(new MongoException("Error en MongoDB"));

            await Assert.ThrowsAsync<MongoException>(() => creator.CrearAsync(evento));
        }



        [Fact]
        public async Task CrearAsync_CuandoOcurreExcepcionGenerica_DebeLanzarException()
        {
            var creator = new MongoCreateProducto(_mockMongoDatabase.Object);
            var evento = new ProductoCreadoEvent(Guid.NewGuid(), "Test", 10m, "Cat", "http://example.com/valid_image.png");

            _mockMongoCollection.Setup(c => c.InsertOneAsync(It.IsAny<ProductMongo>(), null, default))
                .ThrowsAsync(new Exception("Error inesperado"));

            await Assert.ThrowsAsync<Exception>(() => creator.CrearAsync(evento));
        }

}


