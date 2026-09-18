# Guía de Arquitectura y Base de Conocimientos (AGENTS.md)
**Proyecto:** Zombie Checkpoint VR (Inspirado en *Papers, Please* y *Zombie Quarantine*)  
**Curso:** Interacción Humano-Computador (IHC)  
**Motor:** Unity 6 (6000.0.0f1) + XR Interaction Toolkit (XRI 3.x) + URP  
**Espacio de trabajo:** `/home/antony/Documents/IHC/Game-VR`  
**Última actualización:** 2026-09-17  

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
│   ├── ComponentExtensions.cs   // Utilidades seguras frente a fake-null (GetOrAddComponent)
│   ├── GameState.cs             // Enums (InspectionState, VerdictType, HandSide)
│   ├── CheckpointFlowManager.cs // Control de ciclo de rondas, spawn de civiles y despacho
│   └── EventBus.cs              // Bus de eventos desacoplado del juego
├── Decision/
│   ├── DecisionButton.cs           // Pulsadores físicos de veredicto (Aprobado / Cuarentena)
│   └── InspectionCommandButton.cs  // Pulsadores físicos de orden clínica (Levantar brazos / Torso)
├── Documents/
│   ├── DocumentData.cs          // POCO con datos biográficos, vigencia y fotos
│   ├── DocumentInteractable.cs  // XRGrabInteractable con soporte de sellado físico
│   └── DocumentView.cs          // Renderizado dinámico en Canvas WorldSpace
├── Environment/
│   └── QuarantineDoorController.cs // Control cinemático y sonoro de compuertas
├── HCI/
│   ├── Gestures/                // Módulo de comandos por manos (interacción sin mandos)
│   │   ├── HandGestureType.cs        // Léxico gestual y restricciones de orientación
│   │   ├── HandGestureSample.cs      // Muestra normalizada de una mano (struct sin allocs)
│   │   ├── HandGestureDefinition.cs  // Definición declarativa de un gesto (rangos + dwell)
│   │   ├── HandGestureCatalog.cs     // Catálogo calibrado por defecto y enlaces gesto→orden
│   │   ├── HandGestureRecognizer.cs  // Reconocedor articular (XRHandSubsystem + XRFingerShape)
│   │   ├── HandCommandDispatcher.cs  // Traducción gesto→orden clínica + feedback multimodal
│   │   ├── GestureCommandContext.cs  // Resolución segura de actores vivos de la escena
│   │   ├── IGestureCommand.cs        // Contrato de orden ejecutable y enlaces serializables
│   │   ├── HandGestureFeedback.cs    // Estado visualizable de una mano (gesto, progreso, pose)
│   │   ├── HandCommandHud.cs         // Orquestador de retroalimentación anclada a la mano
│   │   ├── HandFeedbackRing.cs       // Anillo de confirmación sobre la palma que gesticula
│   │   ├── HandCheatSheetPanel.cs    // Chuleta de gestos al girar la palma hacia la cara
│   │   ├── RingSpriteFactory.cs      // Generador procedural del sprite anular
│   │   ├── HandGestureKeyboardSimulator.cs // Inyección de gestos por teclado (solo Editor)
│   │   └── Commands/                 // Órdenes concretas (brazos, torso, alto, UV, veredicto)
│   ├── Grip/                    // Agarre natural con la palma (herramientas empuñadas, no pellizcadas)
│   │   ├── HandAxis.cs               // Ejes anatómicos de la mano y conversión especular izq/der
│   │   ├── PalmGripAnchor.cs         // Ancla de agarre en la palma por mano + agarre por cierre de mano
│   │   ├── HandGraspSelectReader.cs  // Bypass de selección de XRI: pinza O puño
│   │   ├── PalmGripProfile.cs        // Asiento del objeto en la palma (filtro de selección por mano)
│   │   ├── PalmGripPresets.cs        // Empuñaduras calibradas: linterna, sello, estetoscopio, pasaporte
│   │   └── NaturalHandGripSystem.cs  // Servicio de escena que instala anclas y perfiles en runtime
│   ├── HandPalmFrame.cs         // Marco anatómico de la palma compartido por gestos y agarre
│   ├── HapticManager.cs         // API háptica dual (XRI 3.x HapticImpulsePlayer + OpenXR)
│   ├── SpatialAudioManager.cs   // Generador de audio posicional para latidos, alarmas y sellos
│   ├── UsabilityMetricsTracker.cs // Métricas IHC: tiempos de decisión, aciertos y turnos
│   └── XRSimulatorDesktopEnhancer.cs // Ergonomía completa para desarrollo sin visor VR
├── Survivors/
│   ├── IInspectableBodyPart.cs  // Contrato de extremidades examinables
│   ├── BodyPartExaminer.cs      // Colisionador de inspección en huesos
│   ├── SurvivorModel.cs         // Estado biológico, síntomas e identidad del PNJ
│   ├── SurvivorHumanoidController.cs // Posturas naturales, cinemática bimanual y pose clínica
│   ├── TorsoClothingController.cs    // Desvestimiento/remangado del polo y torso descubierto
│   └── Symptoms/
│       ├── ISymptom.cs          // Contrato base para síntomas clínicos
│       ├── BiteMarkSymptom.cs   // Mordeduras de infectado en antebrazo
│       ├── HeartbeatSymptom.cs  // Latido cardíaco (auscultación en Spine1)
│       ├── PupilSymptom.cs      // Reflejo fotomotor pupilar (linterna en Head)
│       └── RashSymptom.cs       // Erupción cutánea y petequias fluorescentes bajo UV
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
| **Ordenar Levantar / Bajar Brazos** | Tecla **V** (o **B**) | Ordena al civil elevar brazos a $82^\circ$ simétricos para examinar axilas y tórax. Evita conflicto con el botón secundario del simulador. |
| **Retirar / Levantar Polo (Torso)** | Tecla **C** | Descubre el torso del civil revelando erupciones cutáneas o marcas. |
| **Conmutar Modo Mando / Manos** | Tecla **H** | Alterna entre visualización y simulación de Mandos y Manos Articuladas en `XR Device Simulator`. |
| **Mostrar / Ocultar Ayuda HUD** | Teclas **F1** / **O** | Despliega una tarjeta semi-transparente con los controles y el ángulo actual de la muñeca. |
| **Simular Gesto de Mano (Comandos)** | Teclas **1 – 8** (mantener **Shift Izq** = mano izquierda) | Inyecta gestos sintéticos en el módulo de comandos por manos cuando no hay seguimiento articular real: **1** palma arriba, **2** palma abajo, **3** índice señalando, **4** pulgar arriba, **5** pulgar abajo, **6** palma al frente, **7** pinza, **8** puño. Exclusivo del Editor. |

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

