using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proy3Frontend.Models;
using Proy3Frontend.Servicios;

namespace Proy3Frontend.Pages.Peticiones
{
    public class EstadisticasModel : PageModel
    {
        private readonly ApiClient _api;

        public EstadisticasModel(ApiClient api)
        {
            _api = api;
        }

        [BindProperty(SupportsGet = true)]
        public string? Desde { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Hasta { get; set; }

        public EstadisticasResultado? Resultado { get; private set; }
        public string? Error { get; private set; }

        public async Task OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(Desde) && string.IsNullOrWhiteSpace(Hasta)) return;

            RespuestaApi<EstadisticasResultado> r = await _api.GetAsync<EstadisticasResultado>(
                "/Estadisticas", ("desde", Desde), ("hasta", Hasta));

            if (r.Ok) Resultado = r.Valor;
            else Error = r.Error;
        }
    }
}
