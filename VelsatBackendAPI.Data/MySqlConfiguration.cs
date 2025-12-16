using MySql.Data.MySqlClient;
using System;

namespace VelsatBackendAPI.Data
{
    public class MySqlConfiguration
    {
        public MySqlConfiguration(string defaultConnection, string secondConnection, string doConnection)
        {
            // ✅ Validar que no sean nulas o vacías
            if (string.IsNullOrWhiteSpace(defaultConnection))
                throw new ArgumentNullException(nameof(defaultConnection),
                    "La cadena de conexión por defecto no puede estar vacía");

            if (string.IsNullOrWhiteSpace(secondConnection))
                throw new ArgumentNullException(nameof(secondConnection),
                    "La cadena de conexión secundaria no puede estar vacía");

            if (string.IsNullOrWhiteSpace(doConnection))
                throw new ArgumentNullException(nameof(doConnection),
                    "La cadena de conexión DO no puede estar vacía");

            // ✅ NORMALIZAR las connection strings para evitar duplicados en el pool
            DefaultConnection = NormalizeConnectionString(defaultConnection);
            SecondConnection = NormalizeConnectionString(secondConnection);
            DOConnection = NormalizeConnectionString(doConnection);
        }

        public string DefaultConnection { get; set; }
        public string SecondConnection { get; set; }
        public string DOConnection { get; set; }

        /// <summary>
        /// Normaliza una connection string para garantizar formato consistente.
        /// Esto previene que el MySqlPoolManager considere la misma conexión como diferente.
        /// </summary>
        private static string NormalizeConnectionString(string connectionString)
        {
            try
            {
                var builder = new MySqlConnectionStringBuilder(connectionString);
                string normalized = builder.ConnectionString;

                System.Diagnostics.Debug.WriteLine(
                    $"[MySqlConfiguration] Connection string normalizada:\n" +
                    $"  Original: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...\n" +
                    $"  Normalizada: {normalized.Substring(0, Math.Min(50, normalized.Length))}...");

                return normalized;
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException(
                    $"Error al normalizar la cadena de conexión. Verifica que sea válida. Error: {ex.Message}",
                    ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error inesperado al procesar la cadena de conexión: {ex.Message}",
                    ex);
            }
        }
    }
}