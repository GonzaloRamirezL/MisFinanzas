using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MisFinanzas.App.Utilidades;

/// <summary>
/// Base mínima para ViewModels: avisa a la pantalla cuando una propiedad cambia.
/// Se escribió a mano (sin CommunityToolkit) para que los ViewModels no dependan de MAUI
/// y se puedan probar como código .NET normal.
/// </summary>
public abstract class Observable : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool Asignar<T>(ref T campo, T valor, [CallerMemberName] string? propiedad = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor)) return false;
        campo = valor;
        Notificar(propiedad);
        return true;
    }

    protected void Notificar([CallerMemberName] string? propiedad = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));

    /// <summary>Avisa varias propiedades calculadas de una vez.</summary>
    protected void Notificar(params string[] propiedades)
    {
        foreach (var p in propiedades) Notificar(p);
    }
}
