using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using Producto.Domain.Aggregates;

namespace Producto.Infrastructure.Persistance.DataBase
{
    public class MongoInitializer
    {
        private readonly IMongoClient _mongoClient;

        public MongoInitializer(IMongoClient mongoClient)
        {
            _mongoClient = mongoClient;
        }

        public void Initialize()
        {
            var database = _mongoClient.GetDatabase("productos_db");
            var collection = database.GetCollection<ProductMongo>("productos");

        }
    }
}