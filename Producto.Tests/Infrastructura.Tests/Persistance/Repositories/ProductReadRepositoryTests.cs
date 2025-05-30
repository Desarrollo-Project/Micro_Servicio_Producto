using Xunit;
using Moq;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Producto.Infrastructure.Persistance.Repositories;
using Producto.Domain.Aggregates;
using Producto.Domain.VO;

namespace Producto.Tests.Infrastructura.Tests.Persistance.Repositories;

public class ProductReadRepositoryTests
    {
        private readonly Mock<IMongoDatabase> _mockDatabase;
        private readonly Mock<IMongoCollection<ProductMongo>> _mockCollection;
        private readonly ProductReadRepository _repository;

        public ProductReadRepositoryTests()
        {
            _mockDatabase = new Mock<IMongoDatabase>();
            _mockCollection = new Mock<IMongoCollection<ProductMongo>>();

            _mockDatabase
                .Setup(db => db.GetCollection<ProductMongo>("productos", null))
                .Returns(_mockCollection.Object);

            _repository = new ProductReadRepository(_mockDatabase.Object);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsMappedProducts_WhenProductsExist()
        {
            // Arrange
            var productosMongo = new List<ProductMongo>
            {
                new ProductMongo
                {
                    Id = Guid.NewGuid(),
                    Nombre = "Test",
                    PrecioBase = 100,
                    Categoria = "Electrónica",
                    ImagenUrl = "http://img.com/test.png"
                }
            };

            var asyncCursorMock = CreateAsyncCursorMock(productosMongo);

            _mockCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<ProductMongo>>(),
                    It.IsAny<FindOptions<ProductMongo, ProductMongo>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(asyncCursorMock.Object);

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal("Test", result.First().Nombre);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEmptyList_WhenNoProductsExist()
        {
            // Arrange
            var asyncCursorMock = CreateAsyncCursorMock(new List<ProductMongo>());

            _mockCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<ProductMongo>>(),
                    It.IsAny<FindOptions<ProductMongo, ProductMongo>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(asyncCursorMock.Object);

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsProduct_WhenFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            var productMongo = new ProductMongo
            {
                Id = id,
                Nombre = "Test",
                PrecioBase = 100,
                Categoria = "Electrónica",
                ImagenUrl = "http://img.com/test.png"
            };

            var asyncCursorMock = CreateAsyncCursorMock(new List<ProductMongo> { productMongo });

            _mockCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<ProductMongo>>(),
                    It.IsAny<FindOptions<ProductMongo, ProductMongo>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(asyncCursorMock.Object);

            // Act
            var result = await _repository.GetByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result?.Nombre);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            var asyncCursorMock = CreateAsyncCursorMock(new List<ProductMongo>());

            _mockCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<ProductMongo>>(),
                    It.IsAny<FindOptions<ProductMongo, ProductMongo>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(asyncCursorMock.Object);

            // Act
            var result = await _repository.GetByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Constructor_ThrowsException_WhenDatabaseIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ProductReadRepository(null!));
        }

        // Utilidad para simular un cursor async de MongoDB
        private Mock<IAsyncCursor<ProductMongo>> CreateAsyncCursorMock(List<ProductMongo> list)
        {
            var mock = new Mock<IAsyncCursor<ProductMongo>>();
            mock.SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);

            mock.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            mock.SetupGet(_ => _.Current).Returns(list);
            return mock;
        }
    }

