using System.Windows.Input;
using MisFinanzas.App.Utilidades;

namespace MisFinanzas.App.ViewModels;

/// <summary>
/// Una lista del mes (cuentas fijas, deudas, compras o pagos recibidos) que se muestra colapsada:
/// solo los primeros <see cref="Limite"/> registros, con un botón "Ver más" para desplegar el resto.
/// </summary>
public class SeccionMovimientos : Observable
{
    /// <summary>Cuántos registros se ven sin desplegar la lista.</summary>
    public const int Limite = 5;

    private IReadOnlyList<MovimientoItem> _todos = [];
    private bool _expandida;

    public SeccionMovimientos()
    {
        AlternarCommand = new ComandoAsync(() =>
        {
            _expandida = !_expandida;
            NotificarCambios();
            return Task.CompletedTask;
        });
    }

    /// <summary>Despliega o colapsa la lista.</summary>
    public ICommand AlternarCommand { get; }

    /// <summary>Todos los registros, del más reciente al más antiguo.</summary>
    public IReadOnlyList<MovimientoItem> Todos => _todos;

    /// <summary>Lo que se muestra: los primeros 5, o todos si la lista está desplegada.</summary>
    public IReadOnlyList<MovimientoItem> Visibles => _expandida ? _todos : _todos.Take(Limite).ToList();

    public bool Expandida => _expandida;
    public bool Vacia => _todos.Count == 0;
    /// <summary>True si hay más de 5 registros (se muestra el botón).</summary>
    public bool HayMas => _todos.Count > Limite;
    public int Ocultos => Math.Max(0, _todos.Count - Limite);

    public string TextoBoton => _expandida
        ? "Ver menos ▴"
        : Ocultos == 1 ? "Ver 1 más ▾" : $"Ver {Ocultos} más ▾";

    /// <summary>Carga los registros. Conserva si la lista estaba desplegada.</summary>
    public void Actualizar(IReadOnlyList<MovimientoItem> todos)
    {
        _todos = todos;
        if (!HayMas) _expandida = false;
        NotificarCambios();
    }

    /// <summary>Vuelve a colapsar (por ejemplo, al cambiar de mes).</summary>
    public void Colapsar()
    {
        if (!_expandida) return;
        _expandida = false;
        NotificarCambios();
    }

    private void NotificarCambios() => Notificar(
        nameof(Todos), nameof(Visibles), nameof(Expandida), nameof(Vacia),
        nameof(HayMas), nameof(Ocultos), nameof(TextoBoton));
}
