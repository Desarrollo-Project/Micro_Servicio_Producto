// En: Producto.Infrastructure/EventBus/Consumer/ProductoActualizadoEventConsumer.cs
using System;
using System.Text;
using System.Text.Json; // Es preferible usar System.Text.Json consistentemente
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Producto.Domain.Events;
using Producto.Infrastructure.EventBus.InterfaceConsumer; // Para IEventConsumerConnection
using Producto.Infrastructure.Persistence.MongoOperations; // Para MongoUpdateProducto
using Producto.Infrastructure.Persistence.MongoOperations;
using RabbitMQ.Client.Events; // Para BasicDeliverEventArgs

namespace Producto.Infrastructure.EventBus.Consumer
{
    public class ProductoActualizadoEventConsumer : BackgroundService // Hereda de BackgroundService
    {
        private readonly IEventConsumerConnection _eventConsumerConnection;
        private readonly IServiceProvider _serviceProvider;
        private const string QueueName = "productos_actualizado_mongo_queue"; // Cola específica
        private const string ExchangeName = "productos_exchange"; // Exchange al que se bindea

        // Constructor modificado para inyectar IEventConsumerConnection
        public ProductoActualizadoEventConsumer(
            IEventConsumerConnection eventConsumerConnection,
            IServiceProvider serviceProvider)
        {
            _eventConsumerConnection = eventConsumerConnection ?? throw new ArgumentNullException(nameof(eventConsumerConnection));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            Console.WriteLine("[ProductoActualizadoEventConsumer] Servicio instanciado.");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"[ProductoActualizadoEventConsumer] ExecuteAsync iniciado. Registrando para Queue: '{QueueName}', Exchange: '{ExchangeName}'");
            stoppingToken.Register(() =>
                Console.WriteLine("[ProductoActualizadoEventConsumer] Solicitud de cancelación recibida."));

            try
            {
                // Llama a StartConsuming pasando el nombre del exchange también
                _eventConsumerConnection.StartConsuming(QueueName, ExchangeName, HandleMessageAsync);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProductoActualizadoEventConsumer] Error fatal al iniciar consumición: {ex.Message}");
            }

            // Mantiene el BackgroundService ejecutándose
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

        // Método 'internal' para facilitar las pruebas y mantener la lógica de manejo de mensajes aquí.
        // Este método ahora es el callback para IEventConsumerConnection.
        internal async Task<bool> HandleMessageAsync(BasicDeliverEventArgs ea)
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var eventType = ea.BasicProperties?.Type; // Usar elvis operator

            Console.WriteLine($"[ProductoActualizadoEventConsumer] Mensaje recibido. Tipo: '{eventType ?? "N/A"}'");

            // Este consumidor SOLO se interesa por ProductoActualizadoEvent
            if (eventType != null && eventType.Equals(nameof(ProductoActualizadoEvent), StringComparison.OrdinalIgnoreCase))
            {
                return await ProcessProductoActualizadoEventLogic(message);
            }
            else
            {
                Console.WriteLine($"[ProductoActualizadoEventConsumer] Evento tipo '{eventType}' ignorado. Será ACKed (si el callback devuelve true).");
                return true; // Indica a RabbitMQEventConsumerConnection que haga ACK para mensajes irrelevantes para este consumidor.
            }
        }

        // Lógica de procesamiento específica, ahora 'internal' y retorna bool para ACK/NACK
        internal async Task<bool> ProcessProductoActualizadoEventLogic(string message)
        {
            Console.WriteLine("[ProductoActualizadoEventConsumer] Procesando ProductoActualizadoEvent...");
            ProductoActualizadoEvent? evento = null;
            try
            {
                // Usar System.Text.Json consistentemente si tu publicador lo usa
                evento = JsonSerializer.Deserialize<ProductoActualizadoEvent>(message);
                if (evento == null)
                {
                    Console.WriteLine("[ProductoActualizadoEventConsumer] ERROR: Evento ProductoActualizadoEvent deserializado a null.");
                    return false; // Indica fallo para NACK
                }

                using var scope = _serviceProvider.CreateScope();
                var mongoDbUpdater = scope.ServiceProvider.GetRequiredService<MongoUpdateProducto>();
                await mongoDbUpdater.ActualizarAsync(evento);
                Console.WriteLine($"[ProductoActualizadoEventConsumer] Producto ID {evento.Id} procesado por MongoUpdateProducto.");
                return true; // Indica éxito para ACK
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"[ProductoActualizadoEventConsumer] ERROR deserializando JSON: {jsonEx.Message}. Mensaje: {message}");
                return false; // Indica fallo para NACK
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProductoActualizadoEventConsumer] ERROR procesando evento (ID: {evento?.Id}): {ex.Message}.");
                return false; // Indica fallo para NACK
            }
        }

        public override void Dispose()
        {
            Console.WriteLine("[ProductoActualizadoEventConsumer] Dispose llamado.");
            _eventConsumerConnection.Dispose();
            base.Dispose();
        }
    }
}