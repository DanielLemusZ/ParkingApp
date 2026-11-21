using Microsoft.Extensions.Time.Testing;

using ParkingApp.Aplicacion.Identidad;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Configuracion;
using ParkingApp.Dominio.Pagos;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Identidad;

// Escenarios del issue #68: el Administrador administra los usuarios y sus roles.
public class AdministrarUsuariosTests
{
    private const string Administrador = "admin@parkingapp.local";
    private const string Operador = "operador@parkingapp.local";

    // Sábado 21 de noviembre, 10:00 en Bogotá (UTC-5) = 15:00 UTC.
    private static readonly DateTimeOffset SabadoALas1000 = new DateTimeOffset(2026, 11, 21, 15, 0, 0, TimeSpan.Zero);

    private readonly DirectorioDeUsuariosEnMemoria directorio = new DirectorioDeUsuariosEnMemoria();
    private readonly RepositorioPagosEnMemoria pagos = new RepositorioPagosEnMemoria();
    private readonly FakeTimeProvider reloj = new FakeTimeProvider(SabadoALas1000);
    private readonly AdministrarUsuarios administrar;

    public AdministrarUsuariosTests()
    {
        administrar = new AdministrarUsuarios(directorio, pagos, reloj);
        directorio.Agregar(Administrador, Roles.Administrador);
    }

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Crear_operador2_con_rol_Operador_lo_deja_en_la_lista_activo()
    {
        var resultado = await administrar.CrearAsync("operador2@parkingapp.local", Roles.Operador, "Clave-Segura-3", Cancelacion);

        Assert.True(resultado.Exito);
        Assert.Equal("Usuario creado: operador2@parkingapp.local (Operador)", resultado.Motivo);

        var usuario = Assert.Single(await administrar.ListarAsync("operador2", Cancelacion));
        Assert.Equal(Roles.Operador, usuario.Rol);
        Assert.Equal(EstadoDeUsuario.Activo, usuario.Estado);
    }

    [Fact]
    public async Task Crear_un_usuario_con_un_correo_que_ya_existe_se_rechaza_con_ese_correo()
    {
        directorio.Agregar(Operador, Roles.Operador);

        var resultado = await administrar.CrearAsync(Operador, Roles.Operador, "Clave-Segura-3", Cancelacion);

        Assert.False(resultado.Exito);
        Assert.Equal("Ya existe un usuario con el correo operador@parkingapp.local", resultado.Motivo);
        Assert.Equal(0, directorio.VecesQueSeCreo);
    }

    [Fact]
    public async Task Crear_con_la_contrasena_1234_se_rechaza_con_la_politica_de_contrasenas()
    {
        var resultado = await administrar.CrearAsync("operador2@parkingapp.local", Roles.Operador, "1234", Cancelacion);

        Assert.False(resultado.Exito);
        Assert.Equal(DirectorioDeUsuariosEnMemoria.ContrasenaMuyCorta, resultado.Motivo);
        Assert.Empty(await administrar.ListarAsync("operador2", Cancelacion));
    }

    [Fact]
    public async Task Crear_con_un_rol_que_no_existe_se_rechaza()
    {
        var resultado = await administrar.CrearAsync("invitado@parkingapp.local", "Invitado", "Clave-Segura-3", Cancelacion);

        Assert.False(resultado.Exito);
        Assert.Equal("El rol Invitado no existe", resultado.Motivo);
    }

    [Fact]
    public async Task Buscar_por_parte_del_correo_lista_solo_los_que_coinciden_sin_distinguir_mayusculas()
    {
        directorio.Agregar(Operador, Roles.Operador);
        directorio.Agregar("caja2@parkingapp.local", Roles.Operador);

        var encontrados = await administrar.ListarAsync("OPERA", Cancelacion);
        var todos = await administrar.ListarAsync(null, Cancelacion);

        Assert.Equal(Operador, Assert.Single(encontrados).Correo);
        Assert.Equal(3, todos.Count);
        Assert.Equal(Administrador, todos[0].Correo);
    }

