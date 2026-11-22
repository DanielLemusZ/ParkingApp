using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Aplicacion.Accesos.Validaciones;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Configuracion;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Accesos;

// Escenarios del issue #16 (F-06, escenario 9): una lectura dudosa no abre y el operador valida o digita la placa.
public class ValidarLecturaTests
{
    private const string Operador = "operador@parkingapp.local";

    // Miércoles 18 de noviembre, 08:00 en Bogotá (UTC-5) = 13:00 UTC.
    private static readonly DateTimeOffset MiercolesALas0800 = new DateTimeOffset(2026, 11, 18, 13, 0, 0, TimeSpan.Zero);

    private readonly CarrilDeEntradaDePrueba entrada;
    private readonly CarrilDeSalidaDePrueba salida;

    public ValidarLecturaTests()
    {
        entrada = new CarrilDeEntradaDePrueba(MiercolesALas0800);
        salida = new CarrilDeSalidaDePrueba(entrada);
    }

    // Pruebas de éxito del issue.

    [Fact]
    public async Task Con_confianza_60_la_entrada_queda_pendiente()
    {
        var resultado = await entrada.IngresarAsync("ABC123", confianza: 60);

        Assert.False(resultado.AbreBarrera);
        Assert.True(resultado.PendienteDeValidacion);
        Assert.Equal(ConfianzaSuficiente.ConfianzaBaja, resultado.Motivo);
        Assert.Empty(entrada.Eventos.Eventos);
        Assert.Empty(entrada.Cobros.Cobros);

        var pendiente = Assert.Single(await ListarAsync());
        Assert.Equal("ABC123", pendiente.Placa);
        Assert.Equal(MotivoDeLecturaPendiente.ConfianzaBaja, pendiente.Motivo);
    }

    [Fact]
    public async Task Al_corregir_a_ABC128_ingresa_como_lectura_manual_con_cobro()
    {
        await entrada.IngresarAsync("ABC123", confianza: 60);

        var resultado = await ValidarLaUltimaAsync(" abc-128 ");

        Assert.True(resultado.Validada);
        Assert.True(resultado.AbreBarrera);

        var ingreso = Assert.Single(entrada.Eventos.Eventos);
        Assert.Equal("ABC128", ingreso.Placa.Valor);
        Assert.Equal(TipoDeVehiculo.Carro, ingreso.TipoDeVehiculo);
        Assert.True(ingreso.EsLecturaManual);
        Assert.Equal(Operador, ingreso.ValidadoPor);
        Assert.Equal(60, ingreso.Confianza);

        var cobro = Assert.Single(entrada.Cobros.Cobros);
        Assert.Equal("ABC128", cobro.Placa.Valor);
        Assert.Equal(8000m, cobro.Valor);
        Assert.Empty(await ListarAsync());
    }

    [Fact]
    public async Task CD1234_al_indicar_carro_ingresa_con_cobro()
    {
        var lectura = await entrada.IngresarAsync("CD1234", confianza: 95);
        Assert.False(lectura.AbreBarrera);
        Assert.Equal(MotivoDeLecturaPendiente.FormatoNoReconocido, Assert.Single(await ListarAsync()).Motivo);

        var resultado = await ValidarLaUltimaAsync("CD1234", TipoDeVehiculo.Carro);

        Assert.True(resultado.AbreBarrera);

        var ingreso = Assert.Single(entrada.Eventos.Eventos);
        Assert.Equal("CD1234", ingreso.Placa.Valor);
        Assert.Equal(TipoDeVehiculo.Carro, ingreso.TipoDeVehiculo);
        Assert.Equal(Operador, ingreso.ValidadoPor);
        Assert.Equal(8000m, Assert.Single(entrada.Cobros.Cobros).Valor);
    }

