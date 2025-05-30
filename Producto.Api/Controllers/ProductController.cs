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

        // --- NUEVO ENDPOINT PARA ACTUALIZAR PRODUCTO ---
        [HttpPut("{id}")] // Ruta: PUT api/productos/{id}
        public async Task<IActionResult> UpdateProduct([FromRoute] Guid id, [FromBody] UpdateProductoDTO dto)
        {
            try
            {
                // Es una buena práctica asegurarse de que el ID en la ruta coincida con el ID en el DTO,
                // o que el ID del DTO se establezca a partir del ID de la ruta para evitar confusiones.
                // Aquí, haremos que el ID de la ruta sea el autoritativo.
                if (dto.Id != Guid.Empty && dto.Id != id)
                {
                    return BadRequest(new { message = "El ID del producto en la URL no coincide con el ID en el cuerpo de la solicitud." });
                }

                // Aseguramos que el DTO que va al comando tenga el ID correcto (el de la ruta).
                dto.Id = id;

                var command = new UpdateProductCommand(dto);
                await _mediator.Send(command);

                // HTTP 204 No Content es una respuesta común y apropiada para una actualización exitosa
                // si no se devuelve el contenido actualizado.
                return NoContent();

            }
            catch (KeyNotFoundException knfex) // Excepción específica para "no encontrado"
            {
                return NotFound(new { message = knfex.Message });
            }
            catch (Exception ex) // Otras excepciones (ej. validación fallida desde el dominio/VOs)
            {
                // Podrías tener un manejo más granular aquí para distintos tipos de excepciones.
                return BadRequest(new { message = ex.Message });
            }
        }





    }
}