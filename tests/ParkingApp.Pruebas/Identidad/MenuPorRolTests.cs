using ParkingApp.Aplicacion.Identidad;

namespace ParkingApp.Pruebas.Identidad;

// Criterios 4 y 6 del issue #23, criterio 1 del #68 y criterio 2 del #71: el menú muestra solo las pestañas del rol.
// Con los mockups (#71), Tarifas y Usuarios pasan al submenú de Configuración, y Consultas se llama Historial.
// Cupos es solo de consulta: la ven los dos roles.
public class MenuPorRolTests
{
    private static readonly string[] PestanasDelOperador = ["/carril", "/caja", "/cupos"];

    private static readonly string[] TodasLasPestanas = ["/carril", "/caja", "/cupos", "/historial", "/configuracion"];

    [Fact]
    public void Operador_ve_solo_monitoreo_de_acceso_caja_y_cupos()
    {
        var opciones = MenuPorRol.Para([Roles.Operador]);

        Assert.Equal(PestanasDelOperador, opciones.Select(opcion => opcion.Ruta));
    }

    [Fact]
    public void Administrador_ve_ademas_las_pestanas_de_historial_y_configuracion()
    {
        var opciones = MenuPorRol.Para([Roles.Administrador]);

        Assert.Equal(TodasLasPestanas, opciones.Select(opcion => opcion.Ruta));
    }

    [Fact]
    public void Usuario_con_los_dos_roles_ve_cada_opcion_una_sola_vez()
    {
        var opciones = MenuPorRol.Para([Roles.Operador, Roles.Administrador]);

        Assert.Equal(TodasLasPestanas, opciones.Select(opcion => opcion.Ruta));
    }

    [Fact]
    public void Pestana_del_carril_se_llama_monitoreo_de_acceso()
    {
        var carril = Assert.Single(MenuPorRol.Opciones, opcion => opcion.Ruta == "/carril");

        Assert.Equal("Monitoreo de acceso", carril.Texto);
    }

    [Theory]
    [InlineData]
    [InlineData("Invitado")]
    [InlineData("operador")]
    public void Usuario_sin_un_rol_conocido_no_ve_opciones_del_menu(params string[] roles)
    {
        // Los roles distinguen mayúsculas, igual que [Authorize(Roles = ...)].
        Assert.Empty(MenuPorRol.Para(roles));
    }
}
