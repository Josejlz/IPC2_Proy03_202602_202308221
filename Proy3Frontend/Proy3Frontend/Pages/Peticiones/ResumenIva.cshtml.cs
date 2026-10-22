using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proy3Frontend.Models;
using Proy3Frontend.Servicios;

namespace Proy3Frontend.Pages.Peticiones
{
    public class ResumenIvaModel : PageModel
    {
        private readonly ApiClient _api;

        public ResumenIvaModel(ApiClient api)
        {
            _api = api;
        }

        [BindProperty(SupportsGet = true)]
        public string? Fecha { get; set; }

        public ResumenIvaResultado? Resultado { get; private set; }
        public string? Error { get; private set; }

        public async Task OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(Fecha)) return;

            RespuestaApi<ResumenIvaResultado> r = await _api.GetAsync<ResumenIvaResultado>("/ResumenIva", ("fecha", Fecha));

            if (r.Ok) Resultado = r.Valor;
            else Error = r.Error;
        }
    }
}
