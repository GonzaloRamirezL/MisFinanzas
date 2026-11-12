using MisFinanzas.App.ViewModels;

namespace MisFinanzas.App.Vistas;

public partial class AjustesPage : ContentPage
{
    private readonly AjustesViewModel _vm;

    public AjustesPage(AjustesViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.CargarCommand.Execute(null);
    }
}
