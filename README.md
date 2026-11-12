# 🌸 Mis Finanzas

App Android de finanzas personales, 100 % local (sin nube ni cuentas), hecha en **C# con .NET MAUI**.
Nació para reemplazar una planilla Excel con resumen mensual, cuentas fijas, deudas, compras, presupuestos por categoría y alertas.

## Funciones

- Meses que se abren solos, con las **cuentas fijas** generadas desde gastos permanentes configurables (con mes desde/hasta).
- **Categorías dinámicas** con emoji y presupuesto opcional. Las que ya tienen historial se archivan en vez de borrarse.
- Deudas y gastos con estado Pagado/Pendiente, compras con fecha y pagos recibidos.
- Cálculos: ingresos, gastos, "me queda", "por pagar", % del ingreso gastado y gasto por categoría vs presupuesto.
- Alertas: categoría al límite o excedida, cuenta que subió más de X % respecto del mes anterior y gasto sobre X % del ingreso.
- **Exportación a Markdown** (mes o historial), pensada para guardarla en Drive o dársela a una IA para que la analice.

## Arquitectura

```
MisFinanzas.slnx
├── src/
│   ├── MisFinanzas.Core      → modelos, reglas de negocio, cálculos y exportación (sin dependencias)
│   ├── MisFinanzas.Datos     → implementación SQLite (sqlite-net-pcl)
│   └── MisFinanzas.App       → interfaz .NET MAUI (paso 2)
└── tests/
    └── MisFinanzas.Core.Tests → pruebas xUnit con repositorio en memoria
```

- `Core` no conoce SQLite ni MAUI. Recibe un `IFinanzasRepositorio` y por eso se puede probar sin teléfono.
- `Calculadora` es código puro (datos de entrada → resultados) y reemplaza las fórmulas del Excel.
- `ServicioFinanzas` concentra las reglas: no duplica cuentas fijas, no altera meses pasados al cambiar un monto y archiva lo que tiene historial.

## Cómo correr las pruebas

```bash
dotnet test
```

## Stack

C# · .NET 10 · .NET MAUI · SQLite · xUnit
