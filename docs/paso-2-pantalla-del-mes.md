# Paso 2 · Estilo visual y pantalla del mes

**Objetivo:** al final de este paso la app abre en tu S24 con estilo sakura pastel, tiene 3 pestañas (Mes, Resumen, Ajustes) y la pestaña **Mes** ya funciona con datos reales guardados en el teléfono.

---

## 1. Antes de compilar (3 cosas a mano)

### a) Descargar la tipografía

1. Entra a <https://fonts.google.com/specimen/M+PLUS+Rounded+1c> → **Get font** → **Download all**.
2. Del zip, copia estos 3 archivos a `MisFinanzas.App\Resources\Fonts\`:
   - `MPLUSRounded1c-Regular.ttf`
   - `MPLUSRounded1c-Medium.ttf`
   - `MPLUSRounded1c-Bold.ttf`
3. Puedes borrar `OpenSans-Regular.ttf` y `OpenSans-Semibold.ttf`, porque ya no se usan.

> Si te saltas esto, la app funciona igual, pero con la letra por defecto de Android.

### b) Borrar la página de ejemplo

En el Explorador de soluciones, borra **`MainPage.xaml`** (se borra junto con su `.xaml.cs`). Ya no se usa.

### c) Restaurar paquetes

Clic derecho en la solución → **Restaurar paquetes NuGet**. Cambió el paquete de SQLite (ver sección 4).

## 2. Probar

1. Proyecto de inicio: **MisFinanzas.App**. Destino: tu **Galaxy S24 Ultra**. Pulsa ▶️.
2. Primera prueba sugerida:
   - **Editar** sueldo → escribe tu sueldo (acepta `2217418` o `2.217.418`).
   - **+** en Compras → agrega una compra → mira cómo cambian *Me queda* y *¿En qué se fue?*.
   - **+** en Cuentas fijas → agrega "Luz" sin marcar pagado → aparece en *Por pagar aún*.
   - Toca la fila de "Luz" → **Marcar como pagado**.
   - **›** para ir al mes siguiente y **‹** para volver.
   - **📤 Exportar este mes** → elige Drive (o "Guardar en Archivos").
3. Cierra la app por completo y vuelve a abrirla: los datos siguen ahí (quedan en SQLite, dentro del teléfono).

## 3. Qué se construyó

### Estilo

| Archivo | Qué tiene |
| --- | --- |
| `Resources/Styles/Colors.xaml` | Paleta sakura pastel (rosa, lavanda, menta, coral, ámbar). Todos los textos cumplen un contraste ≥ 4,4:1. |
| `Resources/Styles/Tema.xaml` | Estilos propios: `Tarjeta`, `Burbuja`, `Pildora`, `MontoGrande`, `BotonPrincipal`, `BotonCirculo`… |
| `Resources/Styles/Styles.xaml` | La plantilla de MAUI, ahora con la tipografía redondeada y el fondo crema. |
| `Resources/AppIcon` y `Splash` | Ícono y pantalla de carga: flor de sakura sobre rosa (reemplaza el logo de .NET). |

La app se fuerza a **modo claro**, para que la paleta se vea igual aunque el teléfono esté en modo oscuro.

### Pantalla del mes

- Cambio de mes con ‹ › (solo puedes adelantarte un mes) y "Volver al mes actual".
- Tarjeta principal con degradado: **Me queda**, ingresos, gastos, barra de % gastado (se pone coral si pasas el umbral) y **Por pagar aún**.
- Sueldo editable.
- Alertas (presupuesto excedido o al límite, cuentas que subieron, gasto sobre el umbral).
- 4 secciones con **+**: Cuentas fijas, Deudas y gastos, Compras y Me pagaron.
- Al tocar una fila: marcar pagado o pendiente, editar, o quitar. Las cuentas que vienen de un gasto permanente se quitan **solo de ese mes**.
- **¿En qué se fue?**: gasto por categoría con barra de presupuesto (menta, ámbar o coral).
- Botón para exportar el mes a `.md` con el menú *Compartir* de Android.

### Arquitectura (MVVM sin dependencias extra)

```
MisFinanzas.App/
├── Utilidades/      Observable (avisa cambios a la vista), ComandoAsync, Montos
├── Servicios/       Contratos.cs (IDialogos, INavegacion, ICompartir) + ServiciosMaui.cs
├── ViewModels/      MesViewModel, MovimientoViewModel, Items (filas ya formateadas)
└── Vistas/          MesPage, MovimientoPage, ProximamentePage (Resumen y Ajustes)
```

- Los **ViewModels no dependen de MAUI**: hablan con la interfaz a través de `IDialogos`, `INavegacion` e `ICompartir`. Por eso se pudieron compilar y probar fuera del teléfono, con un escenario completo: cargar, editar el sueldo, agregar, validar, marcar pagado, quitar, editar, cambiar de mes y exportar.
- Las vistas usan **bindings compilados** (`x:DataType`): si un nombre está mal escrito, Visual Studio avisa al compilar en vez de fallar en silencio.
- Todo se registra con **inyección de dependencias** en `MauiProgram.cs`.

## 4. Cambio en SQLite (la advertencia de vulnerabilidad)

`MisFinanzas.Datos` ahora usa:

- `sqlite-net-base` en vez de `sqlite-net-pcl`: es la misma librería, pero sin traer su propia versión de SQLite.
- `SQLitePCLRaw.bundle_e_sqlite3` **3.0.5**, que incluye una versión de SQLite sin la vulnerabilidad.
- En el repositorio se agregó `SQLitePCL.Batteries_V2.Init()`, que esta combinación necesita.

Después de restaurar paquetes, la advertencia amarilla del Explorador de soluciones debería desaparecer.

## 5. Si algo falla

- **Error de compilación en XAML:** cópiame el mensaje completo de la Lista de errores (incluye archivo y línea).
- **La app se cierra al abrir:** en Visual Studio, menú **Ver → Salida**, elige "Depurar" y cópiame las últimas líneas en rojo.
- **No se ven las letras redondeadas:** revisa que los 3 `.ttf` estén en `Resources\Fonts` con esos nombres exactos.

## Siguiente: Paso 3

Pestaña **Ajustes**: crear, editar y archivar categorías (con emoji y presupuesto), gastos permanentes (con mes desde/hasta) y umbrales de alerta.
