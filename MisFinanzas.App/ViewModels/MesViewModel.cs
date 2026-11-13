using System.Windows.Input;
using MisFinanzas.App.Servicios;
using MisFinanzas.App.Utilidades;
using MisFinanzas.Core.Calculos;
using MisFinanzas.Core.Exportacion;
using MisFinanzas.Core.Modelos;
using MisFinanzas.Core.Servicios;

namespace MisFinanzas.App.ViewModels;

/// <summary>Pantalla principal: un mes con su resumen, sus listas y "¿En qué se fue?".</summary>
public class MesViewModel : Observable
{
    private readonly ServicioFinanzas _servicio;
    private readonly ExportadorMarkdown _exportador;
    private readonly IDialogos _dialogos;
    private readonly INavegacion _navegacion;
    private readonly ICompartir _compartir;

    private Periodo _periodo;
    private ResumenMes? _resumen;
    private List<Categoria> _categorias = [];
    private bool _inicializado;
    private bool _cargando;

    public MesViewModel(ServicioFinanzas servicio, ExportadorMarkdown exportador,
        IDialogos dialogos, INavegacion navegacion, ICompartir compartir)
    {
        _servicio = servicio;
        _exportador = exportador;
        _dialogos = dialogos;
        _navegacion = navegacion;
        _compartir = compartir;
        _periodo = servicio.PeriodoActual;

        CargarCommand = new ComandoAsync(CargarAsync);
        MesAnteriorCommand = new ComandoAsync(() => CambiarMesAsync(_periodo.Anterior()));
        MesSiguienteCommand = new ComandoAsync(() => CambiarMesAsync(_periodo.Siguiente()));
        IrAMesActualCommand = new ComandoAsync(() => CambiarMesAsync(_servicio.PeriodoActual));
        EditarSueldoCommand = new ComandoAsync(EditarSueldoAsync);
        AgregarCommand = new ComandoAsync(p => AgregarAsync(LeerTipo(p)));
        TocarMovimientoCommand = new ComandoAsync(p => p is MovimientoItem item ? OpcionesAsync(item) : Task.CompletedTask);
        ExportarCommand = new ComandoAsync(ExportarAsync);
    }

    // ───────────── Comandos ─────────────

    public ICommand CargarCommand { get; }
    public ICommand MesAnteriorCommand { get; }
    public ICommand MesSiguienteCommand { get; }
    public ICommand IrAMesActualCommand { get; }
    public ICommand EditarSueldoCommand { get; }
    /// <summary>Parámetro: "CuentaFija", "Deuda", "Compra" o "PagoRecibido".</summary>
    public ICommand AgregarCommand { get; }
    public ICommand TocarMovimientoCommand { get; }
    public ICommand ExportarCommand { get; }

    // ───────────── Datos para la vista ─────────────

    public Periodo Periodo => _periodo;
    public string TituloMes => _periodo.NombreLargo;
    public bool EsMesActual => _periodo == _servicio.PeriodoActual;
    public bool NoEsMesActual => !EsMesActual;

    public bool Cargando
    {
        get => _cargando;
        private set => Asignar(ref _cargando, value);
    }

    public string MeQuedaTexto => Formato.Pesos(_resumen?.MeQueda ?? 0);
    public bool MeQuedaNegativo => _resumen?.MeQueda < 0;
    public string IngresosTexto => Formato.Pesos(_resumen?.Ingresos ?? 0);
    public string GastosTexto => Formato.Pesos(_resumen?.TotalGastos ?? 0);
    public string SueldoTexto => Formato.Pesos(_resumen?.Sueldo ?? 0);
    public string PorPagarTexto => Formato.Pesos(_resumen?.PorPagar ?? 0);
    public bool HayPorPagar => _resumen?.PorPagar > 0;

    /// <summary>0 a 1 para la barra de "gastado del ingreso".</summary>
    public double PorcentajeGastado => (double)Math.Min(_resumen?.PorcentajeGastado ?? 0, 1m);
    public string PorcentajeGastadoTexto => $"{Formato.Porcentaje(_resumen?.PorcentajeGastado ?? 0)} del ingreso gastado";
    public bool SuperaUmbralGasto => _resumen?.SuperaUmbralGasto ?? false;

