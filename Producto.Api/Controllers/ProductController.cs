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

        [HttpDelete("Eliminar/{id:guid}")] // Ruta: DELETE api/productos/{id}
        public async Task<IActionResult> DeleteProduct([FromRoute] Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest(new { message = "El ID del producto proporcionado no es válido." });
                }

                var command = new DeleteProductCommand(id);
                await _mediator.Send(command);

                // HTTP 204 No Content es la respuesta estándar para una eliminación exitosa.
                return NoContent();
            }
            catch (KeyNotFoundException knfex) // Captura la excepción si el producto no se encontró
            {
                return NotFound(new { message = knfex.Message });
            }
            // catch (ProductoNotFoundException pnex) // Si usas tu excepción personalizada
            // {
            //     _logger.LogWarning(pnex, "Intento de eliminar producto no encontrado con ID: {ProductId}", id);
            //     return NotFound(new { message = pnex.Message });
            // }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Ocurrió un error al intentar eliminar el producto: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                Console.WriteLine("[ProductosController] Solicitud recibida para GET api/productos");
                var command = new GetProductsCommand();
                IEnumerable<Product> result = await _mediator.Send(command);

                if (result == null || !result.Any())
                {
                    Console.WriteLine("[ProductosController] No se encontraron productos para devolver.");
                    return Ok(new List<Product>());
                }

                Console.WriteLine($"[ProductosController] Devolviendo {result.Count()} entidades Product.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProductosController] ERROR: {ex.Message}");
                return StatusCode(500, new { message = "Ocurrió un error al procesar la solicitud." });
            }
        }



    }
}