# Propuesta de la solucion para hackaton ceiba 2021
## Descripcion de la solucion 
Se implementa una solución basada en un patrón proxy reverso, para consumir las peticiones del cliente. 
Para optimizar el tiempo de respuesta en las peticiones, se implementó un caché en memoria, para minimizar el numero de peticiones fallidas se implementó una política de reintentos, con 3 reintentos, con una base para el primer intento de 2 seg y luego se realizan de manera exponencial.

## Drivers de la aplicación
Resiliencia con la política de reintentos, Usabilidad con la implemnetación de la memoria en caché.


# Diagrama de la solución.

![Diagrama componentes!](./solucion.drawio.svg "Diagrama de Solución")
