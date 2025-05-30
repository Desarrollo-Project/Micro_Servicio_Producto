// En: Producto.Infrastructure/Persistence/MongoOperations/MongoDeleteProducto.cs
using MongoDB.Driver;
using Producto.Domain.Aggregates; // Para ProductMongo (si tu modelo de lectura es ese)
using Producto.Domain.Events;   // Para ProductoEliminadoEvent
using System;
using System.Threading.Tasks;

namespace Producto.Infrastructure.Persistence.MongoOperations
{
    public class MongoDeleteProducto
    {
        private readonly IMongoCollection<ProductMongo> _productosCollection; // Usando ProductMongo

        public MongoDeleteProducto(IMongoDatabase mongoDatabase)
        {
            if (mongoDatabase == null)
                throw new ArgumentNullException(nameof(mongoDatabase));

            string collectionName = "productos";
            _productosCollection = mongoDatabase.GetCollection<ProductMongo>(collectionName);
            Console.WriteLine($"[MongoDeleteProducto] Conectado a la colección MongoDB: {collectionName}");
        }

        public virtual async Task EliminarAsync(ProductoEliminadoEvent evento)
        {
            if (evento == null)
            {
                Console.WriteLine("[MongoDeleteProducto] Evento ProductoEliminadoEvent es null.");
                return;
            }

            Console.WriteLine($"[MongoDeleteProducto] Eliminando documento en MongoDB para Producto ID: {evento.Id}");
            try
            {
                var filter = Builders<ProductMongo>.Filter.Eq(p => p.Id, evento.Id);
                var result = await _productosCollection.DeleteOneAsync(filter);

                if (result.IsAcknowledged)
                {
                    if (result.DeletedCount > 0)
                    {
                        Console.WriteLine($"[MongoDeleteProducto] Producto ELIMINADO de MongoDB. ID: {evento.Id}. Documentos eliminados: {result.DeletedCount}");
                    }
                    else
                    {
                        Console.WriteLine($"[MongoDeleteProducto] WARN: Se intentó eliminar el producto con ID: {evento.Id} de MongoDB, pero no se encontró. Documentos eliminados: {result.DeletedCount}");
                    }
                }
                else
                {
                    Console.WriteLine($"[MongoDeleteProducto] WARN: Operación DeleteOne en MongoDB para ID {evento.Id} no fue reconocida (acknowledged).");
                }
            }
            catch (MongoException ex)
            {
                Console.WriteLine($"[MongoDeleteProducto] ERROR de MongoDB al eliminar ProductoEliminadoEvent ID {evento.Id}: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MongoDeleteProducto] ERROR general al procesar ProductoEliminadoEvent ID {evento.Id}: {ex.Message}");
                throw;
            }
        }
    }
}