using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Proy3Frontend.Pages.Ayuda
{
    public class DocumentacionModel : PageModel
    {
        private const string RutaBase = "/docs/documentacion.pdf";
        private readonly IWebHostEnvironment _entorno;

        public DocumentacionModel(IWebHostEnvironment entorno)
        {
            _entorno = entorno;
        }

        public bool Existe { get; private set; }
        public string RutaPublica { get; private set; } = string.Empty;

        public void OnGet()
        {
            var fileInfo = _entorno.WebRootFileProvider.GetFileInfo(RutaBase);
            Existe = fileInfo.Exists;

            if (Existe)
            {
                
                long version = fileInfo.LastModified.Ticks;
                RutaPublica = $"{RutaBase}?v={version}";
            }
        }
    }
}
