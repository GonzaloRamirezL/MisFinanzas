using System.Windows.Input;
using MisFinanzas.App.Servicios;
using MisFinanzas.App.Utilidades;
using MisFinanzas.Core.Calculos;
using MisFinanzas.Core.Exportacion;
using MisFinanzas.Core.Modelos;
using MisFinanzas.Core.Servicios;

namespace MisFinanzas.App.ViewModels;

/// <summary>Período que abarca el resumen.</summary>
public enum RangoResumen
{
    SeisMeses,
    DoceMeses,
    Todo,
}

/// <summary>
/// Una fila de "Mes a mes" con dos barras (ingresos y gastos).
/// Los anchos se calculan en píxeles a partir del ancho disponible en pantalla.
/// </summary>
public class MesResumenItem(ResumenMes r, decimal maximo, double anchoDisponible)
{
    public string Titulo { get; } = r.Periodo.NombreLargo;
    public string IngresosTexto { get; } = Formato.Pesos(r.Ingresos);
    public string GastosTexto { get; } = Formato.Pesos(r.TotalGastos);
    public string MeQuedoTexto { get; } = Formato.Pesos(r.MeQueda);
    public bool MeQuedoNegativo { get; } = r.MeQueda < 0;
    public string PorcentajeTexto { get; } = $"{Formato.Porcentaje(r.PorcentajeGastado)} gastado";
    public double AnchoIngresos { get; } = Barras.Ancho(r.Ingresos, maximo, anchoDisponible);
    public double AnchoGastos { get; } = Barras.Ancho(r.TotalGastos, maximo, anchoDisponible);
}

/// <summary>Una fila de "¿En qué se fue? (acumulado)".</summary>
public class CategoriaTotalItem(ResumenCategoria c, decimal totalGastos, decimal maximo, double anchoDisponible)
{
    public string Emoji { get; } = c.Emoji;
    public string Nombre { get; } = c.Nombre;
    public string TotalTexto { get; } = Formato.Pesos(c.Gastado);
    public string PorcentajeTexto { get; } = totalGastos == 0 ? "" : $"{Formato.Porcentaje(c.Gastado / totalGastos)} del gasto";
    public double Ancho { get; } = Barras.Ancho(c.Gastado, maximo, anchoDisponible);
}

internal static class Barras
{
    /// <summary>Ancho proporcional al máximo. Un valor mayor que cero siempre se ve (mínimo 6 px).</summary>
    public static double Ancho(decimal valor, decimal maximo, double disponible)
    {
        if (valor <= 0 || maximo <= 0 || disponible <= 0) return 0;
        return Math.Max(6, disponible * (double)(valor / maximo));
    }
}

/// <summary>Pestaña Resumen (la hoja "Resumen" del Excel, con gráfico y exportación del historial).</summary>
public class ResumenViewModel : Observable
{
    private readonly ServicioFinanzas _servicio;
    private readonly ExportadorMarkdown _exportador;
    private readonly ICompartir _compartir;

    private RangoResumen _rango = RangoResumen.DoceMeses;
    private ResumenGeneral _general = new();
    private double _ancho;

    public ResumenViewModel(ServicioFinanzas servicio, ExportadorMarkdown exportador, ICompartir compartir)
    {
        _servicio = servicio;
        _exportador = exportador;
        _compartir = compartir;

        CargarCommand = new ComandoAsync(CargarAsync);
        CambiarRangoCommand = new ComandoAsync(p =>
        {
            _rango = p switch
            {
                RangoResumen r => r,
                string s when Enum.TryParse<RangoResumen>(s, out var r) => r,
                _ => _rango,
            };
            return CargarAsync();
        });
        ExportarCommand = new ComandoAsync(ExportarAsync);
    }

    public ICommand CargarCommand { get; }
    /// <summary>Parámetro: "SeisMeses", "DoceMeses" o "Todo".</summary>
    public ICommand CambiarRangoCommand { get; }
    public ICommand ExportarCommand { get; }

    // ───────────── Rango ─────────────

