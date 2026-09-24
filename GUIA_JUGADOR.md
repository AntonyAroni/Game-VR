# Guía del Jugador y Manual de Operaciones: Zombie Checkpoint VR

**Puesto de Control Sanitario Fronterizo — Zona de Cuarentena**  
*Inspirado en Papers, Please y dinámicas de interacción biomédica en Realidad Virtual.*

---

## 1. Tu Misión como Oficial Sanitario

Te encuentras al mando de la cabina de inspección fronteriza. Tu objetivo es examinar a los ciudadanos que se aproximan y determinar con precisión si están **SANOS** (autorizados para ingresar a la Zona Segura) o **INFECTADOS** (enviados de inmediato a la esclusa de Cuarentena).

Cometer un error tiene consecuencias graves:
* Si dejas pasar a un infectado, el virus entrará a la población sana.
* Si aíslas a un civil sano, violarás el protocolo sanitario.

---

## 2. Las Herramientas de tu Mesa de Trabajo

Toda la mesa de control es interactiva. Las herramientas se agarran **directamente con tus manos**, tal como en la vida real.

```text
 ┌──────────────────────────────────────────────────────────────┐
 │ [MONITOR CARDÍACO]                     [SALIDA ZONA SEGURA]  │
 │  ┌──────────────┐                                            │
 │  │ ♥ 72 BPM    │       [CIVIL A INSPECCIONAR]                │
 │  │ ──/\_/\_/\── │                                            │
 │  │ [ PROBAR ]   │                                            │
 │  └──────────────┘                                            │
 │                                                              │
 │ [BRAZOS] [PECHO]    [ESTETOSCOPIO]   [LINTERNA]  [APROB] [CUAR]│
 │  (Cian)  (Ámbar)                     (Normal/UV) (Verde) (Rojo)│
 │                                                              │
 │               [PASAPORTE / PASE SANITARIO]                   │
 └──────────────────────────────────────────────────────────────┘
```

---

### A. Linterna Médica / Luz Forense UV (395nm)
* **¿Cómo se usa?** Tómala con la mano derecha o izquierda. Se enciende automáticamente.
* **Luz Blanca (Modo Normal):** 
  * Ilumina el rostro y los ojos. En una persona sana, las pupilas se contraen visiblemente ante la luz. En un infectado, permanecen dilatadas e inmóviles.
  * Úsala para revisar heridas visibles en el cuerpo o los brazos.
* **Luz Ultravioleta (Modo UV / Lámpara de Wood):**
  * Cambia de modo con el botón secundario del mando, clic central o tecla **U**.
  * **Ojos:** Los ojos de un infectado emitirán un brillo verde fosforescente patognomónico.
  * **Pecho:** Las manchas víricas en la piel brillarán en verde bajo la radiación UV.
  * **Pasaporte:** Revela la marca de agua de seguridad oficial (**✦ SELLO OFICIAL AUTÉNTICO**) o la marca de falsificación (**✖ SIN SELLO / FALSO**).

---

### B. Estetoscopio Clínico 3D
* **¿Cómo se usa?** Agarra la campana metálica del instrumento con tu mano y llévala directamente al centro del pecho del ciudadano.
* **Respuesta sensorial:**
  * Escucharás el latido en sonido 3D posicional.
  * Sentirás las pulsaciones sincronizadas vibrando en tu mano.
  * El monitor de la mesa registrará automáticamente el electrocardiograma.

---

### C. La Caja Negra de la Mesa: Monitor Cardíaco (ECG)
> **¿Qué es la caja negra que está a la izquierda de la mesa?**  
> Es un **Electrocardiógrafo diegético en tiempo real** con pantalla osciloscópica de barrido de fósforo.

* **En espera:** Su pantalla muestra `MONITOR CARDÍACO: Usa el estetoscopio en el pecho` y el LED de estado parpadea suavemente.
* **Al auscultar al paciente:**
  * **Ciudadano Sano:** Trazo verde rítmico, **70 a 75 BPM** con mensaje `♥ LATIDO NORMAL`.
  * **Sujeto Infectado:** Trazo rojo errático y caótico, **más de 150 BPM** con mensaje `⚠ LATIDO ANORMAL (¡Corazón descontrolado!)`.
* **Botón `[ PROBAR ]`:** Puedes tocar o presionar el pulsador amarillo en la base del monitor para probar el altavoz del equipo, emitir un pitido de prueba y verificar que el sistema esté operativo.

---

### D. Pasaporte / Pase Sanitario
* Tómalo y acércalo a tu rostro para leerlo con facilidad.
* **Comprobación 1 (Datos):** Verifica que el nombre y la edad coincidan con el ciudadano que tienes al frente.
* **Comprobación 2 (Fecha de Vencimiento):** Confirma que el documento esté `[VIGENTE]` y no vencido o irregular.
* **Comprobación 3 (Luz UV):** Pasa la linterna UV por el documento para ver la marca de agua oculta del Ministerio de Salud.

---

### E. Pulsadores Rápidos de Orden Clínica (En la Mesa)
* **Botón Cian (`BRAZOS ARRIBA` / Tecla V):**  
  Ordena al civil levantar los brazos en postura clínica natural. Despeja las axilas, costados y región pectoral para facilitar la auscultación y la búsqueda de mordeduras.
