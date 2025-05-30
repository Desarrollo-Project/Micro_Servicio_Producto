using Xunit;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Producto.Domain.Events;
using Producto.Infrastructure.Persistence.MongoOperations;
using Producto.Infrastructure.EventBus.Consumer;
using Producto.Infrastructure.EventBus.InterfaceConsumer;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using FluentAssertions;
using MongoDB.Driver;
using Producto.Infrastructure.Persistence.MongoOperations;

namespace Producto.Tests.Infrastructura.Tests.EventBus.Consumer // Ajusta tu namespace
{
    public class ProductoActualizadoEventConsumerTests
    {
        private readonly Mock<IEventConsumerConnection> _mockEventConsumerConnection;
        private readonly Mock<IServiceProvider> _mockRootServiceProvider;
        private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
        private readonly Mock<IServiceScope> _mockServiceScope;
        private readonly Mock<IServiceProvider> _mockScopedServiceProvider;
        private readonly Mock<MongoUpdateProducto> _mockMongoUpdateProducto;

        private Func<BasicDeliverEventArgs, Task<bool>>? _capturedHandleMessageCallback;

        public ProductoActualizadoEventConsumerTests()
        {
            _mockEventConsumerConnection = new Mock<IEventConsumerConnection>();
            var mockMongoDb = new Mock<IMongoDatabase>();
            _mockMongoUpdateProducto = new Mock<MongoUpdateProducto>(mockMongoDb.Object);

            _mockScopedServiceProvider = new Mock<IServiceProvider>();
            _mockScopedServiceProvider
                .Setup(sp => sp.GetService(typeof(MongoUpdateProducto)))
                .Returns(_mockMongoUpdateProducto.Object);

            _mockServiceScope = new Mock<IServiceScope>();
            _mockServiceScope.Setup(s => s.ServiceProvider).Returns(_mockScopedServiceProvider.Object);

            _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
            _mockServiceScopeFactory.Setup(sf => sf.CreateScope()).Returns(_mockServiceScope.Object);

            _mockRootServiceProvider = new Mock<IServiceProvider>();
            _mockRootServiceProvider
                .Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                .Returns(_mockServiceScopeFactory.Object);

            _mockEventConsumerConnection
                .Setup(c => c.StartConsuming(
                    "productos_actualizado_mongo_queue",
                    "productos_exchange",
                    It.IsAny<Func<BasicDeliverEventArgs, Task<bool>>>()))
                .Callback<string, string, Func<BasicDeliverEventArgs, Task<bool>>>((queue, exchange, callback) =>
                {
                    _capturedHandleMessageCallback = callback;
                });
        }

        private ProductoActualizadoEventConsumer CreateConsumer()
        {
            return new ProductoActualizadoEventConsumer(
                _mockEventConsumerConnection.Object,
                _mockRootServiceProvider.Object
            );
        }

        private BasicDeliverEventArgs CreateBasicDeliverEventArgs(string eventType, object payload, ulong deliveryTag = 1)
        {
            var mockBasicProperties = new Mock<IBasicProperties>();
            mockBasicProperties.SetupGet(p => p.Type).Returns(eventType);
            var messageBody = JsonSerializer.Serialize(payload);
            var bodyBytes = Encoding.UTF8.GetBytes(messageBody);
            return new BasicDeliverEventArgs("ctag", deliveryTag, false, "exchange", "rkey",
                                             mockBasicProperties.Object, new ReadOnlyMemory<byte>(bodyBytes));
        }

        // --- Pruebas del Constructor (ya las tenías) ---
        [Fact]
        public void Constructor_CuandoEventConsumerConnectionEsNull_DebeLanzarArgumentNullException()
        {
            Action action = () => new ProductoActualizadoEventConsumer(null!, _mockRootServiceProvider.Object);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("eventConsumerConnection");
        }

        [Fact]
        public void Constructor_CuandoServiceProviderEsNull_DebeLanzarArgumentNullException()
        {
            Action action = () => new ProductoActualizadoEventConsumer(_mockEventConsumerConnection.Object, null!);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        // --- Prueba de ExecuteAsync (ya la tenías) ---
        [Fact]
        public async Task ExecuteAsync_CuandoSeLlama_DebeInvocarStartConsumingEnConnectionManagerCorrectamente()
        {
            var consumer = CreateConsumer();
            var cancellationTokenSource = new CancellationTokenSource();
            var executeTask = consumer.StartAsync(cancellationTokenSource.Token);
            cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));
            try { await executeTask; } catch (OperationCanceledException) { /* Esperado */ }

