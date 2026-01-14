using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Model.Latam
{
    public class RegistroExcelLatam
    {
        public string Idunico { get; set; }
        public string Fecha { get; set; } //En formato 13/01/2026
        public string Accion { get; set; } //Zarpe = S ^ Recogida = I
        public string Proveedor { get; set; } //Siempre enviar LATAM
        public string Hora_llegada { get; set; }
        public string Hora_parada { get; set; }
        public string Bp { get; set; }
        public string Nombre { get; set; }
        public string Celular { get; set; }
        public string Direccion { get; set; }
        public string Comuna { get; set; }
        public string Lat { get; set; }
        public string Long { get; set; } //Enviar con punto, NO coma
        public string Depot { get; set; } //Enviar con punto, NO coma
        public string Numero_vuelo { get; set; }
        public string Pasajeros { get; set; }
        public string Orden { get; set; }
    }
}
