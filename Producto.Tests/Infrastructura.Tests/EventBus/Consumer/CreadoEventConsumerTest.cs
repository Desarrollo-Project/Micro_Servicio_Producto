using Xunit;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting; // Para IHostedService (BackgroundService hereda de esto)
using Producto.Domain.Events;
using Producto.Infrastructure.Persistence.MongoOperations; // Para MongoCreateProducto
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
using Producto.Infrastructure.Persistance.MongoOperations; // Para mockear IMongoDatabase para MongoCreateProducto

namespace Producto.Tests.Infrastructura.Tests.EventBus.Consumer // Ajusta tu namespace
{
    public class CreadoEventConsumerServiceTests
    {
        private readonly Mock<IEventConsumerConnection> _mockEventConsumerConnection;
        private readonly Mock<IServiceProvider> _mockRootServiceProvider;
        private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
        private readonly Mock<IServiceScope> _mockServiceScope;
        private readonly Mock<IServiceProvider> _mockScopedServiceProvider;
        private readonly Mock<MongoCreateProducto> _mockMongoCreateProducto;

        // Para capturar el callback que se pasa a StartConsuming
        private Func<BasicDeliverEventArgs, Task<bool>>? _capturedHandleMessageCallback;

        public CreadoEventConsumerServiceTests()
        {
            _mockEventConsumerConnection = new Mock<IEventConsumerConnection>();

            var mockMongoDb = new Mock<IMongoDatabase>();
            // MongoCreateProducto necesita IMongoDatabase en su constructor
            _mockMongoCreateProducto = new Mock<MongoCreateProducto>(mockMongoDb.Object);

            _mockScopedServiceProvider = new Mock<IServiceProvider>();
            _mockScopedServiceProvider
                .Setup(sp => sp.GetService(typeof(MongoCreateProducto)))
                .Returns(_mockMongoCreateProducto.Object);

            _mockServiceScope = new Mock<IServiceScope>();
            _mockServiceScope.Setup(s => s.ServiceProvider).Returns(_mockScopedServiceProvider.Object);

            _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
            _mockServiceScopeFactory.Setup(sf => sf.CreateScope()).Returns(_mockServiceScope.Object);

            _mockRootServiceProvider = new Mock<IServiceProvider>();
            _mockRootServiceProvider
                .Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                .Returns(_mockServiceScopeFactory.Object);

            // Capturar el callback pasado a StartConsuming
            _mockEventConsumerConnection
                .Setup(c => c.StartConsuming(
                    // Asegúrate que estos nombres coincidan con los de ProductoCreadoEventConsumerService
                    "productos_creado_mongo_queue",
                    "productos_exchange",
                    It.IsAny<Func<BasicDeliverEventArgs, Task<bool>>>()))
                .Callback<string, string, Func<BasicDeliverEventArgs, Task<bool>>>((queue, exchange, callback) =>
                {
                    _capturedHandleMessageCallback = callback;
                });
        }

        private ProductoCreadoEventConsumer CreateConsumer()
        {
            // Usa el constructor que inyecta la conexión mockeada y el IServiceProvider raíz mockeado
            return new ProductoCreadoEventConsumer(
                _mockEventConsumerConnection.Object,
                _mockRootServiceProvider.Object
            );
        }

        // Helper para crear BasicDeliverEventArgs
        private BasicDeliverEventArgs CreateBasicDeliverEventArgs(string eventType, object? payload, ulong deliveryTag = 1)
        {
            var mockBasicProperties = new Mock<IBasicProperties>();
            mockBasicProperties.SetupGet(p => p.Type).Returns(eventType);

            // Manejar payload null para que JsonSerializer no lance excepción si no es su intención
            var messageBody = payload != null ? JsonSerializer.Serialize(payload) : "null";
            var bodyBytes = Encoding.UTF8.GetBytes(messageBody);

            return new BasicDeliverEventArgs("ctag", deliveryTag, false, "exchange", "rkey",
                                             mockBasicProperties.Object, new ReadOnlyMemory<byte>(bodyBytes));
        }

        // --- PRUEBAS ---