### I. Robustez de Componentes y Manejo Seguro de "Fake-Null" (PR #1)
* **Utilidad `ComponentExtensions.GetOrAddComponent`:** Soluciona el fallo silente del operador C# `??` con `UnityEngine.Object`, asegurando que `GetComponent()` inexistente devuelva o añada el componente de forma transparente y robusta.
* **Refactorización de Búsqueda Segura:** Implementación de `TryGetComponent` en `XRSimulatorDesktopEnhancer.cs`, `FlashlightTool.cs`, `SurvivorHumanoidController.cs` y `CheckpointFlowManager.cs`.
* **Higiene de Versionado:** Exclusión de carpetas personales de IDE (`.vscode/`, `*.slnx`) en `.gitignore`.

### J. Manos Virtuales Articuladas y XR Hands (`Complete XR Origin Set Up Hands Variant`)
* **Sustitución del Rig de Mandos:** Migración del rig base a `Complete XR Origin Set Up Hands Variant.prefab` (GUID `77e7c27b2c5525e4aa8cc9f99d654486`) procedente de la plantilla oficial de RV de Unity.
* **Gestión Bimodal Automática (`XRInputModalityManager`):** Conmuta de manera transparente entre manos articuladas con tracking de dedos esquelético (`LeftHandQuestVisual` y `RightHandQuestVisual`) y mandos estándar con retroalimentación háptica.
* **Permisos Meta Quest:** Integración en escena de `Hands Permissions Manager.prefab` para activar automáticamente el subsistema OpenXR de seguimiento de manos en visores Meta Quest sin fricción de configuración.

