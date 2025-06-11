using MongoDB.Driver;
using Producto.Domain.Events;
using Producto.Domain.Aggregates; // Para ProductMongo
using Producto.Domain.VO;
using Producto.Infrastructure.Persistance.MongoOperations; // Para MongoCreateProducto
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Producto.Infrastructure.Persistance.MongoOperations
{
    public class MongoCreateProducto
    {
        private readonly IMongoCollection<ProductMongo> _productosCollection;

        public MongoCreateProducto(IMongoDatabase mongoDatabase)
        {
            if (mongoDatabase == null)
                throw new ArgumentNullException(nameof(mongoDatabase));

            string collectionName = "productos";
            _productosCollection = mongoDatabase.GetCollection<ProductMongo>(collectionName);
            Console.WriteLine($"[ProductoMongoDbCreator] Conectado a la colección MongoDB: {collectionName}");
        }

        public virtual async Task CrearAsync(ProductoCreadoEvent evento)
        {
            if (evento == null)
            {
                Console.WriteLine("[ProductoMongoDbCreator] Evento ProductoCreadoEvent es null. No se puede crear el documento.");
                return;
            }

            Console.WriteLine($"[ProductoMongoDbCreator] Creando documento en MongoDB para Producto ID: {evento.Id}");
            try
            {
                var productoMongo = new ProductMongo
                {
                    Id = evento.Id,
                    Nombre = new NombreProductoVO(evento.Nombre),
                    PrecioBase = new PrecioBaseVO(evento.PrecioBase),
                    Categoria = new CategoriaVO(evento.Categoria),
                    ImagenUrl = new ImagenUrlVo(evento.ImagenUrl),
                    Estado = new EstadoVO(evento.Estado),
                    Id_Usuario = new Id_Usuario_VO(evento.Id_Usuario)

                };

                await _productosCollection.InsertOneAsync(productoMongo);
                Console.WriteLine($"[ProductoMongoDbCreator] Producto CREADO en MongoDB. ID: {evento.Id}");
            }
            catch (MongoException ex)
            {
                Console.WriteLine($"[ProductoMongoDbCreator] ERROR de MongoDB al insertar ProductoCreadoEvent ID {evento.Id}: {ex.Message}");
                throw;
            }
            catch (Exception ex) // Captura errores de constructores de VO, etc.
            {
                Console.WriteLine($"[ProductoMongoDbCreator] ERROR general al procesar ProductoCreadoEvent ID {evento.Id}: {ex.Message}");
                throw;
            }
        }
    }
}
