using MediatR;
using Microsoft.AspNetCore.Mvc;
using Producto.Application.Commands;
using Producto.Application.DTO;
using System; // Para ArgumentNullException y Exception
using System.Threading.Tasks; // Para Task
using System.Collections.Generic;
using Producto.Application.Contracts.Persistence;
using Producto.Domain.Aggregates;
using Producto.Domain.Repositories; 
using Producto.Domain.Excepciones; 

namespace Producto.Presentation.Controllers
{
    [ApiController]
    [Route("api/productos")] // Ruta base para todos los endpoints de este controlador
    public class ProductosController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly I_Servicio_Imagen _servicioImagen;
        private readonly IProductReadRepository _productReadRepository;

        public ProductosController(IMediator mediator,I_Servicio_Imagen servicioImagen, IProductReadRepository productReadRepository)
        {
            _mediator = mediator;
            _servicioImagen = servicioImagen;
            _productReadRepository = productReadRepository;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductoDTO dto)
        {
            try
            {
                var Url = _servicioImagen.Cargar_Imagen(dto.IForm);
                dto.ImagenUrl = await Url; // Asignar la URL de la imagen cargada al DTO
                var command = new CreateProductCommand(dto); // Creamos el comando con el DTO
                var productId = await _mediator.Send(command);

                return CreatedAtAction(nameof(CreateProduct), new { id = productId }, new { id = productId });
            }
            catch (Exception ex) // Considera manejar excepciones de validación de forma más específica si es necesario
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("Actualizar/{id:guid}")] 
        public async Task<IActionResult> UpdateProduct([FromRoute] Guid id, [FromBody] UpdateProductoDTO dto)
        {
            try
            {
                if (dto.Id != Guid.Empty && dto.Id != id)
                {
                    return BadRequest(new { message = "El ID del producto en la URL no coincide con el ID en el cuerpo de la solicitud." });
                }

                dto.Id = id;

                var command = new UpdateProductCommand(dto);
                await _mediator.Send(command);
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
                    throw new Excepcion_Guid_Invalido("El GUID no puede ser vacio");
                }

                //var url_Producto = _productReadRepository.Obtener_Url_Producto(id);
                //this._servicioImagen.Eliminar_Imagen(null);


                var command = new DeleteProductCommand(id);
                 await _mediator.Send(command);

                // Deberia retornar RESPONSE 204 que es el tipico para la eliminacion
                return NoContent();
            }
            catch (KeyNotFoundException knfex) // Captura la excepción si el producto no se encontró
            {
                return NotFound(new { message = knfex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Ocurrió un error al intentar eliminar el producto: {ex.Message}" });
            }
        }

        
        
        // Get Para Productos 
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