### K. Cinemática Bimanual y Postura de Inspección Torácica (`SurvivorHumanoidController.cs`)
* **Manipulación Bimanual de Muñecas:** Implementación de `XRGrabInteractable` independientes para ambas manos (`rightHandGrab` y `leftHandGrab`) con tracking cinemático suave y rotación axial de antebrazos.
* **Pose de Inspección Clínica ("Levante los brazos"):** Cinemática guiada que eleva los brazos del sospechoso a $85^\circ$ con separación de codos, despejando completamente el campo visual de las axilas, costados y región pectoral para auscultación torácica y búsqueda de mordeduras o anomalías.
* **Comando y Acceso Rápido:** Accionable físicamente desde el mostrador (Botón Cian) o tecla de teclado **B** en pruebas de escritorio.

### L. Desvestimiento / Remangado del Polo y Torso Anatómico (`TorsoClothingController.cs`)
* **Exposición Anatómica del Tórax:** Alterna dinámicamente entre la ropa superior del PNJ (`npc_hmn_..._top`) y la malla del torso descubierto (`npc_hmn_01m_torso1.fbx` / `npc_hmn_01f_torso1.fbx`), vinculando automáticamente los huesos del `SkinnedMeshRenderer` al rig activo del personaje.
* **Affordance Tangible:** Incluye asa esférica interactiva en el dobladillo inferior del polo (`XRGrabInteractable`) que permite levantarlo tirando hacia arriba en VR.
* **Audio y Atajo:** Emisión de audio procedural de tela (`cloth_rustle` a $340\text{ Hz}$) y atajo de teclado **C**.

### M. Síntoma Dermatológico: Erupción Infecciosa / Petequias (`RashSymptom.cs`)
* **Manifestación Clínica:** Generación procedural de máculas eritematosas y petequias inflamatorias sobre el esternón y las costillas, invisibles mientras el sujeto lleva puesta la camisa y reveladas al descubrir el torso.
* **Respuesta Fotónica Dual:** Bajo luz blanca normal se evidencia la lesión dérmica; bajo el haz UV de la linterna clínica (395nm), la erupción reacciona emitiendo fluorescencia verde vítrea esmeralda patognomónica de la cepa vírica.

### N. Pulsadores Diegéticos de Mesa para Comandos Clínicos (`InspectionCommandButton.cs`)
* **Botones Físicos en Mostrador:** Pulsador Cian (`Button_RaiseArms`) y Pulsador Ámbar (`Button_InspectTorso`) situados al alcance ergonómico de la mano izquierda en la mesa de control.
* **Feedback Multimodal:** Carrera mecánica de descenso de $18\text{ mm}$, sonido de clic de contacto (`button_click`), vibración háptica bimanual y etiquetas legibles en Canvas WorldSpace con acceso directo `[ V ]` y `[ C ]`.

### O. Auditoría Integral y Resolución de Bugs Críticos
* **Eliminación de Salto Accidental en Cabina:** Desactivación y aislamiento de `JumpProvider` y el GameObject `Locomotion/Jump` en el rig XR instanciado. Elimina el salto accidental que ocurría al presionar la tecla B (SecondaryButton del simulador Quest).
* **Simetría Anatómica Exacta de Brazos (`SurvivorHumanoidController.cs`):** Identificación del espejo de coordenadas en el hueso `RightShoulder` del rig humanoide (`npc_casual_set_00`). Al unificar las rotaciones locales a $(355.1^\circ, 351.9^\circ, 298.1^\circ)$ para ambos brazos, ambos se elevan de forma idéntica a $82^\circ$ simétricos en espacio de mundo.
* **Eliminación de Translucidez en Manos Virtuales:** Reemplazo del shader translúcido `Shader Graphs/Unity_Hand_Noise` y pase `Unlit/DepthOnly` por el material PBR opaco `M_Hand_OpaqueSkin.mat` (`Universal Render Pipeline/Lit`) con tono de piel natural y renderQueue 2000 (Opaque).
* **Sensibilidad y Recorrido Ágil del Puntero / Rayo (`XRSimulatorDesktopEnhancer.cs`):** 
  - Soporte de reflexión dinámico para `XRSimulatedHandState` (`euler` y `rotation`), permitiendo inclinación de mesa (tecla **T**) y ajuste fino (**Q / E**) tanto en mandos como en manos articuladas.
  - Incremento de sensibilidad de rotación con ratón de $0.2$ a $1.0$ (mapeo natural 1:1).
  - Desactivación de `HandsOneEuroFilterPostProcessor` y anulación de la estabilización artificial en `CurveInteractionCaster` y `InteractionAttachController` durante pruebas en PC para eliminar latencias.
