using DotNetEnv;

using MudBlazor.Services;

using ParkingApp.Aplicacion;
using ParkingApp.Infraestructura.Persistencia;
using ParkingApp.Infraestructura.ServiciosExternos;
using ParkingApp.Web.Components;

// Carga el .env de la raíz del repositorio. Si una variable ya existe en el entorno (por ejemplo, en Docker), se respeta.
Env.NoClobber().TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AgregarAplicacion();
builder.Services.AgregarBaseDeDatos(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AgregarServiciosExternos(); // <--- Aquí registramos los servicios externos de forma limpia

var app = builder.Build();

// Sin base de datos la aplicación no puede funcionar: si no hay conexión, se detiene y explica por qué.
if (!await app.Services.VerificarConexionAsync(app.Logger))
{
    Environment.ExitCode = 1;
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Swagger queda disponible en todos los entornos: el MVP corre en local y la API se prueba desde ahí.
app.UseSwagger();
app.UseSwaggerUI();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();