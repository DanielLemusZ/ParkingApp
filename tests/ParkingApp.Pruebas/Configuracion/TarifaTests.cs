using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Pruebas.Configuracion;

public class TarifaTests
{
    private static readonly DateOnly Inicio = new DateOnly(2026, 11, 1);

    [Fact]
    public void Una_tarifa_abierta_aplica_cualquier_dia_desde_su_inicio()
    {
        var tarifa = new Tarifa(TipoDeVehiculo.Carro, FranjaTarifaria.Diurna, 8000, Inicio);

        Assert.True(tarifa.EstaVigenteEn(Inicio));
        Assert.True(tarifa.EstaVigenteEn(new DateOnly(2027, 1, 1)));
        Assert.False(tarifa.EstaVigenteEn(Inicio.AddDays(-1)));
    }

    [Fact]
    public void Una_tarifa_cerrada_no_aplica_despues_de_su_ultimo_dia()
    {
        var tarifa = new Tarifa(TipoDeVehiculo.Carro, FranjaTarifaria.Diurna, 8000, Inicio);

        tarifa.FinalizarVigencia(new DateOnly(2026, 11, 20));

        Assert.True(tarifa.EstaVigenteEn(new DateOnly(2026, 11, 20)));
        Assert.False(tarifa.EstaVigenteEn(new DateOnly(2026, 11, 21)));
    }

    [Fact]
    public void Una_tarifa_no_se_puede_cerrar_antes_de_su_inicio()
    {
        var tarifa = new Tarifa(TipoDeVehiculo.Carro, FranjaTarifaria.Diurna, 8000, Inicio);

        Assert.Throws<ArgumentOutOfRangeException>(() => tarifa.FinalizarVigencia(Inicio.AddDays(-1)));
    }
}
