using MisFinanzas.Core.Calculos;
using MisFinanzas.Core.Exportacion;
using MisFinanzas.Core.Modelos;
using MisFinanzas.Core.Servicios;

namespace MisFinanzas.Core.Tests;

public class PruebasPeriodo
{
    [Fact]
    public void Codigo_y_clave_coinciden_con_el_excel()
    {
        var p = Periodo.DesdeCodigo("NOV26");
        Assert.Equal(2026, p.Anio);
        Assert.Equal(11, p.NumeroMes);
        Assert.Equal(202611, p.Clave);
        Assert.Equal("NOV26", p.Codigo);
        Assert.Equal("Noviembre 2026", p.NombreLargo);
        Assert.Equal("2026-11", p.Iso);
    }

    [Fact]
    public void Cambio_de_anio_funciona()
    {
        Assert.Equal(new Periodo(2027, 1), new Periodo(2026, 12).Siguiente());
        Assert.Equal(new Periodo(2026, 12), new Periodo(2027, 1).Anterior());
        Assert.Equal(14, Periodo.Rango(new(2026, 11), new(2027, 12)).Count());
    }
}

public class PruebasCalculadora
{
    private static readonly List<Categoria> Cats =
    [
        new() { Id = 1, Nombre = "Servicios", PresupuestoMensual = 100_000 },
        new() { Id = 2, Nombre = "Mascotas", PresupuestoMensual = 400_000 },
        new() { Id = 3, Nombre = "Personal" },
    ];

    [Fact]
    public void Calcula_totales_como_la_hoja_del_mes()
    {
        var mes = new Mes { Clave = 202611, Sueldo = 2_000_000 };
        List<Movimiento> movs =
        [
            new() { Tipo = TipoMovimiento.CuentaFija, Concepto = "Luz", CategoriaId = 1, Monto = 82_900, Estado = EstadoPago.Pagado },
            new() { Tipo = TipoMovimiento.CuentaFija, Concepto = "Agua", CategoriaId = 1, Monto = 30_000, Estado = EstadoPago.Pendiente },
            new() { Tipo = TipoMovimiento.Deuda, Concepto = "Dentista", CategoriaId = 2, Monto = 380_000, Estado = EstadoPago.Pendiente },
            new() { Tipo = TipoMovimiento.Compra, Concepto = "Libro", CategoriaId = 3, Monto = 7_000 },
            new() { Tipo = TipoMovimiento.PagoRecibido, Concepto = "Me devolvió", Monto = 50_000 },
            new() { Tipo = TipoMovimiento.CuentaFija, Concepto = "Omitida", CategoriaId = 1, Monto = 999_999, Omitido = true },
        ];

        var r = Calculadora.CalcularMes(mes, movs, Cats, new Ajustes());

        Assert.Equal(2_050_000, r.Ingresos);
        Assert.Equal(112_900, r.CuentasFijas);
        Assert.Equal(380_000, r.Deudas);
        Assert.Equal(7_000, r.Compras);
        Assert.Equal(499_900, r.TotalGastos);
        Assert.Equal(1_550_100, r.MeQueda);
        Assert.Equal(410_000, r.PorPagar); // agua + dentista
        Assert.False(r.SuperaUmbralGasto);

        var servicios = r.PorCategoria.Single(c => c.Nombre == "Servicios");
        Assert.Equal(AlertaPresupuesto.MePase, servicios.Alerta);       // 112.900 > 100.000
        Assert.Equal(AlertaPresupuesto.AlLimite, r.PorCategoria.Single(c => c.Nombre == "Mascotas").Alerta); // 95 %
        Assert.Equal(AlertaPresupuesto.SinPresupuesto, r.PorCategoria.Single(c => c.Nombre == "Personal").Alerta);
    }

