using Xunit;
using Moq;
using RabbitMQ.Client;
using Producto.Infrastructure.EventBus.Events;
using System.Text.Json;
using System.Text;

namespace Producto.Tests.Infrastructura.Tests.EventBus.Events;

    public class TestEvent
    {
        public Guid Id { get; set; }
        public string Data { get; set; }
    }

    public class RabbitMQEventPublisherTests
    {
        private readonly Mock<IModel> _mockChannel;
        private readonly Mock<IBasicProperties> _mockBasicProperties;
        private readonly RabbitMQEventPublisher _publisher;

        public RabbitMQEventPublisherTests()
        {
            _mockChannel = new Mock<IModel>();
            _mockBasicProperties = new Mock<IBasicProperties>();

            // Configurar el mock del canal para que devuelva el mock de IBasicProperties
            _mockChannel.Setup(c => c.CreateBasicProperties()).Returns(_mockBasicProperties.Object);

            // Usamos el constructor interno para inyectar el canal mockeado
            _publisher = new RabbitMQEventPublisher(_mockChannel.Object);
        }

        [Fact]
        public void Publish_ConMensajeValido_DebeDeclararExchange()
        {
            // Arrange
            var testEvent = new TestEvent { Id = Guid.NewGuid(), Data = "Test Data" };
            var exchangeName = "test-exchange";
            var routingKey = "";

            // Act
            _publisher.Publish(testEvent, exchangeName, routingKey);

            // Assert
            // Verifica que ExchangeDeclare fue llamado con los parámetros correctos
            _mockChannel.Verify(c => c.ExchangeDeclare(
                exchangeName,
                ExchangeType.Fanout, // El tipo que usas en tu publicador
                true,  // durable
                false, // autoDelete
                null   // arguments
            ), Times.Once);
        }

        [Fact]
        public void Publish_ConMensajeValido_DebeCrearBasicProperties()
        {
            // Arrange
            var testEvent = new TestEvent { Id = Guid.NewGuid(), Data = "Test Data" };

            // Act
            _publisher.Publish(testEvent, "test-exchange", "");

            // Assert
            // Verifica que CreateBasicProperties fue llamado una vez
            _mockChannel.Verify(c => c.CreateBasicProperties(), Times.Once);
        }

        [Fact]
        public void Publish_ConMensajeValido_DebeEstablecerPropiedadesCorrectamente()
        {
            // Arrange
            var testEvent = new TestEvent { Id = Guid.NewGuid(), Data = "Test Data" };

            // Act
            _publisher.Publish(testEvent, "test-exchange", "");

            // Assert
            // Verifica que la propiedad Persistent se estableció a true
            _mockBasicProperties.VerifySet(p => p.Persistent = true, Times.Once);
            // Verifica que la propiedad Type se estableció con el nombre de la clase del evento
            _mockBasicProperties.VerifySet(p => p.Type = nameof(TestEvent), Times.Once);
        }

        [Fact]
        public void Publish_ConMensajeValido_DebeLlamarABasicPublishConArgumentosCorrectos()
        {
            // Arrange
            var testEvent = new TestEvent { Id = Guid.NewGuid(), Data = "Hello RabbitMQ" };
            var exchangeName = "my-exchange";
            var routingKey = "my.key";

            var expectedMessage = JsonSerializer.Serialize(testEvent);
            var expectedBody = Encoding.UTF8.GetBytes(expectedMessage); // Esto es byte[]

            // Act
            _publisher.Publish(testEvent, exchangeName, routingKey);

            // Assert
            // Verifica la llamada al método de instancia subyacente en IModel
            _mockChannel.Verify(c => c.BasicPublish(
                exchangeName,
                routingKey,
                false, // Parámetro 'mandatory' que usualmente es false por defecto en la extensión
                _mockBasicProperties.Object, // El objeto de propiedades que configuramos
                                             // Moq necesita que coincida el tipo ReadOnlyMemory<byte> para el body aquí
                It.Is<ReadOnlyMemory<byte>>(actualBody => actualBody.ToArray().SequenceEqual(expectedBody))
            ), Times.Once);
        }

        [Fact]
        public void Dispose_CuandoSeLlama_DebeIntentarDisposeDelChannel()
        {
            // Arrange
            // El _publisher ya está creado con un _mockChannel

            // Act
            _publisher.Dispose();

            // Assert
            // Verifica que Dispose (o Close si fuera el caso) se llamó en el canal
            // En este caso, como no mockeamos la conexión para este constructor, solo probamos el canal.
            _mockChannel.Verify(c => c.Dispose(), Times.Once);
        }


    }