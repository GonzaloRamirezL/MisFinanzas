using System.Text;
using MisFinanzas.Core.Calculos;
using MisFinanzas.Core.Modelos;

namespace MisFinanzas.Core.Exportacion;

/// <summary>
/// Genera reportes .md pensados para dos lectores: tú (se ven bien en cualquier visor de Markdown)
/// y una IA (encabezado con metadatos, tablas con columnas fijas, montos en un solo formato).
/// Puedes subir varios meses a un chat o a un Drive y pedir análisis tipo "Power BI".
/// </summary>
public class ExportadorMarkdown(Func<DateTime>? reloj = null)
{
    private readonly Func<DateTime> _ahora = reloj ?? (() => DateTime.Now);

    public const string VersionFormato = "1";

    /// <summary>Nombre sugerido para el archivo de un mes, ej: "finanzas_2026-11.md".</summary>
    public static string NombreArchivoMes(Periodo periodo) => $"finanzas_{periodo.Iso}.md";

    /// <summary>Nombre sugerido para el historial, ej: "finanzas_historial_2026-01_a_2026-11.md".</summary>
    public static string NombreArchivoHistorial(ResumenGeneral general) =>
        general.Meses.Count == 0
            ? "finanzas_historial.md"
            : $"finanzas_historial_{general.Meses[0].Periodo.Iso}_a_{general.Meses[^1].Periodo.Iso}.md";

    // ─────────────────────────── Reporte de un mes ───────────────────────────

    public string ExportarMes(
        ResumenMes resumen,
        IEnumerable<Movimiento> movimientos,
        IEnumerable<Categoria> categorias,
        Ajustes ajustes)
    {
        var cats = categorias.ToDictionary(c => c.Id);
        var movs = movimientos.Where(m => !m.Omitido).ToList();
        var sb = new StringBuilder();

        EscribirMetadatos(sb, ajustes, ("tipo", "reporte_mensual"), ("periodo", resumen.Periodo.Iso));

        sb.AppendLine($"# Finanzas · {resumen.Periodo.NombreLargo}");
        sb.AppendLine();
        EscribirNotaParaIa(sb, ajustes);

        sb.AppendLine("## Resumen del mes");
        sb.AppendLine();
        Tabla(sb, ["Indicador", "Valor"],
        [
            ["Sueldo", Formato.Pesos(resumen.Sueldo)],
            ["Me pagaron (otros ingresos)", Formato.Pesos(resumen.PagosRecibidos)],
            ["**Ingresos totales**", $"**{Formato.Pesos(resumen.Ingresos)}**"],
            ["Cuentas fijas", Formato.Pesos(resumen.CuentasFijas)],
            ["Deudas y gastos del mes", Formato.Pesos(resumen.Deudas)],
            ["Compras", Formato.Pesos(resumen.Compras)],
            ["**Total gastos**", $"**{Formato.Pesos(resumen.TotalGastos)}**"],
            ["**Me queda**", $"**{Formato.Pesos(resumen.MeQueda)}**"],
            ["Por pagar aún", Formato.Pesos(resumen.PorPagar)],
            ["% del ingreso gastado", Formato.Porcentaje(resumen.PorcentajeGastado)],
        ]);

        EscribirAlertas(sb, resumen, ajustes);

        sb.AppendLine("## ¿En qué se fue? (por categoría)");
        sb.AppendLine();
        Tabla(sb, ["Categoría", "Gastado", "Presupuesto", "% usado", "Estado"],
            resumen.PorCategoria.Select(c => new[]
            {
                $"{c.Emoji} {c.Nombre}",
                Formato.Pesos(c.Gastado),
                c.Presupuesto is null ? "–" : Formato.Pesos(c.Presupuesto.Value),
                c.PorcentajeUsado is null ? "–" : Formato.Porcentaje(c.PorcentajeUsado.Value),
                TextoAlerta(c.Alerta),
            }));

        string NombreCat(int? id) => id is not null && cats.TryGetValue(id.Value, out var c) ? c.Nombre : "–";
        List<Movimiento> DeTipo(TipoMovimiento t) => movs.Where(m => m.Tipo == t).OrderBy(m => m.Fecha).ThenBy(m => m.Id).ToList();

        EscribirSeccion(sb, "Cuentas fijas", DeTipo(TipoMovimiento.CuentaFija),
            ["Concepto", "Categoría", "Monto", "Estado", "Permanente"],
            m => [m.Concepto, NombreCat(m.CategoriaId), Formato.Pesos(m.Monto), m.Estado.ToString(), m.GastoFijoId is null ? "no" : "sí"]);

        EscribirSeccion(sb, "Deudas y gastos del mes", DeTipo(TipoMovimiento.Deuda),
            ["Concepto", "Categoría", "Monto", "Estado", "Nota"],
            m => [m.Concepto, NombreCat(m.CategoriaId), Formato.Pesos(m.Monto), m.Estado.ToString(), m.Nota ?? ""]);

        EscribirSeccion(sb, "Compras", DeTipo(TipoMovimiento.Compra),
            ["Fecha", "Detalle", "Categoría", "Monto"],
            m => [Formato.Fecha(m.Fecha), m.Concepto, NombreCat(m.CategoriaId), Formato.Pesos(m.Monto)]);

        EscribirSeccion(sb, "Pagos que me hicieron", DeTipo(TipoMovimiento.PagoRecibido),
            ["Fecha", "Detalle", "De quién", "Monto"],
            m => [Formato.Fecha(m.Fecha), m.Concepto, m.DeQuien ?? "–", Formato.Pesos(m.Monto)]);

        return sb.ToString();
    }

