using System.Windows.Input;
using MisFinanzas.App.Utilidades;
using MisFinanzas.Core.Calculos;
using MisFinanzas.Core.Exportacion;
using MisFinanzas.Core.Modelos;

namespace MisFinanzas.App.ViewModels;

/// <summary>Una fila de las listas del mes, ya formateada para mostrar.</summary>
public class MovimientoItem
{
    public MovimientoItem(Movimiento modelo, Categoria? categoria, Func<MovimientoItem, Task>? alTocar = null)
    {
        Modelo = modelo;
        TocarCommand = new ComandoAsync(() => alTocar?.Invoke(this) ?? Task.CompletedTask);
        Concepto = modelo.Concepto;
        MontoTexto = Formato.Pesos(modelo.Monto);
        Emoji = modelo.Tipo == TipoMovimiento.PagoRecibido ? "💌" : categoria?.Emoji ?? "🏷️";
        MuestraEstado = modelo.Tipo is TipoMovimiento.CuentaFija or TipoMovimiento.Deuda;
        EstaPagado = modelo.Estado == EstadoPago.Pagado;
        EstadoTexto = EstaPagado ? "Pagado" : "Pendiente";

        Detalle = modelo.Tipo switch
        {
            TipoMovimiento.Compra => $"{categoria?.Nombre ?? "Sin categoría"} · {modelo.Fecha:dd/MM}",
            TipoMovimiento.PagoRecibido => string.IsNullOrWhiteSpace(modelo.DeQuien)
                ? (modelo.Fecha is null ? "" : $"{modelo.Fecha:dd/MM}")
                : $"{modelo.DeQuien} · {modelo.Fecha:dd/MM}",
            _ => categoria?.Nombre ?? "Sin categoría",
        };
    }

    public Movimiento Modelo { get; }
    /// <summary>Lo que pasa al tocar la fila (abre el menú de opciones).</summary>
    public ICommand TocarCommand { get; }
    public string Emoji { get; }
    public string Concepto { get; }
    public string Detalle { get; }
    public string MontoTexto { get; }
    public bool MuestraEstado { get; }
    public bool EstaPagado { get; }
    public bool EstaPendiente => MuestraEstado && !EstaPagado;
    public string EstadoTexto { get; }
}

/// <summary>Una fila de "¿En qué se fue?".</summary>
public class CategoriaItem(ResumenCategoria r)
{
    public string Emoji { get; } = r.Emoji;
    public string Nombre { get; } = r.Nombre;
    public string GastadoTexto { get; } = Formato.Pesos(r.Gastado);
    public bool TienePresupuesto { get; } = r.Presupuesto is not null;

    public string DetalleTexto { get; } = r.Presupuesto is null
        ? "Sin presupuesto"
        : $"de {Formato.Pesos(r.Presupuesto.Value)} · {ExportadorMarkdown.TextoAlerta(r.Alerta)}";

    /// <summary>0 a 1 para la barra. Si se pasó del presupuesto queda llena.</summary>
    public double Progreso { get; } = r.PorcentajeUsado is null ? 0 : (double)Math.Min(r.PorcentajeUsado.Value, 1m);

    /// <summary>"Ok", "AlLimite", "MePase" o "SinPresupuesto". La vista lo usa para el color de la barra.</summary>
    public string Estado { get; } = r.Alerta.ToString();
}
