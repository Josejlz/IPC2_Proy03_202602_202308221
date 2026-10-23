using Proy3Frontend.Models;
using Proy3Frontend.Servicios;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Proy3Frontend.Pages.Peticiones
{
    public class ResumenRangoModel : PageModel
    {
        private readonly ApiClient _api;
        public ResumenRangoModel(ApiClient api)
        {
            _api = api;
        }

        [BindProperty(SupportsGet = true)]
        public string? Desde { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Hasta { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Valores { get; set; } = "total";

        public ResumenRangoResultado? Resultado { get; private set; }
        public string? Error { get; private set; }

        public async Task OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(Desde) && string.IsNullOrWhiteSpace(Hasta)) return;

            RespuestaApi<ResumenRangoResultado> r = await _api.GetAsync<ResumenRangoResultado>(
                "/ResumenRango", ("desde", Desde), ("hasta", Hasta), ("valores", Valores));

            if (r.Ok) Resultado = r.Valor;
            else Error = r.Error;
        }

    }
}
