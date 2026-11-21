using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Configuracion;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Accesos;

// Escenarios del issue #14 (F-05 y RN-06): la barrera de salida abre solo si el vehículo no tiene cobros pendientes.
// El pago se hace con el dominio (Cobro.Pagar), sin pasar por la caja.
public class RegistrarSalidaTests
{
    // Miércoles 18 de noviembre, 08:00 en Bogotá (UTC-5) = 13:00 UTC.
    private static readonly DateTimeOffset MiercolesALas0800 = new DateTimeOffset(2026, 11, 18, 13, 0, 0, TimeSpan.Zero);

    private readonly CarrilDeEntradaDePrueba entrada;
    private readonly CarrilDeSalidaDePrueba salida;

    public RegistrarSalidaTests()
    {
        entrada = new CarrilDeEntradaDePrueba(MiercolesALas0800);
        salida = new CarrilDeSalidaDePrueba(entrada);
    }

    [Fact]
    public async Task Moto_sin_cobro_que_sale_a_las_1400_abre_con_sin_cobro_y_registra_la_salida()
    {
        await entrada.IngresarAsync("XYZ12D");

        // 14:00 en Bogotá.
        entrada.Reloj.Advance(TimeSpan.FromHours(6));
        var resultado = await salida.SalirAsync("XYZ12D");

        Assert.True(resultado.AbreBarrera);
        Assert.Equal("Abre: sin cobro", resultado.Motivo);
        Assert.Equal(2, entrada.Eventos.Eventos.Count);
        Assert.Equal(TipoDeEvento.Salida, entrada.Eventos.Eventos[1].Tipo);
        Assert.Equal(TipoDeVehiculo.Moto, resultado.Salida?.TipoDeVehiculo);
    }

    [Fact]
    public async Task Carro_con_el_cobro_de_hoy_pagado_abre_con_cobro_pagado_y_registra_la_salida()
    {
        await entrada.IngresarAsync("ABC123");
        entrada.Cobros.PagarEnEfectivo(entrada.Cobros.Cobros[0]);

        entrada.Reloj.Advance(TimeSpan.FromHours(3));
        var resultado = await salida.SalirAsync("ABC123");

        Assert.True(resultado.AbreBarrera);
        Assert.Equal("Abre: cobro pagado", resultado.Motivo);
        Assert.Equal(2, entrada.Eventos.Eventos.Count);
    }

    [Fact]
    public async Task Placa_diplomatica_de_ingreso_manual_que_pago_abre_con_cobro_pagado_sin_exigir_formato()
    {
        // El ingreso manual llega con su propia historia: aquí el cobro de CD1234 se guarda directamente.
        var tarifa = new Tarifa(TipoDeVehiculo.Carro, FranjaTarifaria.Diurna, 8000m, new DateOnly(2026, 11, 1));
        var cobro = Cobro.Generar(Placa.Crear("CD1234"), TipoDeVehiculo.Carro, entrada.Sede.Id, new DateOnly(2026, 11, 18), tarifa, MiercolesALas0800);
        await entrada.Cobros.AgregarAsync(cobro, TestContext.Current.CancellationToken);
        entrada.Cobros.PagarEnEfectivo(cobro);

        var resultado = await salida.SalirAsync("CD1234");

        Assert.True(resultado.AbreBarrera);
        Assert.Equal("Abre: cobro pagado", resultado.Motivo);
        Assert.Equal("CD1234", resultado.Salida?.Placa);
        Assert.Equal(TipoDeVehiculo.NoClasificado, resultado.Salida?.TipoDeVehiculo);
    }

    [Fact]
    public async Task Carro_con_cobro_pendiente_de_8000_no_abre_y_no_registra_la_salida()
    {
        await entrada.IngresarAsync("ABC123");

        var resultado = await salida.SalirAsync("ABC123");

        Assert.False(resultado.AbreBarrera);
        Assert.Equal("No abre: cobro pendiente de $8.000", resultado.Motivo);
        Assert.Null(resultado.Salida);
        Assert.Single(entrada.Eventos.Eventos);
    }

