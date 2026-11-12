using System.Globalization;

namespace MisFinanzas.Core.Exportacion;

/// <summary>
/// Formato chileno fijo ($1.234.567 y 85,3 %), sin depender del idioma configurado en el teléfono.
/// </summary>
public static class Formato
{
    private static readonly NumberFormatInfo Chile = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
        NegativeSign = "-",
    };

    public static string Pesos(decimal monto)
    {
        var redondeado = Math.Round(monto, 0, MidpointRounding.AwayFromZero);
        var texto = Math.Abs(redondeado).ToString("#,0", Chile);
        return redondeado < 0 ? $"-${texto}" : $"${texto}";
    }

    /// <summary>0,853 → "85,3 %".</summary>
    public static string Porcentaje(decimal fraccion) =>
        (fraccion * 100).ToString("0.0", Chile) + " %";

    public static string Fecha(DateTime? fecha) => fecha?.ToString("yyyy-MM-dd") ?? "–";
}