    [Fact]
    public async Task Operador_bloqueado_por_5_intentos_aparece_bloqueado_y_al_desbloquearlo_queda_activo()
    {
        var id = directorio.Agregar(Operador, Roles.Operador);

        // Identity lo bloqueó 5 minutos por los intentos fallidos.
        directorio.BloquearHasta(Operador, SabadoALas1000.AddMinutes(5));
        Assert.Equal(EstadoDeUsuario.Bloqueado, await EstadoDeAsync(Operador));

        var resultado = await administrar.DesbloquearAsync(id, Cancelacion);

        Assert.True(resultado.Exito);
        Assert.Equal("Usuario desbloqueado: operador@parkingapp.local", resultado.Motivo);
        Assert.Equal(EstadoDeUsuario.Activo, await EstadoDeAsync(Operador));
        Assert.Null(directorio.BloqueadoHastaDe(Operador));
    }

    [Fact]
    public async Task Un_bloqueo_vencido_aparece_como_activo()
    {
        directorio.Agregar(Operador, Roles.Operador);
        directorio.BloquearHasta(Operador, SabadoALas1000.AddMinutes(5));

        reloj.Advance(TimeSpan.FromMinutes(6));

        Assert.Equal(EstadoDeUsuario.Activo, await EstadoDeAsync(Operador));
    }

    [Fact]
    public async Task Desbloquear_un_usuario_que_no_esta_bloqueado_se_rechaza()
    {
        var id = directorio.Agregar(Operador, Roles.Operador);

        var resultado = await administrar.DesbloquearAsync(id, Cancelacion);

        Assert.False(resultado.Exito);
        Assert.Equal(AdministrarUsuarios.NoEstaBloqueado, resultado.Motivo);
    }

    [Fact]
    public async Task Cambiar_el_rol_de_un_operador_a_Administrador()
    {
        var id = directorio.Agregar(Operador, Roles.Operador);

        var resultado = await administrar.CambiarRolAsync(id, Roles.Administrador, Administrador, Cancelacion);

        Assert.True(resultado.Exito);
        Assert.Equal("Rol cambiado: operador@parkingapp.local ahora es Administrador", resultado.Motivo);
        Assert.Equal(Roles.Administrador, directorio.RolesPorCorreo[Operador]);
    }

    [Fact]
    public async Task Restablecer_la_contrasena_de_un_usuario_que_la_olvido()
    {
        var id = directorio.Agregar(Operador, Roles.Operador);

        var resultado = await administrar.RestablecerContrasenaAsync(id, "Clave-Nueva-9", Cancelacion);

        Assert.True(resultado.Exito);
        Assert.Equal("Contraseña restablecida: operador@parkingapp.local", resultado.Motivo);
        Assert.Equal("Clave-Nueva-9", directorio.ContrasenaDe(Operador));
    }

    [Fact]
    public async Task Restablecer_con_una_contrasena_que_no_cumple_la_politica_se_rechaza()
    {
        var id = directorio.Agregar(Operador, Roles.Operador);

        var resultado = await administrar.RestablecerContrasenaAsync(id, "1234", Cancelacion);

        Assert.False(resultado.Exito);
        Assert.Equal(DirectorioDeUsuariosEnMemoria.ContrasenaMuyCorta, resultado.Motivo);
    }

    [Fact]
    public async Task Desactivar_y_reactivar_un_usuario()
    {
        var id = directorio.Agregar(Operador, Roles.Operador);

        var desactivacion = await administrar.DesactivarAsync(id, Administrador, Cancelacion);

        Assert.True(desactivacion.Exito);
        Assert.Equal(EstadoDeUsuario.Desactivado, await EstadoDeAsync(Operador));

        var reactivacion = await administrar.ReactivarAsync(id, Cancelacion);

        Assert.True(reactivacion.Exito);
        Assert.Equal("Usuario reactivado: operador@parkingapp.local", reactivacion.Motivo);
        Assert.Equal(EstadoDeUsuario.Activo, await EstadoDeAsync(Operador));
    }