    [Fact]
    public async Task Al_confirmar_una_salida_se_revisan_los_cobros_pendientes()
    {
        await entrada.IngresarAsync("ABC123");
        entrada.Reloj.Advance(TimeSpan.FromHours(1));

        // Primer orden: confirma antes de que pague. No abre.
        var lectura = await salida.SalirAsync("ABC123", confianza: 60);
        Assert.False(lectura.AbreBarrera);
        Assert.True(lectura.PendienteDeValidacion);

        var conDeuda = await ValidarLaUltimaAsync("ABC123");
        Assert.True(conDeuda.Validada);
        Assert.False(conDeuda.AbreBarrera);
        Assert.Equal("No abre: cobro pendiente de $8.000", conDeuda.Motivo);

        // Segundo orden: la placa se lee otra vez, paga y después confirma. Abre de inmediato.
        await salida.SalirAsync("ABC123", confianza: 60);
        entrada.Cobros.PagarEnEfectivo(Assert.Single(entrada.Cobros.Cobros));

        var pagado = await ValidarLaUltimaAsync("ABC123");
        Assert.True(pagado.AbreBarrera);
        Assert.Equal(RegistrarSalida.AbreConCobroPagado, pagado.Motivo);
        Assert.Equal(TipoDeEvento.Salida, entrada.Eventos.Eventos[1].Tipo);
        Assert.Equal(Operador, entrada.Eventos.Eventos[1].ValidadoPor);
    }

    // Pruebas de error del issue.

    [Theory]
    [InlineData(null)]
    [InlineData(TipoDeVehiculo.NoClasificado)]
    [InlineData((TipoDeVehiculo)7)]
    public async Task Sin_indicar_carro_o_moto_se_rechaza_y_sigue_pendiente(TipoDeVehiculo? tipo)
    {
        await entrada.IngresarAsync("CD1234", confianza: 95);

        var resultado = await ValidarLaUltimaAsync("CD1234", tipo);

        Assert.False(resultado.Validada);
        Assert.Equal("Indica si es carro o moto", resultado.Motivo);
        Assert.Single(await ListarAsync());
        Assert.Empty(entrada.Eventos.Eventos);
    }

    [Theory]
    [InlineData("AB#12")]
    [InlineData("ÑBC123")]
    [InlineData("ABC.123")]
    public async Task Una_placa_con_simbolos_se_rechaza_y_sigue_pendiente(string placa)
    {
        await entrada.IngresarAsync("ABC123", confianza: 60);

        var resultado = await ValidarLaUltimaAsync(placa);

        Assert.False(resultado.Validada);
        Assert.Equal("La placa solo admite letras y números", resultado.Motivo);
        Assert.Single(await ListarAsync());
        Assert.Empty(entrada.Eventos.Eventos);
    }

    [Fact]
    public async Task Una_lectura_ya_validada_no_se_valida_de_nuevo()
    {
        await entrada.IngresarAsync("ABC123", confianza: 60);
        await ValidarLaUltimaAsync("ABC123");

        var segunda = await ValidarLaUltimaAsync("ABC123");

        Assert.False(segunda.Validada);
        Assert.Equal("La lectura ya fue validada", segunda.Motivo);
        Assert.Single(entrada.Eventos.Eventos);
        Assert.Single(entrada.Cobros.Cobros);
    }

    [Fact]
    public async Task Si_otro_operador_valida_primero_no_se_registra_otro_ingreso()
    {
        await entrada.IngresarAsync("ABC123", confianza: 60);
        entrada.Lecturas.OtroOperadorValidoPrimero = true;

        var resultado = await ValidarLaUltimaAsync("ABC123");

        Assert.False(resultado.Validada);
        Assert.Equal(ValidarLectura.LecturaYaValidada, resultado.Motivo);
        Assert.Empty(entrada.Eventos.Eventos);
        Assert.Empty(entrada.Cobros.Cobros);
    }

    [Theory]
    [InlineData("  ", "La placa es obligatoria")]
    [InlineData("ABCDEF12345", "La placa no puede tener más de 10 caracteres")]
    public async Task Una_placa_vacia_o_muy_larga_se_rechaza_y_sigue_pendiente(string placa, string motivo)
    {
        await entrada.IngresarAsync("ABC123", confianza: 60);

        var resultado = await ValidarLaUltimaAsync(placa);

        Assert.False(resultado.Validada);
        Assert.Equal(motivo, resultado.Motivo);
        Assert.Single(await ListarAsync());
    }

