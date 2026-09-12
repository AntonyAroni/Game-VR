# Guía de Arquitectura y Convenciones para Agentes de IA (AGENTS.md)
**Proyecto:** Zombie Checkpoint VR (Inspirado en *Papers, Please* y *Zombie Quarantine*)  
**Curso:** Interacción Humano-Computador (IHC)  
**Motor:** Unity 6 (6000.0.0f1) + XR Interaction Toolkit (XRI 3.x) + URP  

---

## 1. Principios de Arquitectura y Buenas Prácticas (SOLID)

El código debe ser **altamente modular, desacoplado y auto-documentado**. Queda estrictamente prohibido crear scripts monolíticos tipo "God Objects" (`GameManager` que controla todo).

### S - Single Responsibility Principle (SRP)
Cada script debe tener una única responsabilidad:
* `StethoscopeTool.cs`: Solo gestiona el agarre, detección de contacto con el cuerpo y activación de auscultación.
* `HapticFeedbackService.cs`: Solo gestiona el envío de impulsos hápticos a los mandos VR.
* `PupilReaction.cs`: Solo gestiona la dilatación o reacción pupilar ante la luz de la linterna.
* `SurvivorModel.cs`: Almacena el estado de salud, síntomas y datos biológicos del PNJ. No maneja UI ni físicas de la mesa.

### O - Open/Closed Principle (OCP)
El sistema debe estar abierto a extensión pero cerrado a modificación mediante interfaces:
* `IInspectableBodyPart`: Interfaz para cualquier extremidad o zona corporal que pueda ser examinada.
* `IInspectionTool`: Interfaz para herramientas (linterna, estetoscopio, termómetro/escáner).
* `ISymptom`: Interfaz para cualquier tipo de síntoma detectable (mordedura, pulso anómalo, dilatación anormal, tos).

### L - Liskov Substitution Principle (LSP)
Cualquier síntoma o herramienta debe poder ser sustituido por otra implementación de su interfaz base sin romper el evaluador de inspección.

### I - Interface Segregation Principle (ISP)
Interfaces pequeñas y específicas en lugar de interfaces enormes:
* `IHapticSource`: Quien emita vibración háptica implementa solo esto.
* `IAudioCueProvider`: Quien emita sonidos espacializados 3D.

### D - Dependency Inversion Principle (DIP) & Event-Driven Architecture
* Desacoplar sistemas mediante eventos de C# (`System.Action` / `event`) o ScriptableObject Event Channels.
* El `InspectionEvaluator` no busca directamente objetos en la escena; escucha eventos como `OnToolAppliedToBodyPart(tool, bodyPart)` o `OnDecisionMade(decisionType)`.

---

## 2. Estructura Modular de Carpetas

Dentro de `Assets/Scripts/`:
```text
Assets/Scripts/
├── Core/
│   ├── GameState.cs             // Enum y máquina de estados (Waiting, Inspecting, Deciding, Result)
│   ├── CheckpointFlowManager.cs // Control de flujo de llegada/salida de supervivientes
│   └── EventBus.cs              // Canales de eventos desacoplados
├── Survivors/
│   ├── SurvivorData.cs          // ScriptableObject o POCO con los datos del PNJ
│   ├── SurvivorController.cs    // Movimiento, animaciones (Mixamo) y posicionamiento
│   ├── BodyPartExaminer.cs      // Detección de giro de brazos/cabeza y colisiones de examen
│   └── Symptoms/
│       ├── ISymptom.cs
│       ├── BiteMarkSymptom.cs
│       ├── HeartbeatSymptom.cs
│       └── PupilSymptom.cs
├── Tools/
│   ├── IInspectionTool.cs
│   ├── FlashlightTool.cs        // Linterna UV / luz blanca
│   ├── StethoscopeTool.cs       // Auscultación con audio 3D + hápticos
│   ├── StampTool.cs             // Sello físico (Aprobado / Cuarentena)
│   └── LeverOrButton.cs         // Accionadores tangibles de decisión
├── Documents/
│   ├── DocumentData.cs          // Datos del documento (nombre, foto, fecha, sello)
│   ├── DocumentView.cs          // Renderizado dinámico en TextMeshPro / UI Canvas WorldSpace
│   └── DocumentInteractable.cs  // Interacción XR Grab con físicas livianas
├── HCI/
│   ├── HapticManager.cs         // API centralizada para enviar hápticos a mandos VR
│   ├── SpatialAudioManager.cs   // Audio 3D para latidos, suspiros y alarmas
│   └── UsabilityMetrics.cs      // Métricas IHC: tiempo de inspección, errores, aciertos
└── UI/
    ├── WorldSpaceBoothUI.cs     // Pantallas o letreros diegéticos dentro del puesto
    └── DecisionDisplay.cs       // Feedback visual del veredicto
```

---

## 3. Normas de Codificación C#

1. **Namespaces:** Todo script debe estar contenido en el namespace raíz `ZombieCheckpoint.<Módulo>`.  
   *Ejemplo:* `namespace ZombieCheckpoint.Tools { ... }`
2. **Serialización limpia:** Usar `[SerializeField] private` en lugar de campos `public` para referencias del Inspector.
3. **Validación de referencias:** Validar con `TryGetComponent` o `null-check` explícito con mensajes claros de advertencia si falta una referencia en el Inspector.
4. **Documentación:** Comentarios XML en métodos y clases (`/// <summary>`) destacando el propósito de IHC (por qué se usa esa metáfora de interacción).
5. **No allocations en Update:** Evitar `new`, `GetComponent` o `FindObjectOfType` dentro de bucles `Update()`.

---

## 4. Reglas para Agentes de IA y MCP Unity

1. **Compilación continua:** Tras crear o editar cualquier script `.cs`, ejecutar `unityMCP.read_console` para verificar que no haya errores de compilación (`CSXXXX`).
2. **No romper prefabs:** No modificar valores de GUIDs o metadatos `.meta` manualmente si no es necesario.
3. **Enfoque en IHC:** Cualquier nueva mecánica debe justificarse según los principios de interacción de Norman (Visibilidad, Affordances, Feedback, Mapeo natural, Restricciones).