    [Fact]
    public async Task Un_cobro_pendiente_de_un_dia_operativo_anterior_tambien_impide_salir()
    {
        await entrada.IngresarAsync("ABC123");

        // Jueves 19, 10:00 en Bogotá: ya es otro día operativo.
        entrada.Reloj.Advance(TimeSpan.FromHours(26));
        var resultado = await salida.SalirAsync("ABC123");

        Assert.False(resultado.AbreBarrera);
        Assert.Equal("No abre: cobro pendiente de $8.000", resultado.Motivo);
    }

    [Fact]
    public async Task Con_dos_cobros_pendientes_la_barrera_muestra_el_total()
    {
        // Entra el miércoles y otra vez el jueves a las 08:00, sin pagar ninguno de los dos días.
        await entrada.IngresarAsync("ABC123");
        entrada.Reloj.Advance(TimeSpan.FromHours(24));
        await entrada.IngresarAsync("ABC123");

        var resultado = await salida.SalirAsync("ABC123");

        Assert.False(resultado.AbreBarrera);
        Assert.Equal("No abre: cobro pendiente de $16.000", resultado.Motivo);
    }

    [Fact]
    public async Task Un_cobro_pagado_de_otro_dia_no_cuenta_como_cobro_pagado_de_hoy()
    {
        await entrada.IngresarAsync("ABC123");
        entrada.Cobros.PagarEnEfectivo(entrada.Cobros.Cobros[0]);

        // Jueves 19, 10:00 en Bogotá: el cobro pagado es del miércoles.
        entrada.Reloj.Advance(TimeSpan.FromHours(26));
        var resultado = await salida.SalirAsync("ABC123");

        Assert.True(resultado.AbreBarrera);
        Assert.Equal("Abre: sin cobro", resultado.Motivo);
    }

    [Fact]
    public async Task Una_placa_sin_ingreso_registrado_sale_como_sin_cobro()
    {
        var resultado = await salida.SalirAsync("DEF456");

        Assert.True(resultado.AbreBarrera);
        Assert.Equal("Abre: sin cobro", resultado.Motivo);
    }

    [Fact]
    public async Task Con_confianza_menor_que_el_umbral_no_abre_y_la_placa_pasa_a_la_caja()
    {
        // El umbral inicial de la sede es 80.
        var resultado = await salida.SalirAsync("XYZ12D", confianza: 50);

        Assert.False(resultado.AbreBarrera);
        Assert.Equal("No abre: confianza baja, la placa se valida en la caja", resultado.Motivo);
        Assert.Empty(entrada.Eventos.Eventos);
    }

    [Fact]
    public async Task Con_la_placa_vacia_no_abre_ni_registra_la_salida()
    {
        var resultado = await salida.SalirAsync("  ");

        Assert.False(resultado.AbreBarrera);
        Assert.Equal("La placa es obligatoria", resultado.Motivo);
        Assert.Empty(entrada.Eventos.Eventos);
    }

    [Fact]
    public async Task Con_la_confianza_fuera_de_rango_no_abre_ni_registra_la_salida()
    {
        var resultado = await salida.SalirAsync("XYZ12D", confianza: 150);

        Assert.False(resultado.AbreBarrera);
        Assert.Equal("La confianza debe estar entre 0 y 100", resultado.Motivo);
        Assert.Empty(entrada.Eventos.Eventos);
    }

    [Fact]
    public async Task La_salida_se_registra_con_la_sede_el_carril_y_la_hora_de_la_sede()
    {
        var resultado = await salida.SalirAsync("XYZ12D", carril: 2);

        var evento = Assert.Single(entrada.Eventos.Eventos);
        Assert.Equal(TipoDeEvento.Salida, evento.Tipo);
        Assert.Equal(entrada.Sede.Id, evento.SedeId);
        Assert.Equal(2, evento.Carril);
        Assert.Equal(MiercolesALas0800, evento.FechaHora);

        var registrada = Assert.IsType<SalidaRegistrada>(resultado.Salida);
        Assert.Equal(TimeSpan.FromHours(-5), registrada.FechaHora.Offset);
        Assert.Equal(8, registrada.FechaHora.Hour);
    }
}
