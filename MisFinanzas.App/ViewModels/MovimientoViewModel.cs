using System.Windows.Input;
using MisFinanzas.App.Servicios;
using MisFinanzas.App.Utilidades;
using MisFinanzas.Core.Modelos;
using MisFinanzas.Core.Servicios;

namespace MisFinanzas.App.ViewModels;

/// <summary>Formulario para agregar o editar un movimiento. Los campos visibles dependen del tipo.</summary>
public class MovimientoViewModel : FormularioBase
{
    private readonly ServicioFinanzas _servicio;
    private readonly IDialogos _dialogos;
    private readonly Periodo _periodo;
    private readonly Movimiento? _existente;

    private string _concepto = "";
    private string _montoTexto = "";
    private CategoriaOpcion? _categoria;
    private bool _pagado;
    private DateTime? _fecha;
    private string _deQuien = "";
    private string _nota = "";

    public MovimientoViewModel(ServicioFinanzas servicio, IDialogos dialogos, Periodo periodo,
        TipoMovimiento tipo, IReadOnlyList<Categoria> categorias, Movimiento? existente)
    {
        _servicio = servicio;
        _dialogos = dialogos;
        _periodo = periodo;
        _existente = existente;
        Tipo = tipo;
        Categorias = categorias.OrderBy(c => c.Orden).Select(c => new CategoriaOpcion(c)).ToList();

        if (existente is not null)
        {
            _concepto = existente.Concepto;
            _montoTexto = Montos.ParaEditar(existente.Monto);
            _categoria = Categorias.FirstOrDefault(o => o.Categoria.Id == existente.CategoriaId);
            _pagado = existente.Estado == EstadoPago.Pagado;
            _fecha = existente.Fecha;
            _deQuien = existente.DeQuien ?? "";
            _nota = existente.Nota ?? "";
        }
        else
        {
            // Fecha sugerida: hoy si estás en el mes actual; si no, el día 1 del mes que estás viendo.
            var hoy = DateTime.Today;
            _fecha = Periodo.DesdeFecha(hoy) == periodo ? hoy : new DateTime(periodo.Anio, periodo.NumeroMes, 1);
        }

        GuardarCommand = new ComandoAsync(GuardarAsync);
    }

    public TipoMovimiento Tipo { get; }
    public bool EsNuevo => _existente is null;

    public ICommand GuardarCommand { get; }

    public string Titulo => (EsNuevo ? "Nueva " : "Editar ") + Tipo switch
    {
        TipoMovimiento.CuentaFija => "cuenta fija",
        TipoMovimiento.Deuda => "deuda o gasto",
        TipoMovimiento.Compra => "compra",
        _ => "entrada de dinero",
    };

    public string Emoji => Tipo switch
    {
        TipoMovimiento.CuentaFija => "🏠",
        TipoMovimiento.Deuda => "💳",
        TipoMovimiento.Compra => "🛍️",
        _ => "💌",
    };

    public string EtiquetaConcepto => Tipo is TipoMovimiento.Compra or TipoMovimiento.PagoRecibido ? "Detalle" : "Concepto";

    public bool MuestraCategoria => Tipo != TipoMovimiento.PagoRecibido;
    public bool MuestraEstado => Tipo is TipoMovimiento.CuentaFija or TipoMovimiento.Deuda;
    public bool MuestraFecha => Tipo is TipoMovimiento.Compra or TipoMovimiento.PagoRecibido;
    public bool MuestraDeQuien => Tipo == TipoMovimiento.PagoRecibido;
    public bool MuestraNota => Tipo == TipoMovimiento.Deuda;

    public List<CategoriaOpcion> Categorias { get; }

    public string Concepto { get => _concepto; set => Asignar(ref _concepto, value); }
    public string MontoTexto { get => _montoTexto; set => Asignar(ref _montoTexto, value); }
    public CategoriaOpcion? CategoriaSeleccionada { get => _categoria; set => Asignar(ref _categoria, value); }
    public bool Pagado { get => _pagado; set => Asignar(ref _pagado, value); }
    public DateTime? Fecha { get => _fecha; set => Asignar(ref _fecha, value); }
    public string DeQuien { get => _deQuien; set => Asignar(ref _deQuien, value); }
    public string Nota { get => _nota; set => Asignar(ref _nota, value); }

    /// <summary>Límites del selector de fecha: solo días del mes que se está editando.</summary>
    public DateTime? FechaMinima => new DateTime(_periodo.Anio, _periodo.NumeroMes, 1);
    public DateTime? FechaMaxima => FechaMinima!.Value.AddMonths(1).AddDays(-1);

    private async Task GuardarAsync()
    {
        if (string.IsNullOrWhiteSpace(Concepto))
        {
            await _dialogos.AvisarAsync("Falta algo", $"Escribe el {EtiquetaConcepto.ToLowerInvariant()}.");
            return;
        }
        if (!Montos.TryParsear(MontoTexto, out var monto))
        {
            await _dialogos.AvisarAsync("Falta algo", "Escribe un monto válido, por ejemplo 35000.");
            return;
        }
        if (MuestraCategoria && CategoriaSeleccionada is null)
        {
            await _dialogos.AvisarAsync("Falta algo", "Elige una categoría.");
            return;
        }

        var m = _existente ?? new Movimiento { MesClave = _periodo.Clave, Tipo = Tipo };
        m.Concepto = Concepto.Trim();
        m.Monto = monto;
        m.CategoriaId = MuestraCategoria ? CategoriaSeleccionada!.Categoria.Id : null;
        m.Estado = Pagado ? EstadoPago.Pagado : EstadoPago.Pendiente;
        m.Fecha = MuestraFecha ? Fecha : m.Fecha;
        m.DeQuien = MuestraDeQuien && !string.IsNullOrWhiteSpace(DeQuien) ? DeQuien.Trim() : null;
        m.Nota = MuestraNota && !string.IsNullOrWhiteSpace(Nota) ? Nota.Trim() : null;

        try
        {
            await _servicio.GuardarMovimientoAsync(m);
        }
        catch (ArgumentException ex)
        {
            await _dialogos.AvisarAsync("No se pudo guardar", ex.Message);
            return;
        }
        Cerrar(true);
    }
}
