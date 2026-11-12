using MisFinanzas.App.ViewModels;

namespace MisFinanzas.App.Servicios;

// Contratos que usan los ViewModels. Las implementaciones con MAUI están en *Maui.cs.
// Así los ViewModels no conocen Shell, páginas ni APIs del teléfono.

public interface IDialogos
{
    Task AvisarAsync(string titulo, string mensaje);
    Task<bool> ConfirmarAsync(string titulo, string mensaje, string aceptar = "Sí", string cancelar = "Cancelar");
    /// <summary>Devuelve la opción elegida, o null si se canceló.</summary>
    Task<string?> ElegirAsync(string titulo, string? destructiva, params string[] opciones);
    /// <summary>Pide un número. Devuelve null si se canceló.</summary>
    Task<string?> PedirNumeroAsync(string titulo, string mensaje, string valorInicial);
}

public interface INavegacion
{
    /// <summary>Abre el formulario que corresponda y espera a que se cierre. Devuelve true si se guardó.</summary>
    Task<bool> AbrirFormularioAsync(FormularioBase formulario);
}

/// <summary>Apariencia de la app.</summary>
public enum TemaApp
{
    /// <summary>Sigue el modo claro u oscuro del teléfono.</summary>
    Sistema,
    Claro,
    Oscuro,
}

public interface IPreferenciaTema
{
    TemaApp Actual { get; }
    /// <summary>Aplica el tema al instante y lo recuerda para la próxima vez.</summary>
    void Cambiar(TemaApp tema);
}

public interface ICompartir
{
    /// <summary>Guarda el texto como archivo y abre el menú "Compartir" del teléfono (Drive, correo, etc.).</summary>
    Task CompartirArchivoAsync(string nombreArchivo, string contenido, string titulo);
}
