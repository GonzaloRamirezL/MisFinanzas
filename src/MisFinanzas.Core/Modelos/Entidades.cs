namespace MisFinanzas.Core.Modelos;

// Entidades simples (POCO). No dependen de ninguna librería: el proyecto MisFinanzas.Datos
// se encarga de guardarlas en SQLite. Así el "cerebro" de la app se puede probar sin teléfono.

/// <summary>Categoría de gasto con presupuesto opcional. Se crean, editan y archivan desde la app.</summary>
public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Emoji { get; set; } = "🏷️";
    /// <summary>Presupuesto mensual. null = sin presupuesto (no genera alertas).</summary>
    public decimal? PresupuestoMensual { get; set; }
    public int Orden { get; set; }
    /// <summary>Las categorías con historial no se borran: se archivan para no romper meses pasados.</summary>
    public bool Activa { get; set; } = true;
}

/// <summary>
/// Gasto permanente (la tabla "Gastos permanentes" del Excel). Es una plantilla:
/// cada mes dentro del rango Desde/Hasta se copia como un <see cref="Movimiento"/> de tipo CuentaFija.
/// Cambiar el monto habitual NO modifica meses pasados.
/// </summary>
public class GastoFijo
{
    public int Id { get; set; }
    public string Concepto { get; set; } = "";
    public int CategoriaId { get; set; }
    public decimal MontoHabitual { get; set; }
    /// <summary>Primer mes en que aparece (clave AAAAMM).</summary>
    public int DesdeClave { get; set; }
    /// <summary>Último mes en que aparece (clave AAAAMM). null = indefinido.</summary>
    public int? HastaClave { get; set; }
    public int Orden { get; set; }

    public bool AplicaEn(Periodo periodo) =>
        periodo.Clave >= DesdeClave && (HastaClave is null || periodo.Clave <= HastaClave);
}

/// <summary>Un mes abierto en la app, con su sueldo.</summary>
public class Mes
{
    /// <summary>Clave AAAAMM, también es la llave primaria.</summary>
    public int Clave { get; set; }
    public decimal Sueldo { get; set; }
    public string? Nota { get; set; }

    public Periodo Periodo => Periodo.DesdeClave(Clave);
}

public enum TipoMovimiento
{
    /// <summary>Cuenta fija generada desde un GastoFijo (o agregada a mano a la sección de fijas).</summary>
    CuentaFija = 0,
    /// <summary>"Deudas y gastos del mes": gastos sueltos que pueden quedar pendientes.</summary>
    Deuda = 1,
    /// <summary>Compras: se asumen pagadas al momento.</summary>
    Compra = 2,
    /// <summary>"Me pagaron": ingreso extra además del sueldo.</summary>
    PagoRecibido = 3,
}

public enum EstadoPago
{
    Pendiente = 0,
    Pagado = 1,
}

/// <summary>
/// Cualquier línea dentro de un mes: cuenta fija, deuda, compra o pago recibido.
/// Un solo tipo de registro para todo simplifica los cálculos y la exportación.
/// </summary>
public class Movimiento
{
    public int Id { get; set; }
    public int MesClave { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public string Concepto { get; set; } = "";
    /// <summary>null para pagos recibidos (no son gasto de ninguna categoría).</summary>
    public int? CategoriaId { get; set; }
    public decimal Monto { get; set; }
    public EstadoPago Estado { get; set; } = EstadoPago.Pendiente;
    public DateTime? Fecha { get; set; }
    /// <summary>Solo para PagoRecibido: quién te pagó.</summary>
    public string? DeQuien { get; set; }
    public string? Nota { get; set; }
    /// <summary>Si vino de un gasto permanente, su Id. Sirve para no duplicarlo y para comparar con el mes anterior.</summary>
    public int? GastoFijoId { get; set; }
    /// <summary>
    /// Una cuenta fija que quitaste solo de este mes. No se borra para que la sincronización
    /// no la vuelva a crear; simplemente no cuenta en los cálculos.
    /// </summary>
    public bool Omitido { get; set; }

    public bool EsGasto => Tipo != TipoMovimiento.PagoRecibido;
}

/// <summary>Ajustes generales (la sección "Alertas" del Excel). Una sola fila.</summary>
public class Ajustes
{
    public int Id { get; set; } = 1;
    /// <summary>Avisar si una cuenta sube más de este % respecto al mes anterior (0,2 = 20 %).</summary>
    public decimal UmbralSubidaCuenta { get; set; } = 0.20m;
    /// <summary>Avisar si el gasto total supera este % del ingreso (0,9 = 90 %).</summary>
    public decimal UmbralGastoIngreso { get; set; } = 0.90m;
    /// <summary>Una categoría se marca "Al límite" al pasar este % de su presupuesto.</summary>
    public decimal UmbralAlLimiteCategoria { get; set; } = 0.90m;
    public string Moneda { get; set; } = "CLP";
    public string NombreUsuaria { get; set; } = "";
}
