using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Producto.Domain.Excepciones;

namespace Producto.Domain.VO
{
    public class Id_Usuario_VO
    {
        public string Valor { get; }
        public Id_Usuario_VO(string valor){
 
            Valor = valor;
        }
        public static implicit operator String(Id_Usuario_VO idUsuario) => idUsuario.Valor;
        public static implicit operator Id_Usuario_VO(String valor) => new Id_Usuario_VO(valor);
    }
}

