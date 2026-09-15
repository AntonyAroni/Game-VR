# Guía de Arquitectura y Base de Conocimientos (AGENTS.md)
**Proyecto:** Zombie Checkpoint VR (Inspirado en *Papers, Please* y *Zombie Quarantine*)  
**Curso:** Interacción Humano-Computador (IHC)  
**Motor:** Unity 6 (6000.0.0f1) + XR Interaction Toolkit (XRI 3.x) + URP  
**Espacio de trabajo:** `/home/antony/Documents/IHC/Game-VR`  
**Última actualización:** 2026-09-15  

---

## 1. Principios de Arquitectura y Buenas Prácticas (SOLID)

El código debe ser **altamente modular, desacoplado y auto-documentado**. Queda estrictamente prohibido crear scripts monolíticos tipo "God Objects" (`GameManager` que controla todo).

### S - Single Responsibility Principle (SRP)
Cada script debe tener una única responsabilidad:
* `StethoscopeTool.cs`: Solo gestiona el agarre, detección de proximidad con la campana auscultadora y disparo de eventos de examen torácico.
* `HapticManager.cs`: Solo gestiona el envío de impulsos hápticos a los mandos VR (híbrido XRI `HapticImpulsePlayer` y OpenXR).
* `PupilSymptom.cs`: Solo gestiona la respuesta fotomotora pupilar ante el haz de la linterna.
* `SurvivorModel.cs`: Almacena el estado biológico, síntomas y veredicto del PNJ. No maneja UI ni físicas de la mesa.
* `XRSimulatorDesktopEnhancer.cs`: Gestiona la ergonomía de pruebas en PC mediante ratón y teclado.

### O - Open/Closed Principle (OCP)
El sistema debe estar abierto a extensión pero cerrado a modificación mediante interfaces:
* `IInspectableBodyPart`: Interfaz para cualquier extremidad o zona corporal que pueda ser examinada.
* `IInspectionTool`: Interfaz para herramientas (linterna, estetoscopio, sellos).
* `ISymptom`: Interfaz para cualquier tipo de síntoma detectable (mordedura, pulso anómalo, reflejo pupilar, tos).

### L - Liskov Substitution Principle (LSP)
Cualquier síntoma o herramienta debe poder ser sustituido por otra implementación de su interfaz base sin romper el evaluador de inspección ni el `EventBus`.

### I - Interface Segregation Principle (ISP)
Interfaces pequeñas y específicas:
* `IInspectionTool`: Quien sea manipulable como herramienta diagnóstica.
* `ISymptom`: Quien contenga lógica biológica evaluable.

### D - Dependency Inversion Principle (DIP) & Event-Driven Architecture
* Desacoplar sistemas mediante eventos de C# estáticos y fuertemente tipados (`ZombieCheckpoint.Core.EventBus`).
* Las herramientas y síntomas no se buscan por jerarquía rígida; se comunican mediante colisiones/triggers y notifican a través del `EventBus` (`OnSymptomDiscovered`, `OnVerdictSubmitted`, `OnHapticRequested`, etc.).

---

## 2. Estructura Modular de Carpetas

