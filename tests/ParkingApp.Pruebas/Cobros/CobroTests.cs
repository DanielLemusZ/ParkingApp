using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Configuracion;
using ParkingApp.Dominio.Pagos;

namespace ParkingApp.Pruebas.Cobros;

public class CobroTests
{
    private static readonly DateOnly Lunes = new DateOnly(2026, 11, 16);

    // Lunes 16 de noviembre de 2026 a las 08:00 en Bogotá son las 13:00 en UTC.
    private static readonly DateTimeOffset LunesALas8EnUtc = new DateTimeOffset(2026, 11, 16, 13, 0, 0, TimeSpan.Zero);

    // Lunes 16 de noviembre de 2026 a las 11:00 en Bogotá son las 16:00 en UTC.
    private static readonly DateTimeOffset LunesALas11EnUtc = new DateTimeOffset(2026, 11, 16, 16, 0, 0, TimeSpan.Zero);

    private static readonly Placa Carro = Placa.Crear("ABC123");

    [Fact]
    public void El_cobro_guarda_valor_franja_y_tarifa_con_que_se_calculo()
    {
        var tarifa = new Tarifa(TipoDeVehiculo.Carro, FranjaTarifaria.Diurna, 8000m, new DateOnly(2026, 11, 1));
        var sedeId = Guid.CreateVersion7();

        var cobro = Cobro.Generar(Carro, TipoDeVehiculo.Carro, sedeId, Lunes, tarifa, LunesALas8EnUtc);

        Assert.Equal(EstadoDeCobro.Pendiente, cobro.Estado);
        Assert.Equal(8000m, cobro.Valor);
        Assert.Equal(FranjaTarifaria.Diurna, cobro.Franja);
        Assert.Equal(tarifa.Id, cobro.TarifaId);
        Assert.Equal(Lunes, cobro.DiaOperativo);
        Assert.Equal(sedeId, cobro.SedeId);
        Assert.Equal(Carro, cobro.Placa);
    }

    [Fact]
    public void Una_tarifa_de_cero_no_genera_cobro()
    {
        var tarifa = new Tarifa(TipoDeVehiculo.Moto, FranjaTarifaria.Diurna, 0m, new DateOnly(2026, 11, 1));

        Assert.False(tarifa.GeneraCobro);
        Assert.Throws<ArgumentException>(() =>
            Cobro.Generar(Placa.Crear("XYZ12D"), TipoDeVehiculo.Moto, Guid.CreateVersion7(), Lunes, tarifa, LunesALas8EnUtc));
    }

    [Fact]
    public void Un_cobro_con_la_tarifa_de_otro_tipo_de_vehiculo_se_rechaza()
    {
        var tarifaDeMoto = new Tarifa(TipoDeVehiculo.Moto, FranjaTarifaria.Diurna, 1000m, new DateOnly(2026, 11, 1));

        Assert.Throws<ArgumentException>(() =>
            Cobro.Generar(Carro, TipoDeVehiculo.Carro, Guid.CreateVersion7(), Lunes, tarifaDeMoto, LunesALas8EnUtc));
    }

    [Fact]
    public void Pagar_un_cobro_pendiente_lo_deja_pagado_y_crea_el_pago_con_su_valor()
    {
        var cobro = CobroDiurnoDe8000();
        var referencia = Guid.NewGuid();

        var pago = cobro.Pagar(MedioDePago.Efectivo, "caja", referencia, LunesALas11EnUtc);

        Assert.Equal(EstadoDeCobro.Pagado, cobro.Estado);
        Assert.Equal(cobro.Id, pago.CobroId);
        Assert.Equal(8000m, pago.Valor);
        Assert.Equal(MedioDePago.Efectivo, pago.Medio);
        Assert.Equal("caja", pago.Operador);
        Assert.Equal(referencia, pago.Referencia);
        Assert.Equal(LunesALas11EnUtc, pago.FechaHora);
    }

