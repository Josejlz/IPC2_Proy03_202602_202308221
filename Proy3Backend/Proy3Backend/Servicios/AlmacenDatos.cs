using Proy3Backend.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Proy3Backend.Servicios
{
    public class AlmacenDatos
    {

        private readonly string _carpeta;

        private readonly JsonSerializerOptions _opciones = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public object Lock { get; } = new();

        public Dictionary<string, Contribuyente> Rtu { get; private set; } = new();

        public List<Factura> Facturas { get; private set; } = new();

        public HashSet<string> ReferenciasAprobadas { get; private set; } = new();

        public CorrelativoDiario Correlativo { get; private set; } = new();

        public AlmacenDatos(IWebHostEnvironment entorno)
        {
            _carpeta = Path.Combine(entorno.ContentRootPath, "Data");
            Directory.CreateDirectory(_carpeta);
            Cargar();
        }

        private string Ruta(string archivo) => Path.Combine(_carpeta, archivo);

        private T Leer<T>(string archivo, T porDefecto)
        {
            string ruta = Ruta(archivo);
            if (!File.Exists(ruta)) return porDefecto;

            try
            {
                T? valor = JsonSerializer.Deserialize<T>(File.ReadAllText(ruta), _opciones);
                return valor == null ? porDefecto : valor;
            }
            catch (JsonException)
            {
                // por si el archivo no estaba bien
                return porDefecto;
            }

        }

        private void Escribir<T>(string archivo, T contenido)
        {
            File.WriteAllText(Ruta(archivo), JsonSerializer.Serialize(contenido, _opciones));
        }

        private void Cargar()
        {
            lock (Lock)
            {
                Rtu = new Dictionary<string, Contribuyente>();
                foreach (Contribuyente c in Leer(archivo: "rtu.json", porDefecto: new List<Contribuyente>()))
                {
                    Rtu[c.Nit] = c;
                }

                Facturas = Leer(archivo: "facturas.json", porDefecto: new List<Factura>());

                ReferenciasAprobadas = new HashSet<string>();
                foreach (Factura f in Facturas)
                {
                    if (f.Aprobada) ReferenciasAprobadas.Add(f.Referencia);
                }

                Correlativo = Leer(archivo: "correlativo.json", porDefecto: new CorrelativoDiario());
            }
        }

        public void GuardarRtu()
        {
            Escribir("rtu.json", Rtu.Values.OrderBy(c => c.Nit, StringComparer.Ordinal).ToList());
        }

        public void GuardarFacturas() => Escribir("facturas.json", Facturas);

        public void GuardarCorrelativo() => Escribir("correlativo.json", Correlativo);

        public void Inicializar()
        {
            lock (Lock)
            {
                Rtu = new Dictionary<string, Contribuyente>();
                Facturas = new List<Factura>();
                ReferenciasAprobadas = new HashSet<string>();
                Correlativo = new CorrelativoDiario();

                foreach (string archivo in new[] { "rtu.json", "facturas.json", "correlativo.json" })
                {
                    string ruta = Ruta(archivo);
                    if (File.Exists(ruta)) File.Delete(ruta);
                }
            }
        }

    }
}
