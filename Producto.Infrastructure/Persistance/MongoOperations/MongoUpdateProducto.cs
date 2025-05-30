// En: Producto.Infrastructure/Persistence/MongoOperations/MongoUpdateProducto.cs
using MongoDB.Driver;
using Producto.Domain.Aggregates; // ProductMongo
using Producto.Domain.Events;   // ProductoActualizadoEvent
using Producto.Domain.VO;       // Value Objects
using System;
using System.Threading.Tasks;

namespace Producto.Infrastructure.Persistence.MongoOperations
{
    public class MongoUpdateProducto
    {
        private readonly IMongoCollection<ProductMongo> _productosCollection;

        public MongoUpdateProducto(IMongoDatabase mongoDatabase)
        {
            if (mongoDatabase == null)
                throw new ArgumentNullException(nameof(mongoDatabase));

            string collectionName = "productos";
            _productosCollection = mongoDatabase.GetCollection<ProductMongo>(collectionName);
            Console.WriteLine($"[MongoUpdateProducto] Conectado a la colección MongoDB: {collectionName}");
        }

        public virtual async Task ActualizarAsync(ProductoActualizadoEvent evento)
        {
            if (evento == null)
            {
                Console.WriteLine("[MongoUpdateProducto] Evento ProductoActualizadoEvent es null.");
                return;
            }

            Console.WriteLine($"[MongoUpdateProducto] Actualizando/Insertando documento en MongoDB para Producto ID: {evento.Id}");
            try
            {
                var filter = Builders<ProductMongo>.Filter.Eq(p => p.Id, evento.Id);
                var productoMongoActualizado = new ProductMongo
                {
                    Id = evento.Id,
                    Nombre = new NombreProductoVO(evento.Nombre),
                    PrecioBase = new PrecioBaseVO(evento.PrecioBase),
                    Categoria = new CategoriaVO(evento.Categoria),
                    ImagenUrl = new ImagenUrlVo(evento.ImagenUrl)
                };

                var result = await _productosCollection.ReplaceOneAsync(filter, productoMongoActualizado, new ReplaceOptions { IsUpsert = true });

                if (result.IsAcknowledged)
                {
                    if (result.MatchedCount > 0 && result.ModifiedCount > 0)
                        Console.WriteLine($"[MongoUpdateProducto] Producto ACTUALIZADO en MongoDB. ID: {evento.Id}");
                    else if (result.UpsertedId != null)
                        Console.WriteLine($"[MongoUpdateProducto] Producto INSERTADO (Upsert) en MongoDB. ID: {evento.Id}, UpsertedId: {result.UpsertedId}");
                    else if (result.MatchedCount > 0 && result.ModifiedCount == 0)
                        Console.WriteLine($"[MongoUpdateProducto] Producto encontrado en MongoDB, pero no se requirieron modificaciones. ID: {evento.Id}");
                    else
                        Console.WriteLine($"[MongoUpdateProducto] WARN: Producto no encontrado para actualizar y no se insertó (Upsert) en MongoDB. ID: {evento.Id}");
                }
                else
                {
                    Console.WriteLine($"[MongoUpdateProducto] WARN: Operación ReplaceOne en MongoDB para ID {evento.Id} no fue reconocida.");
                }
            }
            catch (MongoException ex)
            {
                Console.WriteLine($"[MongoUpdateProducto] ERROR de MongoDB al actualizar ProductoActualizadoEvent ID {evento.Id}: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MongoUpdateProducto] ERROR general al procesar ProductoActualizadoEvent ID {evento.Id}: {ex.Message}");
                throw;
            }
        }
    }
}