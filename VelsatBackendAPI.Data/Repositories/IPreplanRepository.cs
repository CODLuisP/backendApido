using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VelsatBackendAPI.Model.GestionPasajeros;
using VelsatBackendAPI.Model.Latam;
using VelsatBackendAPI.Model.MovilProgramacion;

namespace VelsatBackendAPI.Data.Repositories
{
    public interface IPreplanRepository
    {
        Task<List<Pedido>> GetPedidos(string dato, string empresa, string usuario);

        Task<InsertPedidoResponse> InsertPedido(IEnumerable<ExcelAvianca> excel, string fecact, string tipo, string usuario);

        Task<int> SavePedidos(IEnumerable<Pedido> pedidos, string usuario);

        Task<int> BorrarPlan(string empresa, string fecha, string usuario);

        Task<List<LugarCliente>> GetLugares(string coddcliente);

        Task<int> UpdateDirec(string coddire, string codigo);

        Task<List<Usuario>> GetConductores(string usuario);

        Task<List<Unidad>> GetUnidades(string usuario);

        Task<List<Servicio>> CreateServicios(string fecha, string empresa, string usuario);

        Task<List<Servicio>> GetServicios(string fecha, string usuario);

        Task<List<Usuario>> GetPasajeros(string palabra, string codusuario);
        Task<List<Usuario>> GetPasajerosEmpresa(string palabra, string codusuario, string empresa);


        Task<List<Servicio>> GetServicioPasajero(string usuario, string fec, string codcliente);

        Task<string> AsignacionServicio(List<Servicio> listaServicios); //Aregar List<Pedido> listaSubservicios

        Task<int> EliminacionMultiple(List<Servicio> listaServicios);

        Task<List<Usuario>> GetConductorDetalle(string palabra);

        Task<List<Pedido>> ListaPasajeroServicio(string codservicio);

        Task<int> UpdateControlServicio(Servicio servicio);

        Task<int> CancelarAsignacion(string codservicio);

        Task<int> CancelarServicio(Servicio servicio);

        Task<string> NewServicio(Servicio servicio, string usuario);

        Task<int> UpdateEstadoServicio(Pedido pedido);

        Task<List<Pedido>> ReporteDiferencia(string fecini, string fecfin, string aerolinea, string usuario, string tipo);

        Task<List<Pedido>> ReporteFormatoAvianca(string fecini, string fecfin, string aerolinea, string usuario);

        Task<List<Pedido>> ReporteFormatoAremys(string fecini, string fecfin, string aerolinea);

        Task<int> RegistrarPasajeroGrupo(Pedido pedido, string usuario);

        Task<int> UpdateHorasServicio(string codservicio, string fecha, string fecplan);

        Task<int> UpdateDestinoServicio(string codservicio, string newcoddestino, string newcodubicli);

        Task<List<Usuario>> GetDestinos(string palabra);

        Task<int> EliminarGrupoCero(string usuario);

        Task<List<Conductor>> GetConductoresxUsuario(string usuario);

        Task<List<Carro>> GetUnidadesxUsuario(string usuario);

        Task<int> GuardarConductorAsync(Conductor conductor, string usuario);

        Task<int> ModificarConductorAsync(Conductor conductor);

        Task<int> HabilitarConductorAsync(int codigoConductor);

        Task<int> DeshabilitarConductorAsync(int codigoConductor);

        Task<int> LiberarConductorAsync(int codigoConductor);

        Task<int> EliminarConductorAsync(int codigoConductor);

        Task<int> HabilitarUnidadAsync(string placa);

        Task<int> DeshabilitarUnidadAsync(string placa);

        Task<int> LiberarUnidadAsync(string placa);

        Task<int> UpdDirPasServicio(int codpedido, string codubicli);

        Task<int> NuevoLugarCliente(LugarCliente lugarCliente);

        Task<int> EliminarLugarCliente(int codlugar);

        Task<List<ServicioLatam>> InsertPedidoLatam(List<List<RegistroExcelLatam>> gruposRegistros, string usuario);
    }
}