    [Fact]
    public async Task Una_lectura_inexistente_o_de_otra_sede_no_se_valida_ni_se_lista()
    {
        var deOtraSede = LecturaPendiente.Registrar(
            TipoDeEvento.Ingreso, MotivoDeLecturaPendiente.ConfianzaBaja, Placa.Crear("ABC123"), Guid.CreateVersion7(), 1, 60, MiercolesALas0800);
        await entrada.Lecturas.AgregarAsync(deOtraSede, TestContext.Current.CancellationToken);

        var ajena = await ValidarAsync(deOtraSede.Id, "ABC123");
        var inexistente = await ValidarAsync(Guid.CreateVersion7(), "ABC123");

        Assert.Equal(ValidarLectura.LecturaNoEncontrada, ajena.Motivo);
        Assert.Equal(ValidarLectura.LecturaNoEncontrada, inexistente.Motivo);
        Assert.Empty(await ListarAsync());
        Assert.Empty(entrada.Eventos.Eventos);
    }

    // Umbral y motivos (criterios 1, 2, 3 y 8).

    [Theory]
    [InlineData(79, true)]
    [InlineData(80, false)]
    public async Task Con_79_queda_pendiente_y_con_80_pasa(int confianza, bool quedaPendiente)
    {
        var ingreso = await entrada.IngresarAsync("ABC123", confianza);
        var salidaDeMoto = await salida.SalirAsync("XYZ12D", confianza);

        Assert.Equal(quedaPendiente, ingreso.PendienteDeValidacion);
        Assert.Equal(!quedaPendiente, ingreso.AbreBarrera);
        Assert.Equal(quedaPendiente, salidaDeMoto.PendienteDeValidacion);
        Assert.Equal(!quedaPendiente, salidaDeMoto.AbreBarrera);

        foreach (var evento in entrada.Eventos.Eventos)
        {
            Assert.False(evento.EsLecturaManual);
        }
    }

    [Fact]
    public async Task El_umbral_configurado_se_respeta()
    {
        entrada.ConfigurarUmbral(90);

        var ingreso = await entrada.IngresarAsync("ABC123", confianza: 85);
        var salidaDeMoto = await salida.SalirAsync("XYZ12D", confianza: 85);

        Assert.True(ingreso.PendienteDeValidacion);
        Assert.True(salidaDeMoto.PendienteDeValidacion);
    }

    [Fact]
    public async Task La_confianza_se_revisa_antes_que_el_cupo()
    {
        entrada.ConfigurarCupos(cupoCarros: 0, cupoMotos: 0);

        var resultado = await entrada.IngresarAsync("ABC123", confianza: 60);

        Assert.True(resultado.PendienteDeValidacion);
        Assert.Equal(ConfianzaSuficiente.ConfianzaBaja, resultado.Motivo);
    }

    [Fact]
    public async Task La_salida_no_exige_formato_reconocido()
    {
        var automatica = await salida.SalirAsync("CD1234", confianza: 95);
        Assert.True(automatica.AbreBarrera);
        Assert.Empty(entrada.Lecturas.Lecturas);

        await salida.SalirAsync("CD5678", confianza: 60);
        var validada = await ValidarLaUltimaAsync("CD5678");

        Assert.True(validada.AbreBarrera);
        Assert.Equal(TipoDeVehiculo.NoClasificado, validada.Salida?.TipoDeVehiculo);
    }