* **Detección Lumínica Tolerante (`FlashlightTool.cs`):** Integración de `Physics.SphereCast` ($r = 0.06\text{ m}$) con `QueryTriggerInteraction.Collide` para asegurar la detección de pupilas y erupciones torácicas sin depender de un rayo infinitesimal.
* **Refresco Robusto de Auscultación (`StethoscopeTool.cs`):** Validación de referencias vivas frente a objetos destruidos entre rondas para asegurar auscultación continua en cada nuevo civil.

### P. Calibración de Altura en Meta Quest y Aislamiento del Simulador (`QuestBuildHelper.cs` & `XRSimulatorDesktopEnhancer.cs`)
* **Diagnóstico del "Spawn de Ratón":** En compilaciones Android para Meta Quest, la presencia de `XR Device Simulator` en la escena secuestraba los dispositivos de entrada (`removeOtherHMDDevices = true`), anulando el tracking 6DOF del visor físico e inyectando una posición estática $(0, 0, 0)$. Sumado a `TrackingOriginMode.NotSpecified`, la cámara se fijaba a ras de suelo ($Y = 0\text{ m}$), mirando únicamente las zapatillas del sospechoso.
* **Calibración de Suelo (Floor Tracking):** `XROrigin.RequestedTrackingOriginMode` configurado explícitamente en `TrackingOriginMode.Floor` con `CameraYOffset = 1.65f`. Esto calibra automáticamente la altura de la cámara respecto al guardián físico de Meta Quest, situando la línea de visión del jugador a nivel natural frente al civil ($1.70\text{ m}$) y el mostrador clínico ($1.05\text{ m}$).
* **Preprocesador de Compilación Automático (`QuestBuildSceneProcessor`):** Implementación de `IProcessSceneWithReport` en `QuestBuildHelper.cs` que suprime automáticamente en memoria el GameObject `XR Device Simulator` durante el empaquetado del APK de Android, preservándolo intacto en el Editor de Unity para desarrollo en PC.
* **Autodestrucción en Runtime (Guarda de Seguridad):** Inclusión de `#if !UNITY_EDITOR` en el `Awake()` de `XRSimulatorDesktopEnhancer.cs` para destruir inmediatamente el simulador si alguna vez llega a instanciarse en un ejecutable Standalone/Android.

### Q. Módulo de Comandos por Manos (`Assets/Scripts/HCI/Gestures/`)
* **Interacción Sin Mandos ni Botones:** El oficial puede dirigir toda la inspección usando únicamente sus manos desnudas. El módulo lee el esqueleto articular real de XR Hands 1.9 (`XRHandSubsystem` + `XRFingerShapeMath`) y traduce posturas estáticas en órdenes clínicas, sin depender de assets de los Samples del paquete.
* **Vocabulario Gestual Calibrado (`HandGestureCatalog.cs`):**

  | Gesto | Orden Emitida | Mapeo Natural (IHC) |
  | :--- | :--- | :--- |
  | Palma abierta hacia arriba | Levantar brazos del civil | Metáfora física de "arriba" |
  | Palma abierta hacia abajo | Bajar brazos | Metáfora física de "abajo" |
  | Índice señalando al frente | Descubrir / cubrir torso | "Descúbrase ahí" |
  | Palma al frente (alto) | Detener la marcha del civil | Gesto universal de detención en controles |
  | Pulgar arriba (mano derecha) | Veredicto **APROBADO** | Signo cultural de aprobación |
  | Pulgar abajo (mano derecha) | Veredicto **CUARENTENA** | Signo cultural de rechazo |
  | Pinza índice-pulgar / Puño | Reservados (sin asignar) | Evitan colisión con el `select` de XRI y con la postura de agarre |

