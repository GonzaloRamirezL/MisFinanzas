using System.Windows.Input;
using MisFinanzas.App.Servicios;
using MisFinanzas.App.Utilidades;
using MisFinanzas.Core.Modelos;
using MisFinanzas.Core.Servicios;

namespace MisFinanzas.App.ViewModels;

/// <summary>Formulario para crear o editar una categoría (emoji, nombre y presupuesto opcional).</summary>
public class CategoriaFormViewModel : FormularioBase
{
    private readonly ServicioFinanzas _servicio;
    private readonly IDialogos _dialogos;
    private readonly Categoria? _existente;

    private string _emoji = "🏷️";
    private string _nombre = "";
    private string _presupuestoTexto = "";

    public CategoriaFormViewModel(ServicioFinanzas servicio, IDialogos dialogos, Categoria? existente)
    {
        _servicio = servicio;
        _dialogos = dialogos;
        _existente = existente;

        if (existente is not null)
        {
            _emoji = existente.Emoji;
            _nombre = existente.Nombre;
            _presupuestoTexto = existente.PresupuestoMensual is decimal p ? Montos.ParaEditar(p) : "";
        }

        GuardarCommand = new ComandoAsync(GuardarAsync);
        ElegirEmojiCommand = new ComandoAsync(p =>
        {
            if (p is string e) Emoji = e;
            return Task.CompletedTask;
        });
    }

    public string Titulo => _existente is null ? "Nueva categoría" : "Editar categoría";

    public ICommand GuardarCommand { get; }
    /// <summary>Parámetro: el emoji tocado en la lista de sugerencias.</summary>
    public ICommand ElegirEmojiCommand { get; }

    public List<string> EmojisSugeridos { get; } =
    [
        "💡", "🏠", "👨‍👩‍👧", "🩺", "🐾", "💳", "🌸", "📦", "🍜", "☕",
        "🛒", "🚌", "🎮", "🎌", "📚", "🎧", "✈️", "🎁", "💄", "🏋️",
    ];

    public string Emoji { get => _emoji; set => Asignar(ref _emoji, value); }
    public string Nombre { get => _nombre; set => Asignar(ref _nombre, value); }
    /// <summary>Vacío = sin presupuesto.</summary>
    public string PresupuestoTexto { get => _presupuestoTexto; set => Asignar(ref _presupuestoTexto, value); }

    private async Task GuardarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nombre))
        {
            await _dialogos.AvisarAsync("Falta algo", "Escribe el nombre de la categoría.");
            return;
        }

        decimal? presupuesto = null;
        if (!string.IsNullOrWhiteSpace(PresupuestoTexto))
        {
            if (!Montos.TryParsear(PresupuestoTexto, out var p))
            {
                await _dialogos.AvisarAsync("Ups", "El presupuesto no se entiende. Escribe solo números o déjalo vacío.");
                return;
            }
            presupuesto = p;
        }

        var categoria = _existente ?? new Categoria();
        var respaldo = (categoria.Emoji, categoria.Nombre, categoria.PresupuestoMensual);

        categoria.Emoji = string.IsNullOrWhiteSpace(Emoji) ? "🏷️" : Emoji.Trim();
        categoria.Nombre = Nombre.Trim();
        categoria.PresupuestoMensual = presupuesto;

        try
        {
            await _servicio.GuardarCategoriaAsync(categoria);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            // Si no se pudo guardar, la categoría vuelve a como estaba.
            (categoria.Emoji, categoria.Nombre, categoria.PresupuestoMensual) = respaldo;
            await _dialogos.AvisarAsync("No se pudo guardar", ex.Message);
            return;
        }
        Cerrar(true);
    }
}
