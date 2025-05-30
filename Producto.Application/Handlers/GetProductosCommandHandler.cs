// En Producto.Application.Handlers/GetProductsCommandHandler.cs
using MediatR;
using Producto.Application.Commands;    // Donde está GetProductsCommand
using Producto.Application.Contracts.Persistence; // Donde está IProductReadRepository
using Producto.Domain.Aggregates;      // Para Product
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Producto.Application.Handlers
{
    // Devuelve IEnumerable<Product>
    public class GetProductsCommandHandler : IRequestHandler<GetProductsCommand, IEnumerable<Product>>
    {
        private readonly IProductReadRepository _productReadRepository;
        
        public GetProductsCommandHandler(IProductReadRepository productReadRepository)
        {
            _productReadRepository = productReadRepository ?? throw new ArgumentNullException(nameof(productReadRepository));
            Console.WriteLine("[GetProductsCommandHandler] Inicializado con IProductReadRepository.");
        }

        // Devuelve Task<IEnumerable<Product>>
        public async Task<IEnumerable<Product>> Handle(GetProductsCommand request, CancellationToken cancellationToken)
        {
            Console.WriteLine("[GetProductsCommandHandler] Procesando GetProductsCommand usando IProductReadRepository para obtener agregados Product...");

            var productos = await _productReadRepository.GetAllAsync(cancellationToken);

            Console.WriteLine($"[GetProductsCommandHandler] Se obtuvieron {productos.Count()} agregados Product.");
            return productos;
        }
    }
}