* **Botón Ámbar (`VER PECHO` / Tecla C):**  
  Descubre el torso del civil, permitiéndote inspeccionar si presenta erupciones cutáneas o manchas rojas causadas por el patógeno.

---

### F. Sellos Físicos de Veredicto (*Papers, Please*)
Una vez que hayas evaluado al ciudadano y tengas tu veredicto:
* **Sello Verde (`APROBADO`):**  
  Tómalo y presiónalo con fuerza hacia abajo sobre el pasaporte sanitario. Autoriza al ciudadano sano a pasar a la Zona Segura (salida derecha iluminada).
* **Sello Rojo (`CUARENTENA`):**  
  Tómalo y presiónalo sobre el pasaporte. Dispara la alarma de contención, abre las compuertas blindadas y envía al sospechoso a aislamiento médico.
* *(Nota: También puedes usar los dos botones pulsadores grandes en la mesa para dictar el veredicto).*

---

## 3. Guía de Detección Rápida: Sano vs. Infectado

| Examen Clínico | Herramienta | Persona Sana (Aprobar) | Infectado (Cuarentena) |
| :--- | :--- | :--- | :--- |
| **Brazos y Piel** | Inspección visual directa | Piel limpia, sin lesiones ni marcas. | **Mordedura abierta o rasgadura visible en el antebrazo.** |
| **Latido Cardíaco** | Estetoscopio + Monitor ECG | Latido tranquilo (70-75 BPM), onda verde regular. | **Corazón acelerado (>150 BPM), ritmo caótico, onda roja.** |
| **Reacción Pupilar** | Linterna (Luz Blanca) | Las pupilas se contraen rápidamente ante la luz. | **Pupilas dilatadas e inmóviles; no reaccionan a la luz.** |
| **Fluorescencia Ocular** | Linterna (Luz UV 395nm) | Los ojos no brillan bajo luz ultravioleta. | **Los ojos emiten un brillo verde fosforescente vítreo.** |
| **Torso y Pecho** | Botón `VER PECHO` + UV | Piel despejada, uniforme y limpia. | **Erupción con manchas rojas que brillan en verde bajo UV.** |
| **Pase Sanitario** | Lectura visual + Luz UV | Datos coincidentes, vigente y con sello oficial UV. | **Vencido, nombre incorrecto o con sello UV falsificado.** |

> [!TIP]
> **Regla de Oro:** Con **un solo síntoma positivo** o una irregularidad documental, el sujeto debe ser enviado a **CUARENTENA**. Solo se aprueba a quienes pasen **todas** las pruebas sin anomalías.

---

## 4. Cómo Jugar en Meta Quest 2 (Solo Manos / Hand Tracking)

El juego está diseñado para jugarse **sin mandos**, usando exclusivamente tus manos reales en Realidad Virtual:

1. **Agarrar Objetos (Power Grip):**  
   Extiende tu mano hacia la linterna, estetoscopio, pasaporte o sellos y **cierra tus dedos en puño**. El objeto se acomodará de forma anatómica en tu palma.
2. **Soltar Objetos:**  
   Simplemente **abre la mano**. La herramienta caerá de vuelta a la mesa con físicas realistas. Si se cae accidentalmente al piso, reaparecerá sola en su bandeja.
3. **Auscultar:**  
   Con el estetoscopio en la mano, apoya la campana directamente sobre el pecho del civil.
4. **Estampar:**  
   Toma el sello verde o rojo y presiónalo hacia abajo sobre el documento apoyado en la mesa.
5. **Presionar Botones:**  
   Extiende el dedo índice y presiona físicamente cualquiera de los botones de la mesa o el monitor.

---

## 5. Cómo Jugar en PC / Computadora (Teclado y Ratón)

Si estás probando el juego en el Editor de Unity o versión de escritorio:

| Control | Acción |
| :--- | :--- |
| **Mover Ratón** | Mueve la mano activa en el espacio 3D (X, Y). |
| **Clic Izquierdo** | Agarra herramientas, estampa o pulsa botones. |
| **Rueda del Ratón** | Acerca o aleja la mano (Profundidad Z). |
| **Mantener Alt + Mover Ratón** | Gira y orienta la muñeca en cualquier ángulo. |
| **Tecla T** | Alterna entre orientación horizontal e inclinación ergonómica hacia la mesa (35°). |
| **Tecla Q / E** | Ajuste fino vertical de la mano (subir / bajar 5°). |
| **Mantener Clic Derecho** | Mirar alrededor con la cámara de la cabeza (vista FPS). |
| **WASD** | Caminar dentro del puesto de control. |
| **Tecla Tab** | Cambiar entre controlar la mano derecha o izquierda. |
| **Tecla U o Clic Central** | Alternar linterna entre Luz Blanca y Luz Forense UV. |
| **Tecla V** | Ordenar al civil levantar los brazos (`BRAZOS ARRIBA`). |
| **Tecla C** | Descubrir el torso del civil (`VER PECHO`). |
| **Tecla F1 / O** | Mostrar u ocultar la tarjeta de ayuda en pantalla. |

---

¡Buena suerte, Oficial! La seguridad de la zona de cuarentena está en tus manos.
