using MisFinanzas.App.ViewModels;

namespace MisFinanzas.App.Vistas;

public partial class CategoriaPage : ContentPage
{
    private readonly CategoriaFormViewModel _vm;

    public CategoriaPage(CategoriaFormViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    /// <summary>
    /// Toque en un emoji sugerido. Se maneja aquí porque dentro de la lista cada emoji
    /// es un texto suelto y no tiene acceso directo al comando del formulario.
    /// </summary>
    private void AlTocarEmoji(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: string emoji })
            _vm.ElegirEmojiCommand.Execute(emoji);
    }

    protected override bool OnBackButtonPressed()
    {
        _vm.CerrarSinGuardar();
        return true;
    }
}