* **Validación de Forma + Orientación:** Cada gesto exige rangos de curvatura por dedo (`FullCurl` 0–1) **y** una restricción de orientación en espacio de mundo (palma/pulgar/índice contra arriba, abajo o la mirada del jugador), transformando las poses articulares del espacio de seguimiento del `XROrigin` a mundo. La normal de la palma se calcula geométricamente con producto vectorial, invirtiendo el signo entre manos por la simetría anatómica especular.
* **Restricciones Anti Falsos Positivos (IHC):** Dwell obligatorio de $0.6\text{ s}$ en órdenes reversibles y $1.1\text{ s}$ en veredictos irreversibles, cooldown por gesto, bloqueo de repetición hasta deshacer la postura y **zona de comando ergonómica** (la mano debe estar a menos de $0.95\text{ m}$ de la cabeza y dentro del cono de visión) para que un brazo en reposo nunca emita órdenes.
* **Feedback Anclado a la Mano, No a la Cara (`HandFeedbackRing.cs` + `HandCheatSheetPanel.cs`):** Se descarta cualquier panel fijo delante del visor por invasivo: tapaba al propio civil que se está inspeccionando. En su lugar, un **anillo de confirmación flota sobre la palma que gesticula** (corona procedural de $\approx 10\text{ cm}$ con relleno radial cian → verde y porcentaje en el centro), aparece sólo mientras se sostiene la postura y se desvanece al resolverse. La **chuleta de gestos** se despliega únicamente al girar la palma hacia la cara con los dedos hacia arriba (metáfora de consultar un reloj), exigiendo $0.35\text{ s}$ de sostén para no parpadear. Ambos widgets se encaran al jugador e interpolan posición y relleno por fotograma, de modo que el muestreo a $16\text{ Hz}$ del reconocedor se percibe continuo.
* **Confirmación Multimodal de la Orden:** Al ejecutarse, el anillo destella verde con la etiqueta de la orden (o ámbar con “ahora no” si el gesto se entendió pero no era aplicable en ese estado), acompañado de vibración háptica en la mano emisora y sonido diegético del puesto.
* **Arquitectura Desacoplada:** `HandGestureRecognizer` desconoce las órdenes; `HandCommandDispatcher` trabaja solo contra `IGestureCommand` y resuelve los enlaces desde una lista serializable editable en el Inspector. Los eventos `OnHandGestureProgress`, `OnHandGesturePerformed` y `OnHandCommandExecuted` se publican además en el `EventBus` para métricas IHC. El veredicto sigue pasando por `CheckpointFlowManager`, que conserva la protección contra doble veredicto.
* **Instalación e Iteración:** Menú `ZombieCheckpoint ▸ Instalar Modulo de Comandos por Manos` (`HandCommandModuleInstaller.cs`) añade o repara el módulo en la escena abierta sin reconstruirla; `CheckpointSceneBuilder` también lo instala en las reconstrucciones procedurales. Para pruebas en PC, `HandGestureKeyboardSimulator` inyecta gestos con las teclas **1 – 8** y se autodestruye fuera del Editor, igual que el simulador XR.

