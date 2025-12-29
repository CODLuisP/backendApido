using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.Talma
{
    public class RegistroExcel
    {
        public int Id { get; set; }
        public string Codlan { get; set; }
        public char Tipo { get; set; }
        public string Fecha { get; set; }
        public string Hora { get; set; }
        public string Usuario { get; set; }
    }
}
