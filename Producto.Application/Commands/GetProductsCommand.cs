// En Producto.Application.Commands/GetProductsCommand.cs
using MediatR;
using Producto.Domain.Aggregates; // Para Product
using System.Collections.Generic;

namespace Producto.Application.Commands
{
    public class GetProductsCommand : IRequest<IEnumerable<Product>> // Correcto, espera Product
    {
        public GetProductsCommand() { }
    }
}