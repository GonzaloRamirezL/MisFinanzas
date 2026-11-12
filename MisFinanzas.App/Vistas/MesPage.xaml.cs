using MisFinanzas.App.ViewModels;

namespace MisFinanzas.App.Vistas;

public partial class MesPage : ContentPage
{
    private readonly MesViewModel _vm;

    public MesPage(MesViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Recarga cada vez que vuelves a esta pestaña (por ejemplo, después de cambiar ajustes).
        _vm.CargarCommand.Execute(null);
    }
}
