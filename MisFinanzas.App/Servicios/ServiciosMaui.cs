using MisFinanzas.App.ViewModels;
using MisFinanzas.App.Vistas;

namespace MisFinanzas.App.Servicios;

/// <summary>Diálogos nativos (alertas, menús, pedir un número) sobre la página visible.</summary>
public class DialogosMaui : IDialogos
{
    private static Page PaginaActual
    {
        get
        {
            var shell = Shell.Current;
            return shell.Navigation.ModalStack.LastOrDefault() ?? shell.CurrentPage ?? shell;
        }
    }

    public Task AvisarAsync(string titulo, string mensaje) =>
        MainThread.InvokeOnMainThreadAsync(() => PaginaActual.DisplayAlertAsync(titulo, mensaje, "OK"));

    public Task<bool> ConfirmarAsync(string titulo, string mensaje, string aceptar = "Sí", string cancelar = "Cancelar") =>
        MainThread.InvokeOnMainThreadAsync(() => PaginaActual.DisplayAlertAsync(titulo, mensaje, aceptar, cancelar));

    public async Task<string?> ElegirAsync(string titulo, string? destructiva, params string[] opciones)
    {
        const string cancelar = "Cancelar";
        var eleccion = await MainThread.InvokeOnMainThreadAsync(() =>
            PaginaActual.DisplayActionSheetAsync(titulo, cancelar, destructiva, opciones));
        return eleccion is null or cancelar ? null : eleccion;
    }

    public Task<string?> PedirNumeroAsync(string titulo, string mensaje, string valorInicial) =>
        MainThread.InvokeOnMainThreadAsync(() => PaginaActual.DisplayPromptAsync(
            titulo, mensaje, accept: "Guardar", cancel: "Cancelar",
            placeholder: "0", maxLength: 14, keyboard: Keyboard.Numeric, initialValue: valorInicial));
}

/// <summary>Abre formularios como ventanas modales y espera a que se cierren.</summary>
public class NavegacionMaui : INavegacion
{
    public async Task<bool> AbrirFormularioAsync(FormularioBase formulario)
    {
        Page pagina = formulario switch
        {
            MovimientoViewModel m => new MovimientoPage(m),
            CategoriaFormViewModel c => new CategoriaPage(c),
            GastoFijoFormViewModel g => new GastoFijoPage(g),
            _ => throw new ArgumentException($"No hay pantalla para {formulario.GetType().Name}"),
        };

        var navegacion = Shell.Current.Navigation;
        await navegacion.PushModalAsync(pagina);
        var guardado = await formulario.Resultado;
        await navegacion.PopModalAsync();
        return guardado;
    }
}

/// <summary>Guarda el archivo en la caché de la app y abre el menú "Compartir" de Android.</summary>
public class CompartirMaui : ICompartir
{
    public async Task CompartirArchivoAsync(string nombreArchivo, string contenido, string titulo)
    {
        var ruta = Path.Combine(FileSystem.CacheDirectory, nombreArchivo);
        await File.WriteAllTextAsync(ruta, contenido);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = titulo,
            File = new ShareFile(ruta, "text/markdown"),
        });
    }
}

/// <summary>Guarda el tema elegido en las preferencias del teléfono y lo aplica.</summary>
public class PreferenciaTemaMaui : IPreferenciaTema
{
    private const string Clave = "tema";

    public TemaApp Actual =>
        Enum.TryParse<TemaApp>(Preferences.Default.Get(Clave, nameof(TemaApp.Sistema)), out var t) ? t : TemaApp.Sistema;

    public void Cambiar(TemaApp tema)
    {
        Preferences.Default.Set(Clave, tema.ToString());
        Aplicar(tema);
    }

    /// <summary>Se llama al abrir la app para respetar lo que se eligió la última vez.</summary>
    public void AplicarGuardado() => Aplicar(Actual);

    private static void Aplicar(TemaApp tema)
    {
        if (Application.Current is null) return;
        Application.Current.UserAppTheme = tema switch
        {
            TemaApp.Claro => AppTheme.Light,
            TemaApp.Oscuro => AppTheme.Dark,
            _ => AppTheme.Unspecified,
        };
    }
}
