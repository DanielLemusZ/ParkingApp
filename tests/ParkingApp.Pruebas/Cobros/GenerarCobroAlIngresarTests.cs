using ParkingApp.Aplicacion.Accesos.Validaciones;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Configuracion;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Cobros;

// Escenarios del issue #11 (F-03): el primer ingreso del día operativo genera el cobro.
public class GenerarCobroAlIngresarTests
{
    [Fact]
    public async Task Carro_que_entra_a_las_0800_genera_cobro_diurno_pendiente_de_8000()
    {
        // Lunes 16 de noviembre, 08:00 en Bogotá (UTC-5) = 13:00 UTC.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 16, 13, 0, 0, TimeSpan.Zero));

        var resultado = await carril.IngresarAsync("ABC123");

        Assert.True(resultado.AbreBarrera);
        Assert.Equal("Cobro diurno pendiente: $8.000", resultado.Motivo);
        var cobro = Assert.Single(carril.Cobros.Cobros);
        Assert.Equal(EstadoDeCobro.Pendiente, cobro.Estado);
        Assert.Equal(FranjaTarifaria.Diurna, cobro.Franja);
        Assert.Equal(8000m, cobro.Valor);
        Assert.Equal(new DateOnly(2026, 11, 16), cobro.DiaOperativo);
        Assert.Equal(cobro.Id, resultado.Ingreso?.Cobro?.Id);
    }

    [Fact]
    public async Task Carro_que_entra_a_las_1900_genera_cobro_nocturno_de_5000()
    {
        // Lunes 16 de noviembre, 19:00 en Bogotá = martes 17, 00:00 UTC.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 17, 0, 0, 0, TimeSpan.Zero));

        var resultado = await carril.IngresarAsync("ABC123");

        Assert.Equal("Cobro nocturno pendiente: $5.000", resultado.Motivo);
        var cobro = Assert.Single(carril.Cobros.Cobros);
        Assert.Equal(FranjaTarifaria.Nocturna, cobro.Franja);
        Assert.Equal(5000m, cobro.Valor);
        Assert.Equal(new DateOnly(2026, 11, 16), cobro.DiaOperativo);
    }

    [Fact]
    public async Task Moto_entra_sin_generar_cobro_y_abre_la_barrera()
    {
        // Lunes 16 de noviembre, 10:00 en Bogotá = 15:00 UTC.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 16, 15, 0, 0, TimeSpan.Zero));

        var resultado = await carril.IngresarAsync("XYZ12D");

        Assert.True(resultado.AbreBarrera);
        Assert.Equal("Moto: sin cobro", resultado.Motivo);
        Assert.Single(carril.Eventos.Eventos);
        Assert.Empty(carril.Cobros.Cobros);
        Assert.Null(resultado.Ingreso?.Cobro);
    }

    [Fact]
    public async Task Carro_que_entra_a_las_0559_del_jueves_queda_en_el_dia_operativo_del_miercoles()
    {
        // Jueves 19 de noviembre, 05:59 en Bogotá = 10:59 UTC.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 19, 10, 59, 0, TimeSpan.Zero));

        await carril.IngresarAsync("ABC123");

        var cobro = Assert.Single(carril.Cobros.Cobros);
        Assert.Equal(new DateOnly(2026, 11, 18), cobro.DiaOperativo);
        Assert.Equal(FranjaTarifaria.Nocturna, cobro.Franja);
        Assert.Equal(5000m, cobro.Valor);
    }

    [Fact]
    public async Task Sin_tarifa_vigente_se_rechaza_el_ingreso_sin_registrar_evento_ni_cobro()
    {
        // Lunes 16 de noviembre, 08:00 en Bogotá. Sin tarifas cargadas.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 16, 13, 0, 0, TimeSpan.Zero), conTarifasSemilla: false);

        var resultado = await carril.IngresarAsync("ABC123");

        Assert.False(resultado.AbreBarrera);
        Assert.Equal("No hay tarifa vigente para carro en franja diurna", resultado.Motivo);
        Assert.Empty(carril.Eventos.Eventos);
        Assert.Empty(carril.Cobros.Cobros);
    }

    [Fact]
    public async Task Placa_sin_formato_reconocido_no_genera_cobro()
    {
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 16, 13, 0, 0, TimeSpan.Zero));

        var resultado = await carril.IngresarAsync("ABC12");

        Assert.False(resultado.AbreBarrera);
        Assert.Equal(ClasificacionDeVehiculoValida.FormatoNoReconocido, resultado.Motivo);
        Assert.Empty(carril.Cobros.Cobros);
    }

    [Fact]
    public async Task Un_segundo_ingreso_en_el_mismo_dia_operativo_no_genera_otro_cobro()
    {
        // Lunes 16 de noviembre, 08:00 y luego 22:00 en Bogotá: mismo día operativo.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 16, 13, 0, 0, TimeSpan.Zero));
        await carril.IngresarAsync("ABC123");
        carril.Reloj.Advance(TimeSpan.FromHours(14));

        var reingreso = await carril.IngresarAsync("ABC123");

        Assert.True(reingreso.AbreBarrera);
        Assert.Null(reingreso.Ingreso?.Cobro);
        Assert.Equal(2, carril.Eventos.Eventos.Count);
        Assert.Single(carril.Cobros.Cobros);
    }

    [Fact]
    public async Task Moto_con_tarifa_mayor_que_cero_genera_cobro()
    {
        // Si el administrador les pone tarifa a las motos, se cobra sin cambiar código (RN-01).
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 16, 15, 0, 0, TimeSpan.Zero), conTarifasSemilla: false);
        carril.AgregarTarifa(TipoDeVehiculo.Moto, FranjaTarifaria.Diurna, 1000m);

        var resultado = await carril.IngresarAsync("XYZ12D");

        Assert.Equal("Cobro diurno pendiente: $1.000", resultado.Motivo);
        Assert.Equal(1000m, Assert.Single(carril.Cobros.Cobros).Valor);
    }
}
