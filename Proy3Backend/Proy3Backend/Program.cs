using Proy3Backend.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(builder.Configuration["urls"]?? "http://localhost:5000");
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSingleton<AlmacenDatos>();
builder.Services.AddSingleton<RtuService>();
builder.Services.AddSingleton<AutorizacionService>();
builder.Services.AddSingleton<ConsultaService>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.MapControllers();

app.Run();
