using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Producto.Domain.Repositories
{
    public interface IEventPublisher
    {
        void Publish<T>(T message, string exchangeName, string routingKey);
    }
}
