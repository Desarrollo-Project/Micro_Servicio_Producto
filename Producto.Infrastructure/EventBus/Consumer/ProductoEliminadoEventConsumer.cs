// En: Producto.Infrastructure/EventBus/Consumer/ProductoEliminadoEventConsumerService.cs
using System;
using System.Text;
using System.Text.Json; // Usaremos System.Text.Json consistentemente
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Producto.Domain.Events;
using Producto.Infrastructure.EventBus.InterfaceConsumer; // Para IEventConsumerConnection
using Producto.Infrastructure.Persistence.MongoOperations; // Para MongoDeleteProducto
using Producto.Infrastructure.Persistence.MongoOperations;
using RabbitMQ.Client.Events; // Para BasicDeliverEventArgs

namespace Producto.Infrastructure.EventBus.Consumer
{
    public class ProductoEliminadoEventConsumer : BackgroundService // Hereda de BackgroundService
    {
        private readonly IEventConsumerConnection _eventConsumerConnection;
        private readonly IServiceProvider _serviceProvider;
        private const string QueueName = "productos_eliminado_mongo_queue"; // Cola específica
        private const string ExchangeName = "productos_exchange"; // Exchange al que se bindea

        // Constructor modificado para inyectar IEventConsumerConnection
        public ProductoEliminadoEventConsumer(
            IEventConsumerConnection eventConsumerConnection,
            IServiceProvider serviceProvider)
        {
            _eventConsumerConnection = eventConsumerConnection ?? throw new ArgumentNullException(nameof(eventConsumerConnection));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            Console.WriteLine("[ProductoEliminadoEventConsumer] Servicio instanciado.");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"[ProductoEliminadoEventConsumer] ExecuteAsync iniciado. Registrando para Queue: '{QueueName}', Exchange: '{ExchangeName}'");

            stoppingToken.Register(() =>
                Console.WriteLine("[ProductoEliminadoEventConsumer] Solicitud de cancelación recibida."));

            try
            {
                // Llama a StartConsuming pasando el nombre del exchange también
                _eventConsumerConnection.StartConsuming(QueueName, ExchangeName, HandleMessageAsync);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProductoEliminadoEventConsumer] Error fatal al iniciar consumición: {ex.Message}");
                // El BackgroundService podría detenerse si ExecuteAsync lanza una excepción aquí.
            }

            // Mantiene el BackgroundService ejecutándose hasta que se solicite la cancelación
            return Task.Run(async () => {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(Timeout.Infinite, stoppingToken);
                    }
                    catch (TaskCanceledException)
                    {
                        // Esto es esperado cuando stoppingToken.Cancel() es llamado.
                        Console.WriteLine($"[ProductoEliminadoEventConsumer] Task.Delay cancelado en ExecuteAsync para '{QueueName}'.");
                    }
                }
            }, stoppingToken);
        }

        // El callback que se pasa a StartConsuming, ahora como método separado y 'internal' para pruebas.
        // Recibe BasicDeliverEventArgs y debe retornar Task<bool> para el ACK/NACK.
        internal async Task<bool> HandleMessageAsync(BasicDeliverEventArgs ea)
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var eventType = ea.BasicProperties?.Type; // Usar elvis operator por si BasicProperties es null

            Console.WriteLine($"[ProductoEliminadoEventConsumer] Mensaje recibido. Tipo: '{eventType ?? "N/A"}'");

            // Este consumidor SOLO se interesa por ProductoEliminadoEvent
            if (eventType != null && eventType.Equals(nameof(ProductoEliminadoEvent), StringComparison.OrdinalIgnoreCase))
            {
                return await ProcessProductoEliminadoEventLogic(message);
            }
            else
            {
                Console.WriteLine($"[ProductoEliminadoEventConsumer] Evento tipo '{eventType}' ignorado. Será ACKed.");
                return true; // Indica a RabbitMQEventConsumerConnection que haga ACK para mensajes irrelevantes
            }
        }

        // Lógica de procesamiento específica, ahora 'internal' y retorna bool para ACK/NACK
        internal async Task<bool> ProcessProductoEliminadoEventLogic(string message)
        {
            Console.WriteLine("[ProductoEliminadoEventConsumer] Procesando ProductoEliminadoEvent...");
            ProductoEliminadoEvent? evento = null;
            try
            {
                evento = JsonSerializer.Deserialize<ProductoEliminadoEvent>(message);
                if (evento == null)
                {
                    Console.WriteLine("[ProductoEliminadoEventConsumer] ERROR: Evento ProductoEliminadoEvent deserializado a null.");
                    return false; // Indica fallo para NACK
                }

                using var scope = _serviceProvider.CreateScope();
                var mongoDbDeleter = scope.ServiceProvider.GetRequiredService<MongoDeleteProducto>();
                await mongoDbDeleter.EliminarAsync(evento); // Asumimos que MongoDeleteProducto.EliminarAsync existe
                Console.WriteLine($"[ProductoEliminadoEventConsumer] Producto ID {evento.Id} procesado por MongoDeleteProducto.");
                return true; // Indica éxito para ACK
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"[ProductoEliminadoEventConsumer] ERROR deserializando JSON: {jsonEx.Message}. Mensaje: {message}");
                return false; // Indica fallo para NACK
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProductoEliminadoEventConsumer] ERROR procesando evento (ID: {evento?.Id}): {ex.Message}.");
                return false; // Indica fallo para NACK
            }
        }

        public override void Dispose() // Sobrescribe Dispose de BackgroundService
        {
            Console.WriteLine("[ProductoEliminadoEventConsumer] Dispose llamado.");
            _eventConsumerConnection.Dispose();
            base.Dispose();
        }
    }
}