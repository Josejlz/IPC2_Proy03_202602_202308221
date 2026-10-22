using Proy3Frontend.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Proy3Frontend.Pages
{

    public class InicializarModel : PageModel
    {
        private readonly ApiClient _api;

        public InicializarModel(ApiClient api)
        {
            _api = api;
        }

        public string? Mensaje { get; private set; }
        public string? Error { get; private set; }

        public void OnGet()
        {
        }

        public async Task OnPostAsync()
        {
            RespuestaApi<string> respuesta = await _api.PostAsync("/Inicializar");

            if (respuesta.Ok) Mensaje = "El sistema volvió al estado inicial: no hay datos almacenados.";
            else Error = respuesta.Error;
        }
    }
}
