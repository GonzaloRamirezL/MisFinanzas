using MisFinanzas.Core.Datos;
using MisFinanzas.Core.Modelos;

namespace MisFinanzas.Core.Tests;

/// <summary>Repositorio falso para probar la lógica sin SQLite ni teléfono.</summary>
public class RepositorioEnMemoria : IFinanzasRepositorio
{
    public List<Categoria> Categorias { get; } = [];
    public List<GastoFijo> GastosFijos { get; } = [];
    public List<Mes> Meses { get; } = [];
    public List<Movimiento> Movimientos { get; } = [];
    public Ajustes Ajustes { get; set; } = new();
    private int _siguienteId = 1;

    public Task InicializarAsync()
    {
        if (Categorias.Count == 0)
            foreach (var c in DatosIniciales.Categorias()) { c.Id = _siguienteId++; Categorias.Add(c); }
        return Task.CompletedTask;
    }

    public Task<List<Categoria>> ObtenerCategoriasAsync(bool incluirArchivadas = false) =>
        Task.FromResult(Categorias.Where(c => incluirArchivadas || c.Activa).OrderBy(c => c.Orden).ToList());

    public Task GuardarCategoriaAsync(Categoria c) => Guardar(Categorias, c, c.Id, id => c.Id = id);
    public Task EliminarCategoriaAsync(int id) { Categorias.RemoveAll(c => c.Id == id); return Task.CompletedTask; }

    public Task<bool> CategoriaTieneUsoAsync(int id) =>
        Task.FromResult(Movimientos.Any(m => m.CategoriaId == id) || GastosFijos.Any(g => g.CategoriaId == id));

    public Task<List<GastoFijo>> ObtenerGastosFijosAsync() => Task.FromResult(GastosFijos.ToList());
    public Task GuardarGastoFijoAsync(GastoFijo g) => Guardar(GastosFijos, g, g.Id, id => g.Id = id);
    public Task EliminarGastoFijoAsync(int id) { GastosFijos.RemoveAll(g => g.Id == id); return Task.CompletedTask; }

    public Task<Mes?> ObtenerMesAsync(int clave) => Task.FromResult(Meses.FirstOrDefault(m => m.Clave == clave));
    public Task<List<Mes>> ObtenerMesesAsync() => Task.FromResult(Meses.OrderBy(m => m.Clave).ToList());

    public Task GuardarMesAsync(Mes mes)
    {
        Meses.RemoveAll(m => m.Clave == mes.Clave);
        Meses.Add(mes);
        return Task.CompletedTask;
    }

    public Task<List<Movimiento>> ObtenerMovimientosAsync(int clave) =>
        Task.FromResult(Movimientos.Where(m => m.MesClave == clave).ToList());

    public Task<List<Movimiento>> ObtenerMovimientosDeGastoFijoAsync(int gastoFijoId) =>
        Task.FromResult(Movimientos.Where(m => m.GastoFijoId == gastoFijoId).ToList());

    public Task GuardarMovimientoAsync(Movimiento m) => Guardar(Movimientos, m, m.Id, id => m.Id = id);
    public Task EliminarMovimientoAsync(int id) { Movimientos.RemoveAll(m => m.Id == id); return Task.CompletedTask; }

    public Task<Ajustes> ObtenerAjustesAsync() => Task.FromResult(Ajustes);
    public Task GuardarAjustesAsync(Ajustes a) { Ajustes = a; return Task.CompletedTask; }

    private Task Guardar<T>(List<T> lista, T entidad, int id, Action<int> asignarId)
    {
        if (id == 0) { asignarId(_siguienteId++); lista.Add(entidad); }
        // Si ya tiene Id, es la misma instancia en la lista: no hay nada que hacer.
        else if (!lista.Contains(entidad)) lista.Add(entidad);
        return Task.CompletedTask;
    }
}