    [Fact]
    public void Detecta_cuentas_que_subieron_mas_del_umbral()
    {
        var anterior = new List<Movimiento>
        {
            new() { Tipo = TipoMovimiento.CuentaFija, Concepto = "Luz", GastoFijoId = 10, Monto = 50_000 },
            new() { Tipo = TipoMovimiento.CuentaFija, Concepto = "Agua", GastoFijoId = 11, Monto = 30_000 },
        };
        var actual = new List<Movimiento>
        {
            new() { Tipo = TipoMovimiento.CuentaFija, Concepto = "Luz", GastoFijoId = 10, Monto = 82_900, CategoriaId = 1 },
            new() { Tipo = TipoMovimiento.CuentaFija, Concepto = "Agua", GastoFijoId = 11, Monto = 33_000, CategoriaId = 1 },
        };

        var r = Calculadora.CalcularMes(new Mes { Clave = 202612, Sueldo = 1 }, actual, Cats, new Ajustes(), anterior);

        var subio = Assert.Single(r.CuentasQueSubieron);
        Assert.Equal("Luz", subio.Concepto); // +65,8 %; el agua subió solo 10 %
    }

    [Fact]
    public void Resumen_general_suma_y_encuentra_mes_mas_caro()
    {
        var a = new ResumenMes { Periodo = new(2026, 11), Sueldo = 1000, Compras = 300 };
        var b = new ResumenMes { Periodo = new(2026, 12), Sueldo = 1000, Compras = 900 };
        var g = Calculadora.CalcularGeneral([b, a]);

        Assert.Equal(2000, g.TotalIngresos);
        Assert.Equal(1200, g.TotalGastos);
        Assert.Equal(800, g.TotalAhorrado);
        Assert.Equal(600, g.GastoPromedio);
        Assert.Equal(new Periodo(2026, 12), g.MesMasCaro!.Periodo);
        Assert.Equal(new Periodo(2026, 11), g.Meses[0].Periodo); // ordenado
    }
}

public class PruebasServicio
{
    private static (ServicioFinanzas, RepositorioEnMemoria) Crear(DateTime hoy)
    {
        var repo = new RepositorioEnMemoria();
        repo.InicializarAsync().Wait();
        return (new ServicioFinanzas(repo, () => hoy), repo);
    }

    [Fact]
    public async Task Abrir_mes_genera_cuentas_fijas_sin_duplicar()
    {
        var (s, repo) = Crear(new DateTime(2026, 11, 12));
        await s.GuardarGastoFijoAsync(new GastoFijo { Concepto = "Luz", CategoriaId = 1, MontoHabitual = 80_000 });
        await s.GuardarGastoFijoAsync(new GastoFijo { Concepto = "Netflix", CategoriaId = 2, MontoHabitual = 14_400 });

        await s.AbrirMesActualAsync();
        await s.AbrirMesActualAsync(); // otra vez: no debe duplicar

        var movs = await s.ObtenerMovimientosAsync(s.PeriodoActual);
        Assert.Equal(2, movs.Count);
        Assert.All(movs, m => Assert.Equal(EstadoPago.Pendiente, m.Estado));
    }

    [Fact]
    public async Task Nuevo_gasto_fijo_aparece_en_el_mes_ya_abierto()
    {
        var (s, _) = Crear(new DateTime(2026, 11, 12));
        await s.AbrirMesActualAsync();
        await s.GuardarGastoFijoAsync(new GastoFijo { Concepto = "Crunchyroll", CategoriaId = 2, MontoHabitual = 5_000 });

        var movs = await s.ObtenerMovimientosAsync(s.PeriodoActual);
        Assert.Contains(movs, m => m.Concepto == "Crunchyroll");
    }

    [Fact]
    public async Task Cambiar_monto_habitual_no_modifica_meses_pasados()
    {
        var (s, _) = Crear(new DateTime(2026, 12, 1));
        var luz = new GastoFijo { Concepto = "Luz", CategoriaId = 1, MontoHabitual = 80_000, DesdeClave = 202611 };
        await s.GuardarGastoFijoAsync(luz);
        await s.AbrirMesAsync(new Periodo(2026, 11));

        luz.MontoHabitual = 95_000;
        await s.GuardarGastoFijoAsync(luz);
        await s.AbrirMesAsync(new Periodo(2026, 12));

        Assert.Equal(80_000, (await s.ObtenerMovimientosAsync(new(2026, 11))).Single().Monto);
        Assert.Equal(95_000, (await s.ObtenerMovimientosAsync(new(2026, 12))).Single().Monto);
    }

