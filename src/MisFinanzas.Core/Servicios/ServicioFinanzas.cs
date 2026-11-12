using MisFinanzas.Core.Calculos;
using MisFinanzas.Core.Datos;
using MisFinanzas.Core.Modelos;

namespace MisFinanzas.Core.Servicios;

/// <summary>
/// Reglas de negocio que necesitan la base de datos: abrir meses, generar las cuentas fijas,
/// archivar categorías, terminar gastos permanentes y calcular resúmenes.
/// Las pantallas de la app solo hablan con este servicio.
/// </summary>
public class ServicioFinanzas(IFinanzasRepositorio repo, Func<DateTime>? reloj = null)
{
    private readonly Func<DateTime> _hoy = reloj ?? (() => DateTime.Today);

    public Periodo PeriodoActual => Periodo.DesdeFecha(_hoy());

    /// <summary>
    /// Prepara la base de datos (tablas y datos iniciales). Se llama al abrir la app;
    /// es seguro llamarlo más de una vez.
    /// </summary>
    public Task InicializarAsync() => repo.InicializarAsync();

    // ───────────── Meses ─────────────

    /// <summary>
    /// Abre un mes: si no existe lo crea (con el sueldo del mes anterior como sugerencia)
    /// y agrega las cuentas fijas que le correspondan y aún no estén.
    /// Es seguro llamarlo muchas veces: nunca duplica.
    /// </summary>
    public async Task<Mes> AbrirMesAsync(Periodo periodo)
    {
        var mes = await repo.ObtenerMesAsync(periodo.Clave);
        if (mes is null)
        {
            var anterior = await repo.ObtenerMesAsync(periodo.Anterior().Clave);
            mes = new Mes { Clave = periodo.Clave, Sueldo = anterior?.Sueldo ?? 0 };
            await repo.GuardarMesAsync(mes);
        }

        await SincronizarCuentasFijasAsync(periodo);
        return mes;
    }

    /// <summary>Abre el mes actual. Lo normal es llamarlo al iniciar la app.</summary>
    public Task<Mes> AbrirMesActualAsync() => AbrirMesAsync(PeriodoActual);

    /// <summary>Meses existentes, del más reciente al más antiguo (como en el Excel: aparecen cuando empiezan).</summary>
    public async Task<List<Mes>> ObtenerMesesAsync() =>
        (await repo.ObtenerMesesAsync()).OrderByDescending(m => m.Clave).ToList();

    public async Task ActualizarSueldoAsync(Periodo periodo, decimal sueldo)
    {
        var mes = await AbrirMesAsync(periodo);
        mes.Sueldo = sueldo;
        await repo.GuardarMesAsync(mes);
    }

    private async Task SincronizarCuentasFijasAsync(Periodo periodo)
    {
        var existentes = await repo.ObtenerMovimientosAsync(periodo.Clave);
        var yaGenerados = existentes
            .Where(m => m.GastoFijoId is not null)
            .Select(m => m.GastoFijoId!.Value)
            .ToHashSet();

        foreach (var gasto in (await repo.ObtenerGastosFijosAsync()).Where(g => g.AplicaEn(periodo)))
        {
            if (yaGenerados.Contains(gasto.Id)) continue;
            await repo.GuardarMovimientoAsync(new Movimiento
            {
                MesClave = periodo.Clave,
                Tipo = TipoMovimiento.CuentaFija,
                Concepto = gasto.Concepto,
                CategoriaId = gasto.CategoriaId,
                Monto = gasto.MontoHabitual,
                Estado = EstadoPago.Pendiente,
                GastoFijoId = gasto.Id,
            });
        }
    }

    // ───────────── Movimientos ─────────────

    public async Task<List<Movimiento>> ObtenerMovimientosAsync(Periodo periodo) =>
        (await repo.ObtenerMovimientosAsync(periodo.Clave)).Where(m => !m.Omitido).ToList();

    public async Task GuardarMovimientoAsync(Movimiento movimiento)
    {
        if (string.IsNullOrWhiteSpace(movimiento.Concepto))
            throw new ArgumentException("El movimiento necesita un concepto.");
        if (movimiento.Monto < 0)
            throw new ArgumentException("El monto no puede ser negativo.");
        if (movimiento.EsGasto && movimiento.CategoriaId is null)
            throw new ArgumentException("Los gastos necesitan una categoría.");

        // Las compras y los pagos recibidos ya ocurrieron: se marcan pagados.
        if (movimiento.Tipo is TipoMovimiento.Compra or TipoMovimiento.PagoRecibido)
            movimiento.Estado = EstadoPago.Pagado;

        await repo.GuardarMovimientoAsync(movimiento);
    }

