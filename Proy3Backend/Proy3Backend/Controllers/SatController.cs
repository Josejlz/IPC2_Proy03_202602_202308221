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

        // Escritura (Post)

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

        //Lectura (Get)

        [HttpGet("/Rtu")]
        public IActionResult Rtu()
        {
            return Ok(_rtu.Listar());
        }

        [HttpGet("/ConsultaDatos")]
        [HttpGet("/ConsultarDatos")]
        public IActionResult ConsultaDatos(
            [FromQuery] string? nit,
            [FromQuery] string? tipo,
            [FromQuery] string? desde,
            [FromQuery] string? hasta)
        {
            string? nitNormalizado = NitValidator.Normalizar(nit);
            if (nitNormalizado == null)
            {
                return Falla("El parametro 'nit' es obligatorio.");
            }

            string tipoNormalizado = (tipo ?? "").Trim().ToLowerInvariant();
            bool emitidas;

            if (tipoNormalizado.StartsWith("emit"))
            {
                emitidas = true;
            } else if (tipoNormalizado.StartsWith("recib"))
            {
                emitidas = false;
            } else
            {
                return Falla("El parametro 'tipo' debe ser 'emitidas' o 'recibidas'.");
            }

            string? error = LeerRango(desde, hasta, out DateTime fechaDesde, out DateTime fechaHasta);

            if (error!=null)
            {
                return Falla(error);
            }

            return Ok(_consultas.ConsultaDatos(nitNormalizado, emitidas, fechaDesde, fechaHasta));

        }

        [HttpGet("/ResumenIva")]
        public IActionResult ResumenIva([FromQuery] string? fecha)
        {
            if (!ParserFecha.TryParseConsulta(fecha, out DateTime f))
            {
                return Falla("El parametro 'fecha' es obligatorio.");
            }

            return Ok(_consultas.ResumenIva(f));
        }

        [HttpGet("/ResumenRango")]
        public IActionResult ResumenRango(
            [FromQuery] string? desde,
            [FromQuery] string? hasta,
            [FromQuery] string? valores)
        {
            string? error = LeerRango(desde, hasta, out DateTime fechaDesde,out DateTime fechaHasta);
            if (error!=null)
            {
                return Falla(error);
            }

            return Ok(_consultas.ResumenRango(fechaDesde,fechaHasta, EsSinIva(valores)));
        }

        [HttpGet("/Grafica")]
        public IActionResult Grafica(
            [FromQuery] string? resumen,
            [FromQuery] string? fecha,
            [FromQuery] string? desde,
            [FromQuery] string? hasta,
            [FromQuery] string? valores)
        {
            string tipo = (resumen ?? "").Trim().ToLowerInvariant();

            if (tipo == "iva")
            {
                if(!ParserFecha.TryParseConsulta(fecha, out DateTime f)){
                    return Falla("Para resumen = iva, el parametro 'fecha' es obligatorio.");
                }
                return Ok(_consultas.GraficaIva(f));
            }
            if (tipo == "rango")
            {
                string? error = LeerRango(desde, hasta, out DateTime fechaDesde, out DateTime fechaHasta);
                if (error != null)
                {
                    return Falla(error);
                }
                return Ok(_consultas.GraficaRango(fechaDesde, fechaHasta, EsSinIva(valores)));
            }

            return Falla("El parametro 'resumen' debe ser 'iva' o 'rango'.");

        }

        [HttpGet("/Estadisticas")]
        public IActionResult Estadisticas([FromQuery] string? desde,[FromQuery] string? hasta)
        {
            string? error = LeerRango(desde, hasta, out DateTime fechaDesde, out DateTime fechaHasta);

            if (error!=null)
            {
                return Falla(error);
            }

            return Ok(_consultas.Estadisticas(fechaDesde, fechaHasta));

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

            if (!ParserFecha.TryParseConsulta(hasta, out fechaHasta))
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
