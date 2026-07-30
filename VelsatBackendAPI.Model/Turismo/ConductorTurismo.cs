using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.Turismo
{
    // Representa la tabla "taxi" (conductores) completa.
    public class ConductorTurismo
    {
        public int Codtaxi { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? Imagen { get; set; }
        public string? Login { get; set; }
        public string? Clave { get; set; }
        public string? Sexo { get; set; }
        public string? Estado { get; set; }
        public string? Servicioactual { get; set; }
        public string? Codusuario { get; set; }
        public string? Telefono { get; set; }
        public string? Dni { get; set; }
        public string? Brevete { get; set; }
        public string? Turismo { get; set; }
        public string? Turno { get; set; }
        public string? Horainicio { get; set; }
        public string? Tipo { get; set; }
        public string? Email { get; set; }
        public string? Sctr { get; set; }
        public string? Planilla { get; set; }
        public string? Catbrevete { get; set; }
        public string? Estbrevete { get; set; }
        public string? Fecvalidbrevete { get; set; }
        public string? Fecsegvial { get; set; }
        public string? Fecgtu { get; set; }
        public string? Puntos { get; set; }
        public string? Habilitado { get; set; }
        public string? Calificacion { get; set; }
        public string? Unidadasig { get; set; }
    }
}