    [Fact]
    public async Task Un_administrador_puede_desactivar_a_otro_administrador()
    {
        var id = directorio.Agregar("admin2@parkingapp.local", Roles.Administrador);

        var resultado = await administrar.DesactivarAsync(id, Administrador, Cancelacion);

        // Quien desactiva sigue activo: siempre queda al menos un Administrador.
        Assert.True(resultado.Exito);
        Assert.Equal(EstadoDeUsuario.Activo, await EstadoDeAsync(Administrador));
    }

    [Fact]
    public async Task Borrar_un_operador_que_nunca_registro_un_pago_lo_quita_de_la_lista()
    {
        var id = directorio.Agregar(Operador, Roles.Operador);

        var resultado = await administrar.BorrarAsync(id, Administrador, Cancelacion);

        Assert.True(resultado.Exito);
        Assert.Equal("Usuario borrado: operador@parkingapp.local", resultado.Motivo);
        Assert.Empty(await administrar.ListarAsync(Operador, Cancelacion));
    }

    [Fact]
    public async Task Borrar_un_operador_que_registro_pagos_se_rechaza_y_ofrece_desactivarlo()
    {
        var id = directorio.Agregar(Operador, Roles.Operador);
        await PagarUnCobroConElOperadorAsync(Operador);

        var resultado = await administrar.BorrarAsync(id, Administrador, Cancelacion);

        Assert.False(resultado.Exito);
        Assert.Equal("El usuario tiene pagos registrados: desactívalo en lugar de borrarlo", resultado.Motivo);
        Assert.Single(await administrar.ListarAsync(Operador, Cancelacion));
    }

    [Fact]
    public async Task El_unico_administrador_activo_no_puede_desactivarse_borrarse_ni_quitarse_el_rol()
    {
        var id = Assert.Single(await administrar.ListarAsync(Administrador, Cancelacion)).Id;

        var desactivacion = await administrar.DesactivarAsync(id, Administrador, Cancelacion);
        var borrado = await administrar.BorrarAsync(id, Administrador, Cancelacion);
        var cambioDeRol = await administrar.CambiarRolAsync(id, Roles.Operador, Administrador, Cancelacion);

        Assert.Equal(AdministrarUsuarios.NoPuedeDesactivarse, desactivacion.Motivo);
        Assert.Equal(AdministrarUsuarios.NoPuedeBorrarse, borrado.Motivo);
        Assert.Equal(AdministrarUsuarios.NoPuedeCambiarseElRol, cambioDeRol.Motivo);

        var administrador = Assert.Single(await administrar.ListarAsync(Administrador, Cancelacion));
        Assert.Equal(Roles.Administrador, administrador.Rol);
        Assert.Equal(EstadoDeUsuario.Activo, administrador.Estado);
    }

    [Fact]
    public async Task Una_accion_sobre_un_usuario_que_no_existe_responde_usuario_no_encontrado()
    {
        var desconocido = Guid.CreateVersion7();

        var desactivacion = await administrar.DesactivarAsync(desconocido, Administrador, Cancelacion);
        var borrado = await administrar.BorrarAsync(desconocido, Administrador, Cancelacion);

        Assert.Equal(AdministrarUsuarios.UsuarioNoEncontrado, desactivacion.Motivo);
        Assert.Equal(AdministrarUsuarios.UsuarioNoEncontrado, borrado.Motivo);
    }

    private async Task<EstadoDeUsuario> EstadoDeAsync(string correo)
    {
        var usuario = Assert.Single(await administrar.ListarAsync(correo, Cancelacion));
        return usuario.Estado;
    }

    // Un pago guarda el correo del operador que lo registró.
    private async Task PagarUnCobroConElOperadorAsync(string operador)
    {
        var tarifa = new Tarifa(TipoDeVehiculo.Carro, FranjaTarifaria.Diurna, 8000m, new DateOnly(2026, 11, 1));
        var cobro = Cobro.Generar(Placa.Crear("ABC123"), TipoDeVehiculo.Carro, Guid.CreateVersion7(), new DateOnly(2026, 11, 21), tarifa, SabadoALas1000);
        var pago = cobro.Pagar(MedioDePago.Efectivo, operador, Guid.NewGuid(), SabadoALas1000);

        await pagos.AgregarAsync(pago, Cancelacion);
    }
}
