# Paso 3 · Ajustes (y paleta lila)

**Objetivo:** administrar todo lo que antes vivía en la hoja *Config* del Excel, directamente desde el teléfono.

---

## Cambios de diseño de este paso

- **Paleta lila:** el color principal pasó de rosa sakura a lila (`#7650C4`), con rosa como color secundario. Las claves de color ahora se llaman `Lila`, `LilaClaro`, `LilaSuave`, `Rosa`, `RosaClaro` y `RosaSuave`. Todos los textos siguen con contraste ≥ 4,5:1.
- **Compras más arriba:** en la pantalla del mes, la tarjeta de Compras va justo debajo de la tarjeta principal ("Me queda").
- El ícono y la pantalla de carga ahora usan fondo lila.

## Pestaña Ajustes

### 🔁 Gastos fijos

Equivalen a la tabla *Gastos permanentes* del Excel.

- **+** para agregar: concepto, monto habitual, categoría y meses **desde / hasta** ("Sin fecha de término" si no tiene fin).
- Al tocar uno:
  - **Editar.** Por defecto también actualiza la cuenta de este mes si sigue pendiente; puedes desactivarlo con el interruptor. Los meses pasados nunca cambian.
  - **Dejar de pagarlo desde el próximo mes.** Queda hasta el mes actual y desaparece de los meses siguientes (si estaban pendientes).
  - **Volver a activarlo** si estaba terminado.
  - **Eliminar.** Solo es posible si nunca apareció en un mes; si ya tiene historial, la app te sugiere "Dejar de pagarlo".
- Arriba se muestra el total de gastos fijos activos al mes.

### 🏷️ Categorías

- **+** para crear: nombre, emoji (de la lista sugerida o del teclado de emojis) y presupuesto mensual opcional.
- Al tocar una:
  - **Editar.**
  - **Quitar.** Si nunca se usó, se borra. Si tiene historial, pasa a **Archivadas**: no aparece para gastos nuevos, pero los meses pasados quedan intactos.
- Las archivadas se pueden **restaurar**.
- No se permiten dos categorías con el mismo nombre.

### 🔔 Alertas

Toca un valor para cambiarlo:

| Alerta | Valor inicial | Qué hace |
| --- | --- | --- |
| Cuenta fija que sube | 20 % | Avisa si una cuenta sube más que eso respecto al mes anterior. |
| Gasto del mes | 90 % | La barra se pone coral y aparece una alerta si gastas más de ese % del ingreso. |
| Presupuesto al límite | 90 % | La categoría se marca "Al límite" (ámbar) al pasar ese % de su presupuesto. |

## Cómo probarlo

1. Ve a **Ajustes** → **+** en Gastos fijos → agrega "Netflix", $14.400, categoría Hogar.
2. Vuelve a **Mes**: Netflix aparece en Cuentas fijas como pendiente.
3. En Ajustes, edita Netflix a $15.900 (con "Aplicar también a este mes" activado) → en Mes ya se ve $15.900.
4. Crea una categoría "Anime 🎌" con presupuesto $30.000 y regístrale una compra. La barra de "¿En qué se fue?" debería avanzar.
5. Cambia "Presupuesto al límite" a 50 % y mira cómo cambia la alerta.

## Qué se agregó al código

| Archivo | Qué hace |
| --- | --- |
| `ViewModels/FormularioBase.cs` | Base común de los formularios: abrir, esperar el resultado y cerrar. |
| `ViewModels/AjustesViewModel.cs` | Lógica de la pestaña: listas, menús de opciones y umbrales. |
| `ViewModels/CategoriaFormViewModel.cs` / `GastoFijoFormViewModel.cs` | Formularios con validación. Si algo falla al guardar, los datos vuelven a como estaban. |
| `Vistas/AjustesPage`, `CategoriaPage`, `GastoFijoPage` | Las pantallas. |
| `INavegacion.AbrirFormularioAsync` | Ahora sirve para cualquier formulario. |
| `ServicioFinanzas.GuardarGastoFijoAsync(…, actualizarMesActual)` | Regla nueva: aplicar el cambio al mes actual y sacar meses pendientes que quedaron fuera del rango. |
| `ServicioFinanzas.RestaurarCategoriaAsync` | Vuelve a activar una categoría archivada. |

Pruebas: 19 pruebas del núcleo (4 nuevas) y un escenario completo de los ViewModels (24 comprobaciones), ambos en verde.

## Siguiente: Paso 4

Pestaña **Resumen**: totales acumulados, gasto promedio, mes más caro, gráfico mes a mes y **exportar el historial completo en .md** para analizarlo con una IA.
