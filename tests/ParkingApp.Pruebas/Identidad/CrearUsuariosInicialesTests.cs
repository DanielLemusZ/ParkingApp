using ParkingApp.Aplicacion.Identidad;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Identidad;

// Criterios 1 y 8 del issue #23: los usuarios iniciales se crean al arrancar con las credenciales de .env.
public class CrearUsuariosInicialesTests
{
    private readonly DirectorioDeUsuariosEnMemoria directorio = new DirectorioDeUsuariosEnMemoria();

    [Fact]
    public async Task Crea_los_usuarios_iniciales_que_no_existen_con_su_rol()
    {
        var resultados = await CrearAsync(
            new UsuarioInicial("admin@parkingapp.local", "Clave-Segura-1", Roles.Administrador),
            new UsuarioInicial("operador@parkingapp.local", "Clave-Segura-2", Roles.Operador));

        Assert.All(resultados, resultado => Assert.Equal(EstadoDeUsuarioInicial.Creado, resultado.Estado));
        Assert.Equal(Roles.Administrador, directorio.RolesPorCorreo["admin@parkingapp.local"]);
        Assert.Equal(Roles.Operador, directorio.RolesPorCorreo["operador@parkingapp.local"]);
    }

    [Fact]
    public async Task No_duplica_ni_cambia_un_usuario_inicial_que_ya_existe()
    {
        var operador = new UsuarioInicial("operador@parkingapp.local", "Clave-Segura-2", Roles.Operador);
        await CrearAsync(operador);

        // Segundo arranque: el usuario ya existe, aunque el correo llegue con otras mayúsculas.
        var resultados = await CrearAsync(operador with { Correo = " OPERADOR@parkingapp.local " });

        Assert.Equal(EstadoDeUsuarioInicial.YaExistia, Assert.Single(resultados).Estado);
        Assert.Equal(1, directorio.VecesQueSeCreo);
    }

    [Theory]
    [InlineData(null, "Clave-Segura-1")]
    [InlineData("admin@parkingapp.local", null)]
    [InlineData("  ", "Clave-Segura-1")]
    public async Task Sin_credenciales_en_env_no_crea_el_usuario_y_avisa(string? correo, string? contrasena)
    {
        var resultados = await CrearAsync(new UsuarioInicial(correo, contrasena, Roles.Administrador));

        var resultado = Assert.Single(resultados);
        Assert.Equal(EstadoDeUsuarioInicial.SinCredenciales, resultado.Estado);
        Assert.Equal("Faltan el correo o la contraseña en .env", resultado.Motivo);
        Assert.Empty(directorio.RolesPorCorreo);
    }

    [Fact]
    public async Task Una_contrasena_que_no_cumple_la_politica_se_rechaza_y_los_demas_usuarios_se_crean()
    {
        var resultados = await CrearAsync(
            new UsuarioInicial("admin@parkingapp.local", "corta", Roles.Administrador),
            new UsuarioInicial("operador@parkingapp.local", "Clave-Segura-2", Roles.Operador));

        Assert.Equal(EstadoDeUsuarioInicial.Rechazado, resultados[0].Estado);
        Assert.Equal("La contraseña es muy corta", resultados[0].Motivo);
        Assert.Equal(EstadoDeUsuarioInicial.Creado, resultados[1].Estado);
    }

    [Fact]
    public async Task Un_rol_que_no_existe_se_rechaza()
    {
        var resultados = await CrearAsync(new UsuarioInicial("invitado@parkingapp.local", "Clave-Segura-1", "Invitado"));

        Assert.Equal(EstadoDeUsuarioInicial.Rechazado, Assert.Single(resultados).Estado);
        Assert.Empty(directorio.RolesPorCorreo);
    }

    [Fact]
    public void Un_usuario_inicial_nunca_muestra_su_contrasena_al_escribirse()
    {
        var usuario = new UsuarioInicial("admin@parkingapp.local", "Clave-Segura-1", Roles.Administrador);

        Assert.DoesNotContain("Clave-Segura-1", usuario.ToString(), StringComparison.Ordinal);
    }

    private Task<IReadOnlyList<ResultadoDeUsuarioInicial>> CrearAsync(params UsuarioInicial[] usuarios)
    {
        return new CrearUsuariosIniciales(directorio).EjecutarAsync(usuarios, TestContext.Current.CancellationToken);
    }
}