Dentro de `Assets/Scripts/`:
```text
Assets/Scripts/
├── Core/
│   ├── GameState.cs             // Enums (InspectionState, VerdictType, HandSide)
│   ├── CheckpointFlowManager.cs // Control de ciclo de rondas, spawn de civiles y despacho
│   └── EventBus.cs              // Bus de eventos desacoplado del juego
├── Decision/
│   └── DecisionButton.cs        // Pulsadores físicos tangibles en el escritorio
├── Documents/
│   ├── DocumentData.cs          // POCO con datos biográficos, vigencia y fotos
│   ├── DocumentInteractable.cs  // XRGrabInteractable con soporte de sellado físico
│   └── DocumentView.cs          // Renderizado dinámico en Canvas WorldSpace
├── Environment/
│   └── QuarantineDoorController.cs // Control cinemático y sonoro de compuertas
├── HCI/
│   ├── HapticManager.cs         // API háptica dual (XRI 3.x HapticImpulsePlayer + OpenXR)
│   ├── SpatialAudioManager.cs   // Generador de audio posicional para latidos, alarmas y sellos
│   ├── UsabilityMetricsTracker.cs // Métricas IHC: tiempos de decisión, aciertos y turnos
│   └── XRSimulatorDesktopEnhancer.cs // Ergonomía completa para desarrollo sin visor VR
├── Survivors/
│   ├── IInspectableBodyPart.cs  // Contrato de extremidades examinables
│   ├── BodyPartExaminer.cs      // Colisionador de inspección en huesos
│   ├── SurvivorModel.cs         // Estado biológico, síntomas e identidad del PNJ
│   ├── SurvivorHumanoidController.cs // Posturas naturales, respiración y giro de muñeca
│   └── Symptoms/
│       ├── ISymptom.cs          // Contrato base para síntomas clínicos
│       ├── BiteMarkSymptom.cs   // Mordeduras de infectado en antebrazo
│       ├── HeartbeatSymptom.cs  // Latido cardíaco (auscultación en Spine1)
│       └── PupilSymptom.cs      // Reflejo fotomotor pupilar (linterna en Head)
├── Tools/
│   ├── IInspectionTool.cs       // Contrato base para herramientas diagnósticas
│   ├── FlashlightTool.cs        // Linterna clínica con cono volumétrico y switch mecánico
│   ├── StethoscopeTool.cs       // Estetoscopio 3D con auscultación en campana (Bell)
│   └── StampTool.cs             // Sello tangible para Aprobado y Cuarentena
└── Editor/
    ├── CheckpointSceneBuilder.cs // Generador/reconstructor procedural de la escena
    ├── DefaultSceneAutoLoader.cs // Carga automática de CheckpointBoothScene al pulsar Play
    └── QuestBuildHelper.cs       // Automatización de compilación APK para Meta Quest
```

---

## 3. Normas de Codificación C#

1. **Namespaces:** Todo script debe estar contenido en el namespace raíz `ZombieCheckpoint.<Módulo>`.  
   *Ejemplo:* `namespace ZombieCheckpoint.Tools { ... }`
2. **Serialización limpia:** Usar `[SerializeField] private` en lugar de campos `public` para referencias del Inspector.
3. **Validación de referencias:** Validar con `TryGetComponent` o `null-check` explícito con mensajes claros si falta una referencia.
4. **Documentación:** Comentarios XML en métodos y clases (`/// <summary>`) destacando el propósito de IHC (por qué se usa esa metáfora de interacción).
5. **No allocations en Update:** Prohibido `new`, `GetComponent` o `FindObjectOfType` dentro de bucles `Update()`.

---

## 4. Reglas para Agentes de IA y MCP Unity

1. **Compilación continua:** Tras crear o editar cualquier script `.cs`, ejecutar `unityMCP.read_console` para verificar que no haya errores de compilación (`CSXXXX`).
2. **No romper prefabs:** No modificar valores de GUIDs o metadatos `.meta` manualmente.
3. **Enfoque en IHC:** Toda interacción debe justificarse según los principios de Don Norman:
   - **Visibilidad:** El estado del sistema debe ser evidente (luces, monitores, audio).
   - **Affordance:** La forma del objeto indica cómo se usa (mango del sello, campana del estetoscopio).
   - **Feedback:** Confirmación inmediata multimodal (sonido + vibración háptica + cambio visual).
   - **Mapeo Natural:** Mover el ratón mueve la mano; girar con modificador orienta la muñeca; presionar hacia abajo estampa el papel.
   - **Restricciones:** Cooldowns y protecciones contra doble veredicto.

---

## 5. Mapeo de Controles y Simulación de Escritorio (PC / Teclado + Ratón)

Para permitir pruebas continuas y fluidas sin necesidad de conectar el visor Meta Quest físico en cada iteración, la escena incorpora `XR Device Simulator` junto a `XRSimulatorDesktopEnhancer.cs`:

