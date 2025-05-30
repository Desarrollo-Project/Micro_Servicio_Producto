using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Producto.Application.Commands;
using Producto.Domain.Repositories;
using Producto.Domain.Aggregates;
using Producto.Domain.VO;
using Producto.Domain.Events;
using Producto.Application.Handlers;


namespace Producto.Application.Handlers
{
    public class CreateProductoCommandHandler : IRequestHandler<CreateProductCommand, Guid>
    {

        private readonly IProductoRepository _productoRepository;
        private readonly IMediator _mediator;
        private readonly IEventPublisher _eventPublisher;

        public CreateProductoCommandHandler(
            IProductoRepository productoRepository,
            IMediator mediator,
            IEventPublisher eventPublisher)
        {
            _productoRepository = productoRepository;
            _mediator = mediator;
            _eventPublisher = eventPublisher;
        }


        public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            var dto = request.PrDto;

            var producto = new Product(
                Guid.NewGuid(),
                new NombreProductoVO(dto.Nombre),
                new PrecioBaseVO(dto.PrecioBase),
                new CategoriaVO(dto.Categoria),
                new ImagenUrlVo(dto.ImagenUrl)
            );


            await _productoRepository.AgregarAsync(producto);

            // Emitimos el evento para RabbitMQ
            var productoCreadoEvent = new ProductoCreadoEvent(
                producto.Id,
                producto.Nombre.Valor,
                producto.PrecioBase.Valor,
                producto.Categoria.Valor,
                producto.ImagenUrl.Valor
            );

            // Publicar el evento en RabbitMQ
            _eventPublisher.Publish(productoCreadoEvent, "productos_exchange", ""); // Fanout: routingKey vacío

            // (Opcional: si quieres seguir usando MediatR internamente)
            await _mediator.Publish(productoCreadoEvent);


            return producto.Id;
        }
    }

}




