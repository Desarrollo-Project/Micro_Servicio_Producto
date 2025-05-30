// En: Producto.Infrastructure/EventBus/InterfaceConsumer/IEventConsumerConnection.cs
using System;
using System.Threading.Tasks;
using RabbitMQ.Client.Events; // Para BasicDeliverEventArgs

namespace Producto.Infrastructure.EventBus.InterfaceConsumer
{
    public interface IEventConsumerConnection : IDisposable
    {
        void StartConsuming(
            string queueName,
            string exchangeName,
            Func<BasicDeliverEventArgs, Task<bool>> handleMessageCallback);

    }
}