        [Fact]
        public void Constructor_CuandoEventConsumerConnectionEsNull_DebeLanzarArgumentNullException()
        {
            // Arrange
            IEventConsumerConnection? nullConnection = null;

            // Act
            Action action = () => new ProductoCreadoEventConsumer(nullConnection!, _mockRootServiceProvider.Object);

            // Assert
            action.Should().ThrowExactly<ArgumentNullException>()
                  .And.ParamName.Should().Be("eventConsumerConnection");
        }

        [Fact]
        public void Constructor_CuandoServiceProviderEsNull_DebeLanzarArgumentNullException()
        {
            // Arrange
            IServiceProvider? nullServiceProvider = null;

            // Act
            Action action = () => new ProductoCreadoEventConsumer(_mockEventConsumerConnection.Object, nullServiceProvider!);

            // Assert
            action.Should().ThrowExactly<ArgumentNullException>()
                  .And.ParamName.Should().Be("serviceProvider");
        }

        [Fact]
        public async Task ExecuteAsync_CuandoSeLlama_DebeInvocarStartConsumingConParametrosCorrectos()
        {
            // Arrange
            var consumer = CreateConsumer();
            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            // ExecuteAsync es protegido, pero StartAsync (de IHostedService que BackgroundService implementa) lo llama.
            var executeTask = consumer.StartAsync(cancellationTokenSource.Token);

            // Cancelar después de un breve tiempo para que el bucle en ExecuteAsync (Task.Delay(Timeout.Infinite)) no se quede indefinidamente.
            cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

            try
            {
                await executeTask; // Esperar a que la tarea de ExecuteAsync reaccione a la cancelación
            }
            catch (OperationCanceledException)
            {
                // Es esperado si Task.Delay(Timeout.Infinite, stoppingToken) se cancela
            }

            // Assert
            // Verifica que StartConsuming fue llamado con los parámetros correctos
            _mockEventConsumerConnection.Verify(c => c.StartConsuming(
                "productos_creado_mongo_queue", // QueueName esperado
                "productos_exchange",           // ExchangeName esperado
                It.IsAny<Func<BasicDeliverEventArgs, Task<bool>>>()), // Verifica que se pasó un callback
                Times.Once);
        }

        [Fact]
        public async Task HandleMessageAsync_ConProductoCreadoEventValido_DebeLlamarMongoCreateYRetornarTrue()
        {
            // Arrange
            var consumer = CreateConsumer();
            // Llamar a StartAsync para que _capturedHandleMessageCallback se establezca
            var cts = new CancellationTokenSource();
            var executeTask = consumer.StartAsync(cts.Token);
            cts.CancelAfter(100);
            try { await executeTask; } catch (OperationCanceledException) { /* esperado */ }

            _capturedHandleMessageCallback.Should().NotBeNull("El callback para StartConsuming no fue capturado.");
            if (_capturedHandleMessageCallback == null) return; // Guarda para análisis nulo y evita NRE abajo

            var evento = new ProductoCreadoEvent(Guid.NewGuid(), "Laptop Gamer Pro", 2999.99m, "Gaming Laptops", "http://example.com/gamerpro.png");
            var eventArgs = CreateBasicDeliverEventArgs(nameof(ProductoCreadoEvent), evento);

            _mockMongoCreateProducto
                .Setup(m => m.CrearAsync(It.Is<ProductoCreadoEvent>(e => e.Id == evento.Id)))
                .Returns(Task.CompletedTask); // Simular que CrearAsync completa sin error

            // Act
            var result = await _capturedHandleMessageCallback(eventArgs);

            // Assert
            result.Should().BeTrue("El callback debería retornar true en procesamiento exitoso.");
            _mockMongoCreateProducto.Verify(m => m.CrearAsync(
                It.Is<ProductoCreadoEvent>(e => e.Id == evento.Id && e.Nombre == evento.Nombre)),
                Times.Once);
        }