    [Fact]
    public async Task Terminar_gasto_fijo_lo_saca_de_meses_siguientes_pendientes()
    {
        var (s, _) = Crear(new DateTime(2026, 11, 12));
        var hbo = new GastoFijo { Concepto = "HBO", CategoriaId = 2, MontoHabitual = 3_500, DesdeClave = 202611 };
        await s.GuardarGastoFijoAsync(hbo);
        await s.AbrirMesAsync(new(2026, 11));
        await s.AbrirMesAsync(new(2026, 12));

        await s.TerminarGastoFijoAsync(hbo, new Periodo(2026, 11));
        await s.AbrirMesAsync(new(2026, 12)); // reabrir no debe recrearlo
        await s.AbrirMesAsync(new(2027, 1));

        Assert.Single(await s.ObtenerMovimientosAsync(new(2026, 11)));
        Assert.Empty(await s.ObtenerMovimientosAsync(new(2026, 12)));
        Assert.Empty(await s.ObtenerMovimientosAsync(new(2027, 1)));
    }

    [Fact]
    public async Task Quitar_cuenta_fija_de_un_mes_no_la_hace_reaparecer()
    {
        var (s, _) = Crear(new DateTime(2026, 11, 12));
        await s.GuardarGastoFijoAsync(new GastoFijo { Concepto = "Luz", CategoriaId = 1, MontoHabitual = 80_000 });
        await s.AbrirMesActualAsync();

        var luz = (await s.ObtenerMovimientosAsync(s.PeriodoActual)).Single();
        await s.QuitarMovimientoAsync(luz);
        await s.AbrirMesActualAsync();

        Assert.Empty(await s.ObtenerMovimientosAsync(s.PeriodoActual));
    }

    [Fact]
    public async Task Categoria_usada_se_archiva_y_nueva_se_borra()
    {
        var (s, repo) = Crear(new DateTime(2026, 11, 12));
        await s.AbrirMesActualAsync();
        var servicios = (await s.ObtenerCategoriasAsync()).First(c => c.Nombre == "Servicios");
        await s.GuardarMovimientoAsync(new Movimiento
        {
            MesClave = s.PeriodoActual.Clave, Tipo = TipoMovimiento.Compra, Concepto = "Ampolleta",
            CategoriaId = servicios.Id, Monto = 3_000,
        });

        Assert.False(await s.QuitarCategoriaAsync(servicios));      // archivada
        Assert.DoesNotContain(await s.ObtenerCategoriasAsync(), c => c.Id == servicios.Id);
        Assert.Contains(await s.ObtenerCategoriasAsync(true), c => c.Id == servicios.Id);

        var nueva = new Categoria { Nombre = "Anime 🎌" };
        await s.GuardarCategoriaAsync(nueva);
        Assert.True(await s.QuitarCategoriaAsync(nueva));           // sin uso: borrada
        Assert.DoesNotContain(repo.Categorias, c => c.Id == nueva.Id);
    }

    [Fact]
    public async Task No_permite_categorias_con_nombre_repetido()
    {
        var (s, _) = Crear(new DateTime(2026, 11, 12));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            s.GuardarCategoriaAsync(new Categoria { Nombre = "mascotas" }));
    }

    [Fact]
    public async Task Mes_nuevo_sugiere_el_sueldo_anterior_y_resumen_ignora_meses_futuros()
    {
        var (s, _) = Crear(new DateTime(2026, 12, 5));
        await s.ActualizarSueldoAsync(new(2026, 11), 2_217_418);
        var dic = await s.AbrirMesAsync(new(2026, 12));
        await s.AbrirMesAsync(new(2027, 1)); // futuro

        Assert.Equal(2_217_418, dic.Sueldo);
        Assert.Equal(2, (await s.ObtenerResumenGeneralAsync()).Meses.Count);
    }
}

public class PruebasExportacion
{
    [Fact]
    public void Formato_chileno()
    {
        Assert.Equal("$2.217.418", Formato.Pesos(2_217_418));
        Assert.Equal("-$1.500", Formato.Pesos(-1_500));
        Assert.Equal("85,3 %", Formato.Porcentaje(0.853m));
    }

