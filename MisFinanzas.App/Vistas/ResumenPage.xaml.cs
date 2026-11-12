using MisFinanzas.App.ViewModels;

namespace MisFinanzas.App.Vistas;

public partial class ResumenPage : ContentPage
{
    private readonly ResumenViewModel _vm;

    public ResumenPage(ResumenViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

        // Las barras se dibujan en proporción al ancho real de la tarjeta (cambia al girar el teléfono).
        ContenedorGrafico.SizeChanged += (_, _) => _vm.EstablecerAnchoGrafico(ContenedorGrafico.Width);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.CargarCommand.Execute(null);
    }
}
