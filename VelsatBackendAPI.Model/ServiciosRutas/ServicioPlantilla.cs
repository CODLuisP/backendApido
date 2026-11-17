using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using VelsatBackendAPI.Model.Turismo;

namespace VelsatBackendAPI.Model.ServiciosRutas
{
    // Servicio que se repite: cada día que corresponda, el backend crea un servicio con estos datos.
    // Solo se genera si la plantilla está completa (ver EstaCompleta).
    public class ServicioPlantilla
    {
        public long Id { get; set; }
        public string AccountID { get; set; } = string.Empty;
        public long RutaId { get; set; }

        // Solo lectura: se completan con un JOIN al listar.
        public string? RutaCodigo { get; set; }
        public string? RutaNombre { get; set; }
        public string? RutaGrupo { get; set; }
        public string? RutaSentido { get; set; }

        [JsonConverter(typeof(HoraHmJsonConverter))]
        public TimeSpan? HoraSalida { get; set; }

        public string? DeviceID { get; set; }
        // El conductor se identifica por el brevete (es lo único que se guarda).
        public string? ConductorBrevete { get; set; }

        // Solo lectura: salen de la tabla de conductores según el brevete. Null si el brevete no
        // corresponde a un conductor de rutas activo (ConductorEncontrado = false).
        public string? ConductorNombre { get; set; }
        public string? ConductorCelular { get; set; }

        public bool ConductorEncontrado => !string.IsNullOrWhiteSpace(ConductorNombre);

        // 1 = lunes ... 7 = domingo.
        public List<int> Dias { get; set; } = new();

        public bool Activo { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Texto tal como está guardado en la columna `dias` ("1,2,3"). Solo para leer de la base.
        [JsonIgnore]
        public string? DiasCsv { get; set; }

        // Solo lectura (listado): true si tiene hora, unidad y conductor completos.
        public bool Completa => EstaCompleta;

        [JsonIgnore]
        public bool EstaCompleta =>
            HoraSalida.HasValue
            && !string.IsNullOrWhiteSpace(DeviceID)
            && !string.IsNullOrWhiteSpace(ConductorNombre)
            && !string.IsNullOrWhiteSpace(ConductorBrevete)
            && !string.IsNullOrWhiteSpace(ConductorCelular);

        public static List<int> ParsearDias(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return new List<int>();
            return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => int.TryParse(x, out var n) ? n : 0)
                .Where(n => n >= 1 && n <= 7)
                .Distinct().OrderBy(n => n).ToList();
        }

        public static string DiasACsv(IEnumerable<int> dias) =>
            string.Join(",", dias.Where(n => n >= 1 && n <= 7).Distinct().OrderBy(n => n));
    }

    public class ConfigPlantillasRequest
    {
        // Hora (Perú) desde la cual se crean los servicios del día siguiente, formato "HH:mm".
        public string? HoraGeneracion { get; set; }
    }
}
