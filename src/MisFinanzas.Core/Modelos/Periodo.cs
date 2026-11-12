namespace MisFinanzas.Core.Modelos;

/// <summary>
/// Un mes del calendario (año + mes). Reemplaza los códigos tipo "NOV26" del Excel.
/// Se guarda en la base de datos como un entero AAAAMM (ej: 202611).
/// </summary>
public readonly record struct Periodo : IComparable<Periodo>
{
    private static readonly string[] Abreviaturas =
        ["ENE", "FEB", "MAR", "ABR", "MAY", "JUN", "JUL", "AGO", "SEP", "OCT", "NOV", "DIC"];

    private static readonly string[] Nombres =
        ["Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
         "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"];

    public int Anio { get; }
    public int NumeroMes { get; }

    public Periodo(int anio, int numeroMes)
    {
        if (numeroMes is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(numeroMes), "El mes debe estar entre 1 y 12.");
        if (anio is < 2000 or > 2099)
            throw new ArgumentOutOfRangeException(nameof(anio), "El año debe estar entre 2000 y 2099.");
        Anio = anio;
        NumeroMes = numeroMes;
    }

    /// <summary>Entero AAAAMM, ej: 202611. Es lo que se guarda en la base de datos.</summary>
    public int Clave => Anio * 100 + NumeroMes;

    /// <summary>Código corto estilo Excel, ej: "NOV26".</summary>
    public string Codigo => $"{Abreviaturas[NumeroMes - 1]}{Anio % 100:00}";

    /// <summary>Nombre legible, ej: "Noviembre 2026".</summary>
    public string NombreLargo => $"{Nombres[NumeroMes - 1]} {Anio}";

    /// <summary>Formato ISO, ej: "2026-11". Útil para nombres de archivo.</summary>
    public string Iso => $"{Anio:0000}-{NumeroMes:00}";

    public Periodo Siguiente() => NumeroMes == 12 ? new(Anio + 1, 1) : new(Anio, NumeroMes + 1);
    public Periodo Anterior() => NumeroMes == 1 ? new(Anio - 1, 12) : new(Anio, NumeroMes - 1);

    public static Periodo DesdeFecha(DateTime fecha) => new(fecha.Year, fecha.Month);
    public static Periodo Actual() => DesdeFecha(DateTime.Today);

    public static Periodo DesdeClave(int clave) => new(clave / 100, clave % 100);

    /// <summary>Lee un código estilo Excel ("NOV26"). Útil para importar el Excel más adelante.</summary>
    public static Periodo DesdeCodigo(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 5)
            throw new FormatException($"Código de mes inválido: '{codigo}'.");
        var indice = Array.IndexOf(Abreviaturas, codigo[..3].ToUpperInvariant());
        if (indice < 0 || !int.TryParse(codigo[3..], out var anio2))
            throw new FormatException($"Código de mes inválido: '{codigo}'.");
        return new Periodo(2000 + anio2, indice + 1);
    }

    /// <summary>Todos los periodos desde <paramref name="desde"/> hasta <paramref name="hasta"/>, inclusive.</summary>
    public static IEnumerable<Periodo> Rango(Periodo desde, Periodo hasta)
    {
        for (var p = desde; p <= hasta; p = p.Siguiente())
            yield return p;
    }

    public int CompareTo(Periodo other) => Clave.CompareTo(other.Clave);
    public static bool operator <(Periodo a, Periodo b) => a.Clave < b.Clave;
    public static bool operator >(Periodo a, Periodo b) => a.Clave > b.Clave;
    public static bool operator <=(Periodo a, Periodo b) => a.Clave <= b.Clave;
    public static bool operator >=(Periodo a, Periodo b) => a.Clave >= b.Clave;

    public override string ToString() => Codigo;
}
