using MisFinanzas.Core.Modelos;

namespace MisFinanzas.Core.Calculos;

public enum AlertaPresupuesto
{
    SinPresupuesto,
    Ok,
    /// <summary>"👀 Al límite" en el Excel.</summary>
    AlLimite,
    /// <summary>"😬 Me pasé" en el Excel.</summary>
    MePase,
}

/// <summary>Una fila de "¿En qué se fue?".</summary>
public record ResumenCategoria(
    int CategoriaId,
    string Nombre,
    string Emoji,
    decimal Gastado,
    decimal? Presupuesto,
    AlertaPresupuesto Alerta)
{
    /// <summary>Gastado / presupuesto (null si no hay presupuesto).</summary>
    public decimal? PorcentajeUsado => Presupuesto is > 0 ? Gastado / Presupuesto : null;
}

/// <summary>Una cuenta fija que subió más del umbral respecto al mes anterior (columna "vs mes ant.").</summary>
public record CuentaQueSubio(string Concepto, decimal MontoAnterior, decimal MontoActual)
{
    public decimal Variacion => MontoAnterior == 0 ? 0 : MontoActual / MontoAnterior - 1;
}

/// <summary>Todo lo que muestra la hoja de un mes en el Excel, ya calculado.</summary>
public record ResumenMes
{
    public required Periodo Periodo { get; init; }
    public decimal Sueldo { get; init; }
    public decimal PagosRecibidos { get; init; }
    public decimal Ingresos => Sueldo + PagosRecibidos;

    public decimal CuentasFijas { get; init; }
    public decimal Deudas { get; init; }
    public decimal Compras { get; init; }
    public decimal TotalGastos => CuentasFijas + Deudas + Compras;

    /// <summary>"✨ Me queda".</summary>
    public decimal MeQueda => Ingresos - TotalGastos;

    /// <summary>"⏳ Por pagar aún": cuentas fijas y deudas que no están pagadas.</summary>
    public decimal PorPagar { get; init; }

    /// <summary>"📊 Gastado del ingreso" (0,85 = 85 %). 0 si no hay ingresos.</summary>
    public decimal PorcentajeGastado => Ingresos == 0 ? 0 : TotalGastos / Ingresos;

    /// <summary>True si el gasto supera el umbral configurado del ingreso.</summary>
    public bool SuperaUmbralGasto { get; init; }

    public IReadOnlyList<ResumenCategoria> PorCategoria { get; init; } = [];
    public IReadOnlyList<CuentaQueSubio> CuentasQueSubieron { get; init; } = [];
}

/// <summary>La hoja "Resumen" del Excel.</summary>
public record ResumenGeneral
{
    public IReadOnlyList<ResumenMes> Meses { get; init; } = [];
    public decimal TotalIngresos => Meses.Sum(m => m.Ingresos);
    public decimal TotalGastos => Meses.Sum(m => m.TotalGastos);
    /// <summary>"🐷 Ahorrado": suma de lo que quedó cada mes.</summary>
    public decimal TotalAhorrado => Meses.Sum(m => m.MeQueda);
    /// <summary>"📆 Gasto promedio".</summary>
    public decimal GastoPromedio => Meses.Count == 0 ? 0 : TotalGastos / Meses.Count;
    /// <summary>"🔥 Mes más caro". null si no hay meses.</summary>
    public ResumenMes? MesMasCaro => Meses.Count == 0 ? null : Meses.MaxBy(m => m.TotalGastos);
}
