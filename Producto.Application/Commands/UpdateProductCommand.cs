using MediatR; // Asegúrate de tener este using
using Producto.Application.DTO;

namespace Producto.Application.Commands
{
    // Añade ": IRequest" aquí
    public class UpdateProductCommand : IRequest
    {
        public UpdateProductoDTO PrDto { get; }

        public UpdateProductCommand(UpdateProductoDTO dto)
        {
            PrDto = dto;
        }
    }
}