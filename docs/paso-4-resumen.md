# Paso 4 · Resumen e historial

**Objetivo:** reemplazar la hoja *Resumen* del Excel y poder sacar todo el historial en un `.md` para guardarlo en Drive o analizarlo con una IA.

---

## Pestaña Resumen

| Parte | Qué muestra | Equivale en el Excel a… |
| --- | --- | --- |
| Selector **6 meses / 12 meses / Todo** | Cambia el rango de todo lo que está abajo. Parte en 12 meses. | (nuevo) |
| Tarjeta **🐷 Ahorrado** | Suma de lo que quedó cada mes, ingresos y gastos acumulados, **gasto promedio** y **mes más caro**. | 💵 Ingresos, 💸 Gastos, 🐷 Ahorrado, 📆 Gasto promedio, 🔥 Mes más caro |
| **🗓️ Mes a mes** | Por mes (del más reciente al más antiguo): barra lila de ingresos, barra rosa de gastos, lo que quedó (verde, o coral si fue negativo) y el % gastado. | Tabla "Mes a mes" |
| **🎨 ¿En qué se fue?** | Gasto acumulado por categoría en el rango, de mayor a menor, con el % del gasto total. | (nuevo; antes era solo por mes) |
| **📤 Exportar historial completo** | Genera `finanzas_historial_AAAA-MM_a_AAAA-MM.md` con **todos** los meses y abre el menú Compartir. | (nuevo) |

Igual que en el Excel, los meses futuros no se cuentan, aunque ya los hayas abierto con ›.

## Cómo usar el historial con una IA

1. **Resumen → 📤 Exportar historial completo** → guárdalo en Drive (o compártelo directo).
2. En un chat con una IA, adjunta el archivo y pide algo como:
   - *"¿En qué categorías estoy gastando cada vez más?"*
   - *"¿Cuánto ahorraría si bajo un 30 % las suscripciones de Hogar?"*
   - *"Arma un presupuesto realista para el próximo mes según mi historial."*
3. El archivo trae metadatos al inicio (`tipo: historial`, `desde`, `hasta`, `moneda`), una tabla mes a mes, una **matriz categoría × mes** y las alertas de cada mes. Con eso, la IA puede comparar meses y detectar tendencias sin que le expliques el formato.

> Consejo: exporta el historial una vez al mes y guárdalo siempre en la misma carpeta de Drive. Así tienes un respaldo de tus datos fuera del teléfono.

## Detalles técnicos

- **Barras sin librerías externas:** cada barra es un `BoxView` cuyo ancho se calcula en el ViewModel en proporción al valor más alto. La vista informa el ancho disponible (`SizeChanged`), así que las barras se ajustan al girar el teléfono. Si un valor es mayor que cero, su barra mide al menos 6 px para que siempre se vea.
- **Core:** `ResumenGeneral.GastoPorCategoria` acumula el gasto por categoría de todos los meses del resumen (con su prueba).
- **Pruebas:** 20 pruebas del núcleo y un escenario de ViewModels con 34 comprobaciones (10 nuevas para el Resumen: rango, orden, barras, categorías, exportación y estado vacío).

## Pendiente opcional

- `Vistas/ProximamentePage` ya no la usa ninguna pestaña. Puedes borrarla o dejarla para futuras funciones.
- `MainPage.xaml` (la página de ejemplo de la plantilla) también se puede borrar.

## Ideas para seguir

- **Importar tu Excel actual:** leer `Mis_finanzas.xlsx` y cargar los meses, gastos fijos y categorías que ya tienes.
- **Respaldo y restauración:** exportar e importar toda la base de datos en un solo archivo, por si cambias de teléfono.
- **Recordatorios:** notificación local cuando una cuenta fija lleva días pendiente.
- **Mascota anime:** ilustración original en estados vacíos y celebraciones (por ejemplo, cuando ahorras más que el mes anterior).
