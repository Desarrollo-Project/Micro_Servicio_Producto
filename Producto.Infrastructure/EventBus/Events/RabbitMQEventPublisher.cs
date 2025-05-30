using System;
using System.Text;
using System.Text.Json;
using Producto.Domain.Repositories; // Para IEventPublisher
using RabbitMQ.Client;

namespace Producto.Infrastructure.EventBus.Events;

    public class RabbitMQEventPublisher : IEventPublisher, IDisposable
    {
        private readonly IConnection? _connection; // Lo hacemos nullable para el constructor de prueba
        private readonly RabbitMQ.Client.IModel _channel;

        // Constructor para producción (el que ya tienes)
        public RabbitMQEventPublisher(string host, string username, string password)
        {
            var factory = new ConnectionFactory
            {
                HostName = host,
                UserName = username,
                Password = password
            };
            _connection = factory.CreateConnection(); // Esto intenta una conexión real
            _channel = _connection.CreateModel();
            Console.WriteLine($"[RabbitMQEventPublisher] Conexión real creada para {host}");
        }

        // Constructor interno PARA PRUEBAS UNITARIAS
        // Este nos permite inyectar un IModel mockeado.
        public RabbitMQEventPublisher(RabbitMQ.Client.IModel channel)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _connection = null; // No manejamos una conexión real en este constructor de prueba
            Console.WriteLine($"[RabbitMQEventPublisher] Instanciado con IModel para pruebas.");
        }

        public void Publish<T>(T message, string exchangeName, string routingKey)
        {
            // La declaración del Exchange es idempotente, está bien aquí para asegurar que exista.
            _channel.ExchangeDeclare(exchangeName, ExchangeType.Fanout, durable: true);

            var eventMessage = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(eventMessage);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.Type = message.GetType().Name;

            _channel.BasicPublish(
                exchange: exchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body
            );
            Console.WriteLine($"[RabbitMQEventPublisher] Evento '{properties.Type}' publicado a exchange '{exchangeName}'.");
        }

        public void Dispose()
        {
            // Solo intenta cerrar/dispose si fueron creados por el constructor de producción.
            // El IModel pasado al constructor de pruebas será gestionado por el test.
            _channel?.Dispose();
            _connection?.Dispose();
            Console.WriteLine($"[RabbitMQEventPublisher] Dispose llamado.");
            GC.SuppressFinalize(this);
        }
    }
