using Xunit;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting; // Para BackgroundService y IHostApplicationLifetime si se necesitara
using Producto.Domain.Events;
using Producto.Infrastructure.Persistence.MongoOperations; // Para MongoDeleteProducto
using Producto.Infrastructure.EventBus.Consumer;
using Producto.Infrastructure.EventBus.InterfaceConsumer; // Para IEventConsumerConnection
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;       // Para IBasicProperties
using RabbitMQ.Client.Events; // Para BasicDeliverEventArgs
using FluentAssertions;
using MongoDB.Driver;
using Producto.Infrastructure.Persistence.MongoOperations; // Para mockear IMongoDatabase para MongoDeleteProducto

namespace Producto.Tests.Infrastructura.Tests.EventBus.Consumer // Ajusta tu namespace
{
    public class ProductoEliminadoEventConsumerServiceTests
    {
        private readonly Mock<IEventConsumerConnection> _mockEventConsumerConnection;
        private readonly Mock<IServiceProvider> _mockRootServiceProvider;
        private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
        private readonly Mock<IServiceScope> _mockServiceScope;
        private readonly Mock<IServiceProvider> _mockScopedServiceProvider;
        private readonly Mock<MongoDeleteProducto> _mockMongoDeleteProducto;

        // Para capturar el callback que se pasa a StartConsuming
        private Func<BasicDeliverEventArgs, Task<bool>>? _capturedHandleMessageCallback;

        public ProductoEliminadoEventConsumerServiceTests()
        {
            _mockEventConsumerConnection = new Mock<IEventConsumerConnection>();

            var mockMongoDb = new Mock<IMongoDatabase>();
            _mockMongoDeleteProducto = new Mock<MongoDeleteProducto>(mockMongoDb.Object); // MongoDeleteProducto necesita IMongoDatabase

            _mockScopedServiceProvider = new Mock<IServiceProvider>();
            _mockScopedServiceProvider
                .Setup(sp => sp.GetService(typeof(MongoDeleteProducto)))
                .Returns(_mockMongoDeleteProducto.Object);

            _mockServiceScope = new Mock<IServiceScope>();
            _mockServiceScope.Setup(s => s.ServiceProvider).Returns(_mockScopedServiceProvider.Object);

            _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
            _mockServiceScopeFactory.Setup(sf => sf.CreateScope()).Returns(_mockServiceScope.Object);

            _mockRootServiceProvider = new Mock<IServiceProvider>();
            _mockRootServiceProvider
                .Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                .Returns(_mockServiceScopeFactory.Object);

            // Capturar el callback cuando se llama a StartConsuming
            _mockEventConsumerConnection
                .Setup(c => c.StartConsuming(
                    "productos_eliminado_mongo_queue", // QueueName esperado
                    "productos_exchange",              // ExchangeName esperado
                    It.IsAny<Func<BasicDeliverEventArgs, Task<bool>>>()))
                .Callback<string, string, Func<BasicDeliverEventArgs, Task<bool>>>((queue, exchange, callback) =>
                {
                    _capturedHandleMessageCallback = callback;
                });
        }

        private ProductoEliminadoEventConsumer CreateConsumer()
        {
            return new ProductoEliminadoEventConsumer(
                _mockEventConsumerConnection.Object,
                _mockRootServiceProvider.Object
            );
        }

        private BasicDeliverEventArgs CreateBasicDeliverEventArgs(string? eventType, object? payload, ulong deliveryTag = 1)
        {
            var mockBasicProperties = new Mock<IBasicProperties>();
            if (eventType != null) // Solo configurar si no es null, para probar el caso de BasicProperties.Type siendo null
            {
                mockBasicProperties.SetupGet(p => p.Type).Returns(eventType);
            }
            else if (payload == null && eventType == null) // Simular BasicProperties siendo null
            {
                // Para este caso, pasamos null para IBasicProperties.
                // BasicDeliverEventArgs no permite IBasicProperties nulo en algunas versiones de constructor.
                // Si necesitamos simular IBasicProperties siendo null, la construcción de BasicDeliverEventArgs se complica.
                // Por ahora, nos enfocaremos en Type siendo null o diferente.
                // Si el driver permite IBasicProperties null, podríamos pasar null aquí.
                // Para el test de eventType null, ya lo hacemos al pasar eventType=null al helper.
            }


            var messageBody = payload != null ? JsonSerializer.Serialize(payload) : "null";
            var bodyBytes = Encoding.UTF8.GetBytes(messageBody);

            return new BasicDeliverEventArgs("ctag", deliveryTag, false, "exchange", "rkey",
                                             mockBasicProperties.Object, new ReadOnlyMemory<byte>(bodyBytes));
        }

