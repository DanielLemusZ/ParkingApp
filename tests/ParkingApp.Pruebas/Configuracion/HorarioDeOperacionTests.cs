using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Pruebas.Configuracion;

public class HorarioDeOperacionTests
{
    private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);
    private readonly HorarioDeOperacion horario = HorarioDeOperacion.Inicial;

    [Fact]
    public void A_las_0600_empieza_un_nuevo_dia_operativo()
    {
        // Jueves 19 de noviembre de 2026, hora de Bogotá.
        var antesDeLas6 = new DateTimeOffset(2026, 11, 19, 5, 59, 0, Bogota);
        var alasSeis = new DateTimeOffset(2026, 11, 19, 6, 0, 0, Bogota);

        Assert.Equal(new DateOnly(2026, 11, 18), HorarioDeOperacion.DiaOperativo(antesDeLas6));
        Assert.Equal(new DateOnly(2026, 11, 19), HorarioDeOperacion.DiaOperativo(alasSeis));
    }

    [Fact]
    public void Despues_de_medianoche_sigue_el_dia_operativo_anterior()
    {
        var entrada = new DateTimeOffset(2026, 11, 16, 23, 0, 0, Bogota);
        var salida = new DateTimeOffset(2026, 11, 17, 1, 0, 0, Bogota);

        Assert.Equal(HorarioDeOperacion.DiaOperativo(entrada), HorarioDeOperacion.DiaOperativo(salida));
    }

    [Theory]
    [InlineData(6, 0, FranjaTarifaria.Diurna)]
    [InlineData(17, 59, FranjaTarifaria.Diurna)]
    [InlineData(18, 0, FranjaTarifaria.Nocturna)]
    [InlineData(23, 59, FranjaTarifaria.Nocturna)]
    [InlineData(0, 0, FranjaTarifaria.Nocturna)]
    [InlineData(5, 59, FranjaTarifaria.Nocturna)]
    public void Las_1759_son_diurnas_y_las_1800_nocturnas(int hora, int minuto, FranjaTarifaria esperada)
    {
        var momento = new DateTimeOffset(2026, 11, 16, hora, minuto, 0, Bogota);

        Assert.Equal(esperada, horario.Franja(momento));
    }

    [Fact]
    public void Un_horario_con_las_dos_horas_iguales_se_rechaza()
    {
        Assert.Throws<ArgumentException>(() => new HorarioDeOperacion(new TimeOnly(6, 0), new TimeOnly(6, 0)));
    }

    [Fact]
    public void Si_la_franja_diurna_empieza_a_las_0700_el_dia_operativo_sigue_empezando_a_las_0600()
    {
        // El administrador mueve las franjas, pero el día operativo no se configura (S-02).
        var horarioCambiado = new HorarioDeOperacion(new TimeOnly(7, 0), new TimeOnly(19, 0));
        var alasSeisYMedia = new DateTimeOffset(2026, 11, 19, 6, 30, 0, Bogota);

        Assert.Equal(new DateOnly(2026, 11, 19), HorarioDeOperacion.DiaOperativo(alasSeisYMedia));
        Assert.Equal(FranjaTarifaria.Nocturna, horarioCambiado.Franja(alasSeisYMedia));
    }
}