        [Fact]
        public async Task HandleMessageAsync_ConEventTypeIncorrecto_NoDebeLlamarMongoCreateYRetornarTrue()
        {
            // Arrange
            var consumer = CreateConsumer();
            var cts = new CancellationTokenSource();
            var executeTask = consumer.StartAsync(cts.Token);
            cts.CancelAfter(100);
            try { await executeTask; } catch (OperationCanceledException) { /* esperado */ }

            _capturedHandleMessageCallback.Should().NotBeNull();
            if (_capturedHandleMessageCallback == null) return;

            var eventoDummy = new { Info = "Algún otro evento" };
            var eventArgs = CreateBasicDeliverEventArgs("OtroTipoDeEventoCualquiera", eventoDummy);

            // Act
            var result = await _capturedHandleMessageCallback(eventArgs);

            // Assert
            // Debería retornar true porque el mensaje se considera "manejado" al ser ignorado y se debe hacer ACK.
            result.Should().BeTrue();
            _mockMongoCreateProducto.Verify(m => m.CrearAsync(It.IsAny<ProductoCreadoEvent>()), Times.Never);
        }

        [Fact]
        public async Task HandleMessageAsync_ConProductoCreadoEventYJsonInvalido_DebeRetornarFalseYNoLlamarMongoCreate()
        {
            // Arrange
            var consumer = CreateConsumer();
            // Necesario para que _capturedHandleMessageCallback se establezca:
            var cts = new CancellationTokenSource();
            var executeTask = consumer.StartAsync(cts.Token);
            cts.CancelAfter(100);
            try { await executeTask; } catch (OperationCanceledException) { /* esperado */ }
            _capturedHandleMessageCallback.Should().NotBeNull();

            var mockBasicProperties = new Mock<IBasicProperties>();
            mockBasicProperties.SetupGet(p => p.Type).Returns(nameof(ProductoCreadoEvent)); // Tipo correcto
            var bodyBytes = Encoding.UTF8.GetBytes("{json_invalido_de_verdad"); // Cuerpo inválido
            var eventArgs = new BasicDeliverEventArgs("ctag", 123, false, "exchange", "rkey",
                                                     mockBasicProperties.Object, new ReadOnlyMemory<byte>(bodyBytes));

            // Act
            var result = await _capturedHandleMessageCallback!(eventArgs);

            // Assert
            // Debería retornar false porque ProcessProductoCreadoEventLogic atrapará JsonException
            result.Should().BeFalse();
            _mockMongoCreateProducto.Verify(m => m.CrearAsync(It.IsAny<ProductoCreadoEvent>()), Times.Never);
        }

        [Fact]
        public async Task HandleMessageAsync_CuandoEventoDeserializadoEsNull_DebeRetornarFalseYNoLlamarMongoCreate()
        {
            // Arrange
            var consumer = CreateConsumer();
            var cts = new CancellationTokenSource();
            var executeTask = consumer.StartAsync(cts.Token);
            cts.CancelAfter(100);
            try { await executeTask; } catch (OperationCanceledException) { /* esperado */ }
            _capturedHandleMessageCallback.Should().NotBeNull();

            // Creamos BasicDeliverEventArgs con un JSON que deserializa a null
            var eventArgs = CreateBasicDeliverEventArgs(nameof(ProductoCreadoEvent), null); // El helper serializa null a "null"

            // Act
            var result = await _capturedHandleMessageCallback!(eventArgs);

            // Assert
            // Debería retornar false porque ProcessProductoCreadoEventLogic tiene if(evento == null) return false; (implícitamente)
            result.Should().BeFalse();
            _mockMongoCreateProducto.Verify(m => m.CrearAsync(It.IsAny<ProductoCreadoEvent>()), Times.Never);
        }