    [Fact]
    public void Cobro_pagado_no_se_puede_pagar_otra_vez()
    {
        var cobro = CobroDiurnoDe8000();
        cobro.Pagar(MedioDePago.Efectivo, "caja", Guid.NewGuid(), LunesALas11EnUtc);

        Assert.Throws<InvalidOperationException>(() =>
            cobro.Pagar(MedioDePago.Efectivo, "caja", Guid.NewGuid(), LunesALas11EnUtc));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Pago_sin_operador_es_invalido_y_el_cobro_sigue_pendiente(string operador)
    {
        var cobro = CobroDiurnoDe8000();

        Assert.Throws<ArgumentException>(() =>
            cobro.Pagar(MedioDePago.Efectivo, operador, Guid.NewGuid(), LunesALas11EnUtc));
        Assert.Equal(EstadoDeCobro.Pendiente, cobro.Estado);
    }

    [Fact]
    public void Pago_sin_referencia_o_con_hora_local_es_invalido()
    {
        var cobro = CobroDiurnoDe8000();
        var horaEnBogota = new DateTimeOffset(2026, 11, 16, 11, 0, 0, TimeSpan.FromHours(-5));

        Assert.Throws<ArgumentException>(() =>
            cobro.Pagar(MedioDePago.Efectivo, "caja", Guid.Empty, LunesALas11EnUtc));
        Assert.Throws<ArgumentException>(() =>
            cobro.Pagar(MedioDePago.Efectivo, "caja", Guid.NewGuid(), horaEnBogota));
    }

    [Fact]
    public void Pase_vigente_vale_solo_en_el_dia_operativo_del_cobro_pagado()
    {
        var cobro = CobroDiurnoDe8000();
        var martes = Lunes.AddDays(1);

        Assert.False(cobro.DaPaseVigenteEn(Lunes));

        cobro.Pagar(MedioDePago.Efectivo, "caja", Guid.NewGuid(), LunesALas11EnUtc);

        // El martes a las 05:59 sigue siendo el día operativo del lunes; a las 06:00 empieza el del martes.
        Assert.True(cobro.DaPaseVigenteEn(Lunes));
        Assert.False(cobro.DaPaseVigenteEn(martes));
    }

    [Fact]
    public void Pagar_con_la_pasarela_guarda_la_transaccion_y_da_el_mismo_pase_vigente()
    {
        var cobro = CobroDiurnoDe8000();

        var pago = cobro.Pagar(MedioDePago.Pasarela, "caja", Guid.NewGuid(), LunesALas11EnUtc, "12345-1700000000-00001");

        Assert.Equal(MedioDePago.Pasarela, pago.Medio);
        Assert.Equal("12345-1700000000-00001", pago.TransaccionDeLaPasarela);
        Assert.True(cobro.DaPaseVigenteEn(Lunes));
    }

    [Fact]
    public void Pago_con_pasarela_sin_transaccion_o_en_efectivo_con_transaccion_es_invalido()
    {
        var cobro = CobroDiurnoDe8000();

        Assert.Throws<ArgumentException>(() =>
            cobro.Pagar(MedioDePago.Pasarela, "caja", Guid.NewGuid(), LunesALas11EnUtc, "  "));
        Assert.Throws<ArgumentException>(() =>
            cobro.Pagar(MedioDePago.Efectivo, "caja", Guid.NewGuid(), LunesALas11EnUtc, "12345"));
        Assert.Equal(EstadoDeCobro.Pendiente, cobro.Estado);
    }

    [Fact]
    public void El_cobro_por_ingreso_guarda_el_motivo_y_la_hora_del_ingreso()
    {
        var cobro = CobroDiurnoDe8000();

        Assert.Equal(MotivoDeCobro.Ingreso, cobro.Motivo);
        Assert.Equal(LunesALas8EnUtc, cobro.FechaHoraDeIngreso);
        Assert.Equal(LunesALas8EnUtc, cobro.FechaHora);
    }

    [Fact]
    public void El_cobro_por_seguir_adentro_guarda_el_motivo_y_desde_cuando_esta_adentro()
    {
        // Entró el domingo 15 a las 20:00 en Bogotá (lunes 01:00 UTC); el lunes empieza a las 06:00 (11:00 UTC).
        var ingreso = new DateTimeOffset(2026, 11, 16, 1, 0, 0, TimeSpan.Zero);
        var inicioDelLunes = new DateTimeOffset(2026, 11, 16, 11, 0, 0, TimeSpan.Zero);
        var tarifa = new Tarifa(TipoDeVehiculo.Carro, FranjaTarifaria.Diurna, 8000m, new DateOnly(2026, 11, 1));

        var cobro = Cobro.GenerarPorPermanencia(Carro, TipoDeVehiculo.Carro, Guid.CreateVersion7(), Lunes, tarifa, ingreso, inicioDelLunes);

        Assert.Equal(MotivoDeCobro.SigueAdentro, cobro.Motivo);
        Assert.Equal(ingreso, cobro.FechaHoraDeIngreso);
        Assert.Equal(inicioDelLunes, cobro.FechaHora);
        Assert.Equal(EstadoDeCobro.Pendiente, cobro.Estado);
        Assert.Equal(FranjaTarifaria.Diurna, cobro.Franja);
    }

    [Fact]
    public void El_cobro_por_seguir_adentro_no_acepta_tarifa_nocturna_ni_un_ingreso_posterior_al_inicio_del_dia()
    {
        var inicioDelLunes = new DateTimeOffset(2026, 11, 16, 11, 0, 0, TimeSpan.Zero);
        var nocturna = new Tarifa(TipoDeVehiculo.Carro, FranjaTarifaria.Nocturna, 5000m, new DateOnly(2026, 11, 1));
        var diurna = new Tarifa(TipoDeVehiculo.Carro, FranjaTarifaria.Diurna, 8000m, new DateOnly(2026, 11, 1));

        Assert.Throws<ArgumentException>(() => Cobro.GenerarPorPermanencia(
            Carro, TipoDeVehiculo.Carro, Guid.CreateVersion7(), Lunes, nocturna, inicioDelLunes.AddHours(-10), inicioDelLunes));
        Assert.Throws<ArgumentException>(() => Cobro.GenerarPorPermanencia(
            Carro, TipoDeVehiculo.Carro, Guid.CreateVersion7(), Lunes, diurna, inicioDelLunes, inicioDelLunes));
    }

    private static Cobro CobroDiurnoDe8000()
    {
        var tarifa = new Tarifa(TipoDeVehiculo.Carro, FranjaTarifaria.Diurna, 8000m, new DateOnly(2026, 11, 1));
        return Cobro.Generar(Carro, TipoDeVehiculo.Carro, Guid.CreateVersion7(), Lunes, tarifa, LunesALas8EnUtc);
    }
}
