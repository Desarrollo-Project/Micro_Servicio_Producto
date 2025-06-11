using MediatR;
using Producto.Application.Commands;
// using Producto.Application.DTO; // No es directamente necesario aquí si el Command ya lo tiene
using Producto.Domain.Repositories;
using Producto.Domain.Aggregates;
// using Producto.Domain.VO; // No es directamente necesario aquí si el Agregado los maneja internamente
using Producto.Domain.Events;
using System;
using System.Threading;
using System.Threading.Tasks;
using Producto.Application.DTO;

namespace Producto.Application.Handlers
{
    public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand>
    {
        private readonly IProductoRepository _productoRepository;
        private readonly IMediator _mediator;
        private readonly IEventPublisher _eventPublisher;

        public UpdateProductCommandHandler(
            IProductoRepository productoRepository,
            IMediator mediator,
            IEventPublisher eventPublisher)
        {
            _productoRepository = productoRepository ?? throw new ArgumentNullException(nameof(productoRepository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var dto = request.PrDto;

            // 1. Obtener el producto existente desde el repositorio
            var producto = await _productoRepository.ObtenerPorIdAsync(dto.Id);

            if (producto == null)
            {
                throw new KeyNotFoundException($"Producto con ID {dto.Id} no encontrado.");
            }

            // 2. Actualizar las propiedades del producto agregado utilizando el método del agregado.
            producto.Actualizar(
                dto.Nombre,
                dto.PrecioBase,
                dto.Categoria,
                dto.ImagenUrl,
                dto.Estado,
                dto.Id_Usuario
            );

            // 3. Persistir los cambios en la base de datos
            await _productoRepository.ActualizarAsync(producto);

   
            var productoActualizadoEvent = new ProductoActualizadoEvent
            {
                Id = producto.Id,
                Nombre = producto.Nombre.Valor,
                PrecioBase = producto.PrecioBase.Valor, 
                Categoria = producto.Categoria.Valor,
                ImagenUrl = producto.ImagenUrl.Valor,
                Estado = producto.Estado.Valor,
                Id_Usuario = producto.Id_Usuario.Valor
            };

            _eventPublisher.Publish(productoActualizadoEvent, "productos_exchange", "producto.actualizado");
            await _mediator.Publish(productoActualizadoEvent, cancellationToken);
        }
    }
}