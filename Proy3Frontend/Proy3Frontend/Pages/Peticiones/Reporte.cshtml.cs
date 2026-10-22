using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proy3Frontend.Servicios;

namespace Proy3Frontend.Pages.Peticiones
{
    public class ReporteModel : PageModel
    {
        private readonly ApiClient _api;

        public ReporteModel(ApiClient api)
        {
            _api = api;
        }

        [BindProperty]
        public string? Nit { get; set; }

        [BindProperty]
        public string Tipo { get; set; } = "emitidas";

        [BindProperty]
        public string? Desde { get; set; }

        [BindProperty]
        public string? Hasta { get; set; }

        [BindProperty]
        public string? Fecha { get; set; }

        [BindProperty]
        public string Valores { get; set; } = "total";

        public void OnGet()
        {
        }

        /// Genera el PDF
        public async Task<IActionResult> OnPostAsync()
        {
            byte[] pdf = await ReportePdfBuilder.ConstruirAsync(_api, Nit, Tipo, Desde, Hasta, Fecha, Valores);
            return File(pdf, "application/pdf");
        }
    }
}
