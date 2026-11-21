using ParkingApp.Aplicacion.Identidad;

namespace ParkingApp.Pruebas.Identidad;

// Criterios 4 y 6 del issue #23 y criterio 1 del #68: el menú muestra solo las opciones del rol, y Usuarios es solo
// del Administrador.
public class MenuPorRolTests
{
    private static readonly string[] CarrilYCaja = ["/carril", "/caja"];

    private static readonly string[] TodasLasPantallas = ["/carril", "/caja", "/tarifas", "/consultas", "/configuracion", "/usuarios"];

    [Fact]
    public void Operador_ve_en_el_menu_solo_el_carril_y_la_caja()
    {
        var opciones = MenuPorRol.Para([Roles.Operador]);

        Assert.Equal(CarrilYCaja, opciones.Select(opcion => opcion.Ruta));
    }

    [Fact]
    public void Administrador_ve_en_el_menu_todas_las_opciones()
    {
        var opciones = MenuPorRol.Para([Roles.Administrador]);

        Assert.Equal(TodasLasPantallas, opciones.Select(opcion => opcion.Ruta));
    }

    [Fact]
    public void Usuario_con_los_dos_roles_ve_cada_opcion_una_sola_vez()
    {
        var opciones = MenuPorRol.Para([Roles.Operador, Roles.Administrador]);

        Assert.Equal(TodasLasPantallas, opciones.Select(opcion => opcion.Ruta));
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