    public IReadOnlyList<string> Alertas { get; private set; } = [];
    public bool HayAlertas => Alertas.Count > 0;

    // Cada lista muestra 5 registros (los más recientes primero) y un botón "Ver más".
    public SeccionMovimientos CuentasFijas { get; } = new();
    public SeccionMovimientos Deudas { get; } = new();
    public SeccionMovimientos Compras { get; } = new();
    public SeccionMovimientos PagosRecibidos { get; } = new();

    public string TotalCuentasFijasTexto => Formato.Pesos(_resumen?.CuentasFijas ?? 0);
    public string TotalDeudasTexto => Formato.Pesos(_resumen?.Deudas ?? 0);
    public string TotalComprasTexto => Formato.Pesos(_resumen?.Compras ?? 0);
    public string TotalPagosRecibidosTexto => Formato.Pesos(_resumen?.PagosRecibidos ?? 0);


    /// <summary>Solo categorías con gasto o con presupuesto (las vacías sin presupuesto no aportan).</summary>
    public IReadOnlyList<CategoriaItem> PorCategoria { get; private set; } = [];
    public bool SinGastosPorCategoria => PorCategoria.Count == 0;

    // ───────────── Lógica ─────────────

    public async Task CargarAsync()
    {
        Cargando = true;
        try
        {
            if (!_inicializado)
            {
                await _servicio.InicializarAsync();
                _inicializado = true;
            }

            await _servicio.AbrirMesAsync(_periodo);
            _categorias = await _servicio.ObtenerCategoriasAsync(incluirArchivadas: true);
            var cats = _categorias.ToDictionary(c => c.Id);
            var ajustes = await _servicio.ObtenerAjustesAsync();
            var movimientos = await _servicio.ObtenerMovimientosAsync(_periodo);
            _resumen = await _servicio.ObtenerResumenMesAsync(_periodo);

            // Orden descendente: lo último que agregaste aparece arriba (el Id crece con cada registro nuevo).
            List<MovimientoItem> DeTipo(TipoMovimiento tipo) => movimientos
                .Where(m => m.Tipo == tipo)
                .OrderByDescending(m => m.Id)
                .Select(m => new MovimientoItem(m, m.CategoriaId is int id && cats.TryGetValue(id, out var c) ? c : null, OpcionesAsync))
                .ToList();

            CuentasFijas.Actualizar(DeTipo(TipoMovimiento.CuentaFija));
            Deudas.Actualizar(DeTipo(TipoMovimiento.Deuda));
            Compras.Actualizar(DeTipo(TipoMovimiento.Compra));
            PagosRecibidos.Actualizar(DeTipo(TipoMovimiento.PagoRecibido));

            PorCategoria = _resumen.PorCategoria
                .Where(c => c.Gastado > 0 || c.Presupuesto is not null)
                .OrderByDescending(c => c.Gastado)
                .Select(c => new CategoriaItem(c))
                .ToList();

            Alertas = ExportadorMarkdown.DescribirAlertas(_resumen, ajustes).ToList();

            NotificarTodo();
        }
        finally
        {
            Cargando = false;
        }
    }

    private void NotificarTodo() => Notificar(
        nameof(Periodo), nameof(TituloMes), nameof(EsMesActual), nameof(NoEsMesActual),
        nameof(MeQuedaTexto), nameof(MeQuedaNegativo), nameof(IngresosTexto), nameof(GastosTexto),
        nameof(SueldoTexto), nameof(PorPagarTexto), nameof(HayPorPagar),
        nameof(PorcentajeGastado), nameof(PorcentajeGastadoTexto), nameof(SuperaUmbralGasto),
        nameof(Alertas), nameof(HayAlertas),
        nameof(TotalCuentasFijasTexto), nameof(TotalDeudasTexto), nameof(TotalComprasTexto), nameof(TotalPagosRecibidosTexto),
        nameof(PorCategoria), nameof(SinGastosPorCategoria));

