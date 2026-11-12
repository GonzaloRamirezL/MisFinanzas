using MisFinanzas.Core.Datos;
using MisFinanzas.Core.Modelos;
using SQLite;

namespace MisFinanzas.Datos;

/// <summary>
/// Guarda todo en un archivo SQLite dentro del teléfono. Sin nube, sin cuentas.
/// En la app MAUI la ruta será: Path.Combine(FileSystem.AppDataDirectory, "misfinanzas.db3").
/// </summary>
public class SqliteFinanzasRepositorio : IFinanzasRepositorio
{
    private readonly SQLiteAsyncConnection _db;
    private bool _inicializado;

    // "Id" pasa a ser llave primaria autoincremental sin tener que ensuciar los modelos con atributos.
    private const CreateFlags LlaveImplicita = CreateFlags.ImplicitPK | CreateFlags.AutoIncPK;

    public SqliteFinanzasRepositorio(string rutaBaseDatos)
    {
        _db = new SQLiteAsyncConnection(rutaBaseDatos,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
    }

    public async Task InicializarAsync()
    {
        if (_inicializado) return;

        await _db.CreateTableAsync<Categoria>(LlaveImplicita);
        await _db.CreateTableAsync<GastoFijo>(LlaveImplicita);
        await _db.CreateTableAsync<Movimiento>(LlaveImplicita);
        await _db.CreateTableAsync<MesFila>();
        await _db.CreateTableAsync<Ajustes>(CreateFlags.ImplicitPK);

        await _db.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Movimiento_MesClave ON Movimiento (MesClave)");
        await _db.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Movimiento_GastoFijoId ON Movimiento (GastoFijoId)");

        if (await _db.Table<Categoria>().CountAsync() == 0)
            await _db.InsertAllAsync(DatosIniciales.Categorias());

        if (await _db.Table<Ajustes>().CountAsync() == 0)
            await _db.InsertAsync(new Ajustes());

        _inicializado = true;
    }

    // ───────────── Categorías ─────────────

    public async Task<List<Categoria>> ObtenerCategoriasAsync(bool incluirArchivadas = false)
    {
        var todas = await _db.Table<Categoria>().ToListAsync();
        return todas
            .Where(c => incluirArchivadas || c.Activa)
            .OrderBy(c => c.Orden).ThenBy(c => c.Nombre)
            .ToList();
    }

    public Task GuardarCategoriaAsync(Categoria categoria) => Guardar(categoria, categoria.Id);

    public Task EliminarCategoriaAsync(int categoriaId) => _db.DeleteAsync<Categoria>(categoriaId);

    public async Task<bool> CategoriaTieneUsoAsync(int categoriaId)
    {
        var enMovimientos = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Movimiento WHERE CategoriaId = ?", categoriaId);
        var enGastosFijos = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM GastoFijo WHERE CategoriaId = ?", categoriaId);
        return enMovimientos + enGastosFijos > 0;
    }

    // ───────────── Gastos permanentes ─────────────

    public async Task<List<GastoFijo>> ObtenerGastosFijosAsync() =>
        (await _db.Table<GastoFijo>().ToListAsync())
            .OrderBy(g => g.Orden).ThenBy(g => g.Concepto)
            .ToList();

    public Task GuardarGastoFijoAsync(GastoFijo gasto) => Guardar(gasto, gasto.Id);

    public Task EliminarGastoFijoAsync(int gastoFijoId) => _db.DeleteAsync<GastoFijo>(gastoFijoId);

    // ───────────── Meses ─────────────

    public async Task<Mes?> ObtenerMesAsync(int clave)
    {
        var fila = await _db.FindAsync<MesFila>(clave);
        return fila?.AModelo();
    }

    public async Task<List<Mes>> ObtenerMesesAsync() =>
        (await _db.Table<MesFila>().OrderBy(m => m.Clave).ToListAsync())
            .Select(f => f.AModelo())
            .ToList();

    public Task GuardarMesAsync(Mes mes) => _db.InsertOrReplaceAsync(MesFila.DesdeModelo(mes));

    // ───────────── Movimientos ─────────────

    public Task<List<Movimiento>> ObtenerMovimientosAsync(int mesClave) =>
        _db.Table<Movimiento>().Where(m => m.MesClave == mesClave).ToListAsync();

    public Task<List<Movimiento>> ObtenerMovimientosDeGastoFijoAsync(int gastoFijoId) =>
        _db.Table<Movimiento>().Where(m => m.GastoFijoId == gastoFijoId).ToListAsync();

    public Task GuardarMovimientoAsync(Movimiento movimiento) => Guardar(movimiento, movimiento.Id);

    public Task EliminarMovimientoAsync(int movimientoId) => _db.DeleteAsync<Movimiento>(movimientoId);

    // ───────────── Ajustes ─────────────

    public async Task<Ajustes> ObtenerAjustesAsync() =>
        await _db.FindAsync<Ajustes>(1) ?? new Ajustes();

    public Task GuardarAjustesAsync(Ajustes ajustes)
    {
        ajustes.Id = 1;
        return _db.InsertOrReplaceAsync(ajustes);
    }

    // ───────────── Utilidades ─────────────

    /// <summary>Id 0 = registro nuevo (SQLite le asigna Id). Si no, se actualiza.</summary>
    private Task Guardar(object entidad, int id) =>
        id == 0 ? _db.InsertAsync(entidad) : _db.UpdateAsync(entidad);
}

/// <summary>Tabla de meses. La llave es la clave AAAAMM, no un Id autoincremental.</summary>
[Table("Mes")]
internal sealed class MesFila
{
    [PrimaryKey]
    public int Clave { get; set; }
    public decimal Sueldo { get; set; }
    public string? Nota { get; set; }

    public Mes AModelo() => new() { Clave = Clave, Sueldo = Sueldo, Nota = Nota };
    public static MesFila DesdeModelo(Mes m) => new() { Clave = m.Clave, Sueldo = m.Sueldo, Nota = m.Nota };
}
