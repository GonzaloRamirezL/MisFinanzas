using System.Windows.Input;

namespace MisFinanzas.App.Utilidades;

/// <summary>
/// Comando asíncrono para botones. Mientras se ejecuta queda deshabilitado,
/// así un doble toque no guarda dos veces lo mismo.
/// </summary>
public class ComandoAsync(Func<object?, Task> accion) : ICommand
{
    private bool _ejecutando;

    public ComandoAsync(Func<Task> accion) : this(_ => accion()) { }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parametro) => !_ejecutando;

    public async void Execute(object? parametro)
    {
        if (_ejecutando) return;
        _ejecutando = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            await accion(parametro);
        }
        catch (Exception ex)
        {
            // Un error en un botón no debe cerrar la app. Se informa y se sigue.
            ErrorNoControlado?.Invoke(ex);
        }
        finally
        {
            _ejecutando = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>La app conecta aquí un aviso al usuario (ver MauiProgram).</summary>
    public static Action<Exception>? ErrorNoControlado { get; set; }
}
