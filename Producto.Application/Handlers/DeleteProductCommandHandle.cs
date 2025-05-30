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

            // Paso 1: (Opcional pero recomendado) Verificar si el producto existe antes de intentar eliminarlo.
            // Esto permite devolver un 404 más específico si no se encuentra.
            var productoExistente = await _productoRepository.ObtenerPorIdAsync(request.Id);
            if (productoExistente == null)
            {
                // Puedes usar una excepción personalizada si la tienes, o KeyNotFoundException.
                // throw new ProductoNotFoundException(request.Id); 
                throw new KeyNotFoundException($"Producto con ID {request.Id} no encontrado.");
            }

            // Paso 2: Eliminar el producto del repositorio de escritura (PostgreSQL)
            await _productoRepository.EliminarAsync(request.Id);

            // Paso 3: Crear el evento de dominio
            var productoEliminadoEvent = new ProductoEliminadoEvent(request.Id);

            // Paso 4: Publicar el evento al bus de eventos externo (RabbitMQ)
            // Ajusta el exchange y routing key según tu configuración
            _eventPublisher.Publish(productoEliminadoEvent, "productos_exchange", "producto.eliminado");

            // Paso 5: Publicar el evento internamente a través de MediatR
            await _mediator.Publish(productoEliminadoEvent, cancellationToken);

            // MediatR se encarga de devolver Unit.Value implícitamente para IRequest sin TResponse
        }
    }
}