using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Producto.Domain.Events
{
    public class ProductoEliminadoEvent : INotification
    {
        public Guid Id { get; } // El ID del producto que fue eliminado

        public ProductoEliminadoEvent(Guid id)
        {
            Id = id;
        }
    }
}
