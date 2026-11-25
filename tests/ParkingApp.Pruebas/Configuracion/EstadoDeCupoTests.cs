using ParkingApp.Aplicacion.Configuracion;

namespace ParkingApp.Pruebas.Configuracion;

// Página Cupos (mockup del #71): verde hasta 80 %, ámbar por encima de 80 % y rojo con "LLENO" al llegar al cupo.
public class EstadoDeCupoTests
{
    [Fact]
    public void Calcula_libres_y_porcentaje_de_la_ocupacion()
    {
        var estado = new EstadoDeCupo(Cupo: 50, Ocupados: 8);

        Assert.Equal(42, estado.Libres);
        Assert.Equal(16, estado.Porcentaje);
        Assert.Equal(0, estado.Excedente);
        Assert.Equal(NivelDeOcupacion.Normal, estado.Nivel);
    }

    [Theory]
    [InlineData(40, NivelDeOcupacion.Normal)]
    [InlineData(41, NivelDeOcupacion.Alto)]
    [InlineData(49, NivelDeOcupacion.Alto)]
    [InlineData(50, NivelDeOcupacion.Lleno)]
    public void El_nivel_sigue_la_leyenda_del_mockup(int ocupados, NivelDeOcupacion esperado)
    {
        // Con cupo 50: 40 es 80 % (todavía verde), 41 es 82 % (ámbar) y 50 llega al cupo (LLENO).
        Assert.Equal(esperado, new EstadoDeCupo(50, ocupados).Nivel);
    }

    [Fact]
    public void Sin_cupos_libres_esta_lleno_al_100_por_ciento()
    {
        var estado = new EstadoDeCupo(Cupo: 30, Ocupados: 30);

        Assert.Equal(0, estado.Libres);
        Assert.Equal(100, estado.Porcentaje);
        Assert.Equal(NivelDeOcupacion.Lleno, estado.Nivel);
    }

    [Fact]
    public void Si_la_ocupacion_supera_el_cupo_no_hay_libres_negativos_y_se_indica_el_excedente()
    {
        // Pasa si el administrador baja el cupo con vehículos adentro.
        var estado = new EstadoDeCupo(Cupo: 10, Ocupados: 13);

        Assert.Equal(0, estado.Libres);
        Assert.Equal(3, estado.Excedente);
        Assert.Equal(100, estado.Porcentaje);
        Assert.Equal(NivelDeOcupacion.Lleno, estado.Nivel);
    }

    [Fact]
    public void Un_cupo_de_cero_cuenta_como_lleno()
    {
        // Igual que en el carril: con cupo 0 no entra ningún vehículo de ese tipo.
        var estado = new EstadoDeCupo(Cupo: 0, Ocupados: 0);

        Assert.Equal(100, estado.Porcentaje);
        Assert.Equal(NivelDeOcupacion.Lleno, estado.Nivel);
    }

    [Fact]
    public void El_porcentaje_se_redondea_hacia_abajo_para_no_marcar_100_antes_de_llenarse()
    {
        Assert.Equal(99, new EstadoDeCupo(200, 199).Porcentaje);
    }
}
