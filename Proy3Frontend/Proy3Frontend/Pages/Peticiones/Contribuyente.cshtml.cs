using Proy3Frontend.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proy3Frontend.Models;

namespace Proy3Frontend.Pages.Peticiones
{
    public class ContribuyenteModel : PageModel
    {

        private readonly ApiClient _api;
        public ContribuyenteModel(ApiClient api)
        {
            _api = api;
        }

        [BindProperty(SupportsGet = true)]
        public string? Nit { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Tipo { get; set; } = "emitidas";

        [BindProperty(SupportsGet = true)]
        public string? Desde { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Hasta { get; set; }

        public ConsultaDatosResultado? Resultado { get; private set; }
        public string? Error { get; private set; }

        public async Task OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(Nit) && string.IsNullOrWhiteSpace(Desde) && string.IsNullOrWhiteSpace(Hasta)) return;

            RespuestaApi<ConsultaDatosResultado> r = await _api.GetAsync<ConsultaDatosResultado>(
            "/ConsultaDatos", ("nit", Nit), ("tipo", Tipo), ("desde", Desde), ("hasta", Hasta));

            if (r.Ok) 
            {
                Resultado = r.Valor; 
            }
            else 
            { 
                Error = r.Error; 
            }

        }
    }
}
