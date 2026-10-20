using Microsoft.AspNetCore.Mvc;
using Proy3Backend.Servicios;
using Proy3Backend.Utilidades;
using System.Text.Json;

namespace Proy3Backend.Controllers
{
    [ApiController]
    public class SatController : ControllerBase
    {
        private readonly AlmacenDatos _almacen;
        private readonly RtuService _rtu;
        private readonly AutorizacionService _autorizaciones;
        private readonly ConsultaService _consultas;

        public SatController(AlmacenDatos almacen, RtuService rtu, AutorizacionService autorizaciones, ConsultaService consultas)
        {
            _almacen = almacen;
            _rtu = rtu;
            _autorizaciones = autorizaciones;
            _consultas = consultas;
        }

        // Escritura

        [HttpPost("/Procesar")]
        public async Task<IActionResult> Procesar()
        {
            using JsonDocument? documento = await LeerJsonAsync();
            
            if (documento == null)
            {
                return Falla("El cuerpo de la peticion no es Json Valido.");
            }
            JsonElement raiz = documento.RootElement;

            foreach (JsonElement elemento in JsonLector.Elementos(raiz))
            {
                if (JsonLector.Tiene(elemento, "REFERENCIA") || (JsonLector.Tiene(elemento, "NIT_EMISOR")))
                {
                    return Ok(_autorizaciones.Procesar(raiz));
                }
                if (JsonLector.Tiene(elemento, "NIT"))
                {
                    return Ok(_rtu.Actualizar(raiz));
                }
                break;
            }

            return Falla("No se reconocio el mensaje: Se esperaba lista de contribuyentes o solicitudes de autorizacion.");
        }

        [HttpPost("/ActualizarRtu")]
        public async Task<IActionResult> ActualizarRtu()
        {
            using JsonDocument? documento = await LeerJsonAsync();

            if (documento == null)
            {
                return Falla("El cuerpo de la peticion no es un JsonValido.");
            }

            return Ok(_rtu.Actualizar(documento.RootElement));
        
        }

        [HttpPost("/Autorizaciones")]
        public async Task<IActionResult> Autorizaciones()
        {
            using JsonDocument? documento = await LeerJsonAsync();

            if (documento == null)
            {
                return Falla("El cuerpo de la peticion no es un Json valido.");
            }

            return Ok(_autorizaciones.Procesar(documento.RootElement));

        }

        [HttpPost("/Inicializar")]
        public IActionResult Inicializar()
        {
            _almacen.Inicializar();
            return Ok(new { mensaje  = "Sistema inicializado: no hay datos almacenados."});
        }

        //Utilitarios -------------------------------
        private async Task<JsonDocument?> LeerJsonAsync()
        {
            try
            {
                return await JsonDocument.ParseAsync(Request.Body);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        //null si rango valido, excepcion si no
        private static string? LeerRango(string? desde, string ? hasta, out DateTime fechaDesde, out DateTime fechaHasta)
        {
            fechaHasta = default;
            if (!ParserFecha.TryParseConsulta(desde, out fechaDesde))
            {
                return "El parametro 'desde' es obligatorio.";
            }

            if (!ParserFecha.TryParseConsulta(hasta, out fechaDesde))
            {
                return "El parametro 'hasta' es obligatorio.";
            }

            if (fechaDesde > fechaHasta)
            {
                return "'desde' no puede ser mayor que 'hasta'";
            }
            return null;
        }

        private static bool EsSinIva(string? valores)
        {
            string v = (valores ?? "").Trim().ToLowerInvariant();

            return ((v=="siniva")||(v=="sin_iva")||(v=="sin"));
        }

        private IActionResult Falla(string mensaje)
        {
            return BadRequest(new { error = mensaje });
        }

        
        
    }
}