    [Fact]
    public async Task Exporta_mes_e_historial_en_markdown()
    {
        var repo = new RepositorioEnMemoria();
        var s = new ServicioFinanzas(repo, () => new DateTime(2026, 11, 12));
        await repo.InicializarAsync();
        await s.ActualizarSueldoAsync(s.PeriodoActual, 1_000_000);
        await s.GuardarMovimientoAsync(new Movimiento
        {
            MesClave = 202611, Tipo = TipoMovimiento.Compra, Concepto = "Figura | edición limitada",
            CategoriaId = 7, Monto = 45_000, Fecha = new DateTime(2026, 11, 3),
        });

        var exp = new ExportadorMarkdown(() => new DateTime(2026, 11, 12, 15, 40, 0));
        var cats = await s.ObtenerCategoriasAsync(true);
        var ajustes = await s.ObtenerAjustesAsync();

        var md = exp.ExportarMes(await s.ObtenerResumenMesAsync(s.PeriodoActual),
            await s.ObtenerMovimientosAsync(s.PeriodoActual), cats, ajustes);

        Assert.StartsWith("---\ntipo: reporte_mensual\nperiodo: 2026-11", md.Replace("\r\n", "\n"));
        Assert.Contains("# Finanzas · Noviembre 2026", md);
        Assert.Contains("| **Me queda** | **$955.000** |", md);
        Assert.Contains("Figura \\| edición limitada", md); // el | no rompe la tabla
        Assert.Equal("finanzas_2026-11.md", ExportadorMarkdown.NombreArchivoMes(s.PeriodoActual));

        var general = await s.ObtenerResumenGeneralAsync();
        var historial = exp.ExportarHistorial(general, cats, ajustes);
        Assert.Contains("tipo: historial", historial);
        Assert.Contains("## Gasto por categoría y mes", historial);
        Assert.Contains("| 🌸 Personal | $45.000 | $45.000 | $45.000 |", historial);
    }
}

public class PruebasAjustes
{
    private static (ServicioFinanzas, RepositorioEnMemoria) Crear(DateTime hoy)
    {
        var repo = new RepositorioEnMemoria();
        repo.InicializarAsync().Wait();
        return (new ServicioFinanzas(repo, () => hoy), repo);
    }

    [Fact]
    public async Task Editar_gasto_fijo_puede_actualizar_el_mes_actual_si_esta_pendiente()
    {
        var (s, _) = Crear(new DateTime(2026, 11, 12));
        var netflix = new GastoFijo { Concepto = "Netflix", CategoriaId = 2, MontoHabitual = 14_400 };
        await s.GuardarGastoFijoAsync(netflix);
        await s.AbrirMesActualAsync();

        netflix.MontoHabitual = 15_900;
        await s.GuardarGastoFijoAsync(netflix); // sin actualizar: el mes queda igual
        Assert.Equal(14_400, (await s.ObtenerMovimientosAsync(s.PeriodoActual)).Single().Monto);

        await s.GuardarGastoFijoAsync(netflix, actualizarMesActual: true);
        Assert.Equal(15_900, (await s.ObtenerMovimientosAsync(s.PeriodoActual)).Single().Monto);
    }

    [Fact]
    public async Task Actualizar_mes_actual_no_toca_cuentas_ya_pagadas()
    {
        var (s, _) = Crear(new DateTime(2026, 11, 12));
        var luz = new GastoFijo { Concepto = "Luz", CategoriaId = 1, MontoHabitual = 80_000 };
        await s.GuardarGastoFijoAsync(luz);
        await s.AbrirMesActualAsync();
        await s.CambiarEstadoAsync((await s.ObtenerMovimientosAsync(s.PeriodoActual)).Single(), EstadoPago.Pagado);

        luz.MontoHabitual = 90_000;
        await s.GuardarGastoFijoAsync(luz, actualizarMesActual: true);
        Assert.Equal(80_000, (await s.ObtenerMovimientosAsync(s.PeriodoActual)).Single().Monto);
    }

    [Fact]
    public async Task Mover_el_desde_hacia_adelante_saca_meses_pendientes_que_quedaron_fuera()
    {
        var (s, _) = Crear(new DateTime(2026, 11, 12));
        var gym = new GastoFijo { Concepto = "Gimnasio", CategoriaId = 7, MontoHabitual = 25_000, DesdeClave = 202611 };
        await s.GuardarGastoFijoAsync(gym);
        await s.AbrirMesAsync(new(2026, 11));
        await s.AbrirMesAsync(new(2026, 12));

        gym.DesdeClave = 202612;
        await s.GuardarGastoFijoAsync(gym);

        Assert.Empty(await s.ObtenerMovimientosAsync(new(2026, 11)));
        Assert.Single(await s.ObtenerMovimientosAsync(new(2026, 12)));
    }

