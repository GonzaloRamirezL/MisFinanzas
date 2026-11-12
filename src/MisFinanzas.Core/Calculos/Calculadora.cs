using MisFinanzas.Core.Modelos;

namespace MisFinanzas.Core.Calculos;

/// <summary>
/// Reemplaza todas las fórmulas del Excel. Es código puro: recibe datos y devuelve resultados,
/// sin tocar la base de datos. Por eso es fácil de probar.
/// </summary>
public static class Calculadora
{
    public static ResumenMes CalcularMes(
        Mes mes,
        IEnumerable<Movimiento> movimientos,
        IEnumerable<Categoria> categorias,
        Ajustes ajustes,
        IEnumerable<Movimiento>? movimientosMesAnterior = null)
    {
        var vigentes = movimientos.Where(m => !m.Omitido).ToList();

        decimal Suma(TipoMovimiento tipo) => vigentes.Where(m => m.Tipo == tipo).Sum(m => m.Monto);

        var cuentasFijas = Suma(TipoMovimiento.CuentaFija);
        var deudas = Suma(TipoMovimiento.Deuda);
        var compras = Suma(TipoMovimiento.Compra);
        var pagosRecibidos = Suma(TipoMovimiento.PagoRecibido);

        // Excel: SUMIFS(... estado <> "Pagado") sobre cuentas fijas y deudas. Las compras se asumen pagadas.
        var porPagar = vigentes
            .Where(m => m.Tipo is TipoMovimiento.CuentaFija or TipoMovimiento.Deuda && m.Estado != EstadoPago.Pagado)
            .Sum(m => m.Monto);

        var ingresos = mes.Sueldo + pagosRecibidos;
        var totalGastos = cuentasFijas + deudas + compras;

        return new ResumenMes
        {
            Periodo = mes.Periodo,
            Sueldo = mes.Sueldo,
            PagosRecibidos = pagosRecibidos,
            CuentasFijas = cuentasFijas,
            Deudas = deudas,
            Compras = compras,
            PorPagar = porPagar,
            SuperaUmbralGasto = ingresos > 0 && totalGastos / ingresos > ajustes.UmbralGastoIngreso,
            PorCategoria = CalcularPorCategoria(vigentes, categorias, ajustes),
            CuentasQueSubieron = movimientosMesAnterior is null
                ? []
                : CalcularCuentasQueSubieron(vigentes, movimientosMesAnterior, ajustes),
        };
    }

    public static ResumenGeneral CalcularGeneral(IEnumerable<ResumenMes> meses) =>
        new() { Meses = meses.OrderBy(m => m.Periodo).ToList() };

    /// <summary>" ¿En qué se fue?": gasto por categoría vs presupuesto.</summary>
    private static List<ResumenCategoria> CalcularPorCategoria(
        List<Movimiento> vigentes, IEnumerable<Categoria> categorias, Ajustes ajustes)
    {
        var gastoPorCategoria = vigentes
            .Where(m => m.EsGasto && m.CategoriaId is not null)
            .GroupBy(m => m.CategoriaId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Monto));

        return categorias
            // Se muestran las activas, y también las archivadas si tuvieron gasto este mes.
            .Where(c => c.Activa || gastoPorCategoria.ContainsKey(c.Id))
            .OrderBy(c => c.Orden).ThenBy(c => c.Nombre)
            .Select(c =>
            {
                var gastado = gastoPorCategoria.GetValueOrDefault(c.Id);
                return new ResumenCategoria(c.Id, c.Nombre, c.Emoji, gastado, c.PresupuestoMensual,
                    EvaluarPresupuesto(gastado, c.PresupuestoMensual, ajustes.UmbralAlLimiteCategoria));
            })
            .ToList();
    }

    public static AlertaPresupuesto EvaluarPresupuesto(decimal gastado, decimal? presupuesto, decimal umbralAlLimite)
    {
        if (presupuesto is null) return AlertaPresupuesto.SinPresupuesto;
        if (gastado > presupuesto) return AlertaPresupuesto.MePase;
        if (gastado > umbralAlLimite * presupuesto) return AlertaPresupuesto.AlLimite;
        return AlertaPresupuesto.Ok;
    }

    /// <summary>
    /// Compara cada cuenta fija con la del mes anterior. Primero empareja por gasto permanente;
    /// si la cuenta se agregó a mano, empareja por nombre (sin importar mayúsculas).
    /// </summary>
    private static List<CuentaQueSubio> CalcularCuentasQueSubieron(
        List<Movimiento> actuales, IEnumerable<Movimiento> anteriores, Ajustes ajustes)
    {
        var fijasAnteriores = anteriores
            .Where(m => !m.Omitido && m.Tipo == TipoMovimiento.CuentaFija)
            .ToList();

        var resultado = new List<CuentaQueSubio>();
        foreach (var actual in actuales.Where(m => m.Tipo == TipoMovimiento.CuentaFija))
        {
            var anterior = actual.GastoFijoId is not null
                ? fijasAnteriores.FirstOrDefault(a => a.GastoFijoId == actual.GastoFijoId)
                : fijasAnteriores.FirstOrDefault(a =>
                    string.Equals(a.Concepto.Trim(), actual.Concepto.Trim(), StringComparison.OrdinalIgnoreCase));

            if (anterior is null || anterior.Monto <= 0) continue;

            if (actual.Monto / anterior.Monto - 1 > ajustes.UmbralSubidaCuenta)
                resultado.Add(new CuentaQueSubio(actual.Concepto, anterior.Monto, actual.Monto));
        }
        return resultado;
    }
}
