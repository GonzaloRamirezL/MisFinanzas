namespace MisFinanzas.App.Vistas;

/// <summary>Pantalla genérica "Próximamente" para funciones futuras. Hoy no la usa ninguna pestaña.</summary>
public partial class ProximamentePage : ContentPage
{
    public ProximamentePage(string titulo, string descripcion)
    {
        InitializeComponent();
        TituloLabel.Text = titulo;
        DescripcionLabel.Text = descripcion;
    }
}
