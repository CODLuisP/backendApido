using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VelsatBackendAPI.Model.Turismo
{
    // Serializa/deserializa TimeSpan? como texto "HH:mm" en el JSON del API.
    public class HoraHmJsonConverter : JsonConverter<TimeSpan?>
    {
        private static readonly string[] FormatosAceptados = { @"hh\:mm", @"hh\:mm\:ss" };
        private const string FormatoSalida = @"hh\:mm";

        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            string? valor = reader.GetString();

            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            if (TimeSpan.TryParseExact(valor, FormatosAceptados, CultureInfo.InvariantCulture, out TimeSpan hora))
            {
                return hora;
            }

            throw new JsonException($"Formato de hora inválido: '{valor}'. Debe ser HH:mm.");
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteStringValue(value.Value.ToString(FormatoSalida, CultureInfo.InvariantCulture));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