            _mockEventConsumerConnection.Verify(c => c.StartConsuming(
                "productos_actualizado_mongo_queue", "productos_exchange",
                It.IsAny<Func<BasicDeliverEventArgs, Task<bool>>>()), Times.Once);
        }

        // --- Prueba de HandleMessageAsync con tipo incorrecto (ya la tenías) ---
        [Fact]
        public async Task HandleMessageAsync_ConEventTypeIncorrecto_NoDebeLlamarMongoUpdateYRetornarTrue()
        {
            var consumer = CreateConsumer();
            await consumer.StartAsync(CancellationToken.None); // Necesario para que se asigne _capturedHandleMessageCallback
            _capturedHandleMessageCallback.Should().NotBeNull();

            var eventoDummy = new { Data = "Otro Evento" };
            var eventArgs = CreateBasicDeliverEventArgs("OtroTipoDeEvento", eventoDummy);
            var result = await _capturedHandleMessageCallback!(eventArgs);

            result.Should().BeTrue();
            _mockMongoUpdateProducto.Verify(m => m.ActualizarAsync(It.IsAny<ProductoActualizadoEvent>()), Times.Never);
        }

        // --- DOS PRUEBAS NUEVAS ---

        [Fact]
        public async Task HandleMessageAsync_ConProductoActualizadoEventValido_DebeLlamarMongoUpdateYRetornarTrue()
        {
            // Arrange
            var consumer = CreateConsumer();
            // Llamar a StartAsync para que _capturedHandleMessageCallback se establezca en el mock de IEventConsumerConnection
            await consumer.StartAsync(CancellationToken.None);
            _capturedHandleMessageCallback.Should().NotBeNull("El callback para StartConsuming no fue capturado.");

            var evento = new ProductoActualizadoEvent(
                Guid.NewGuid(),
                "Producto Bien Actualizado",
                199.99m,
                "Actualizaciones",
                "http://example.com/updated_product.png");
            var eventArgs = CreateBasicDeliverEventArgs(nameof(ProductoActualizadoEvent), evento);

            // Configurar el mock de MongoUpdateProducto para que ActualizarAsync complete exitosamente
            _mockMongoUpdateProducto
                .Setup(m => m.ActualizarAsync(It.Is<ProductoActualizadoEvent>(e => e.Id == evento.Id)))
                .Returns(Task.CompletedTask);

            // Act
            // Llama directamente al callback capturado, que es el método HandleMessageAsync del consumidor
            var result = await _capturedHandleMessageCallback!(eventArgs);

            // Assert
            // Debería retornar true porque el procesamiento (incluyendo la llamada a Mongo) fue exitoso
            result.Should().BeTrue();
            // Verifica que MongoUpdateProducto.ActualizarAsync fue llamado una vez con el evento correcto
            _mockMongoUpdateProducto.Verify(m => m.ActualizarAsync(
                It.Is<ProductoActualizadoEvent>(e => e.Id == evento.Id && e.Nombre == evento.Nombre)),
                Times.Once);
        }

        [Fact]
        public async Task HandleMessageAsync_ConProductoActualizadoEventYJsonInvalido_DebeRetornarFalseYNoLlamarMongoUpdate()
        {
            // Arrange
            var consumer = CreateConsumer();
            await consumer.StartAsync(CancellationToken.None); // Para que se asigne _capturedHandleMessageCallback
            _capturedHandleMessageCallback.Should().NotBeNull();

            // Preparamos BasicDeliverEventArgs con eventType correcto pero cuerpo JSON inválido
            var mockBasicProperties = new Mock<IBasicProperties>();
            mockBasicProperties.SetupGet(p => p.Type).Returns(nameof(ProductoActualizadoEvent));
            var bodyBytes = Encoding.UTF8.GetBytes("este no es un json valido");
            var eventArgs = new BasicDeliverEventArgs("ctag", 123, false, "exchange", "rkey",
                                                     mockBasicProperties.Object, new ReadOnlyMemory<byte>(bodyBytes));

            // Act
            var result = await _capturedHandleMessageCallback!(eventArgs);

            // Assert
            // Debería retornar false porque la deserialización fallará y ProcessProductoActualizadoEventLogic retornará false
            result.Should().BeFalse();
            // Verifica que MongoUpdateProducto.ActualizarAsync NO fue llamado
            _mockMongoUpdateProducto.Verify(m => m.ActualizarAsync(It.IsAny<ProductoActualizadoEvent>()), Times.Never);
        }

