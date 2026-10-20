using Proy3Backend.Models;
using Proy3Backend.Utilidades;
using System.Text.Json;

namespace Proy3Backend.Servicios
{
    public class RtuService
    {
        private readonly AlmacenDatos _almacen;

        public RtuService(AlmacenDatos almacen)
        {
            _almacen = almacen;
        }

        /// NIT valido y nuevo -> se crea. NIT valido y existente -> se actualiza el nombre.
        /// NIT invalido -> no tiene ningun efecto sobre el RTU (solo se cuenta).
        public RespuestaRtu Actualizar(JsonElement raiz)
        {
            var respuesta = new RespuestaRtu();

            lock (_almacen.Lock)
            {
                foreach (JsonElement elemento in JsonLector.Elementos(raiz))
                {
                    string? nit = NitValidator.Normalizar(JsonLector.Texto(elemento, "NIT"));
                    string? nombre = JsonLector.Texto(elemento, "nombre")?.Trim();

                    if (nit == null || !NitValidator.EsValido(nit) || string.IsNullOrEmpty(nombre))
                    {
                        respuesta.NitsInvalidos++;
                        continue;
                    }

                    if (_almacen.Rtu.TryGetValue(nit, out Contribuyente? existente))
                    {
                        existente.Nombre = nombre;
                        respuesta.ContribuyentesActualizados++;
                    }
                    else
                    {
                        _almacen.Rtu[nit] = new Contribuyente { Nit = nit, Nombre = nombre };
                        respuesta.ContribuyentesNuevos++;
                    }
                }

                _almacen.GuardarRtu();
            }

            return respuesta;
        }

        public List<Contribuyente> Listar()
        {
            lock (_almacen.Lock)
            {
                return _almacen.Rtu.Values.OrderBy(c => c.Nit, StringComparer.Ordinal).ToList();
            }
        }
    }
}
