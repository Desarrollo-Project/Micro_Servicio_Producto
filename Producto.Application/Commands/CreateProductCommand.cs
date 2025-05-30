using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Producto.Application.DTO;
using MediatR;

namespace Producto.Application.Commands
{
    public class CreateProductCommand : IRequest<Guid>
    {
        public CreateProductoDTO PrDto { get; }

        public CreateProductCommand(CreateProductoDTO dto)
        {
            PrDto = dto;
        }

    }
}