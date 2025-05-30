using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Producto.Application.Commands
{

        public class DeleteProductCommand : IRequest
        {
            public Guid Id { get; }

            public DeleteProductCommand(Guid id)
            {
                if (id == Guid.Empty)
                {
                    throw new ArgumentException("El ID del producto no puede ser vacío.", nameof(id));
                }
                Id = id;
            }
        }
    
}