    [Fact]
    public async Task Restaurar_categoria_archivada()
    {
        var (s, _) = Crear(new DateTime(2026, 11, 12));
        var otros = (await s.ObtenerCategoriasAsync()).Single(c => c.Nombre == "Otros");
        await s.GuardarGastoFijoAsync(new GastoFijo { Concepto = "X", CategoriaId = otros.Id, MontoHabitual = 1 });
        Assert.False(await s.QuitarCategoriaAsync(otros));
        await s.RestaurarCategoriaAsync(otros);
        Assert.Contains(await s.ObtenerCategoriasAsync(), c => c.Id == otros.Id);
    }
}

public class PruebasResumen
{
    [Fact]
    public void Gasto_por_categoria_acumula_todos_los_meses()
    {
        static ResumenCategoria Cat(int id, string n, decimal g) =>
            new(id, n, "🏷️", g, null, AlertaPresupuesto.SinPresupuesto);

        var nov = new ResumenMes { Periodo = new(2026, 11), PorCategoria = [Cat(1, "Servicios", 100), Cat(2, "Hogar", 50), Cat(3, "Otros", 0)] };
        var dic = new ResumenMes { Periodo = new(2026, 12), PorCategoria = [Cat(1, "Servicios", 120), Cat(2, "Hogar", 300)] };

        var cats = Calculadora.CalcularGeneral([nov, dic]).GastoPorCategoria;

        Assert.Equal(2, cats.Count);                    // "Otros" sin gasto no aparece
        Assert.Equal("Hogar", cats[0].Nombre);          // ordenado de mayor a menor
        Assert.Equal(350, cats[0].Gastado);
        Assert.Equal(220, cats[1].Gastado);
    }
}

public class PruebasBorradoEnCascada
{
    [Fact]
    public async Task Eliminar_gasto_fijo_lo_borra_de_todos_los_meses()
    {
        var repo = new RepositorioEnMemoria();
        await repo.InicializarAsync();
        var s = new ServicioFinanzas(repo, () => new DateTime(2026, 12, 5));
        var luz = new GastoFijo { Concepto = "Luz", CategoriaId = 1, MontoHabitual = 80_000, DesdeClave = 202610 };
        var agua = new GastoFijo { Concepto = "Agua", CategoriaId = 1, MontoHabitual = 30_000, DesdeClave = 202610 };
        await s.GuardarGastoFijoAsync(luz);
        await s.GuardarGastoFijoAsync(agua);
        foreach (var p in new Periodo[] { new(2026, 10), new(2026, 11), new(2026, 12) })
            await s.AbrirMesAsync(p);

        // Un mes pagado y otro quitado solo de ese mes: igual se borran.
        await s.CambiarEstadoAsync((await s.ObtenerMovimientosAsync(new(2026, 10))).Single(m => m.Concepto == "Luz"), EstadoPago.Pagado);
        await s.QuitarMovimientoAsync((await s.ObtenerMovimientosAsync(new(2026, 11))).Single(m => m.Concepto == "Luz"));

        Assert.Equal(3, await s.ContarMesesDeGastoFijoAsync(luz));
        Assert.Equal(3, await s.EliminarGastoFijoAsync(luz));

        Assert.DoesNotContain(repo.Movimientos, m => m.GastoFijoId == luz.Id);
        Assert.DoesNotContain(repo.GastosFijos, g => g.Id == luz.Id);
        Assert.Equal(3, repo.Movimientos.Count(m => m.GastoFijoId == agua.Id));   // el otro gasto no se toca

        // Reabrir un mes no lo vuelve a crear y el total del mes ya no lo incluye.
        await s.AbrirMesAsync(new(2026, 10));
        var resumen = await s.ObtenerResumenMesAsync(new(2026, 10));
        Assert.Equal(30_000, resumen.CuentasFijas);
    }
}
