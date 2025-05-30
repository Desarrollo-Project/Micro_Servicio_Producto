using MediatR;
using Microsoft.AspNetCore.Mvc;
using Producto.Application.Commands;
using Producto.Application.DTO;
using System; // Para ArgumentNullException y Exception
using System.Threading.Tasks; // Para Task
using System.Collections.Generic;
using Producto.Domain.Aggregates; // Para KeyNotFoundException (si no está ya importado)

namespace Producto.Presentation.Controllers
{
    [ApiController]
    [Route("api/productos")] // Ruta base para todos los endpoints de este controlador
    public class ProductosController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProductosController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductoDTO dto)
        {
            try
            {
                var command = new CreateProductCommand(dto); // Creamos el comando con el DTO
                var productId = await _mediator.Send(command);
                // Devuelve 201 Created con la ubicación del nuevo recurso y el ID.
                // Idealmente, nameof(GetProductById) si tienes un endpoint GET para obtener el producto.
                return CreatedAtAction(nameof(CreateProduct), new { id = productId }, new { id = productId });
            }
            catch (Exception ex) // Considera manejar excepciones de validación de forma más específica si es necesario
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        



    }
}