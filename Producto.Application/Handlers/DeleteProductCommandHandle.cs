// En Producto.Application.Handlers/DeleteProductCommandHandler.cs
using MediatR;
using Producto.Application.Commands; // Donde está DeleteProductCommand
using Producto.Domain.Repositories; // IProductoRepository
using Producto.Domain.Events;       // ProductoEliminadoEvent
using System;
using System.Threading;
using System.Threading.Tasks;

// using Producto.Application.Exceptions; // Si tienes ProductoNotFoundException

namespace Producto.Application.Handlers
{
    public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
    {
        private readonly IProductoRepository _productoRepository;
        private readonly IMediator _mediator;
        private readonly IEventPublisher _eventPublisher;

        public DeleteProductCommandHandler(
            IProductoRepository productoRepository,
            IMediator mediator,
            IEventPublisher eventPublisher)
        {
            _productoRepository = productoRepository ?? throw new ArgumentNullException(nameof(productoRepository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {

            var productoExistente = await _productoRepository.ObtenerPorIdAsync(request.Id);
            if (productoExistente == null)
            {
                throw new KeyNotFoundException($"Producto con ID {request.Id} no encontrado.");
            }
            await _productoRepository.EliminarAsync(request.Id);

            var productoEliminadoEvent = new ProductoEliminadoEvent(request.Id);

            _eventPublisher.Publish(productoEliminadoEvent, "productos_exchange", "producto.eliminado");

            await _mediator.Publish(productoEliminadoEvent, cancellationToken);

        }
    }
}