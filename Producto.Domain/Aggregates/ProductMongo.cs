using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Producto.Domain.VO;

namespace Producto.Domain.Aggregates
{
    public class ProductMongo
    {
        [BsonId] // Indica que es el identificador del documento
        [BsonRepresentation(BsonType.String)] // Serializa como string (formato UUID estándar)
        public Guid Id { get; set; }
        public NombreProductoVO? Nombre { get; set; }
        public PrecioBaseVO? PrecioBase { get; set; }
        public CategoriaVO? Categoria { get; set; }
        public ImagenUrlVo? ImagenUrl { get; set; }
        public EstadoVO Estado { get;  set; }
        public Id_Usuario_VO Id_Usuario { get; set; }


    }

    /* ? */
}
