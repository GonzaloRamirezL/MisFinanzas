using System.Windows.Input;
using MisFinanzas.App.Utilidades;
using MisFinanzas.Core.Modelos;

namespace MisFinanzas.App.ViewModels;

/// <summary>
/// Base de todos los formularios (movimiento, categoría, gasto fijo).
/// Quien abre el formulario espera <see cref="Resultado"/>: true si se guardó, false si se canceló.
/// </summary>
public abstract class FormularioBase : Observable
{
    private readonly TaskCompletionSource<bool> _resultado = new();

    protected FormularioBase()
    {
        CancelarCommand = new ComandoAsync(() => { Cerrar(false); return Task.CompletedTask; });
    }

    public Task<bool> Resultado => _resultado.Task;
    public ICommand CancelarCommand { get; }

    /// <summary>Cierra sin guardar (botón atrás de Android).</summary>
    public void CerrarSinGuardar() => Cerrar(false);

    protected void Cerrar(bool guardado) => _resultado.TrySetResult(guardado);
}

/// <summary>Opción del selector de categoría ("💡 Servicios").</summary>
public record CategoriaOpcion(Categoria Categoria)
{
    public override string ToString() => $"{Categoria.Emoji} {Categoria.Nombre}";
}

/// <summary>Opción del selector de mes. Periodo null = "Sin fecha de término".</summary>
public record PeriodoOpcion(Periodo? Periodo, string Texto)
{
    public static PeriodoOpcion De(Periodo p) => new(p, p.NombreLargo);
    public override string ToString() => Texto;
}