### R. Agarre Natural con la Palma (`Assets/Scripts/HCI/Grip/`)
* **Diagnóstico:** El rig `XR Origin Hands (XR Rig)` de XRI 3.x ancla todo agarre al **punto de pinza** entre pulgar e índice: tanto el `InteractionAttachController.transformToFollow` como el origen del `SphereInteractionCaster` cercano apuntan a `Pinch Grab Pose` (un `TrackedPoseDriver` sobre la acción `Pinch Position`). Además, las herramientas no declaraban `attachTransform`, así que su pivote se pegaba a las yemas, y el `VelocityTracking` con suavizado las hacía flotar con retardo detrás de la mano. Resultado: todo se sentía "pellizcado".
* **Ancla en la Palma (`PalmGripAnchor.cs`):** Por cada mano articulada se crea una `Palm Grab Pose` que sigue el centro real de la palma leído de XR Hands (bajo el Camera Offset, el mismo espacio que los `TrackedPoseDriver`), actualizada en las fases `Dynamic` y `BeforeRender` para latencia cero. El interactor la sigue **sólo cuando el objetivo cercano o el objeto sostenido tiene perfil de palma**; las partes del cuerpo del civil y los agarres lejanos por rayo conservan intacta la pinza, porque la cinemática del brazo del civil depende de ella. La detección cercana se recentra en una sonda en el hueco de la mano, que con el radio de $10\text{ cm}$ sigue cubriendo el punto de pinza.
* **Agarrar Cerrando la Mano (`HandGraspSelectReader.cs`):** Se instala como `bypass` del lector de selección de XRI, de modo que la pinza original sigue funcionando y **cerrar la mano** (curvatura media de los cuatro dedos $\geq 0.52$, liberación $\leq 0.36$ con histéresis) se suma como segunda vía. El puño sólo agarra si en el instante de cerrarse hay un objeto a menos de $11\text{ cm}$: cerrar en el aire y luego tocar algo no lo coge, como en la vida real.
* **Asiento Anatómico por Objeto (`PalmGripProfile.cs` + `PalmGripPresets.cs`):** Cada herramienta declara qué eje propio apunta a qué dirección anatómica (dedos, palma, pulgar, meñique). Como las manos son imágenes especulares, el attach se recalcula **por mano** dentro de un `IXRSelectFilter` (el único punto donde XRI conoce qué mano va a agarrar antes de fijar desfases), trabajando en espacio de mundo para ser inmune a escalas no uniformes. Presets calibrados con la geometría de `CheckpointSceneBuilder`:

  | Objeto | Empuñadura | Justificación IHC |
  | :--- | :--- | :--- |
  | Linterna | Puño sobre el mango, haz saliendo por el lado del pulgar | Como sujetar una linterna real para apuntar |
  | Sello | Puño en el vástago, almohadilla bajo el meñique | Se estampa bajando el puño |
  | Estetoscopio | Campana en la palma, cuerpo saliendo por el meñique | Basta apoyar la palma en el pecho para auscultar |
  | Pasaporte | Apoyado en la palma, cara hacia fuera, texto hacia los dedos | Se lee girando la mano hacia la cara |

* **Seguimiento Sin Flotación:** Los objetos con perfil pasan a `MovementType.Instantaneous` sin suavizado: van pegados a la mano en lugar de perseguirla. Contrapartida conocida: mientras se sostienen atraviesan la mesa, porque ya no los frena la física; los sellos y la auscultación siguen funcionando porque dependen de *triggers*, no de colisiones.
* **Coexistencia con los Gestos:** `PalmGripAnchor` publica `EventBus.OnHandGrabStateChanged`; el reconocedor ignora las posturas de una mano que sostiene algo (un puño con el pulgar arriba agarrando la linterna ya no puede emitir un veredicto) y la chuleta de la palma no se despliega mientras se lee el pasaporte.
* **Instalación:** Menú `ZombieCheckpoint ▸ Instalar Agarre Natural de Manos` (no destructivo); `CheckpointSceneBuilder` también lo instala. Todo se aplica en tiempo de ejecución sin modificar los prefabs del rig, y `NaturalHandGripSystem` escucha `XRInteractionManager.interactableRegistered` para cubrir objetos que aparezcan más tarde.

---

## 7. Hoja de Ruta de Siguientes Pasos (Próximas Mejoras de Inmersión)

1. **Respuestas de Voz y Audio Diegético del Civil:**
   - Respuestas breves de agradecimiento o pánico al escuchar la decisión o al recibir las órdenes ("¡Sí, oficial!", "¡No me haga daño!").
2. **Sistema de Escaneo de Equipaje / Maletín Forense:**
   - Bandeja de inspección donde los civiles colocan pertenencias personales, fármacos o frascos biológicos sospechosos.
3. **Optimización Perfilada para Meta Quest Standalone:**
   - Validación de tasa de cuadros a 72/90 FPS fijos y minimización de draw calls con batching estático y GPU instancing.
