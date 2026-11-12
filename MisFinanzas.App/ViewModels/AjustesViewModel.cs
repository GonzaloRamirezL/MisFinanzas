using System.Globalization;
using System.Windows.Input;
using MisFinanzas.App.Servicios;
using MisFinanzas.App.Utilidades;
using MisFinanzas.Core.Exportacion;
using MisFinanzas.Core.Modelos;
using MisFinanzas.Core.Servicios;

namespace MisFinanzas.App.ViewModels;

/// <summary>Una categoría en la lista de Ajustes.</summary>
public class CategoriaAjusteItem
{
    public CategoriaAjusteItem(Categoria categoria, Func<CategoriaAjusteItem, Task> alTocar)
    {
        Modelo = categoria;
        Emoji = categoria.Emoji;
        Nombre = categoria.Nombre;
        Detalle = categoria.PresupuestoMensual is decimal p
            ? $"Presupuesto {Formato.Pesos(p)} al mes"
            : "Sin presupuesto";
        TocarCommand = new ComandoAsync(() => alTocar(this));
    }

    public Categoria Modelo { get; }
    public string Emoji { get; }
    public string Nombre { get; }
    public string Detalle { get; }
    public ICommand TocarCommand { get; }
}

/// <summary>Un gasto permanente en la lista de Ajustes.</summary>
public class GastoFijoItem
{
    public GastoFijoItem(GastoFijo gasto, Categoria? categoria, Periodo actual, Func<GastoFijoItem, Task> alTocar)
    {
        Modelo = gasto;
        Emoji = categoria?.Emoji ?? "🏷️";
        Concepto = gasto.Concepto;
        MontoTexto = Formato.Pesos(gasto.MontoHabitual);

        var desde = Periodo.DesdeClave(gasto.DesdeClave);
        var rango = gasto.HastaClave is int h ? $"{desde.Codigo} a {Periodo.DesdeClave(h).Codigo}" : $"desde {desde.Codigo}";
        Detalle = $"{categoria?.Nombre ?? "Sin categoría"} · {rango}";

        EstaActivo = gasto.AplicaEn(actual);
        EstaTerminado = gasto.HastaClave is int fin && fin < actual.Clave;
        EstadoTexto = EstaActivo ? "Activo" : EstaTerminado ? "Terminado" : "Próximamente";
        TocarCommand = new ComandoAsync(() => alTocar(this));
    }

    public GastoFijo Modelo { get; }
    public string Emoji { get; }
    public string Concepto { get; }
    public string Detalle { get; }
    public string MontoTexto { get; }
    public bool EstaActivo { get; }
    public bool EstaTerminado { get; }
    public string EstadoTexto { get; }
    public ICommand TocarCommand { get; }
}

/// <summary>Pestaña Ajustes: categorías, gastos permanentes y umbrales de alerta.</summary>
public class AjustesViewModel : Observable
{
    private readonly ServicioFinanzas _servicio;
    private readonly IDialogos _dialogos;
    private readonly INavegacion _navegacion;

    private List<Categoria> _todasLasCategorias = [];
    private Ajustes _ajustes = new();

    public AjustesViewModel(ServicioFinanzas servicio, IDialogos dialogos, INavegacion navegacion)
    {
        _servicio = servicio;
        _dialogos = dialogos;
        _navegacion = navegacion;

        CargarCommand = new ComandoAsync(CargarAsync);
        AgregarCategoriaCommand = new ComandoAsync(() => AbrirCategoriaAsync(null));
        AgregarGastoFijoCommand = new ComandoAsync(() => AbrirGastoFijoAsync(null));
        EditarUmbralCommand = new ComandoAsync(p => EditarUmbralAsync(p as string ?? ""));
    }

    public ICommand CargarCommand { get; }
    public ICommand AgregarCategoriaCommand { get; }
    public ICommand AgregarGastoFijoCommand { get; }
    /// <summary>Parámetro: "SubidaCuenta", "GastoIngreso" o "AlLimite".</summary>
    public ICommand EditarUmbralCommand { get; }

    public IReadOnlyList<CategoriaAjusteItem> Categorias { get; private set; } = [];
    public IReadOnlyList<CategoriaAjusteItem> CategoriasArchivadas { get; private set; } = [];
    public bool HayArchivadas => CategoriasArchivadas.Count > 0;