    private async Task CambiarMesAsync(Periodo periodo)
    {
        // No se abren meses muy lejanos en el futuro por accidente.
        if (periodo > _servicio.PeriodoActual.Siguiente())
        {
            await _dialogos.AvisarAsync("Paciencia 🌱", "Solo puedes adelantarte un mes.");
            return;
        }
        _periodo = periodo;
        foreach (var seccion in new[] { CuentasFijas, Deudas, Compras, PagosRecibidos })
            seccion.Colapsar();
        await CargarAsync();
    }

    private async Task EditarSueldoAsync()
    {
        var texto = await _dialogos.PedirNumeroAsync("Sueldo del mes",
            $"¿Cuánto es tu sueldo en {_periodo.NombreLargo}?", Montos.ParaEditar(_resumen?.Sueldo ?? 0));
        if (texto is null) return;

        if (!Montos.TryParsear(texto, out var sueldo))
        {
            await _dialogos.AvisarAsync("Ups", "Ese monto no se entiende. Escribe solo números, por ejemplo 2217418.");
            return;
        }
        await _servicio.ActualizarSueldoAsync(_periodo, sueldo);
        await CargarAsync();
    }

    private static TipoMovimiento LeerTipo(object? parametro) => parametro switch
    {
        TipoMovimiento t => t,
        string s when Enum.TryParse<TipoMovimiento>(s, out var t) => t,
        _ => throw new ArgumentException($"Tipo de movimiento desconocido: {parametro}"),
    };

    private async Task AgregarAsync(TipoMovimiento tipo)
    {
        var formulario = CrearFormulario(tipo, existente: null);
        if (await _navegacion.AbrirFormularioAsync(formulario))
            await CargarAsync();
    }

    private MovimientoViewModel CrearFormulario(TipoMovimiento tipo, Movimiento? existente) =>
        new(_servicio, _dialogos, _periodo, tipo,
            _categorias.Where(c => c.Activa || c.Id == existente?.CategoriaId).ToList(), existente);

    private async Task OpcionesAsync(MovimientoItem item)
    {
        var m = item.Modelo;
        const string marcarPagado = "Marcar como pagado ✅";
        const string marcarPendiente = "Marcar como pendiente ⏳";
        const string editar = "Editar ✏️";
        var quitar = m.GastoFijoId is null ? "Eliminar" : "Quitar solo de este mes";

        var opciones = new List<string>();
        if (item.MuestraEstado) opciones.Add(item.EstaPagado ? marcarPendiente : marcarPagado);
        opciones.Add(editar);

        var eleccion = await _dialogos.ElegirAsync(m.Concepto, quitar, [.. opciones]);
        switch (eleccion)
        {
            case marcarPagado:
                await _servicio.CambiarEstadoAsync(m, EstadoPago.Pagado);
                break;
            case marcarPendiente:
                await _servicio.CambiarEstadoAsync(m, EstadoPago.Pendiente);
                break;
            case editar:
                if (!await _navegacion.AbrirFormularioAsync(CrearFormulario(m.Tipo, m))) return;
                break;
            default:
                if (eleccion != quitar) return;
                var detalle = m.GastoFijoId is null
                    ? "Se borrará de este mes."
                    : "Se quita solo de este mes. Seguirá apareciendo en los próximos meses.";
                if (!await _dialogos.ConfirmarAsync($"¿{quitar}?", $"{m.Concepto} ({item.MontoTexto}). {detalle}")) return;
                await _servicio.QuitarMovimientoAsync(m);
                break;
        }
        await CargarAsync();
    }

    private async Task ExportarAsync()
    {
        if (_resumen is null) return;
        var md = _exportador.ExportarMes(
            _resumen,
            await _servicio.ObtenerMovimientosAsync(_periodo),
            _categorias,
            await _servicio.ObtenerAjustesAsync());

        await _compartir.CompartirArchivoAsync(
            ExportadorMarkdown.NombreArchivoMes(_periodo), md, $"Finanzas · {_periodo.NombreLargo}");
    }
}
