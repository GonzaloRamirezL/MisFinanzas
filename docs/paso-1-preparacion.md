# Paso 1 · Preparar el PC y el núcleo de la app

**Objetivo:** al final de este paso tienes Visual Studio funcionando, el proyecto abierto con sus 15 pruebas en verde y una app de prueba corriendo en tu S24 Ultra.

---

## 1. Instalar Visual Studio 2026 Community (gratis)

1. Descárgalo desde <https://visualstudio.microsoft.com/es/downloads/>.
2. En el instalador marca la carga de trabajo **"Desarrollo de .NET Multi-platform App UI"**.
   - Instala .NET 10, el SDK de Android, el JDK y el emulador. No hace falta Android Studio.
3. Instala también **Git for Windows** (<https://git-scm.com>) si no lo tienes.

> Si ya tenías Visual Studio: abre el **Instalador de Visual Studio**, pulsa **Modificar** y agrega esa carga de trabajo.

## 2. Abrir el proyecto

1. Descomprime `MisFinanzas.zip` en una carpeta corta, por ejemplo `C:\dev\MisFinanzas`. Las rutas muy largas dan problemas con Android.
2. Haz doble clic en **`MisFinanzas.slnx`**.
3. Espera a que Visual Studio termine de restaurar los paquetes NuGet (barra inferior: "Listo").
4. Menú **Prueba → Ejecutar todas las pruebas**. Deberías ver **15 pruebas en verde** ✅.
5. Menú **Compilar → Compilar solución**. Esto confirma que el proyecto `MisFinanzas.Datos` (SQLite) compila.

## 3. Subirlo a GitHub (tu primer repo del portafolio)

Desde una terminal (Ver → Terminal) en la carpeta del proyecto:

```bash
git init
git add .
git commit -m "Núcleo de Mis Finanzas: modelos, cálculos, SQLite y exportación .md"
```

Luego, en GitHub, crea un repo vacío llamado `mis-finanzas` y sigue las instrucciones de "push an existing repository".

> El repo no contiene tus datos reales: las categorías vienen genéricas y los gastos fijos los cargas tú en la app.

## 4. Preparar el Galaxy S24 Ultra

1. **Ajustes → Acerca del teléfono → Información de software** → toca **Número de compilación** 7 veces.
2. **Ajustes → Opciones de desarrollador** → activa **Depuración USB**.
3. En Windows instala el **Samsung Android USB Driver** (búscalo en el sitio de desarrolladores de Samsung). Sin este driver, Windows a veces no reconoce el teléfono.
4. Conecta el teléfono por USB y acepta el aviso **"¿Permitir depuración USB?"** (marca "Permitir siempre").

## 5. Crear el proyecto de la app (MAUI)

1. En el Explorador de soluciones: clic derecho en la carpeta **src** → **Agregar → Nuevo proyecto**.
2. Busca **"Aplicación .NET MAUI"** y elígela.
3. Nombre: **`MisFinanzas.App`**. Ubicación: la carpeta `src` del proyecto. Framework: **.NET 10**.
4. Clic derecho en `MisFinanzas.App` → **Agregar → Referencia de proyecto** → marca **MisFinanzas.Core** y **MisFinanzas.Datos**.
5. Abre `MisFinanzas.App.csproj` y deja solo Android como destino:

   ```xml
   <TargetFrameworks>net10.0-android</TargetFrameworks>
   ```

   (Borra las líneas de iOS, MacCatalyst y Windows. Si después quieres probar en el PC, vuelve a agregar `net10.0-windows10.0.19041.0`.)
6. Arriba, en el selector de destino, elige tu **Galaxy S24 Ultra** y pulsa ▶️.

Si ves la app de ejemplo ("Hello, World!" con un botón contador) en tu teléfono, **el paso 1 está listo** 🎉.

---

## Qué hay en el código (resumen)

| Pieza | Qué hace | Equivale en el Excel a… |
| --- | --- | --- |
| `Periodo` | Un mes (año + mes); entiende códigos tipo `NOV26` | Nombres de las hojas |
| `Categoria` | Nombre, emoji, presupuesto; se puede crear, editar y archivar | Config → Categorías |
| `GastoFijo` | Plantilla con monto habitual y mes desde/hasta | Config → Gastos permanentes |
| `Movimiento` | Cuenta fija, deuda, compra o pago recibido | Las 4 tablas de cada mes |
| `Ajustes` | Umbrales de alerta y moneda | Config → Alertas |
| `Calculadora` | Todos los totales y alertas | Las fórmulas |
| `ServicioFinanzas` | Reglas: abrir meses, generar fijas, archivar, terminar gastos | La lógica "automática" |
| `ExportadorMarkdown` | Reporte de un mes o historial en `.md` | (nuevo) |
| `SqliteFinanzasRepositorio` | Guarda todo en el teléfono | El archivo .xlsx |

### Reglas de configuración dinámica

- **Agregar un gasto fijo:** aparece de inmediato en el mes actual (y en los meses ya abiertos dentro de su rango).
- **Cambiar su monto:** afecta solo los meses que se abran desde ahora. El historial no se toca.
- **Dejar de pagarlo:** se "termina" en un mes y desaparece de los meses siguientes si seguía pendiente.
- **Quitar una cuenta fija solo de un mes:** se oculta y no vuelve a aparecer al reabrir ese mes.
- **Quitar una categoría:** si nunca se usó, se borra. Si tiene historial, se archiva: deja de aparecer para gastos nuevos, pero los meses pasados siguen correctos.
- **Estados:** quedaron fijos en *Pagado / Pendiente*, porque los cálculos de "por pagar" dependen de ellos. Si más adelante quieres otros (por ejemplo, "Parcial"), se agregan.

### Exportación .md

- `finanzas_2026-11.md`: un mes completo (resumen, alertas, categorías, detalle).
- `finanzas_historial_2026-10_a_2026-11.md`: tabla mes a mes, **matriz categoría × mes** y alertas acumuladas.
- Cada archivo parte con metadatos (`tipo`, `periodo`, `moneda`, `formato_version`) para que una IA sepa qué está leyendo. Puedes juntar varios en un chat y pedir cosas como: *"¿en qué categoría estoy gastando cada vez más?"* o *"¿cuánto podría ahorrar si bajo las suscripciones?"*.

## Siguiente: Paso 2

Estilo visual (paleta pastel, tipografía redondeada y componentes base), navegación con pestañas y la **pantalla del mes**, conectada a los datos reales.
