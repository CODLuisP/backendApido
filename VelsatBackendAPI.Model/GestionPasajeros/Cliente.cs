using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.GestionPasajeros
{
    public class Cliente
    {
        public int? Codcliente {  get; set; }
        public string Codlugar { get; set; }
        public string Apellidos {  get; set; }
        public string? Login { get; set; }
        public string? Clave { get; set; }
        public char Estadocuenta { get; set; }
        public string? Codusuario {  get; set; }
        public string? Nombres { get; set; }
        public string Codlan { get; set; }
        public char? Sexo { get; set; }
        public string Empresa { get; set; }
        public string? Area { get; set; }
        public string? Tipo { get; set; }
        public string? Telefono { get; set; }
        public string? Dni { get; set; }
        public string? Correo { get; set; }
        public string? Codelimina { get; set; }
        public string? Fechaelim { get; set; }
        public char? Destino { get; set; }
        public string? Cargo { get; set; }
        public string? Cuenta { get; set; }
    }
}