        [Fact]
        public async Task HandleMessageAsync_CuandoEventoDeserializadoEsNull_DebeRetornarFalseYNoLlamarMongoUpdate()
        {
            // Arrange
            var consumer = CreateConsumer();
            // Forzar que _capturedHandleMessageCallback se establezca
            var cts = new CancellationTokenSource();
            var executeTask = consumer.StartAsync(cts.Token);
            cts.CancelAfter(100);
            try { await executeTask; } catch (OperationCanceledException) { /* esperado */ }
            _capturedHandleMessageCallback.Should().NotBeNull();

            // Crear BasicDeliverEventArgs con un JSON que deserializa a null
            var eventArgs = CreateBasicDeliverEventArgs(nameof(ProductoActualizadoEvent), null); // payload null se serializa a "null"

            // Act
            var result = await _capturedHandleMessageCallback!(eventArgs);

            // Assert
            result.Should().BeFalse(); // Debería indicar fallo para NACK
            _mockMongoUpdateProducto.Verify(m => m.ActualizarAsync(It.IsAny<ProductoActualizadoEvent>()), Times.Never);
        }

        [Fact]
        public async Task HandleMessageAsync_CuandoMongoUpdateLanzaExcepcion_DebeRetornarFalse()
        {
            // Arrange
            var consumer = CreateConsumer();
            await consumer.StartAsync(CancellationToken.None); // Para que se asigne _capturedHandleMessageCallback
            _capturedHandleMessageCallback.Should().NotBeNull();

            var evento = new ProductoActualizadoEvent(Guid.NewGuid(), "Test Error", 50m, "Err", "err.png");
            var eventArgs = CreateBasicDeliverEventArgs(nameof(ProductoActualizadoEvent), evento);

            _mockMongoUpdateProducto.Setup(m => m.ActualizarAsync(It.IsAny<ProductoActualizadoEvent>()))
                                    .ThrowsAsync(new Exception("Error simulado de MongoUpdateProducto"));
            // Act
            var result = await _capturedHandleMessageCallback!(eventArgs);

            // Assert
            result.Should().BeFalse(); // Debería indicar fallo para NACK
            // Verifica que se intentó llamar a ActualizarAsync (lo que causó la excepción simulada)
            _mockMongoUpdateProducto.Verify(m => m.ActualizarAsync(It.Is<ProductoActualizadoEvent>(e => e.Id == evento.Id)), Times.Once);
        }


        [Fact]
        public void Dispose_CuandoSeLlama_DebeLlamarDisposeEnEventConsumerConnection()
        {
            // Arrange
            var consumer = CreateConsumer();

            // Act
            consumer.Dispose();

            // Assert
            // Verifica que Dispose fue llamado en la dependencia _mockEventConsumerConnection
            _mockEventConsumerConnection.Verify(c => c.Dispose(), Times.Once);
        }

