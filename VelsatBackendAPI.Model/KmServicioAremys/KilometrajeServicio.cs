using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.KmServicioAremys
{
    public class KilometrajeServicio
    {
        public int Codservicio { get; set; }
        public string Numero {  get; set; }
        public string Tipo { get; set; }
        public string Codconductor { get; set; }
        public string NombreConductor { get; set; }
        public string Unidad {  get; set; }
        public string? Fechaini { get; set; }
        public string? Fechafin { get; set; }
        public string Empresa { get; set; }

        public double? KilometrosRecorridos { get; set; }
    }
}
