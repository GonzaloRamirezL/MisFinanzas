using MisFinanzas.Core.Modelos;

namespace MisFinanzas.Core.Datos;

/// <summary>
/// Lo que trae la app la primera vez que se abre. Son las mismas categorías del Excel,
/// pero todo es editable desde la pantalla de configuración.
/// (Los gastos permanentes no vienen precargados: los agregas tú desde la app.)
/// </summary>
public static class DatosIniciales
{
    public static List<Categoria> Categorias() =>
    [
        new() { Nombre = "Servicios", Emoji = "💡", Orden = 1 },
        new() { Nombre = "Hogar", Emoji = "🏠", Orden = 2 },
        new() { Nombre = "Familia", Emoji = "👨‍👩‍👧", Orden = 3 },
        new() { Nombre = "Salud", Emoji = "🩺", Orden = 4 },
        new() { Nombre = "Mascotas", Emoji = "🐾", Orden = 5 },
        new() { Nombre = "Tarjetas y créditos", Emoji = "💳", Orden = 6 },
        new() { Nombre = "Personal", Emoji = "🌸", Orden = 7 },
        new() { Nombre = "Otros", Emoji = "📦", Orden = 8 },
    ];
}