    [Fact]
    public async Task Las_pendientes_muestran_motivo_carril_y_hora_de_la_sede()
    {
        await entrada.IngresarAsync("CD1234", confianza: 95, carril: 2);
        entrada.Reloj.Advance(TimeSpan.FromMinutes(5));
        await salida.SalirAsync("XYZ12D", confianza: 50);

        var pendientes = await ListarAsync();

        Assert.Equal(2, pendientes.Count);
        Assert.Equal(TipoDeEvento.Ingreso, pendientes[0].Tipo);
        Assert.Equal("CD1234", pendientes[0].Placa);
        Assert.Equal(95, pendientes[0].Confianza);
        Assert.Equal(MotivoDeLecturaPendiente.FormatoNoReconocido, pendientes[0].Motivo);
        Assert.Equal(2, pendientes[0].Carril);
        Assert.Equal(new DateTime(2026, 11, 18, 8, 0, 0), pendientes[0].FechaHora.DateTime);
        Assert.Equal(TimeSpan.FromHours(-5), pendientes[0].FechaHora.Offset);
        Assert.Equal(TipoDeEvento.Salida, pendientes[1].Tipo);
        Assert.Equal(MotivoDeLecturaPendiente.ConfianzaBaja, pendientes[1].Motivo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0x1B)]
    [InlineData(0xD800)]
    public async Task Una_lectura_con_caracteres_que_no_se_pueden_guardar_se_rechaza(int codigo)
    {
        // PostgreSQL no guarda el carácter nulo ni la mitad de un carácter especial.
        var placa = "AB" + (char)codigo + "12";

        var ingreso = await entrada.IngresarAsync(placa, confianza: 40);
        var salidaLeida = await salida.SalirAsync(placa, confianza: 40);

        Assert.Equal(LecturaValida.CaracteresNoValidos, ingreso.Motivo);
        Assert.Equal(LecturaValida.CaracteresNoValidos, salidaLeida.Motivo);
        Assert.Empty(entrada.Lecturas.Lecturas);
    }

    // Flujo después de validar (criterios 5 y 6).

    [Fact]
    public async Task El_formato_reconocido_manda_sobre_el_tipo_elegido()
    {
        await entrada.IngresarAsync("CD1234", confianza: 95);

        var resultado = await ValidarLaUltimaAsync("ABC123", TipoDeVehiculo.Moto);

        Assert.True(resultado.AbreBarrera);
        Assert.Equal(TipoDeVehiculo.Carro, Assert.Single(entrada.Eventos.Eventos).TipoDeVehiculo);
        Assert.Single(entrada.Cobros.Cobros);
    }

    [Fact]
    public async Task Una_placa_no_reconocida_que_el_operador_indica_como_moto_ocupa_cupo_de_moto_sin_cobro()
    {
        await entrada.IngresarAsync("ABC12", confianza: 95);

        var resultado = await ValidarLaUltimaAsync("ABC12", TipoDeVehiculo.Moto);

        Assert.True(resultado.AbreBarrera);
        Assert.Empty(entrada.Cobros.Cobros);
        Assert.Equal(1, (await entrada.Sedes.ConsultarOcupacionAsync(TestContext.Current.CancellationToken)).Motos);
    }

    [Fact]
    public async Task Una_lectura_pendiente_no_ocupa_cupo()
    {
        entrada.ConfigurarCupos(cupoCarros: 1, cupoMotos: 1);
        await entrada.IngresarAsync("ABC123", confianza: 60);

        var otro = await entrada.IngresarAsync("DEF456");

        Assert.True(otro.AbreBarrera);
    }

