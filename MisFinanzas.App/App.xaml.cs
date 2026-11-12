using MisFinanzas.App.Servicios;

namespace MisFinanzas.App;

public partial class App : Application
{
    public App(PreferenciaTemaMaui tema)
    {
        InitializeComponent();
        // Claro, oscuro o el del teléfono, según lo elegido en Ajustes → Apariencia.
        tema.AplicarGuardado();
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new AppShell());
}
