using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Model;
using VelsatBackendAPI.Model.GestionPasajeros;
using VelsatBackendAPI.Model.MovilProgramacion;

namespace VelsatBackendAPI.Data.Repositories
{
    public interface IPasajerosRepository
    {
        Task<IEnumerable<CodNomPas>> GetCodigo();

        Task<IEnumerable<Pasajero>> GetPasajero(int codcliente);

        Task<IEnumerable<Tarifa>> GetTarifa(string codusuario);

        Task<string> InsertPasajero(Pasajero pasajero, string codusuario);

        Task<string> UpdatePasajero(Pasajero pasajero, string codusuario, int codcliente, string codlan, string codlugar);

        Task<string> DeletePasajero(int codcliente, string codusuario);

        Task<List<Usuario>> GetPasajerosCodigo(string codlan);

        Task<string> InsertDestino(Pasajero pasajero, string codusuario);


    }
}