        [Fact]
        public async Task HandleMessageAsync_CuandoMongoCreateLanzaExcepcion_DebeRetornarFalse()
        {
            // Arrange
            var consumer = CreateConsumer();
            var cts = new CancellationTokenSource();
            var executeTask = consumer.StartAsync(cts.Token);
            cts.CancelAfter(100);
            try { await executeTask; } catch (OperationCanceledException) { /* esperado */ }
            _capturedHandleMessageCallback.Should().NotBeNull();

            var evento = new ProductoCreadoEvent(Guid.NewGuid(), "Producto Con Error en Mongo", 50m, "Errores", "error.jpg");
            var eventArgs = CreateBasicDeliverEventArgs(nameof(ProductoCreadoEvent), evento);

            _mockMongoCreateProducto.Setup(m => m.CrearAsync(It.IsAny<ProductoCreadoEvent>()))
                                    .ThrowsAsync(new Exception("Error simulado de MongoCreateProducto.CrearAsync"));
            // Act
            var result = await _capturedHandleMessageCallback!(eventArgs);

            // Assert
            // Debería retornar false porque ProcessProductoCreadoEventLogic atrapa la excepción y debería indicar fallo.
            result.Should().BeFalse();
            // Verifica que se intentó llamar a CrearAsync (lo que causó la excepción simulada)
            _mockMongoCreateProducto.Verify(m => m.CrearAsync(It.Is<ProductoCreadoEvent>(e => e.Id == evento.Id)), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_CuandoStartConsumingLanzaExcepcion_NoDebePropagarExcepcionDirecta()
        {
            // Arrange
            var consumer = CreateConsumer();
            var cts = new CancellationTokenSource();
            var excepcionSimulada = new Exception("Error al iniciar el consumo");

            _mockEventConsumerConnection
                .Setup(c => c.StartConsuming(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<BasicDeliverEventArgs, Task<bool>>>()))
                .Throws(excepcionSimulada); // Simula que StartConsuming falla

            // Act
            // ExecuteAsync tiene un try-catch para la llamada a StartConsuming
            var executeTask = consumer.StartAsync(cts.Token);
            cts.CancelAfter(100); // Para que el bucle while de ExecuteAsync no se quede

            Func<Task> action = async () => await executeTask;

            // Assert
            // La excepción de StartConsuming es atrapada y logueada por ExecuteAsync.
            // El Task.Delay interno podría lanzar TaskCanceledException si se cancela el token.
            // Lo importante es que la excepción original "Error al iniciar el consumo" no se propague.
            await action.Should().NotThrowAsync<Exception>(because: "ExecuteAsync debe manejar excepciones de StartConsuming y no fallar el servicio alojado directamente por esa causa inicial.");
            // Verifica que se intentó llamar a StartConsuming
            _mockEventConsumerConnection.Verify(c => c.StartConsuming(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<BasicDeliverEventArgs, Task<bool>>>()), Times.Once);
        }

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

            var evento = new ProductoCreadoEvent(Guid.NewGuid(), "Producto Falla", 10m, "Falla", "falla.png");
            var eventArgs = CreateBasicDeliverEventArgs(nameof(ProductoCreadoEvent), evento);

            // Simular que ProcessProductoCreadoEventLogic (a través de MongoCreateProducto) falla y devuelve false
            // Esto se logra si MongoCreateProducto.CrearAsync lanza una excepción, y Process... la atrapa.
            _mockMongoCreateProducto.Setup(m => m.CrearAsync(It.IsAny<ProductoCreadoEvent>()))
                                    .ThrowsAsync(new Exception("Error simulado en MongoCreateProducto"));

            // Act
            var result = await _capturedHandleMessageCallback!(eventArgs);

            // Assert
            result.Should().BeFalse("El callback debe retornar false si la lógica de procesamiento interna falla.");
        }


        [Fact]
        public async Task HandleMessageAsync_CuandoBasicPropertiesEsNullEnEventArgs_NoDebeLlamarProcessLogicYRetornarTrue()
        {
            // Arrange
            var consumer = CreateConsumer();
            var cts = new CancellationTokenSource();
            var executeTask = consumer.StartAsync(cts.Token);
            cts.CancelAfter(100);
            try { await executeTask; } catch (OperationCanceledException) { /* esperado */ }
            _capturedHandleMessageCallback.Should().NotBeNull();

            var payload = new { Data = "Mensaje sin propiedades de evento" };
            var bodyBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
            // Crear BasicDeliverEventArgs con IBasicProperties explícitamente nulo
            var eventArgs = new BasicDeliverEventArgs("ctag", 1, false, "exchange", "rkey",
                                                     null, // <--- IBasicProperties es null
                                                     new ReadOnlyMemory<byte>(bodyBytes));
            // Act
            var result = await _capturedHandleMessageCallback!(eventArgs);

            // Assert
            // Si BasicProperties es null, ea.BasicProperties?.Type será null,
            // por lo que el 'if' para el tipo de evento no se cumplirá.
            result.Should().BeTrue("Debe retornar true para ACKear el mensaje aunque no se procese.");
            _mockMongoCreateProducto.Verify(m => m.CrearAsync(It.IsAny<ProductoCreadoEvent>()), Times.Never);
        }




    }
}