    public bool EsSeisMeses => _rango == RangoResumen.SeisMeses;
    public bool EsDoceMeses => _rango == RangoResumen.DoceMeses;
    public bool EsTodo => _rango == RangoResumen.Todo;

    // ───────────── Totales ─────────────

    public bool HayDatos => _general.Meses.Count > 0;
    public bool SinDatos => !HayDatos;

    public string AhorradoTexto => Formato.Pesos(_general.TotalAhorrado);
    public bool AhorradoNegativo => _general.TotalAhorrado < 0;
    public string IngresosTexto => Formato.Pesos(_general.TotalIngresos);
    public string GastosTexto => Formato.Pesos(_general.TotalGastos);
    public string PromedioTexto => Formato.Pesos(_general.GastoPromedio);
    public string MesMasCaroTexto => _general.MesMasCaro is { } m
        ? $"{m.Periodo.NombreLargo} · {Formato.Pesos(m.TotalGastos)}"
        : "–";
    public string CantidadMesesTexto => _general.Meses.Count switch
    {
        0 => "Sin meses",
        1 => "1 mes",
        var n => $"{n} meses",
    };

    // ───────────── Listas con barras ─────────────

    /// <summary>Del más reciente al más antiguo.</summary>
    public IReadOnlyList<MesResumenItem> Meses { get; private set; } = [];
    public IReadOnlyList<CategoriaTotalItem> Categorias { get; private set; } = [];
    public bool SinCategorias => Categorias.Count == 0;

    /// <summary>
    /// La vista avisa el ancho disponible para las barras (cambia al girar el teléfono).
    /// </summary>
    public void EstablecerAnchoGrafico(double ancho)
    {
        if (ancho <= 0 || Math.Abs(ancho - _ancho) < 1) return;
        _ancho = ancho;
        ConstruirListas();
    }

    // ───────────── Lógica ─────────────

    public async Task CargarAsync()
    {
        await _servicio.InicializarAsync();
        var actual = _servicio.PeriodoActual;
        Periodo? desde = _rango switch
        {
            RangoResumen.SeisMeses => Retroceder(actual, 5),
            RangoResumen.DoceMeses => Retroceder(actual, 11),
            _ => null,
        };

        _general = await _servicio.ObtenerResumenGeneralAsync(desde, actual);
        ConstruirListas();

        Notificar(nameof(EsSeisMeses), nameof(EsDoceMeses), nameof(EsTodo),
            nameof(HayDatos), nameof(SinDatos), nameof(AhorradoTexto), nameof(AhorradoNegativo),
            nameof(IngresosTexto), nameof(GastosTexto), nameof(PromedioTexto),
            nameof(MesMasCaroTexto), nameof(CantidadMesesTexto));
    }

    private static Periodo Retroceder(Periodo p, int meses)
    {
        for (var i = 0; i < meses; i++) p = p.Anterior();
        return p;
    }

    private void ConstruirListas()
    {
        var maximoMes = _general.Meses.Count == 0
            ? 0
            : _general.Meses.Max(m => Math.Max(m.Ingresos, m.TotalGastos));
        Meses = _general.Meses
            .OrderByDescending(m => m.Periodo)
            .Select(m => new MesResumenItem(m, maximoMes, _ancho))
            .ToList();

        var categorias = _general.GastoPorCategoria;
        var maximoCategoria = categorias.Count == 0 ? 0 : categorias[0].Gastado;
        Categorias = categorias
            .Select(c => new CategoriaTotalItem(c, _general.TotalGastos, maximoCategoria, _ancho))
            .ToList();

        Notificar(nameof(Meses), nameof(Categorias), nameof(SinCategorias));
    }

    private async Task ExportarAsync()
    {
        // El historial siempre incluye todos los meses, sin importar el rango que se esté viendo.
        var completo = await _servicio.ObtenerResumenGeneralAsync();
        var md = _exportador.ExportarHistorial(
            completo,
            await _servicio.ObtenerCategoriasAsync(incluirArchivadas: true),
            await _servicio.ObtenerAjustesAsync());

        await _compartir.CompartirArchivoAsync(
            ExportadorMarkdown.NombreArchivoHistorial(completo), md, "Historial de finanzas");
    }
}