    /// <summary>
    /// Quita un movimiento del mes. Si vino de un gasto permanente se marca como omitido
    /// (para que no reaparezca); si no, se borra.
    /// </summary>
    public async Task QuitarMovimientoAsync(Movimiento movimiento)
    {
        if (movimiento.GastoFijoId is not null)
        {
            movimiento.Omitido = true;
            await repo.GuardarMovimientoAsync(movimiento);
        }
        else
        {
            await repo.EliminarMovimientoAsync(movimiento.Id);
        }
    }

    public async Task CambiarEstadoAsync(Movimiento movimiento, EstadoPago estado)
    {
        movimiento.Estado = estado;
        await repo.GuardarMovimientoAsync(movimiento);
    }

    // ───────────── Categorías (dinámicas) ─────────────

    public Task<List<Categoria>> ObtenerCategoriasAsync(bool incluirArchivadas = false) =>
        repo.ObtenerCategoriasAsync(incluirArchivadas);

    public async Task GuardarCategoriaAsync(Categoria categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria.Nombre))
            throw new ArgumentException("La categoría necesita un nombre.");

        var todas = await repo.ObtenerCategoriasAsync(incluirArchivadas: true);
        if (todas.Any(c => c.Id != categoria.Id &&
                           string.Equals(c.Nombre.Trim(), categoria.Nombre.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Ya existe una categoría llamada \"{categoria.Nombre}\".");

        if (categoria.Id == 0 && categoria.Orden == 0)
            categoria.Orden = todas.Count == 0 ? 1 : todas.Max(c => c.Orden) + 1;

        await repo.GuardarCategoriaAsync(categoria);
    }

    /// <summary>
    /// Quita una categoría. Si ya se usó en algún movimiento o gasto permanente, se archiva
    /// (deja de aparecer para cosas nuevas, pero el historial queda intacto). Si no, se borra.
    /// Devuelve true si se borró y false si se archivó.
    /// </summary>
    public async Task<bool> QuitarCategoriaAsync(Categoria categoria)
    {
        if (await repo.CategoriaTieneUsoAsync(categoria.Id))
        {
            categoria.Activa = false;
            await repo.GuardarCategoriaAsync(categoria);
            return false;
        }
        await repo.EliminarCategoriaAsync(categoria.Id);
        return true;
    }

    // ───────────── Gastos permanentes (dinámicos) ─────────────

    public async Task<List<GastoFijo>> ObtenerGastosFijosAsync(bool soloVigentes = false)
    {
        var todos = await repo.ObtenerGastosFijosAsync();
        return soloVigentes ? todos.Where(g => g.AplicaEn(PeriodoActual)).ToList() : todos;
    }

    /// <summary>
    /// Crea o edita un gasto permanente. Si es nuevo, aparece de inmediato en los meses
    /// ya abiertos que caen dentro de su rango. Editar el monto no cambia meses pasados.
    /// </summary>
    /// <param name="actualizarMesActual">
    /// Si es true y la cuenta del mes actual sigue pendiente, también se le aplican
    /// el nuevo concepto, categoría y monto (útil cuando sube el precio de una suscripción).
    /// </param>
    public async Task GuardarGastoFijoAsync(GastoFijo gasto, bool actualizarMesActual = false)
    {
        if (string.IsNullOrWhiteSpace(gasto.Concepto))
            throw new ArgumentException("El gasto necesita un concepto.");
        if (gasto.MontoHabitual < 0)
            throw new ArgumentException("El monto no puede ser negativo.");
        if (gasto.DesdeClave == 0)
            gasto.DesdeClave = PeriodoActual.Clave;
        if (gasto.HastaClave is not null && gasto.HastaClave < gasto.DesdeClave)
            throw new ArgumentException("El mes \"hasta\" no puede ser anterior al mes \"desde\".");

        var esNuevo = gasto.Id == 0;
        gasto.Concepto = gasto.Concepto.Trim();
        await repo.GuardarGastoFijoAsync(gasto);

        if (!esNuevo)
        {
            foreach (var mov in await repo.ObtenerMovimientosDeGastoFijoAsync(gasto.Id))
            {
                // Si se acortó el rango, salen los meses que quedaron fuera (solo si seguían pendientes).
                if (!gasto.AplicaEn(Periodo.DesdeClave(mov.MesClave)))
                {
                    if (mov.Estado == EstadoPago.Pendiente)
                        await repo.EliminarMovimientoAsync(mov.Id);
                }
                else if (actualizarMesActual && mov.MesClave == PeriodoActual.Clave
                         && mov.Estado == EstadoPago.Pendiente && !mov.Omitido)
                {
                    mov.Concepto = gasto.Concepto;
                    mov.CategoriaId = gasto.CategoriaId;
                    mov.Monto = gasto.MontoHabitual;
                    await repo.GuardarMovimientoAsync(mov);
                }
            }
        }

        foreach (var mes in await repo.ObtenerMesesAsync())
            if (gasto.AplicaEn(mes.Periodo))
                await SincronizarCuentasFijasAsync(mes.Periodo);
    }

    /// <summary>Vuelve a activar una categoría archivada.</summary>
    public async Task RestaurarCategoriaAsync(Categoria categoria)
    {
        categoria.Activa = true;
        await repo.GuardarCategoriaAsync(categoria);
    }

    /// <summary>
    /// Deja de pagar un gasto permanente: queda hasta <paramref name="ultimoMes"/> (por defecto, el actual).
    /// Los meses siguientes que ya estaban abiertos pierden esa cuenta si seguía pendiente.
    /// </summary>
    public async Task TerminarGastoFijoAsync(GastoFijo gasto, Periodo? ultimoMes = null)
    {
        var hasta = ultimoMes ?? PeriodoActual;
        gasto.HastaClave = Math.Max(hasta.Clave, gasto.DesdeClave);
        await repo.GuardarGastoFijoAsync(gasto);

        foreach (var mov in await repo.ObtenerMovimientosDeGastoFijoAsync(gasto.Id))
            if (mov.MesClave > gasto.HastaClave && mov.Estado == EstadoPago.Pendiente)
                await repo.EliminarMovimientoAsync(mov.Id);
    }

    /// <summary>Borra un gasto permanente por completo. Solo si nunca se generó en ningún mes; si no, usa Terminar.</summary>
    public async Task<bool> EliminarGastoFijoAsync(GastoFijo gasto)
    {
        if ((await repo.ObtenerMovimientosDeGastoFijoAsync(gasto.Id)).Count > 0)
            return false;
        await repo.EliminarGastoFijoAsync(gasto.Id);
        return true;
    }

    // ───────────── Ajustes ─────────────

    public Task<Ajustes> ObtenerAjustesAsync() => repo.ObtenerAjustesAsync();
    public Task GuardarAjustesAsync(Ajustes ajustes) => repo.GuardarAjustesAsync(ajustes);

    // ───────────── Resúmenes ─────────────

    public async Task<ResumenMes> ObtenerResumenMesAsync(Periodo periodo)
    {
        var mes = await repo.ObtenerMesAsync(periodo.Clave) ?? await AbrirMesAsync(periodo);
        var movimientos = await repo.ObtenerMovimientosAsync(periodo.Clave);
        var anteriorExiste = await repo.ObtenerMesAsync(periodo.Anterior().Clave) is not null;
        var movimientosAnteriores = anteriorExiste
            ? await repo.ObtenerMovimientosAsync(periodo.Anterior().Clave)
            : null;

        return Calculadora.CalcularMes(
            mes, movimientos,
            await repo.ObtenerCategoriasAsync(incluirArchivadas: true),
            await repo.ObtenerAjustesAsync(),
            movimientosAnteriores);
    }

    /// <summary>La hoja "Resumen": todos los meses ya iniciados (no se cuentan meses futuros).</summary>
    public async Task<ResumenGeneral> ObtenerResumenGeneralAsync(Periodo? desde = null, Periodo? hasta = null)
    {
        var tope = hasta ?? PeriodoActual;
        var resumenes = new List<ResumenMes>();
        foreach (var mes in await repo.ObtenerMesesAsync())
        {
            var p = mes.Periodo;
            if (p > tope || (desde is not null && p < desde)) continue;
            resumenes.Add(await ObtenerResumenMesAsync(p));
        }
        return Calculadora.CalcularGeneral(resumenes);
    }
}
