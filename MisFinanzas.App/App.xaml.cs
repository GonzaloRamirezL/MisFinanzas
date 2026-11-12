namespace MisFinanzas.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        // La paleta pastel está pensada para modo claro; así se ve igual aunque el teléfono esté en modo oscuro.
        UserAppTheme = AppTheme.Light;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new AppShell());
}
