using MisFinanzas.Core.Modelos;

namespace MisFinanzas.Core.Datos;

/// <summary>
/// Contrato de acceso a datos. La app usa la implementación SQLite (proyecto MisFinanzas.Datos);
/// las pruebas usan una versión en memoria. El resto del código no sabe ni le importa cuál es.
/// </summary>
public interface IFinanzasRepositorio
{
    Task InicializarAsync();

    // Categorías
    Task<List<Categoria>> ObtenerCategoriasAsync(bool incluirArchivadas = false);
    Task GuardarCategoriaAsync(Categoria categoria);
    Task EliminarCategoriaAsync(int categoriaId);
    Task<bool> CategoriaTieneUsoAsync(int categoriaId);

    // Gastos permanentes
    Task<List<GastoFijo>> ObtenerGastosFijosAsync();
    Task GuardarGastoFijoAsync(GastoFijo gasto);
    Task EliminarGastoFijoAsync(int gastoFijoId);

    // Meses
    Task<Mes?> ObtenerMesAsync(int clave);
    Task<List<Mes>> ObtenerMesesAsync();
    Task GuardarMesAsync(Mes mes);

    // Movimientos
    Task<List<Movimiento>> ObtenerMovimientosAsync(int mesClave);
    Task<List<Movimiento>> ObtenerMovimientosDeGastoFijoAsync(int gastoFijoId);
    Task GuardarMovimientoAsync(Movimiento movimiento);
    Task EliminarMovimientoAsync(int movimientoId);

    // Ajustes
    Task<Ajustes> ObtenerAjustesAsync();
    Task GuardarAjustesAsync(Ajustes ajustes);
}