        [Fact]
        public async Task HandleMessageAsync_ConEventTypeValidoPeroCuerpoVacio_DebeResultarEnFalloDeDeserializacionYRetornarFalse()
        {
            // Arrange
            var consumer = CreateConsumer();
            var cts = new CancellationTokenSource();
            var executeTask = consumer.StartAsync(cts.Token);
            cts.CancelAfter(100);
            try { await executeTask; } catch (OperationCanceledException) { /* esperado */ }
            _capturedHandleMessageCallback.Should().NotBeNull();

            var mockBasicProperties = new Mock<IBasicProperties>();
            mockBasicProperties.SetupGet(p => p.Type).Returns(nameof(ProductoActualizadoEvent)); // Tipo correcto

            var bodyBytes = Encoding.UTF8.GetBytes(""); // Cuerpo vacío
            var eventArgs = new BasicDeliverEventArgs("ctag", 1, false, "exchange", "rkey",
                                                     mockBasicProperties.Object, new ReadOnlyMemory<byte>(bodyBytes));

            // Act
            var result = await _capturedHandleMessageCallback!(eventArgs);

            // Assert
            // La deserialización de un string vacío a un objeto complejo usualmente falla o devuelve null.
            // Tu Process...Logic maneja la deserialización en un try-catch y el evento nulo.
            result.Should().BeFalse("El callback debería retornar false si el cuerpo del mensaje es vacío y causa fallo de deserialización.");
            _mockMongoUpdateProducto.Verify(m => m.ActualizarAsync(It.IsAny<ProductoActualizadoEvent>()), Times.Never);
        }


        [Fact]
        public async Task ExecuteAsync_CuandoCancellationTokenEsCanceladaDuranteBucle_DebeTerminar()
        {
            // Arrange
            var consumer = CreateConsumer();
            var cts = new CancellationTokenSource();
            bool startConsumingCalled = false;

            _mockEventConsumerConnection
                .Setup(c => c.StartConsuming(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<BasicDeliverEventArgs, Task<bool>>>()))
                .Callback(() => {
                    startConsumingCalled = true;
                    // Simular que la cancelación ocurre después de que StartConsuming ha sido llamado
                    // y el bucle de ExecuteAsync está teóricamente en Task.Delay.
                    cts.Cancel();
                });

            // Act
            var executeTask = consumer.StartAsync(cts.Token);

            // Assert
            // Esperamos que la tarea complete (puede ser por TaskCanceledException o normalmente)
            // Lo importante es que no se quede colgada indefinidamente
            Func<Task> action = async () => await executeTask.WaitAsync(TimeSpan.FromSeconds(1)); // Timeout para la prueba
            await action.Should().NotThrowAsync<TimeoutException>("ExecuteAsync debería terminar después de la cancelación.");

            startConsumingCalled.Should().BeTrue();
            executeTask.IsCompleted.Should().BeTrue("La tarea ExecuteAsync debería estar completada.");
        }

        [Fact]
        public async Task HandleMessageAsync_CuandoPayloadContieneIdVacio_YMongoUpdateFalla_DebeRetornarFalse()
        {
            // Arrange
            var consumer = CreateConsumer();
            var cts = new CancellationTokenSource();
            var executeTask = consumer.StartAsync(cts.Token);
            cts.CancelAfter(100);
            try { await executeTask; } catch (OperationCanceledException) { /* esperado */ }
            _capturedHandleMessageCallback.Should().NotBeNull();

            // Evento con ID vacío, asumiendo que esto podría ser un caso de error en MongoUpdateProducto.ActualizarAsync
            // o que el filtro no encuentre nada y el upsert falle, o alguna lógica de negocio en el evento.
            // Sin embargo, ProductoActualizadoEvent toma Guid, por lo que Guid.Empty es un valor válido.
            // El comportamiento aquí dependerá de cómo `MongoUpdateProducto.ActualizarAsync` maneje un evento con Id vacío.
            // Por ahora, simularemos que `ActualizarAsync` lanza una excepción para este caso.
            var eventoConIdVacio = new ProductoActualizadoEvent(Guid.Empty, "Test ID Vacío", 50m, "Test", "http://test.id/vacio.png");
            var eventArgs = CreateBasicDeliverEventArgs(nameof(ProductoActualizadoEvent), eventoConIdVacio);

            _mockMongoUpdateProducto.Setup(m => m.ActualizarAsync(It.Is<ProductoActualizadoEvent>(e => e.Id == Guid.Empty)))
                                    .ThrowsAsync(new InvalidOperationException("ID de producto no puede ser vacío para actualización."));

            // Act
            var result = await _capturedHandleMessageCallback!(eventArgs);

            // Assert
            result.Should().BeFalse("El callback debe retornar false si la lógica de negocio falla.");
            _mockMongoUpdateProducto.Verify(m => m.ActualizarAsync(It.Is<ProductoActualizadoEvent>(e => e.Id == Guid.Empty)), Times.Once);
        }

    }
}