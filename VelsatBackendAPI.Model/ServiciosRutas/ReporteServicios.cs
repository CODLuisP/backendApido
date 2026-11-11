using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using VelsatBackendAPI.Model.Turismo;

namespace VelsatBackendAPI.Model.ServiciosRutas
{
    // Reporte de cumplimiento POR SERVICIO: cada servicio con sus paraderos (hora esperada, llegada y
    // salida reales, estancia, diferencia y estado) y los totales del rango consultado.
    // Mismas convenciones del módulo: fechas "dd/MM/yyyy" y horas "HH:mm" (hora de Perú).
    public class ReporteServiciosResponse
    {
        public int ToleranciaMin { get; set; }
        public ReporteResumen Resumen { get; set; } = new();
        public List<ReporteServicioItem> Servicios { get; set; } = new();
    }

    public class ReporteResumen
    {
        public int TotalServicios { get; set; }
        public int TotalParadas { get; set; }
        public int ParadasCompletadas { get; set; }
        public int ATiempo { get; set; }
        public int Adelantadas { get; set; }
        public int Retrasadas { get; set; }
        public int Omitidas { get; set; }

        // % de paradas a tiempo sobre las completadas (0 si no hay ninguna completada).
        public int PorcentajePuntualidad { get; set; }
        public int PermanenciaTotalMin { get; set; }
        public decimal PermanenciaPromedioMin { get; set; }
    }

    public class ReporteRuta
    {
        public long Id { get; set; }
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        public string? Grupo { get; set; }
        public string? Sentido { get; set; }   // "IDA" | "REGRESO"
    }

    public class ReporteConteo
    {
        public int Total { get; set; }
        public int Completadas { get; set; }
        public int ATiempo { get; set; }
        public int Adelantadas { get; set; }
        public int Retrasadas { get; set; }
        public int Omitidas { get; set; }
        public int PermanenciaTotalMin { get; set; }
    }

    public class ReporteServicioItem
    {
        public long Id { get; set; }
        public string Estado { get; set; } = "PROGRAMADO";   // PROGRAMADO | EN_CURSO | FINALIZADO | CANCELADO

        [JsonConverter(typeof(FechaDdMmYyyyJsonConverter))]
        public DateTime? FechaSalida { get; set; }

        [JsonConverter(typeof(HoraHmJsonConverter))]
        public TimeSpan? HoraSalida { get; set; }

        // "HH:mm" (null si el servicio no empezó / no terminó).
        public string? InicioReal { get; set; }
        public string? FinReal { get; set; }

        // Primera a última hora esperada de los paraderos / inicio a fin reales.
        public int? DuracionEstimadaMin { get; set; }
        public int? DuracionRealMin { get; set; }

        public decimal? DistanciaRealKm { get; set; }
        public string? Observaciones { get; set; }

        public string DeviceID { get; set; } = string.Empty;
        public string ConductorNombre { get; set; } = string.Empty;
        public string ConductorBrevete { get; set; } = string.Empty;
        public string ConductorCelular { get; set; } = string.Empty;

        public ReporteRuta Ruta { get; set; } = new();
        public ReporteConteo Conteo { get; set; } = new();
        public List<ReporteParaderoItem> Paraderos { get; set; } = new();
    }

    public class ReporteParaderoItem
    {
        public int Orden { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = "INTERMEDIO";   // ORIGEN | INTERMEDIO | DESTINO
        public int RadioMetros { get; set; }

        public string? HoraEsperada { get; set; }
        public string? LlegadaReal { get; set; }
        public string? SalidaReal { get; set; }

        // Minutos entre la llegada y la salida (null si falta alguna).
        public int? PermanenciaMin { get; set; }

        // Positivo = retraso, negativo = adelanto.
        public int? DiferenciaMin { get; set; }

        // PENDIENTE | A_TIEMPO | ADELANTADO | RETRASADO | OMITIDO
        public string Estado { get; set; } = "PENDIENTE";
    }

    public static class ReporteServiciosCalculos
    {
        // Completa el conteo de cada servicio y devuelve los totales del rango.
        // Un paradero PENDIENTE en un servicio FINALIZADO se considera OMITIDO.
        public static ReporteResumen Calcular(List<ReporteServicioItem> servicios)
        {
            foreach (var s in servicios)
            {
                if (s.Estado == "FINALIZADO")
                {
                    foreach (var p in s.Paraderos.Where(p => p.Estado == "PENDIENTE" && p.LlegadaReal == null))
                        p.Estado = "OMITIDO";
                }

                s.Conteo = new ReporteConteo
                {
                    Total = s.Paraderos.Count,
                    Completadas = s.Paraderos.Count(p => p.LlegadaReal != null),
                    ATiempo = s.Paraderos.Count(p => p.Estado == "A_TIEMPO"),
                    Adelantadas = s.Paraderos.Count(p => p.Estado == "ADELANTADO"),
                    Retrasadas = s.Paraderos.Count(p => p.Estado == "RETRASADO"),
                    Omitidas = s.Paraderos.Count(p => p.Estado == "OMITIDO"),
                    PermanenciaTotalMin = s.Paraderos.Sum(p => p.PermanenciaMin ?? 0)
                };
            }

            int completadas = servicios.Sum(s => s.Conteo.Completadas);
            int aTiempo = servicios.Sum(s => s.Conteo.ATiempo);
            int permanencia = servicios.Sum(s => s.Conteo.PermanenciaTotalMin);

            return new ReporteResumen
            {
                TotalServicios = servicios.Count,
                TotalParadas = servicios.Sum(s => s.Conteo.Total),
                ParadasCompletadas = completadas,
                ATiempo = aTiempo,
                Adelantadas = servicios.Sum(s => s.Conteo.Adelantadas),
                Retrasadas = servicios.Sum(s => s.Conteo.Retrasadas),
                Omitidas = servicios.Sum(s => s.Conteo.Omitidas),
                PorcentajePuntualidad = completadas > 0 ? (int)Math.Round(100.0 * aTiempo / completadas) : 0,
                PermanenciaTotalMin = permanencia,
                PermanenciaPromedioMin = completadas > 0 ? Math.Round((decimal)permanencia / completadas, 1) : 0
            };
        }
    }
}
