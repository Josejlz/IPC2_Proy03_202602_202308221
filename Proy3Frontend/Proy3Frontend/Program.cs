using Proy3Frontend.Servicios;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpClient<ApiClient>(cliente =>
{
    string url = builder.Configuration["ApiUrl"] ?? "http://localhost:5000";
    cliente.BaseAddress = new Uri(url);
    cliente.Timeout = TimeSpan.FromMinutes(5);
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();
