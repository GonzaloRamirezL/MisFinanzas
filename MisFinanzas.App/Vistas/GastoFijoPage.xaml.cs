using MisFinanzas.App.ViewModels;

namespace MisFinanzas.App.Vistas;

public partial class GastoFijoPage : ContentPage
{
    private readonly GastoFijoFormViewModel _vm;

    public GastoFijoPage(GastoFijoFormViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override bool OnBackButtonPressed()
    {
        _vm.CerrarSinGuardar();
        return true;
    }
}
