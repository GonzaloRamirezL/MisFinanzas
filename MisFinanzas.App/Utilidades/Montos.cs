using System.Globalization;

namespace MisFinanzas.App.Utilidades;

public static class Montos
{
    /// <summary>
    /// Lee lo que la usuaria escribe: "82900", "82.900", "$82.900" o "82900,5".
    /// En pesos chilenos el punto es separador de miles y la coma, de decimales.
    /// </summary>
    public static bool TryParsear(string? texto, out decimal monto)
    {
        monto = 0;
        if (string.IsNullOrWhiteSpace(texto)) return false;

        var limpio = texto.Replace("$", "").Replace(" ", "").Replace(".", "").Replace(",", ".");
        return decimal.TryParse(limpio, NumberStyles.Number, CultureInfo.InvariantCulture, out monto) && monto >= 0;
    }

    /// <summary>Texto para precargar un campo al editar (sin símbolo ni separadores).</summary>
    public static string ParaEditar(decimal monto) =>
        monto % 1 == 0
            ? monto.ToString("0", CultureInfo.InvariantCulture)
            : monto.ToString("0.##", CultureInfo.InvariantCulture).Replace(".", ",");
}
