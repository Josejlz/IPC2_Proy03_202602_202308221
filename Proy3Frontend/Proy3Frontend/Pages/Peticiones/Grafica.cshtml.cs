using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proy3Frontend.Models;
using Proy3Frontend.Servicios;
using System.Text.Json;

namespace Proy3Frontend.Pages.Peticiones
{
    public class GraficaModel : PageModel
    {

        private static readonly JsonSerializerOptions Opciones = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        private readonly ApiClient _api;

        public GraficaModel(ApiClient api)
        {
            _api = api;
        }

        // para grafica de IVA emitido y recibido
        [BindProperty(SupportsGet = true)]
        public string? Fecha { get; set; }

        // Para grafica autorizaciones
        [BindProperty(SupportsGet = true)]
        public string? Desde { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Hasta { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Valores { get; set; } = "total";

        public string? TituloIva { get; private set; }
        public string? JsonIva { get; private set; }
        public string? ErrorIva { get; private set; }

        public string? TituloRango { get; private set; }
        public string? JsonRango { get; private set; }
        public string? ErrorRango { get; private set; }

        public async Task OnGetAsync()
        {
            if (!string.IsNullOrWhiteSpace(Fecha))
            {
                RespuestaApi<DatosGrafica> r = await _api.GetAsync<DatosGrafica>(
                    "/Grafica", ("resumen", "iva"), ("fecha", Fecha));

                if (r.Ok)
                {
                    TituloIva = r.Valor!.Titulo;
                    JsonIva = JsonSerializer.Serialize(r.Valor!, Opciones);
                }
                else
                {
                    ErrorIva = r.Error;
                }
            }

            if (!string.IsNullOrWhiteSpace(Desde) && !string.IsNullOrWhiteSpace(Hasta))
            {
                RespuestaApi<DatosGrafica> r = await _api.GetAsync<DatosGrafica>(
                    "/Grafica", ("resumen", "rango"), ("desde", Desde), ("hasta", Hasta), ("valores", Valores));

                if (r.Ok)
                {
                    TituloRango = r.Valor!.Titulo;
                    JsonRango = JsonSerializer.Serialize(r.Valor!, Opciones);
                }
                else
                {
                    ErrorRango = r.Error;
                }
            }
        }
    }
}
