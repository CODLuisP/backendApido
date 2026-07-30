using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VelsatBackendAPI.Model.Turismo
{
    // Serializa/deserializa DateTime? como texto "dd/MM/yyyy" en el JSON del API.
    public class FechaDdMmYyyyJsonConverter : JsonConverter<DateTime?>
    {
        private const string Formato = "dd/MM/yyyy";

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

            if (DateTime.TryParseExact(valor, Formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fecha))
            {
                return fecha;
            }

            throw new JsonException($"Formato de fecha inválido: '{valor}'. Debe ser {Formato}.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteStringValue(value.Value.ToString(Formato, CultureInfo.InvariantCulture));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