    public IReadOnlyList<GastoFijoItem> GastosFijos { get; private set; } = [];
    public bool SinGastosFijos => GastosFijos.Count == 0;
    public string TotalGastosFijosTexto { get; private set; } = "";

    public string UmbralSubidaTexto => Porcentaje(_ajustes.UmbralSubidaCuenta);
    public string UmbralGastoTexto => Porcentaje(_ajustes.UmbralGastoIngreso);
    public string UmbralLimiteTexto => Porcentaje(_ajustes.UmbralAlLimiteCategoria);

    private static string Porcentaje(decimal fraccion) =>
        (fraccion * 100).ToString("0.#", CultureInfo.InvariantCulture).Replace(".", ",") + " %";

    public async Task CargarAsync()
    {
        await _servicio.InicializarAsync();
        _todasLasCategorias = await _servicio.ObtenerCategoriasAsync(incluirArchivadas: true);
        _ajustes = await _servicio.ObtenerAjustesAsync();
        var actual = _servicio.PeriodoActual;
        var porId = _todasLasCategorias.ToDictionary(c => c.Id);

        Categorias = _todasLasCategorias.Where(c => c.Activa)
            .Select(c => new CategoriaAjusteItem(c, OpcionesCategoriaAsync)).ToList();
        CategoriasArchivadas = _todasLasCategorias.Where(c => !c.Activa)
            .Select(c => new CategoriaAjusteItem(c, OpcionesArchivadaAsync)).ToList();

        var gastos = await _servicio.ObtenerGastosFijosAsync();
        GastosFijos = gastos
            .Select(g => new GastoFijoItem(g, porId.GetValueOrDefault(g.CategoriaId), actual, OpcionesGastoFijoAsync))
            .OrderBy(i => i.EstaTerminado)       // terminados al final
            .ThenBy(i => i.Concepto, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        TotalGastosFijosTexto = $"{Formato.Pesos(gastos.Where(g => g.AplicaEn(actual)).Sum(g => g.MontoHabitual))} al mes";

        Notificar(nameof(Categorias), nameof(CategoriasArchivadas), nameof(HayArchivadas),
            nameof(GastosFijos), nameof(SinGastosFijos), nameof(TotalGastosFijosTexto),
            nameof(UmbralSubidaTexto), nameof(UmbralGastoTexto), nameof(UmbralLimiteTexto));
    }

    // ───────────── Categorías ─────────────

    private async Task AbrirCategoriaAsync(Categoria? existente)
    {
        if (await _navegacion.AbrirFormularioAsync(new CategoriaFormViewModel(_servicio, _dialogos, existente)))
            await CargarAsync();
    }

    private async Task OpcionesCategoriaAsync(CategoriaAjusteItem item)
    {
        const string editar = "Editar ✏️";
        const string quitar = "Quitar";
        var eleccion = await _dialogos.ElegirAsync($"{item.Emoji} {item.Nombre}", quitar, editar);

        if (eleccion == editar)
        {
            await AbrirCategoriaAsync(item.Modelo);
        }
        else if (eleccion == quitar)
        {
            if (!await _dialogos.ConfirmarAsync($"¿Quitar {item.Nombre}?",
                    "Si ya la usaste, se archiva: deja de aparecer para gastos nuevos, pero tus meses pasados no cambian."))
                return;
            var borrada = await _servicio.QuitarCategoriaAsync(item.Modelo);
            if (!borrada)
                await _dialogos.AvisarAsync("Categoría archivada 📦",
                    $"{item.Nombre} tenía historial, así que quedó en \"Archivadas\". Puedes restaurarla cuando quieras.");
            await CargarAsync();
        }
    }

    private async Task OpcionesArchivadaAsync(CategoriaAjusteItem item)
    {
        const string restaurar = "Restaurar ↩️";
        if (await _dialogos.ElegirAsync($"{item.Emoji} {item.Nombre} (archivada)", null, restaurar) != restaurar) return;
        await _servicio.RestaurarCategoriaAsync(item.Modelo);
        await CargarAsync();
    }

    // ───────────── Gastos permanentes ─────────────

    private async Task AbrirGastoFijoAsync(GastoFijo? existente)
    {
        var activas = _todasLasCategorias.Where(c => c.Activa || c.Id == existente?.CategoriaId).ToList();
        if (activas.Count == 0)
        {
            await _dialogos.AvisarAsync("Primero una categoría", "Crea al menos una categoría para poder agregar gastos fijos.");
            return;
        }
        if (await _navegacion.AbrirFormularioAsync(new GastoFijoFormViewModel(_servicio, _dialogos, activas, existente)))
            await CargarAsync();
    }

    private async Task OpcionesGastoFijoAsync(GastoFijoItem item)
    {
        const string editar = "Editar ✏️";
        const string terminar = "Dejar de pagarlo desde el próximo mes ✋";
        const string reactivar = "Volver a activarlo 🔁";
        const string eliminar = "Eliminar";

        var opciones = new List<string> { editar };
        if (item.EstaTerminado) opciones.Add(reactivar);
        else if (item.Modelo.HastaClave is null || item.Modelo.HastaClave > _servicio.PeriodoActual.Clave) opciones.Add(terminar);

        var eleccion = await _dialogos.ElegirAsync($"{item.Emoji} {item.Concepto}", eliminar, [.. opciones]);
        switch (eleccion)
        {
            case editar:
                await AbrirGastoFijoAsync(item.Modelo);
                return;

            case terminar:
                if (!await _dialogos.ConfirmarAsync($"¿Dejar de pagar {item.Concepto}?",
                        $"Se queda en {_servicio.PeriodoActual.NombreLargo} y ya no aparece en los meses siguientes."))
                    return;
                await _servicio.TerminarGastoFijoAsync(item.Modelo, _servicio.PeriodoActual);
                break;

            case reactivar:
                item.Modelo.HastaClave = null;
                await _servicio.GuardarGastoFijoAsync(item.Modelo);
                break;

            case eliminar:
                if (!await _dialogos.ConfirmarAsync($"¿Eliminar {item.Concepto}?", "Se borra de la lista de gastos fijos."))
                    return;
                if (!await _servicio.EliminarGastoFijoAsync(item.Modelo))
                {
                    await _dialogos.AvisarAsync("No se puede eliminar",
                        "Este gasto ya aparece en meses registrados. Usa \"Dejar de pagarlo\" para que no siga apareciendo.");
                    return;
                }
                break;

            default:
                return;
        }
        await CargarAsync();
    }

    // ───────────── Umbrales de alerta ─────────────

    private async Task EditarUmbralAsync(string cual)
    {
        var (titulo, mensaje, actual) = cual switch
        {
            "SubidaCuenta" => ("Cuenta que sube", "Avisarme si una cuenta fija sube más de este % respecto al mes anterior:", _ajustes.UmbralSubidaCuenta),
            "GastoIngreso" => ("Gasto del mes", "Avisarme si gasto más de este % de lo que ingresa:", _ajustes.UmbralGastoIngreso),
            "AlLimite" => ("Presupuesto al límite", "Marcar una categoría \"al límite\" cuando use este % de su presupuesto:", _ajustes.UmbralAlLimiteCategoria),
            _ => throw new ArgumentException($"Umbral desconocido: {cual}"),
        };

        var texto = await _dialogos.PedirNumeroAsync(titulo, mensaje,
            (actual * 100).ToString("0.#", CultureInfo.InvariantCulture));
        if (texto is null) return;

        if (!decimal.TryParse(texto.Replace(",", ".").Replace("%", "").Trim(), NumberStyles.Number,
                CultureInfo.InvariantCulture, out var porcentaje) || porcentaje is <= 0 or > 500)
        {
            await _dialogos.AvisarAsync("Ups", "Escribe un porcentaje entre 1 y 500, por ejemplo 20.");
            return;
        }

        var fraccion = porcentaje / 100;
        switch (cual)
        {
            case "SubidaCuenta": _ajustes.UmbralSubidaCuenta = fraccion; break;
            case "GastoIngreso": _ajustes.UmbralGastoIngreso = fraccion; break;
            default: _ajustes.UmbralAlLimiteCategoria = fraccion; break;
        }
        await _servicio.GuardarAjustesAsync(_ajustes);
        await CargarAsync();
    }
}
