using Microsoft.Extensions.Logging;
using MisFinanzas.App.Servicios;
using MisFinanzas.App.Utilidades;
using MisFinanzas.App.ViewModels;
using MisFinanzas.App.Vistas;
using MisFinanzas.Core.Datos;
using MisFinanzas.Core.Exportacion;
using MisFinanzas.Core.Servicios;
using MisFinanzas.Datos;

namespace MisFinanzas.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                // M PLUS Rounded 1c (Google Fonts). Ver docs/paso-2-pantalla-del-mes.md.
                fonts.AddFont("MPLUSRounded1c-Regular.ttf", "Redonda");
                fonts.AddFont("MPLUSRounded1c-Medium.ttf", "RedondaMedia");
                fonts.AddFont("MPLUSRounded1c-Bold.ttf", "RedondaNegrita");
            });

        // Datos y lógica (proyectos Core y Datos)
        builder.Services.AddSingleton<IFinanzasRepositorio>(_ =>
            new SqliteFinanzasRepositorio(Path.Combine(FileSystem.AppDataDirectory, "misfinanzas.db3")));
        builder.Services.AddSingleton(sp => new ServicioFinanzas(sp.GetRequiredService<IFinanzasRepositorio>()));
        builder.Services.AddSingleton(_ => new ExportadorMarkdown());

        // Servicios de la app
        builder.Services.AddSingleton<IDialogos, DialogosMaui>();
        builder.Services.AddSingleton<INavegacion, NavegacionMaui>();
        builder.Services.AddSingleton<ICompartir, CompartirMaui>();
        builder.Services.AddSingleton<PreferenciaTemaMaui>();
        builder.Services.AddSingleton<IPreferenciaTema>(sp => sp.GetRequiredService<PreferenciaTemaMaui>());

        // Pantallas
        builder.Services.AddSingleton<MesViewModel>();
        builder.Services.AddSingleton<MesPage>();
        builder.Services.AddSingleton<AjustesViewModel>();
        builder.Services.AddSingleton<AjustesPage>();
        builder.Services.AddSingleton<ResumenViewModel>();
        builder.Services.AddSingleton<ResumenPage>();

        QuitarLineaDeLosCampos();

        // Si un botón falla, se muestra un aviso en vez de cerrar la app.
        ComandoAsync.ErrorNoControlado = ex => MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (Shell.Current is { } shell)
                await shell.DisplayAlertAsync("Algo salió mal 😿", ex.Message, "OK");
        });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    /// <summary>
    /// Android dibuja una línea bajo los campos de texto. Como los campos ya van dentro
    /// de una caja redondeada (estilo CajaCampo), se quita esa línea.
    /// </summary>
    private static void QuitarLineaDeLosCampos()
    {
#if ANDROID
        var transparente = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("SinLinea",
            (h, _) => h.PlatformView.BackgroundTintList = transparente);
        Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("SinLinea",
            (h, _) => h.PlatformView.BackgroundTintList = transparente);
        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("SinLinea",
            (h, _) => h.PlatformView.BackgroundTintList = transparente);
        Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("SinLinea",
            (h, _) => h.PlatformView.BackgroundTintList = transparente);
#endif
    }
}