    [Fact]
    public async Task El_ingreso_usa_la_hora_de_la_confirmacion()
    {
        // Se lee a las 05:59 (día operativo del martes, nocturna) y se confirma a las 06:01 (miércoles, diurna).
        var madrugada = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 18, 10, 59, 0, TimeSpan.Zero));
        await madrugada.IngresarAsync("ABC123", confianza: 60);
        madrugada.Reloj.Advance(TimeSpan.FromMinutes(2));

        var resultado = await CrearValidarLectura(madrugada).EjecutarAsync(
            madrugada.Lecturas.Lecturas[0].Id, "ABC123", null, Operador, TestContext.Current.CancellationToken);

        Assert.True(resultado.AbreBarrera);
        Assert.Equal(madrugada.Reloj.GetUtcNow(), Assert.Single(madrugada.Eventos.Eventos).FechaHora);

        var cobro = Assert.Single(madrugada.Cobros.Cobros);
        Assert.Equal(new DateOnly(2026, 11, 18), cobro.DiaOperativo);
        Assert.Equal(FranjaTarifaria.Diurna, cobro.Franja);
    }

    // Si la barrera no puede abrir al confirmar, la lectura queda cerrada.

    [Fact]
    public async Task Si_al_confirmar_esta_lleno_se_cierra_sin_ingreso()
    {
        entrada.ConfigurarCupos(cupoCarros: 0, cupoMotos: 1);
        await entrada.IngresarAsync("CD1234", confianza: 95);

        var resultado = await ValidarLaUltimaAsync("CD1234", TipoDeVehiculo.Carro);

        Assert.True(resultado.Validada);
        Assert.False(resultado.AbreBarrera);
        Assert.Equal("LLENO: carros", resultado.Motivo);
        Assert.Empty(await ListarAsync());
        Assert.Empty(entrada.Eventos.Eventos);
        Assert.Empty(entrada.Cobros.Cobros);

        var cerrada = Assert.Single(entrada.Lecturas.Lecturas);
        Assert.Equal(EstadoDeLectura.Validada, cerrada.Estado);
        Assert.Equal(Operador, cerrada.ValidadaPor);
        Assert.Equal("LLENO: carros", cerrada.Resultado);
    }

    [Fact]
    public async Task Si_al_confirmar_hay_deuda_se_cierra_sin_salida()
    {
        await entrada.IngresarAsync("ABC123");
        await salida.SalirAsync("ABC123", confianza: 60);

        var resultado = await ValidarLaUltimaAsync("ABC123");

        Assert.True(resultado.Validada);
        Assert.False(resultado.AbreBarrera);
        Assert.Empty(await ListarAsync());
        Assert.Single(entrada.Eventos.Eventos);
        Assert.Equal("No abre: cobro pendiente de $8.000", Assert.Single(entrada.Lecturas.Lecturas).Resultado);

        // La deuda sigue. Cuando paga, se vuelve a leer en el carril y abre.
        var cobro = Assert.Single(entrada.Cobros.Cobros);
        Assert.Equal(EstadoDeCobro.Pendiente, cobro.Estado);
        entrada.Cobros.PagarEnEfectivo(cobro);
        Assert.True((await salida.SalirAsync("ABC123")).AbreBarrera);
    }

    // Reglas propias de la lectura en el dominio.

    [Fact]
    public void Solo_una_lectura_pendiente_se_puede_validar()
    {
        var lectura = LecturaPendiente.Registrar(
            TipoDeEvento.Ingreso, MotivoDeLecturaPendiente.ConfianzaBaja, Placa.Crear("ABC123"), entrada.Sede.Id, 1, 60, MiercolesALas0800);
        lectura.Validar(Placa.Crear("ABC123"), Operador, MiercolesALas0800);

        Assert.Throws<InvalidOperationException>(() => lectura.Validar(Placa.Crear("ABC128"), Operador, MiercolesALas0800));
    }

    [Fact]
    public void Una_salida_no_queda_pendiente_por_formato()
    {
        Assert.Throws<ArgumentException>(() => LecturaPendiente.Registrar(
            TipoDeEvento.Salida, MotivoDeLecturaPendiente.FormatoNoReconocido, Placa.Crear("CD1234"), entrada.Sede.Id, 1, 95, MiercolesALas0800));
    }

    private Task<ResultadoDeValidarLectura> ValidarLaUltimaAsync(string placa, TipoDeVehiculo? tipo = null)
    {
        var lecturas = entrada.Lecturas.Lecturas;
        return ValidarAsync(lecturas[lecturas.Count - 1].Id, placa, tipo);
    }

    private Task<ResultadoDeValidarLectura> ValidarAsync(Guid id, string placa, TipoDeVehiculo? tipo = null)
    {
        return CrearValidarLectura(entrada).EjecutarAsync(id, placa, tipo, Operador, TestContext.Current.CancellationToken);
    }

    private Task<IReadOnlyList<LecturaPorRevisar>> ListarAsync()
    {
        var listar = new ListarLecturasPendientes(entrada.Lecturas, entrada.Sede);
        return listar.EjecutarAsync(TestContext.Current.CancellationToken);
    }

    private static ValidarLectura CrearValidarLectura(CarrilDeEntradaDePrueba carril)
    {
        var registrarSalida = new CarrilDeSalidaDePrueba(carril).CrearCasoDeUso();
        return new ValidarLectura(carril.Lecturas, carril.CrearCasoDeUso(), registrarSalida, carril.Sede, carril.Reloj);
    }
}