    // ─────────────────────────── Historial (varios meses) ───────────────────────────

    public string ExportarHistorial(
        ResumenGeneral general,
        IEnumerable<Categoria> categorias,
        Ajustes ajustes)
    {
        var sb = new StringBuilder();
        var meses = general.Meses;

        EscribirMetadatos(sb, ajustes,
            ("tipo", "historial"),
            ("desde", meses.Count > 0 ? meses[0].Periodo.Iso : "–"),
            ("hasta", meses.Count > 0 ? meses[^1].Periodo.Iso : "–"),
            ("meses", meses.Count.ToString()));

        sb.AppendLine("# Historial de finanzas");
        sb.AppendLine();
        EscribirNotaParaIa(sb, ajustes);

        if (meses.Count == 0)
        {
            sb.AppendLine("_Todavía no hay meses registrados._");
            return sb.ToString();
        }

        sb.AppendLine("## Resumen general");
        sb.AppendLine();
        Tabla(sb, ["Indicador", "Valor"],
        [
            ["Ingresos acumulados", Formato.Pesos(general.TotalIngresos)],
            ["Gastos acumulados", Formato.Pesos(general.TotalGastos)],
            ["Ahorrado (suma de lo que quedó)", Formato.Pesos(general.TotalAhorrado)],
            ["Gasto promedio mensual", Formato.Pesos(general.GastoPromedio)],
            ["Mes más caro", general.MesMasCaro is { } caro ? $"{caro.Periodo.Iso} ({Formato.Pesos(caro.TotalGastos)})" : "–"],
        ]);

        sb.AppendLine("## Mes a mes");
        sb.AppendLine();
        Tabla(sb,
            ["Mes", "Sueldo", "Me pagaron", "Ingresos", "Cuentas fijas", "Deudas", "Compras", "Total gastos", "Me quedó", "% gastado", "Por pagar"],
            meses.Select(m => new[]
            {
                m.Periodo.Iso, Formato.Pesos(m.Sueldo), Formato.Pesos(m.PagosRecibidos), Formato.Pesos(m.Ingresos),
                Formato.Pesos(m.CuentasFijas), Formato.Pesos(m.Deudas), Formato.Pesos(m.Compras),
                Formato.Pesos(m.TotalGastos), Formato.Pesos(m.MeQueda), Formato.Porcentaje(m.PorcentajeGastado),
                Formato.Pesos(m.PorPagar),
            }));

        // Matriz categoría × mes: lo más útil para que una IA detecte tendencias.
        var categoriasConGasto = categorias
            .Where(c => meses.Any(m => m.PorCategoria.Any(pc => pc.CategoriaId == c.Id && pc.Gastado > 0)))
            .OrderBy(c => c.Orden).ThenBy(c => c.Nombre)
            .ToList();

        sb.AppendLine("## Gasto por categoría y mes");
        sb.AppendLine();
        if (categoriasConGasto.Count == 0)
        {
            sb.AppendLine("_Sin gastos registrados._");
            sb.AppendLine();
        }
        else
        {
            var encabezados = new List<string> { "Categoría" };
            encabezados.AddRange(meses.Select(m => m.Periodo.Iso));
            encabezados.AddRange(["Total", "Promedio"]);

            Tabla(sb, encabezados, categoriasConGasto.Select(c =>
            {
                var valores = meses
                    .Select(m => m.PorCategoria.FirstOrDefault(pc => pc.CategoriaId == c.Id)?.Gastado ?? 0)
                    .ToList();
                var fila = new List<string> { $"{c.Emoji} {c.Nombre}" };
                fila.AddRange(valores.Select(Formato.Pesos));
                fila.Add(Formato.Pesos(valores.Sum()));
                fila.Add(Formato.Pesos(valores.Average()));
                return fila.ToArray();
            }));
        }

        var alertas = meses
            .SelectMany(m => DescribirAlertas(m, ajustes).Select(a => $"- **{m.Periodo.Iso}:** {a}"))
            .ToList();
        sb.AppendLine("## Alertas registradas");
        sb.AppendLine();
        if (alertas.Count == 0) sb.AppendLine("_Ninguna._");
        else foreach (var a in alertas) sb.AppendLine(a);
        sb.AppendLine();

        return sb.ToString();
    }

