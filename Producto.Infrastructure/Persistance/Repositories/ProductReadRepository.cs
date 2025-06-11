
using MongoDB.Driver;
using Producto.Application.Contracts.Persistence; 
using Producto.Domain.Aggregates;                 
using Producto.Domain.VO;                         
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Producto.Domain.Excepciones;

namespace Producto.Infrastructure.Persistance.Repositories
{
    public class ProductReadRepository : IProductReadRepository // Implementa la nueva interfaz
    {
        private readonly IMongoCollection<ProductMongo> _productosReadCollection;

        public ProductReadRepository(IMongoDatabase mongoDatabase)
        {
            if (mongoDatabase == null)
                throw new ArgumentNullException(nameof(mongoDatabase));

            string collectionName = "productos"; 
            _productosReadCollection = mongoDatabase.GetCollection<ProductMongo>(collectionName);
          
        }

        public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            Console.WriteLine("[ProductReadRepository-Mongo] Obteniendo todos los ProductMongo y mapeando a Product...");
            var productosMongo = await _productosReadCollection.Find(Builders<ProductMongo>.Filter.Empty)
                                                              .ToListAsync(cancellationToken);

            if (productosMongo == null || !productosMongo.Any())
            {
                Console.WriteLine("[ProductReadRepository-Mongo] No se encontraron productos.");
                return new List<Product>();
            }

            // Mapear la lista de ProductMongo a una lista de Product (agregado de dominio)
            var productos = productosMongo.Select(pMongo => new Product( // Usa el constructor de Product
                pMongo.Id,
                pMongo.Nombre,    
                pMongo.PrecioBase,  
                pMongo.Categoria, 
                pMongo.ImagenUrl,
                pMongo.Estado,
                pMongo.Id_Usuario
            )).ToList();

            Console.WriteLine($"[ProductReadRepository-Mongo] Se encontraron y mapearon {productos.Count} productos a agregados Product.");
            return productos;
        }

        public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[ProductReadRepository-Mongo] Obteniendo ProductMongo por ID: {id} y mapeando a Product...");
            var filter = Builders<ProductMongo>.Filter.Eq(p => p.Id, id);
            var productoMongo = await _productosReadCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);

            if (productoMongo == null)
            {
                Console.WriteLine($"[ProductReadRepository-Mongo] Producto con ID: {id} no encontrado.");
                return null;
            }

            // Mapear el ProductMongo encontrado a Product (agregado de dominio)
            var producto = new Product(
                productoMongo.Id,
                productoMongo.Nombre,
                productoMongo.PrecioBase,
                productoMongo.Categoria,
                productoMongo.ImagenUrl,
                productoMongo.Estado,
                productoMongo.Id_Usuario
            );

            Console.WriteLine($"[ProductReadRepository-Mongo] Producto con ID: {id} encontrado y mapeado a agregado Product.");
            return producto;
        }

        public async Task<String> Obtener_Url_Producto(Guid guid_Usuario)
        {

            var filtro = Builders<ProductMongo>.Filter.Eq(p => p.Id, guid_Usuario);

            var producto = await _productosReadCollection.Find(filtro).FirstOrDefaultAsync();

            return producto.ImagenUrl;
        }
    }
}