| Acción de Interacción | Control (Teclado / Ratón) | Comportamiento y Justificación IHC |
| :--- | :--- | :--- |
| **Mover Mano Activa (3D)** | **Mover Ratón** | Traslada la mano en el espacio horizontal y vertical ($X, Y$). |
| **Girar / Orientar Cabezal (Rayo)** | **Mantener Alt Izq (o Ctrl Izq o Clic Central) + Mover Ratón** | Rota la muñeca/cabezal en cualquier ángulo ($Pitch, Yaw$) sin cambiar la posición espacial. Al soltar, regresa a mover la mano. |
| **Inclinación Rápida a la Mesa** | Tecla **T** | Alterna al instante entre orientación horizontal ($0^\circ$) e inclinación ergonómica hacia la mesa ($35^\circ$ hacia abajo). |
| **Ajuste Fino de Inclinación** | Teclas **Q** / **E** | **E** inclina el cabezal $5^\circ$ más hacia abajo; **Q** lo levanta $5^\circ$ hacia arriba. |
| **Conmutar Modo Ratón Fijo** | Tecla **R** | Conmuta permanentemente entre modo traslación de mano y modo rotación de muñeca. |
| **Agarrar Objeto (Grip)** | **Clic Izquierdo** | Agarra el estetoscopio, linterna, pasaporte o acciona botones directamente. |
| **Mirar con la Cámara (Cabeza)** | Mantener **Clic Derecho** + Mover Ratón | Permite inspeccionar el entorno de la cabina con vista FPS. |
| **Profundidad de la Mano (Eje Z)** | **Rueda del Ratón** | Acerca o aleja la mano respecto al cuerpo/mesa. |
| **Desplazarse en la Cabina** | Teclas **W, A, S, D** | Movimiento continuo en el espacio del puesto de control. |
| **Alternar Modo Linterna (Blanca / UV)** | Tecla **U** o **Clic Central** | Conmuta entre Luz Clínica Normal y Luz Forense Ultravioleta 395nm (Lámpara de Wood). |
| **Alternar Mano Activa** | Tecla **Tab** | Cambia el control entre Mano Derecha y Mano Izquierda. |
| **Mostrar / Ocultar Ayuda HUD** | Tecla **H** | Despliega una tarjeta semi-transparente con los controles y el ángulo actual del cabezal. |

---

## 6. Estado Actual de la Implementación (Auditoría e Hitos Completados)

### A. Estetoscopio 3D (`Rigged Stethoscope`)
* **Modelo 3D:** Integrado desde `Assets/Stethoscope/Prefabs/Stethoscope.prefab` con malla detallada, auriculares y mangueras con armature/rig.
* **Shaders URP:** Material `Stethoscope Parts.mat` migrado a `Universal Render Pipeline/Lit` con mapas de Albedo, Normales y Metálico/Smoothness ($0.85$).
* **Físicas y Detección:** Colisionador de agarre en el cuerpo con `XRGrabInteractable` (Velocity Tracking) y `SphereCollider` tipo trigger ($r = 0.045\text{ m}$) ubicado en el hueso `Bell` (campana auscultadora).
* **Lógica de Auscultación:** `StethoscopeTool.cs` calcula proximidad torácica directamente desde el centro de la campana con feedback auditivo de latidos y vibración háptica al pecho.

### B. Sistema de Síntomas Clínicos
* **Latido Cardíaco (`HeartbeatSymptom`):** Montado en el hueso `Spine1` con colisionador esférico de auscultación ($r = 0.22\text{ m}$). Reproduce latidos rítmicos o taquicardia/arritmia irregular.
* **Reflejo Pupilar y Reacción UV (`PupilSymptom`):** Montado en el hueso `Head` con pupilas físicas procedurales. Evalúa miosis fotomotora inmediata ($1.0x \rightarrow 0.35x$) bajo luz blanca en civiles sanos, midriasis fija bilateral ($1.35x$) en infectados, y fluorescencia vítrea corneal verde-esmeralda bajo haz UV.
* **Marcas de Mordedura (`BiteMarkSymptom`):** Montado en el hueso `RightForeArm` con colisionador capsular y visualizador de herida. Requiere inspección visual directa.
* **Manipulación Anatómica:** `SurvivorHumanoidController.cs` permite al usuario agarrar la muñeca del superviviente para rotar el antebrazo y evaluar marcas ocultas.

### C. Sistema Háptico Modernizado (`HapticManager.cs`)
* Vinculación automática en tiempo de ejecución con `HapticImpulsePlayer` en los controladores izquierdo y derecho de XRI 3.x.
* Fallback a `InputDevices.SendHapticImpulse` para máxima compatibilidad con OpenXR.

