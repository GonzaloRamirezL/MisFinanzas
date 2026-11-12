using System.Windows.Input;
using MisFinanzas.App.Servicios;
using MisFinanzas.App.Utilidades;
using MisFinanzas.Core.Modelos;
using MisFinanzas.Core.Servicios;

namespace MisFinanzas.App.ViewModels;

/// <summary>Formulario para crear o editar un gasto permanente (se copia solo a cada mes de su rango).</summary>
public class GastoFijoFormViewModel : FormularioBase
{
    private static readonly PeriodoOpcion SinTermino = new(null, "Sin fecha de término");

    private readonly ServicioFinanzas _servicio;
    private readonly IDialogos _dialogos;
    private readonly GastoFijo? _existente;

    private string _concepto = "";
    private string _montoTexto = "";
    private CategoriaOpcion? _categoria;
    private PeriodoOpcion? _desde;
    private PeriodoOpcion? _hasta;
    private bool _actualizarMesActual = true;

    public GastoFijoFormViewModel(ServicioFinanzas servicio, IDialogos dialogos,
        IReadOnlyList<Categoria> categorias, GastoFijo? existente)
    {
        _servicio = servicio;
        _dialogos = dialogos;
        _existente = existente;

        var actual = servicio.PeriodoActual;
        Categorias = categorias.OrderBy(c => c.Orden).Select(c => new CategoriaOpcion(c)).ToList();

        // Meses para elegir: un año hacia atrás y uno hacia adelante (más el guardado, si queda fuera).
        var desdeLista = new SortedSet<Periodo>(Periodo.Rango(Primero(actual, -12), Primero(actual, 12)));
        if (existente is not null)
        {
            desdeLista.Add(Periodo.DesdeClave(existente.DesdeClave));
            if (existente.HastaClave is int h) desdeLista.Add(Periodo.DesdeClave(h));
        }
        MesesDesde = desdeLista.Select(PeriodoOpcion.De).ToList();
        MesesHasta = [SinTermino, .. MesesDesde];

        if (existente is not null)
        {
            _concepto = existente.Concepto;
            _montoTexto = Montos.ParaEditar(existente.MontoHabitual);
            _categoria = Categorias.FirstOrDefault(o => o.Categoria.Id == existente.CategoriaId);
            _desde = MesesDesde.First(o => o.Periodo?.Clave == existente.DesdeClave);
            _hasta = existente.HastaClave is int hasta ? MesesHasta.First(o => o.Periodo?.Clave == hasta) : SinTermino;
        }
        else
        {
            _desde = MesesDesde.First(o => o.Periodo == actual);
            _hasta = SinTermino;
        }

        GuardarCommand = new ComandoAsync(GuardarAsync);
    }

    private static Periodo Primero(Periodo p, int meses)
    {
        for (var i = 0; i < Math.Abs(meses); i++) p = meses < 0 ? p.Anterior() : p.Siguiente();
        return p;
    }

    public string Titulo => EsEdicion ? "Editar gasto fijo" : "Nuevo gasto fijo";
    public bool EsEdicion => _existente is not null;

    public ICommand GuardarCommand { get; }

    public List<CategoriaOpcion> Categorias { get; }
    public List<PeriodoOpcion> MesesDesde { get; }
    public List<PeriodoOpcion> MesesHasta { get; }

    public string Concepto { get => _concepto; set => Asignar(ref _concepto, value); }
    public string MontoTexto { get => _montoTexto; set => Asignar(ref _montoTexto, value); }
    public CategoriaOpcion? CategoriaSeleccionada { get => _categoria; set => Asignar(ref _categoria, value); }
    public PeriodoOpcion? DesdeSeleccionado { get => _desde; set => Asignar(ref _desde, value); }
    public PeriodoOpcion? HastaSeleccionado { get => _hasta; set => Asignar(ref _hasta, value); }
    /// <summary>Al editar: aplicar el cambio también a la cuenta de este mes (si sigue pendiente).</summary>
    public bool ActualizarMesActual { get => _actualizarMesActual; set => Asignar(ref _actualizarMesActual, value); }

    private async Task GuardarAsync()
    {
        if (string.IsNullOrWhiteSpace(Concepto))
        {
            await _dialogos.AvisarAsync("Falta algo", "Escribe el concepto, por ejemplo: Luz, Netflix.");
            return;
        }
        if (!Montos.TryParsear(MontoTexto, out var monto))
        {
            await _dialogos.AvisarAsync("Falta algo", "Escribe el monto habitual, por ejemplo 21990.");
            return;
        }
        if (CategoriaSeleccionada is null)
        {
            await _dialogos.AvisarAsync("Falta algo", "Elige una categoría.");
            return;
        }
        if (DesdeSeleccionado?.Periodo is not Periodo desde)
        {
            await _dialogos.AvisarAsync("Falta algo", "Elige desde qué mes aplica.");
            return;
        }
        var hasta = HastaSeleccionado?.Periodo;
        if (hasta is not null && hasta < desde)
        {
            await _dialogos.AvisarAsync("Revisa las fechas", "El mes \"hasta\" no puede ser anterior al mes \"desde\".");
            return;
        }

        var gasto = _existente ?? new GastoFijo();
        var respaldo = (gasto.Concepto, gasto.MontoHabitual, gasto.CategoriaId, gasto.DesdeClave, gasto.HastaClave);

        gasto.Concepto = Concepto.Trim();
        gasto.MontoHabitual = monto;
        gasto.CategoriaId = CategoriaSeleccionada.Categoria.Id;
        gasto.DesdeClave = desde.Clave;
        gasto.HastaClave = hasta?.Clave;

        try
        {
            await _servicio.GuardarGastoFijoAsync(gasto, actualizarMesActual: EsEdicion && ActualizarMesActual);
        }
        catch (ArgumentException ex)
        {
            (gasto.Concepto, gasto.MontoHabitual, gasto.CategoriaId, gasto.DesdeClave, gasto.HastaClave) = respaldo;
            await _dialogos.AvisarAsync("No se pudo guardar", ex.Message);
            return;
        }
        Cerrar(true);
    }
}
