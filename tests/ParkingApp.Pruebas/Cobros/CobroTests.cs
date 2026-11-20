using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Pruebas.Cobros;

public class CobroTests
{
    private static readonly DateOnly Lunes = new DateOnly(2026, 11, 16);

    // Lunes 16 de noviembre de 2026 a las 08:00 en Bogotá son las 13:00 en UTC.
    private static readonly DateTimeOffset LunesALas8EnUtc = new DateTimeOffset(2026, 11, 16, 13, 0, 0, TimeSpan.Zero);

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
}