        // --- Pruebas del Constructor ---
        [Fact]
        public void Constructor_CuandoEventConsumerConnectionEsNull_DebeLanzarArgumentNullException()
        {
            Action action = () => new ProductoEliminadoEventConsumer(null!, _mockRootServiceProvider.Object);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("eventConsumerConnection");
        }

        [Fact]
        public void Constructor_CuandoServiceProviderEsNull_DebeLanzarArgumentNullException()
        {
            Action action = () => new ProductoEliminadoEventConsumer(_mockEventConsumerConnection.Object, null!);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        // --- Pruebas para ExecuteAsync ---
        /*   [Fact]
           public async Task ExecuteAsync_CuandoCancellationTokenEsCanceladaAlInicio_NoDebeLlamarStartConsuming()
           {
               var consumer = CreateConsumer();
               var cts = new CancellationTokenSource();
               cts.Cancel();

               // StartAsync llama internamente a ExecuteAsync
               var task = consumer.StartAsync(cts.Token);
               await task; // Espera que complete (debería ser rápido)

               task.IsCompletedSuccessfully.Should().BeTrue();
               _mockEventConsumerConnection.Verify(c => c.StartConsuming(
                   It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<BasicDeliverEventArgs, Task<bool>>>()),
                   Times.Never);
           }*/

        [Fact]
        public async Task ExecuteAsync_CuandoSeLlama_DebeInvocarStartConsumingCorrectamente()
        {
            var consumer = CreateConsumer();
            var cts = new CancellationTokenSource();

            var executeTask = consumer.StartAsync(cts.Token);
            cts.CancelAfter(100); // Para que el Task.Delay(Timeout.Infinite) termine
            try { await executeTask; } catch (OperationCanceledException) { /* Esperado */ }

            _mockEventConsumerConnection.Verify(c => c.StartConsuming(
                "productos_eliminado_mongo_queue",
                "productos_exchange",
                It.IsAny<Func<BasicDeliverEventArgs, Task<bool>>>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_CuandoStartConsumingLanzaExcepcion_DebeLoguearYNoRelanzarHaciaAfueraDeExecuteAsync()
        {
            // Arrange
            _mockEventConsumerConnection.Setup(c => c.StartConsuming(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<BasicDeliverEventArgs, Task<bool>>>()))
                .Throws(new Exception("Error simulado en StartConsuming"));

            var consumer = CreateConsumer();
            var cts = new CancellationTokenSource();

            // Act
            // ExecuteAsync tiene un try-catch que loguea el error.
            Func<Task> action = () => consumer.StartAsync(cts.Token);

            // Para que el Task.Delay no quede colgado, cancelamos.
            // La excepción de StartConsuming debería ser atrapada.
            cts.CancelAfter(100);
            try { await action.Invoke(); } catch (OperationCanceledException) { /* Esperado por el Task.Delay */ }


            // Assert
            // La aserción principal es que ExecuteAsync (vía StartAsync) no relanzó la excepción
            // porque fue atrapada y logueada (Console.WriteLine) por el SUT.
            // No podemos verificar Console.WriteLine fácilmente en unit tests sin más setup.
            // Lo importante es que el servicio no se caiga por esto.
            // La verificación de que no lanzó la excepción original "Error simulado en StartConsuming"
            // se hace con el hecho de que la prueba no falla por una excepción no manejada.
            // (Si quisiéramos ser más explícitos, necesitaríamos un mock de ILogger)
            // Por ahora, el NotThrowAsync no es útil aquí porque la cancelación del Delay puede ocurrir.
            // La ausencia de la excepción "Error simulado en StartConsuming" es la clave.
            // Simplemente nos aseguramos que StartConsuming fue llamado.
            _mockEventConsumerConnection.Verify(c => c.StartConsuming(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<BasicDeliverEventArgs, Task<bool>>>()), Times.Once);
        }


        // --- Pruebas para el callback HandleMessageAsync ---
        [Fact]
        public async Task HandleMessageAsync_ConProductoEliminadoEventValido_DebeLlamarMongoDeleteYRetornarTrue()
        {
            var consumer = CreateConsumer();
            var cts = new CancellationTokenSource(); // Necesario para StartAsync
            var executeTask = consumer.StartAsync(cts.Token);
            cts.CancelAfter(100);
            try { await executeTask; } catch (OperationCanceledException) { /* esperado */ }
            _capturedHandleMessageCallback.Should().NotBeNull();

            var evento = new ProductoEliminadoEvent(Guid.NewGuid());
            var eventArgs = CreateBasicDeliverEventArgs(nameof(ProductoEliminadoEvent), evento);
            _mockMongoDeleteProducto.Setup(m => m.EliminarAsync(It.IsAny<ProductoEliminadoEvent>())).Returns(Task.CompletedTask);

            var result = await _capturedHandleMessageCallback!(eventArgs);

            result.Should().BeTrue();
            _mockMongoDeleteProducto.Verify(m => m.EliminarAsync(
                It.Is<ProductoEliminadoEvent>(e => e.Id == evento.Id)), Times.Once);
        }

        [Fact]
        public async Task HandleMessageAsync_ConEventTypeIncorrecto_NoDebeLlamarMongoDeleteYRetornarTrue()
        {
            var consumer = CreateConsumer();
            var cts = new CancellationTokenSource();
            var executeTask = consumer.StartAsync(cts.Token);
            cts.CancelAfter(100);
            try { await executeTask; } catch (OperationCanceledException) { /* esperado */ }
            _capturedHandleMessageCallback.Should().NotBeNull();

            var eventoDummy = new { Info = "Otro Evento" };
            var eventArgs = CreateBasicDeliverEventArgs("OtroTipoEvento", eventoDummy);

            var result = await _capturedHandleMessageCallback!(eventArgs);

            result.Should().BeTrue(); // ACK para mensajes irrelevantes
            _mockMongoDeleteProducto.Verify(m => m.EliminarAsync(It.IsAny<ProductoEliminadoEvent>()), Times.Never);
        }

        [Fact]
        public async Task HandleMessageAsync_ConEventTypeNull_NoDebeLlamarMongoDeleteYRetornarTrue()
        {
            var consumer = CreateConsumer();
            var cts = new CancellationTokenSource();
            var executeTask = consumer.StartAsync(cts.Token);
            cts.CancelAfter(100);
            try { await executeTask; } catch (OperationCanceledException) { /* esperado */ }
            _capturedHandleMessageCallback.Should().NotBeNull();

            var eventoDummy = new { Info = "Evento sin tipo en propiedades" };
            // CreateBasicDeliverEventArgs con eventType null simula BasicProperties.Type siendo null
            var eventArgs = CreateBasicDeliverEventArgs(null, eventoDummy);

            var result = await _capturedHandleMessageCallback!(eventArgs);

            result.Should().BeTrue(); // ACK para mensajes malformados/sin tipo que no podemos procesar
            _mockMongoDeleteProducto.Verify(m => m.EliminarAsync(It.IsAny<ProductoEliminadoEvent>()), Times.Never);
        }


        [Fact]
        public void Dispose_DebeLlamarADisposeEnEventConsumerConnection()
        {
            var consumer = CreateConsumer();
            consumer.Dispose();
            _mockEventConsumerConnection.Verify(c => c.Dispose(), Times.Once);
        }


        [Fact]
        public async Task HandleMessageAsync_CuandoBasicPropertiesEsNull_NoDebeProcesarYRetornarTrue()
        {
            // Arrange
            var consumer = CreateConsumer();
            var cts = new CancellationTokenSource();
            var executeTask = consumer.StartAsync(cts.Token); // Para que _capturedHandleMessageCallback se establezca
            cts.CancelAfter(100);
            try { await executeTask; } catch (OperationCanceledException) { /* esperado */ }
            _capturedHandleMessageCallback.Should().NotBeNull();

            var eventoDummy = new { Data = "Mensaje sin propiedades" };
            var bodyBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(eventoDummy));
            // Simular que IBasicProperties es null
            var eventArgs = new BasicDeliverEventArgs("ctag", 1, false, "exchange", "rkey",
                                                     null, // <--- BasicProperties es null
                                                     new ReadOnlyMemory<byte>(bodyBytes));
            // Act
            var result = await _capturedHandleMessageCallback!(eventArgs);

            // Assert
            result.Should().BeTrue(); // Debe hacer ACK para descartar el mensaje
            _mockMongoDeleteProducto.Verify(m => m.EliminarAsync(It.IsAny<ProductoEliminadoEvent>()), Times.Never);
        }


        /*   [Fact]
           public async Task StopAsync_DebeLlamarADisposeEnEventConsumerConnection()
           {
               var consumer = CreateConsumer();
               await consumer.StopAsync(CancellationToken.None);
               _mockEventConsumerConnection.Verify(c => c.Dispose(), Times.Once);
               // Nota: En tu SUT, StopAsync llama a _eventConsumer.Dispose(). Si la intención fuera
               // una lógica de parada más gradual (ej. dejar de consumir, esperar a que los mensajes en vuelo terminen),
               // el mock de IEventConsumerConnection necesitaría un método StopConsuming() o similar.
               // Por ahora, probamos lo que hay.
           }*/


        [Fact]
        public async Task HandleMessageAsync_CuandoProcessLogicRetornaFalse_CallbackDebeRetornarFalse()
        {
            // Arrange
            var consumer = CreateConsumer();
            var cts = new CancellationTokenSource();
            var executeTask = consumer.StartAsync(cts.Token); // Para que _capturedHandleMessageCallback se establezca
            cts.CancelAfter(100);
            try { await executeTask; } catch (OperationCanceledException) { /* esperado */ }
            _capturedHandleMessageCallback.Should().NotBeNull();

            var evento = new ProductoEliminadoEvent(Guid.NewGuid());
            var eventArgs = CreateBasicDeliverEventArgs(nameof(ProductoEliminadoEvent), evento);

            // Simular que ProcessProductoEliminadoEventLogic (a través de MongoDeleteProducto) falla y devuelve false
            // Esto se logra si MongoDeleteProducto.EliminarAsync lanza una excepción, 
            // y ProcessProductoEliminadoEventLogic la atrapa y retorna false.
            _mockMongoDeleteProducto.Setup(m => m.EliminarAsync(It.IsAny<ProductoEliminadoEvent>()))
                                    .ThrowsAsync(new Exception("Error simulado en MongoDeleteProducto"));

            // Act
            var result = await _capturedHandleMessageCallback!(eventArgs);

            // Assert
            result.Should().BeFalse("El callback debe retornar false si la lógica de procesamiento interna falla y ProcessLogic retorna false.");
            _mockMongoDeleteProducto.Verify(m => m.EliminarAsync(It.Is<ProductoEliminadoEvent>(e => e.Id == evento.Id)), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_CuandoSeCancelaDuranteElBucleDeEspera_DebeTerminarCorrectamente()
        {
            // Arrange
            var consumer = CreateConsumer();
            var cts = new CancellationTokenSource();
            bool startConsumingCalled = false;

            _mockEventConsumerConnection
                .Setup(c => c.StartConsuming(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<BasicDeliverEventArgs, Task<bool>>>()))
                .Callback(() => {
                    startConsumingCalled = true;
                    // Simular que el bucle de espera se interrumpe por cancelación después de un tiempo
                    Task.Run(async () => { // No await this, let it run in background
                        await Task.Delay(50); // Esperar un poco
                        if (!cts.IsCancellationRequested) cts.Cancel(); // Cancelar si no lo está ya
                    });
                });

            // Act
            var executeTask = consumer.StartAsync(cts.Token);

            // Assert
            // Esperamos que la tarea complete (puede ser por TaskCanceledException o normalmente)
            // Lo importante es que no se quede colgada indefinidamente
            Func<Task> action = async () => await executeTask.WaitAsync(TimeSpan.FromSeconds(1)); // Timeout para la prueba
            await action.Should().NotThrowAsync<TimeoutException>("ExecuteAsync debería terminar después de la cancelación.");

            startConsumingCalled.Should().BeTrue();
        }

    }
}