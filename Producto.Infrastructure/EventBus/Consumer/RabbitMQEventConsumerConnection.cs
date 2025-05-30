// En: Producto.Infrastructure/EventBus/Consumer/RabbitMQEventConsumerConnection.cs
using System;
using System.Text; // No es necesario aquí si el callback maneja la deserialización
using System.Threading.Tasks;
using Producto.Infrastructure.EventBus.InterfaceConsumer; // Para IEventConsumerConnection
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Producto.Infrastructure.EventBus.Consumer
{
    public class RabbitMQEventConsumerConnection : IEventConsumerConnection
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        // private const string CommonExchangeType = ExchangeType.Fanout; // Ya no es necesario aquí si se pasa el tipo

        public RabbitMQEventConsumerConnection(string host, string user, string pass)
        {
            var factory = new ConnectionFactory()
            {
                HostName = host,
                UserName = user,
                Password = pass,
                DispatchConsumersAsync = true // Importante para AsyncEventingBasicConsumer
            };

            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                Console.WriteLine($"[RabbitMQConnection] Conexión y canal creados para {host}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RabbitMQConnection] CRITICAL: No se pudo crear conexión/canal: {ex.Message}");
                throw;
            }
        }

        // ESTA ES LA FIRMA CORRECTA QUE COINCIDE CON LA INTERFAZ REFINADA
        public void StartConsuming(
            string queueName,
            string exchangeName, // El consumidor ahora especifica el exchange
            Func<BasicDeliverEventArgs, Task<bool>> handleMessageCallback)
        {
            try
            {
                Console.WriteLine($"[RabbitMQConnection] Configurando consumición: Queue='{queueName}', Exchange='{exchangeName}'");

                // Asegurar que el exchange exista (idempotente)
                _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Fanout, durable: true); // Asumiendo Fanout

                // Declarar la cola (idempotente)
                _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                // Vincular la cola al exchange (idempotente)
                _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: ""); // Para Fanout

                _channel.BasicQos(0, 1, false); // Procesar un mensaje a la vez

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += async (sender, ea) =>
                {
                    bool processedSuccessfully = false;
                    try
                    {
                        // Llama al callback proporcionado por el consumidor específico (ej. ProductoCreadoEventConsumer)
                        // Este callback ahora recibe BasicDeliverEventArgs completo.
                        processedSuccessfully = await handleMessageCallback(ea);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[RabbitMQConnection] ERROR en handleMessageCallback para DeliveryTag {ea.DeliveryTag} en Queue '{queueName}': {ex.Message}");
                        processedSuccessfully = false;
                    }
                    finally
                    {
                        if (processedSuccessfully)
                        {
                            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                            // Console.WriteLine($"[RabbitMQConnection] Mensaje ACK. DeliveryTag: {ea.DeliveryTag}, Queue: {queueName}");
                        }
                        else
                        {
                            _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false); // No re-encolar si falla
                            Console.WriteLine($"[RabbitMQConnection] Mensaje NACK (no re-encolado). DeliveryTag: {ea.DeliveryTag}, Queue: {queueName}");
                        }
                    }
                };

                _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer); // autoAck: false
                Console.WriteLine($"[RabbitMQConnection] Consumiendo de '{queueName}'. autoAck: false.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RabbitMQConnection] ERROR al iniciar consumición para Queue '{queueName}': {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            Console.WriteLine($"[RabbitMQConnection] Dispose llamado.");
            // Es importante cerrar el canal ANTES que la conexión
            try { _channel?.Close(); } catch (Exception ex) { Console.WriteLine($"[RabbitMQConnection] Error cerrando canal: {ex.Message}"); }
            _channel?.Dispose();
            try { _connection?.Close(); } catch (Exception ex) { Console.WriteLine($"[RabbitMQConnection] Error cerrando conexión: {ex.Message}"); }
            _connection?.Dispose();
            Console.WriteLine($"[RabbitMQConnection] Recursos de RabbitMQ liberados.");
            GC.SuppressFinalize(this);
        }
    }
}