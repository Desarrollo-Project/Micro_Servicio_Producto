// En: Producto.Infrastructure/EventBus/Consumer/ProductoCreadoEventConsumerService.cs
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Producto.Domain.Events;
using Producto.Infrastructure.EventBus.InterfaceConsumer; // Para IEventConsumerConnection
using Producto.Infrastructure.Persistance.MongoOperations;
using RabbitMQ.Client.Events; // Para BasicDeliverEventArgs

namespace Producto.Infrastructure.EventBus.Consumer
{
    public class ProductoCreadoEventConsumer : BackgroundService
    {
        private readonly IEventConsumerConnection _eventConsumerConnection;
        private readonly IServiceProvider _serviceProvider;
        private const string QueueName = "productos_creado_mongo_queue"; // Cola específica
        private const string ExchangeName = "productos_exchange"; // Exchange al que se bindea

        public ProductoCreadoEventConsumer(
            IEventConsumerConnection eventConsumerConnection, // Inyecta la interfaz
            IServiceProvider serviceProvider)
        {
            _eventConsumerConnection = eventConsumerConnection ?? throw new ArgumentNullException(nameof(eventConsumerConnection));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            Console.WriteLine("[ProductoCreadoEventConsumer] Servicio instanciado.");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"[ProductoCreadoEventConsumer] ExecuteAsync iniciado. Registrando para Queue: '{QueueName}', Exchange: '{ExchangeName}'");
            stoppingToken.Register(() =>
                Console.WriteLine("[ProductoCreadoEventConsumer] Solicitud de cancelación recibida."));

            try
            {
                _eventConsumerConnection.StartConsuming(QueueName, ExchangeName, HandleMessageAsync);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProductoCreadoEventConsumer] Error fatal al iniciar consumición: {ex.Message}");
            }

            return Task.Run(async () => {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(Timeout.Infinite, stoppingToken);
                    }
                    catch (TaskCanceledException) { /* Esperado al cancelar */ }
                }
            }, stoppingToken);
        }

        // Método 'internal' para el callback, facilita las pruebas
        internal async Task<bool> HandleMessageAsync(BasicDeliverEventArgs ea)
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var eventType = ea.BasicProperties?.Type;

            Console.WriteLine($"[ProductoCreadoEventConsumer] Mensaje recibido. Tipo: '{eventType ?? "N/A"}'");

            if (eventType != null && eventType.Equals(nameof(ProductoCreadoEvent), StringComparison.OrdinalIgnoreCase))
            {
                return await ProcessProductoCreadoEventLogic(message);
            }
            else
            {
                Console.WriteLine($"[ProductoCreadoEventConsumer] Evento tipo '{eventType}' ignorado. Será ACKed.");
                return true; // ACK para mensajes irrelevantes
            }
        }

        // Lógica de procesamiento específica, 'internal' y retorna bool
        internal async Task<bool> ProcessProductoCreadoEventLogic(string message)
        {
            Console.WriteLine("[ProductoCreadoEventConsumer] Procesando ProductoCreadoEvent...");
            ProductoCreadoEvent? evento = null;
            try
            {
                evento = JsonSerializer.Deserialize<ProductoCreadoEvent>(message);
                if (evento == null)
                {
                    Console.WriteLine("[ProductoCreadoEventConsumer] ERROR: Evento deserializado a null.");
                    return false; // NACK
                }

                using var scope = _serviceProvider.CreateScope();
                var mongoDbCreator = scope.ServiceProvider.GetRequiredService<MongoCreateProducto>();
                await mongoDbCreator.CrearAsync(evento); // MongoCreateProducto.CrearAsync ya está virtual
                Console.WriteLine($"[ProductoCreadoEventConsumer] Producto ID {evento.Id} procesado por MongoCreateProducto.");
                return true; // ACK
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"[ProductoCreadoEventConsumer] ERROR deserializando JSON: {jsonEx.Message}. Mensaje: {message}");
                return false; // NACK
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProductoCreadoEventConsumer] ERROR procesando evento (ID: {evento?.Id}): {ex.Message}.");
                return false; // NACK
            }
        }

        public override void Dispose()
        {
            Console.WriteLine("[ProductoCreadoEventConsumer] Dispose llamado.");
            _eventConsumerConnection.Dispose();
            base.Dispose();
        }
    }
}