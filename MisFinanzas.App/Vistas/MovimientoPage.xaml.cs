using MisFinanzas.App.ViewModels;

namespace MisFinanzas.App.Vistas;

public partial class MovimientoPage : ContentPage
{
    private readonly MovimientoViewModel _vm;

    public MovimientoPage(MovimientoViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    /// <summary>El botón "atrás" de Android equivale a Cancelar.</summary>
    protected override bool OnBackButtonPressed()
    {
        _vm.CerrarSinGuardar();
        return true;
    }
}
