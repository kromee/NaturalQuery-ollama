# Evaluación manual de la POC

Envía cada pregunta por separado a `POST /api/ai/sql`. Compara `answer` con `rows` y revisa que `sql` use correctamente las tablas permitidas. La API devuelve `executed: true` cuando validó y ejecutó la consulta.

| Nivel | Pregunta | Qué revisar |
| --- | --- | --- |
| 1 | ¿Cuántos productos hay? | Conteo de `Producto`. |
| 2 | ¿Cuántos clientes hay? | Conteo de `Cliente`. |
| 3 | ¿Cuántas ventas hay en total? | Conteo de `Venta`. |
| 4 | ¿Cuántas unidades se vendieron en total? | Suma de `DetalleVenta.Cantidad`. |
| 5 | ¿Cuál fue el producto más vendido por cantidad? | Producto y cantidad total; contrasta el resultado con tu base local. |
| 6 | ¿Qué producto generó más ingresos y cuánto? | Suma de `Cantidad * PrecioUnitario`, agrupada por producto. |
| 7 | ¿Qué cliente realizó más ventas y cuántas? | Unión de `Venta` y `Cliente`, con conteo agrupado. |
| 8 | ¿Cuántas ventas hubo en septiembre y octubre de 2026? | Filtro de fechas por intervalos mensuales. |
| 9 | ¿Cuál fue el importe promedio por venta en cada mes? | Primero sumar los detalles por venta; después calcular el promedio por mes. |
| 10 | Compara las unidades vendidas por producto entre septiembre y octubre de 2026 y dime cuáles aumentaron. | Agregación condicional por producto y mes, con comparación correcta. |

## Registro de resultados

Por cada pregunta, anota si la consulta fue aceptada, si el SQL corresponde a la pregunta, si las filas son correctas y si `answer` describe los datos sin inventar valores. Para los resultados agregados, contrasta las filas con una consulta manual en Azure Data Studio.