### D. Configuración Dual de Compilación OpenXR
* **Standalone (Editor PC / Linux):** `m_InitManagerOnStart = 0` para evitar bloqueos por `DllNotFoundException: UnityOpenXR` cuando no hay visor conectado.
* **Android (Meta Quest):** Configuración OpenXR intacta para compilación directa a APK mediante `QuestBuildHelper.cs`.

### E. Sellos Diegéticos y Estampado Físico (*Papers, Please*)
* **Herramientas de Sello (`StampTool.cs`):** Modelos diegéticos de madera noble, bronce y base de tinta de goma elástica para Aprobado (verde) y Cuarentena (rojo).
* **Física Tangible de Sellado:** Sustitución de triggers flotantes por contacto físico real de la base (`OnTriggerEnter` en almohadilla de goma) o gatillo cercano sobre el pasaporte.
* **Tinta Dinámica con Rotación Orgánica:** Marcado con borde oficial enmarcado, rotación aleatoria de estampado manual ($\pm 6^\circ$), sonido de impacto sordo y micro-animación de rebote (*scale-punch*).
* **Protección contra Doble Veredicto:** Cierre de acción definitivo una vez aplicado el primer sello o pulsado el botón de la mesa.

### F. Monitor CRT de Signos Vitales y Osciloscopio ECG (`VitalSignsMonitor.cs`)
* **Pantalla CRT de Fósforo:** Osciloscopio procedural en tiempo real de $256 \times 64$ px con barrido horizontal continuo, persistencia de fósforo verde y ruido basal en espera.
* **Respuesta Diegética a la Auscultación:** Al colocar el estetoscopio en el tórax, traza la onda $P\text{-}Q\text{-}R\text{-}S\text{-}T$ rítmica a $72\text{ BPM}$ en sanos, o fibrilación ventricular/taquicardia errática a $>150\text{ BPM}$ con tonos cardíacos distorsionados en infectados.

### G. Locomoción Orgánica y Despacho del PNJ (`SurvivorHumanoidController.cs` & `CheckpointFlowManager.cs`)
* **Cinemática Procedimental de Marcha:** Control coordinado de cadera (`Hips`), muslos (`UpLeg`) y rodillas (`Leg`) mediante armónicos senoidales. Flexión de rodilla hacia atrás en la fase de despegue y balanceo pendular de brazos en contrafase.
* **Audio de Pasos Sincronizado:** Generador procedural de pisadas en piso hospitalario (`footstep_soft` a $130\text{ Hz}$) emitido en cada contacto de talón.
* **Aproximación y Despacho:** Los civiles entran caminando desde el pasillo hacia el puesto. Al emitir el veredicto, realizan un giro corporal suave (`Quaternion.Slerp`) hacia su salida respectiva (compuerta neumática al fondo para Cuarentena, o corredor derecho iluminado `SALIDA ZONA SEGURA ➔` para Aprobados).

### H. Linterna UV Forense y Marca de Agua Holográfica (`FlashlightTool.cs` & `DocumentView.cs`)
* **Alternancia de Modo Lumínico:** Conmutación entre Luz Blanca Clínica y Lámpara de Wood Ultravioleta a $395\text{ nm}$ con tecla **U** o clic central.
* **Sello Holográfico de Seguridad:** El pasaporte sanitario incorpora una marca de agua forense invisible a simple vista. Bajo radiación UV revela el sello oficial verde brillante (*MINISTERIO DE SALUD - BIO-SEGURIDAD*) o la advertencia carmesí de falsificación en documentos adulterados (*COPIA IRREGULAR*).

---

## 7. Hoja de Ruta de Siguientes Pasos (Próximas Mejoras de Inmersión)

1. **Respuestas de Voz y Audio Diegético del Civil:**
   - Respuestas breves de agradecimiento o pánico al escuchar la decisión.
2. **Sistema de Escaneo de Equipaje / Maletín Forense:**
   - Bandeja de inspección donde los civiles colocan pertenencias personales o frascos biológicos.
3. **Optimización Perfilada para Meta Quest Standalone:**
   - Validación de tasa de cuadros a 72/90 FPS fijos y draw calls reducidos.