    // ─────────────────────────── Piezas comunes ───────────────────────────

    private void EscribirMetadatos(StringBuilder sb, Ajustes ajustes, params (string Clave, string Valor)[] extra)
    {
        sb.AppendLine("---");
        foreach (var (clave, valor) in extra) sb.AppendLine($"{clave}: {valor}");
        sb.AppendLine($"moneda: {ajustes.Moneda}");
        sb.AppendLine($"generado: {_ahora():yyyy-MM-ddTHH:mm}");
        sb.AppendLine("app: Mis Finanzas");
        sb.AppendLine($"formato_version: {VersionFormato}");
        sb.AppendLine("---");
        sb.AppendLine();
    }

    private static void EscribirNotaParaIa(StringBuilder sb, Ajustes ajustes)
    {
        sb.AppendLine($"> Reporte exportado desde la app Mis Finanzas. Montos en {ajustes.Moneda} " +
                      "(punto = separador de miles). \"Me queda\" = ingresos − gastos. " +
                      "\"Por pagar\" = cuentas fijas y deudas aún pendientes (ya incluidas en los gastos). " +
                      "Las compras se consideran pagadas.");
        sb.AppendLine();
    }

    private static void EscribirAlertas(StringBuilder sb, ResumenMes resumen, Ajustes ajustes)
    {
        var alertas = DescribirAlertas(resumen, ajustes).ToList();
        sb.AppendLine("## Alertas");
        sb.AppendLine();
        if (alertas.Count == 0) sb.AppendLine("_Ninguna._ ✅");
        else foreach (var a in alertas) sb.AppendLine($"- {a}");
        sb.AppendLine();
    }

    private static IEnumerable<string> DescribirAlertas(ResumenMes r, Ajustes ajustes)
    {
        if (r.SuperaUmbralGasto)
            yield return $"Gasto del {Formato.Porcentaje(r.PorcentajeGastado)} del ingreso (límite configurado: {Formato.Porcentaje(ajustes.UmbralGastoIngreso)}).";
        if (r.MeQueda < 0)
            yield return $"Gastos mayores que ingresos: faltan {Formato.Pesos(-r.MeQueda)}.";
        foreach (var c in r.PorCategoria.Where(c => c.Alerta is AlertaPresupuesto.MePase or AlertaPresupuesto.AlLimite))
            yield return $"{c.Nombre}: {TextoAlerta(c.Alerta).ToLowerInvariant()} ({Formato.Pesos(c.Gastado)} de {Formato.Pesos(c.Presupuesto!.Value)}).";
        foreach (var s in r.CuentasQueSubieron)
            yield return $"{s.Concepto} subió {Formato.Porcentaje(s.Variacion)} ({Formato.Pesos(s.MontoAnterior)} → {Formato.Pesos(s.MontoActual)}).";
    }

    private static string TextoAlerta(AlertaPresupuesto alerta) => alerta switch
    {
        AlertaPresupuesto.MePase => "Me pasé",
        AlertaPresupuesto.AlLimite => "Al límite",
        AlertaPresupuesto.Ok => "OK",
        _ => "Sin presupuesto",
    };

    private static void EscribirSeccion(StringBuilder sb, string titulo, List<Movimiento> movs,
        string[] encabezados, Func<Movimiento, string[]> fila)
    {
        sb.AppendLine($"## {titulo}");
        sb.AppendLine();
        if (movs.Count == 0)
        {
            sb.AppendLine("_Sin registros._");
            sb.AppendLine();
            return;
        }
        Tabla(sb, encabezados, movs.Select(fila));
    }

    private static void Tabla(StringBuilder sb, IReadOnlyList<string> encabezados, IEnumerable<string[]> filas)
    {
        sb.AppendLine("| " + string.Join(" | ", encabezados.Select(Escapar)) + " |");
        sb.AppendLine("|" + string.Concat(encabezados.Select(_ => " --- |")));
        foreach (var f in filas)
            sb.AppendLine("| " + string.Join(" | ", f.Select(Escapar)) + " |");
        sb.AppendLine();
    }

    private static string Escapar(string texto) =>
        texto.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
}
