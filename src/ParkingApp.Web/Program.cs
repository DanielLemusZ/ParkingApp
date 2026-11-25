using DotNetEnv;

using Microsoft.AspNetCore.Components.Authorization;

using MudBlazor.Services;

using ParkingApp.Aplicacion;
using ParkingApp.Infraestructura.Identidad;
using ParkingApp.Infraestructura.Persistencia;
using ParkingApp.Infraestructura.ServiciosExternos;
using ParkingApp.Web.Components;
using ParkingApp.Web.Cuenta;
using ParkingApp.Web.Tareas;

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
builder.Services.AgregarServiciosExternos(builder.Configuration);
builder.Services.AgregarIdentidad();

// Cobro del día nuevo: corre al arrancar y cada día a las 06:00 de la sede.
builder.Services.AddHostedService<TareaCobrosDelDiaNuevo>();

// Pone el usuario que inició sesión al alcance de todas las pantallas (<AuthorizeView>, [Authorize]).
builder.Services.AddCascadingAuthenticationState();

// Las pantallas interactivas revisan cada minuto que la sesión siga valiendo, por si el administrador desactivó al usuario.
builder.Services.AddScoped<AuthenticationStateProvider, SesionRevalidada>();

var app = builder.Build();

// Sin base de datos la aplicación no puede funcionar: si no hay conexión, se detiene y explica por qué.
if (!await app.Services.VerificarConexionAsync(app.Logger))
{
    Environment.ExitCode = 1;
    return;
}

// Usuarios iniciales de cada rol, con las credenciales de .env. Si ya existen, no se tocan.
await app.Services.CrearUsuariosInicialesAsync(app.Configuration, app.Logger);

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

// Primero se averigua quién es el usuario (lee la cookie) y después si puede entrar (revisa [Authorize]).
// El antiforgery va después, porque su token depende del usuario.
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapearEndpointsDeCuenta();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
