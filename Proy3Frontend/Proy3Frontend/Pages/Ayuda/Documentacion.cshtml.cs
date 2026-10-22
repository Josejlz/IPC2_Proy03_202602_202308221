using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Proy3Frontend.Pages.Ayuda
{
    public class DocumentacionModel : PageModel
    {

        public const string RutaPublica = "/docs/documentacion.pdf";

        private readonly IWebHostEnvironment _entorno;

        public DocumentacionModel(IWebHostEnvironment entorno)
        {
            _entorno = entorno;
        }

        public bool Existe { get; private set; }

        public void OnGet()
        {
            Existe = _entorno.WebRootFileProvider.GetFileInfo("/docs/documentacion.pdf").Exists;
        }
    }
}
