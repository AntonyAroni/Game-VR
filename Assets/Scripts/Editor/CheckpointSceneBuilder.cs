using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ZombieCheckpoint.Core;
using ZombieCheckpoint.Decision;
using ZombieCheckpoint.Documents;
using ZombieCheckpoint.HCI;
using ZombieCheckpoint.Survivors;
using ZombieCheckpoint.Survivors.Symptoms;
using ZombieCheckpoint.Tools;

namespace ZombieCheckpoint.Editor
{
    public static class CheckpointSceneBuilder
    {
        [MenuItem("ZombieCheckpoint/Construir Escena de Inspeccion")]
        public static void BuildScene()
        {
            // 1. Abrir BasicScene como plantilla base
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/BasicScene.unity", OpenSceneMode.Single);

            // 2. Limpiar elementos previos generados
            var existingBooth = GameObject.Find("Checkpoint_Booth");
            if (existingBooth != null) Object.DestroyImmediate(existingBooth);

            var existingRoom = GameObject.Find("Quarantine_Room");
            if (existingRoom != null) Object.DestroyImmediate(existingRoom);

            var outdoorPlane = GameObject.Find("Plane");
            if (outdoorPlane != null) outdoorPlane.SetActive(false);

            // Reemplazar XR Origin con Hands Variant (Manos virtuales articuladas en vez de mandos plásticos)
            var handsVariantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRTemplateAssets/Prefabs/Setup/Complete XR Origin Set Up Hands Variant.prefab")
                                  ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.5.1/Hands Interaction Demo/Prefabs/XR Origin Hands (XR Rig).prefab");

            var existingRig = GameObject.Find("XR Origin (XR Rig)") ?? GameObject.Find("XR Origin Hands (XR Rig)") ?? GameObject.Find("Complete XR Origin Set Up Hands Variant");
            if (existingRig != null && handsVariantPrefab != null)
            {
                Vector3 prevPos = existingRig.transform.position;
                Quaternion prevRot = existingRig.transform.rotation;
                Object.DestroyImmediate(existingRig);

                var newRig = (GameObject)PrefabUtility.InstantiatePrefab(handsVariantPrefab);
                newRig.name = "XR Origin Hands (XR Rig)";
                newRig.transform.position = prevPos;
                newRig.transform.rotation = prevRot;

                ConfigureHandsRig(newRig);
            }
            else if (existingRig != null)
            {
                ConfigureHandsRig(existingRig);
            }

            // Asegurar Hands Permissions Manager para Meta Quest
            var permPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRTemplateAssets/Prefabs/Setup/Hands Permissions Manager.prefab");
            if (permPrefab != null && GameObject.Find("Hands Permissions Manager") == null)
            {
                var permObj = (GameObject)PrefabUtility.InstantiatePrefab(permPrefab);
                permObj.name = "Hands Permissions Manager";
            }

            // Ajustar iluminación ambiental y luz direccional para visibilidad interior nítida
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.40f, 0.42f, 0.48f, 1f);

            var dirLightObj = GameObject.Find("Directional Light");
            if (dirLightObj != null)
            {
                var dirLight = dirLightObj.GetComponent<Light>();
                if (dirLight != null)
                {
                    dirLight.intensity = 0.75f;
                    dirLight.color = new Color(0.9f, 0.93f, 1.0f);
                    dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                }
            }

            // Cargar Materiales del Hospital Abandonado
            Material tileFloorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Abandoned_Asylum/Materials/Tiled/Tiles/Tiles_6.mat")
                                 ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Abandoned_Asylum/Materials/Tiled/Tiles/Tiles_1.mat");
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Abandoned_Asylum/Materials/Tiled/Walls/Wall_1.mat")
                            ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Abandoned_Asylum/Materials/Tiled/Walls/Wall_2.mat");
            Material ceilingMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Abandoned_Asylum/Materials/Tiled/Walls/Wall_4.mat")
                               ?? wallMat;

            // Clones con tiling adecuado para evitar estiramiento de texturas
            Material tiledFloor = tileFloorMat != null ? new Material(tileFloorMat) { mainTextureScale = new Vector2(6f, 7f) } : null;
            Material tiledWallX = wallMat != null ? new Material(wallMat) { mainTextureScale = new Vector2(5f, 3f) } : null;
            Material tiledWallZ = wallMat != null ? new Material(wallMat) { mainTextureScale = new Vector2(6f, 3f) } : null;

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material paperMat = new Material(urpShader) { name = "M_Paper", color = new Color(0.92f, 0.9f, 0.82f) };
            Material greenMat = new Material(urpShader) { name = "M_ApprovedGreen", color = new Color(0.1f, 0.85f, 0.25f) };
            Material redMat = new Material(urpShader) { name = "M_QuarantineRed", color = new Color(0.95f, 0.15f, 0.15f) };
            Material skinMat = new Material(urpShader) { name = "M_MannequinSkin", color = new Color(0.85f, 0.72f, 0.62f) };
            Material woundMat = new Material(urpShader) { name = "M_InfectedWound", color = new Color(0.7f, 0.05f, 0.05f) };
            Material toolMat = new Material(urpShader) { name = "M_MetalTool", color = new Color(0.65f, 0.7f, 0.75f) };
            Material deskMat = new Material(urpShader) { name = "M_MonitorFrame", color = new Color(0.15f, 0.17f, 0.2f) };
            Material stampWoodMat = new Material(urpShader) { name = "M_StampWood", color = new Color(0.32f, 0.2f, 0.12f) };
            Material stampBrassMat = new Material(urpShader) { name = "M_StampBrass", color = new Color(0.72f, 0.62f, 0.35f) };
            Material cyanMat = new Material(urpShader) { name = "M_CommandCyan", color = new Color(0.1f, 0.7f, 0.95f) };
            Material amberMat = new Material(urpShader) { name = "M_CommandAmber", color = new Color(0.95f, 0.6f, 0.1f) };

            // --- 3. HABITACIÓN HOSPITALARIA (SALA DE AISLAMIENTO) ---
            GameObject roomRoot = new GameObject("Quarantine_Room");

            // Suelo con baldosas de hospital
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor_HospitalTiles";
            floor.transform.SetParent(roomRoot.transform);
            floor.transform.position = new Vector3(0f, -0.05f, 1.25f);
            floor.transform.localScale = new Vector3(5.5f, 0.1f, 6.5f);
            if (tiledFloor != null) floor.GetComponent<Renderer>().sharedMaterial = tiledFloor;

            // Techo
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(roomRoot.transform);
            ceiling.transform.position = new Vector3(0f, 3.2f, 1.25f);
            ceiling.transform.localScale = new Vector3(5.5f, 0.1f, 6.5f);
            if (ceilingMat != null) ceiling.GetComponent<Renderer>().sharedMaterial = ceilingMat;

            // Pared trasera (detrás del jugador)
            GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "Wall_Back";
            backWall.transform.SetParent(roomRoot.transform);
            backWall.transform.position = new Vector3(0f, 1.6f, -2.0f);
            backWall.transform.localScale = new Vector3(5.5f, 3.2f, 0.1f);
            if (tiledWallX != null) backWall.GetComponent<Renderer>().sharedMaterial = tiledWallX;

            // Pared frontal (fondo detrás del sujeto)
            GameObject frontWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontWall.name = "Wall_Front";
            frontWall.transform.SetParent(roomRoot.transform);
            frontWall.transform.position = new Vector3(0f, 1.6f, 4.5f);
            frontWall.transform.localScale = new Vector3(5.5f, 3.2f, 0.1f);
            if (tiledWallX != null) frontWall.GetComponent<Renderer>().sharedMaterial = tiledWallX;

            // Pared izquierda
            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "Wall_Left";
            leftWall.transform.SetParent(roomRoot.transform);
            leftWall.transform.position = new Vector3(-2.75f, 1.6f, 1.25f);
            leftWall.transform.localScale = new Vector3(0.1f, 3.2f, 6.5f);
            if (tiledWallZ != null) leftWall.GetComponent<Renderer>().sharedMaterial = tiledWallZ;

            // Pared derecha con vano / apertura para el corredor de salida hacia la Zona Segura
            GameObject rightWallBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWallBack.name = "Wall_Right_Back";
            rightWallBack.transform.SetParent(roomRoot.transform);
            rightWallBack.transform.position = new Vector3(2.75f, 1.6f, -1.05f);
            rightWallBack.transform.localScale = new Vector3(0.1f, 3.2f, 1.9f);
            if (tiledWallZ != null) rightWallBack.GetComponent<Renderer>().sharedMaterial = tiledWallZ;

            GameObject rightWallFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWallFront.name = "Wall_Right_Front";
            rightWallFront.transform.SetParent(roomRoot.transform);
            rightWallFront.transform.position = new Vector3(2.75f, 1.6f, 2.75f);
            rightWallFront.transform.localScale = new Vector3(0.1f, 3.2f, 3.5f);
            if (tiledWallZ != null) rightWallFront.GetComponent<Renderer>().sharedMaterial = tiledWallZ;

            GameObject rightWallLintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWallLintel.name = "Wall_Right_Lintel";
            rightWallLintel.transform.SetParent(roomRoot.transform);
            rightWallLintel.transform.position = new Vector3(2.75f, 2.65f, 0.45f);
            rightWallLintel.transform.localScale = new Vector3(0.1f, 1.1f, 1.3f);
            if (tiledWallZ != null) rightWallLintel.GetComponent<Renderer>().sharedMaterial = tiledWallZ;

            // Letrero luminoso superior de Zona Segura (Affordance y Visibilidad IHC)
            GameObject safeSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            safeSign.name = "Sign_SafeZone_Exit";
            safeSign.transform.SetParent(roomRoot.transform);
            safeSign.transform.position = new Vector3(2.68f, 2.2f, 0.45f);
            safeSign.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            safeSign.transform.localScale = new Vector3(1.1f, 0.28f, 0.05f);
            var signMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            signMat.color = new Color(0.05f, 0.55f, 0.15f);
            signMat.EnableKeyword("_EMISSION");
            signMat.SetColor("_EmissionColor", new Color(0.1f, 0.8f, 0.2f));
            safeSign.GetComponent<Renderer>().sharedMaterial = signMat;

            // Canvas con texto para el cartel de Zona Segura
            GameObject signCanvasObj = new GameObject("SafeSign_Canvas");
            signCanvasObj.transform.SetParent(safeSign.transform, false);
            signCanvasObj.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            signCanvasObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            signCanvasObj.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);
            var signCanvas = signCanvasObj.AddComponent<Canvas>();
            signCanvas.renderMode = RenderMode.WorldSpace;
            var signTmp = signCanvasObj.AddComponent<TextMeshProUGUI>();
            signTmp.text = "SALIDA ZONA SEGURA >>";
            signTmp.fontSize = 26;
            signTmp.fontStyle = FontStyles.Bold;
            signTmp.color = Color.white;
            signTmp.alignment = TextAlignmentOptions.Center;

            // --- 4. MODELOS Y PROPS DEL HOSPITAL ABANDONADO ---
            // A. Puerta de aislamiento / cuarentena al fondo
            var doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Abandoned_Asylum/Prefabs/Door_V1.prefab");
            if (doorPrefab != null)
            {
                var door = (GameObject)PrefabUtility.InstantiatePrefab(doorPrefab, roomRoot.transform);
                door.name = "Quarantine_ExitDoor";
                door.transform.position = new Vector3(0f, 0f, 4.42f);
                door.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                door.AddComponent<ZombieCheckpoint.Environment.QuarantineDoorController>();
            }

            // B. Estante metálico lateral con suministros médicos
            var shelfPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Abandoned_Asylum/Prefabs/BigShelf.prefab");
            if (shelfPrefab != null)
            {
                var shelf = (GameObject)PrefabUtility.InstantiatePrefab(shelfPrefab, roomRoot.transform);
                shelf.name = "Medical_SupplyShelf";
                shelf.transform.position = new Vector3(-2.55f, 0f, 1.5f);
                shelf.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }

            // C. Maletín médico en el estante
            var casePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Abandoned_Asylum/Prefabs/CaseMetallic.prefab");
            if (casePrefab != null)
            {
                var medCase = (GameObject)PrefabUtility.InstantiatePrefab(casePrefab, roomRoot.transform);
                medCase.name = "Biohazard_Case";
                medCase.transform.position = new Vector3(-2.45f, 1.1f, 1.5f);
                medCase.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }

            // D. Silla de oficina hospitalaria para el evaluador
            var chairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Abandoned_Asylum/Prefabs/ChairOffice.prefab");
            if (chairPrefab != null)
            {
                var chair = (GameObject)PrefabUtility.InstantiatePrefab(chairPrefab, roomRoot.transform);
                chair.name = "Evaluator_Chair";
                chair.transform.position = new Vector3(0.65f, 0f, -0.4f);
                chair.transform.rotation = Quaternion.Euler(0f, -30f, 0f);
            }

            // --- 5. ILUMINACIÓN VISTOSA (FOCO PRINCIPAL Y LUZ DE PARED) ---
            // Foco 1: Lámpara fluorescente (NeonLamp) colgada del techo sobre el puesto
            var neonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Abandoned_Asylum/Prefabs/NeonLamp.prefab");
            GameObject ceilingLamp = neonPrefab != null 
                ? (GameObject)PrefabUtility.InstantiatePrefab(neonPrefab, roomRoot.transform)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);

            ceilingLamp.name = "Ceiling_NeonLamp";
            ceilingLamp.transform.position = new Vector3(0f, 3.05f, 0.75f);
            ceilingLamp.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            // Luz directa de la lámpara (Foco intenso y nítido sobre la mesa)
            GameObject overheadLight = new GameObject("Overhead_DeskSpotlight");
            overheadLight.transform.SetParent(ceilingLamp.transform);
            overheadLight.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            overheadLight.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var mainSpot = overheadLight.AddComponent<Light>();
            mainSpot.type = LightType.Spot;
            mainSpot.range = 7.0f;
            mainSpot.spotAngle = 85f;
            mainSpot.innerSpotAngle = 35f;
            mainSpot.intensity = 15.0f; // Foco intenso sobre el mostrador
            mainSpot.color = new Color(1.0f, 0.98f, 0.92f);
            mainSpot.shadows = LightShadows.Soft;

            // Luz de relleno general de la sala (ilumina el cuarto uniformemente)
            GameObject roomFillObj = new GameObject("Room_GeneralLight");
            roomFillObj.transform.SetParent(roomRoot.transform);
            roomFillObj.transform.position = new Vector3(0f, 2.5f, 1.25f);
            var roomFill = roomFillObj.AddComponent<Light>();
            roomFill.type = LightType.Point;
            roomFill.range = 10.0f;
            roomFill.intensity = 3.5f;
            roomFill.color = new Color(0.92f, 0.95f, 1.0f);

            // Foco 2: Lámpara de pared hospitalaria para profundidad visual
            var wallLampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Abandoned_Asylum/Prefabs/LampWall.prefab");
            if (wallLampPrefab != null)
            {
                var wallLamp = (GameObject)PrefabUtility.InstantiatePrefab(wallLampPrefab, roomRoot.transform);
                wallLamp.name = "Wall_HospitalLamp";
                wallLamp.transform.position = new Vector3(-2.68f, 2.1f, -0.5f);
                wallLamp.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                GameObject wallLight = new GameObject("WallLamp_PointLight");
                wallLight.transform.SetParent(wallLamp.transform);
                wallLight.transform.localPosition = new Vector3(0f, 0f, 0.15f);
                var pLight = wallLight.AddComponent<Light>();
                pLight.type = LightType.Point;
                pLight.range = 4.0f;
                pLight.intensity = 0.8f;
                pLight.color = new Color(1.0f, 0.85f, 0.65f); // Luz cálida tenue
            }

            // Foco 3: Alarma de luz roja sobre la puerta de cuarentena
            GameObject alarmLamp = new GameObject("Quarantine_AlarmLight");
            alarmLamp.transform.SetParent(roomRoot.transform);
            alarmLamp.transform.position = new Vector3(0f, 2.5f, 4.3f);
            var aLight = alarmLamp.AddComponent<Light>();
            aLight.type = LightType.Point;
            aLight.range = 3.5f;
            aLight.intensity = 0.9f;
            aLight.color = new Color(1.0f, 0.1f, 0.1f);

            // --- 6. PUESTO DE CONTROL Y MESA DE INSPECCIÓN REAL ---
            GameObject boothRoot = new GameObject("Checkpoint_Booth");

            // Mesa de oficina hospitalaria real (TableOffice)
            var tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Abandoned_Asylum/Prefabs/TableOffice.prefab");
            GameObject deskObj;
            if (tablePrefab != null)
            {
                deskObj = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab, boothRoot.transform);
                deskObj.name = "Inspection_Desk_TableOffice";
                deskObj.transform.position = new Vector3(0f, 0f, 0.75f);
                deskObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Mirando al evaluador
            }
            else
            {
                deskObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                deskObj.name = "Inspection_Desk";
                deskObj.transform.SetParent(boothRoot.transform);
                deskObj.transform.position = new Vector3(0f, 0.4f, 0.75f);
                deskObj.transform.localScale = new Vector3(1.58f, 0.8f, 0.85f);
            }

            // Collider de la mesa para soporte físico de herramientas
            var deskCol = deskObj.GetComponent<Collider>();
            if (deskCol == null)
            {
                var box = deskObj.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.4f, 0f);
                box.size = new Vector3(1.58f, 0.8f, 0.88f);
            }

            // Altura de superficie del escritorio (0.80 m)
            float deskSurfaceY = 0.81f;

            // --- 7. SUPERVIVIENTE / SUJETO DE PRUEBA REAL (Humanoid NPC) ---
            var npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/npc_casual_set_00/Prefabs/npc_csl_00_character_01m_01.prefab");
            GameObject survivorRoot;
            SurvivorModel survivorModel;
            BiteMarkSymptom biteSymptom = null;
            HeartbeatSymptom heartbeat = null;
            PupilSymptom pupil = null;
            RashSymptom rashSymptom = null;

            if (npcPrefab != null)
            {
                survivorRoot = (GameObject)PrefabUtility.InstantiatePrefab(npcPrefab, boothRoot.transform);
                survivorRoot.name = "Survivor_Subject";
                survivorRoot.transform.position = new Vector3(0f, 0f, 1.45f);
                survivorRoot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                survivorModel = survivorRoot.AddComponent<SurvivorModel>();
                var humController = survivorRoot.AddComponent<SurvivorHumanoidController>();

                // Buscar hueso de antebrazo derecho para la herida de mordedura
                Transform rightForeArm = null;
                Transform spine1 = null;
                foreach (var t in survivorRoot.GetComponentsInChildren<Transform>())
                {
                    if (t.name == "RightForeArm") rightForeArm = t;
                    if (t.name == "Spine1") spine1 = t;
                }

                if (rightForeArm != null)
                {
                    GameObject biteMark = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    biteMark.name = "Bite_Wound_Visual";
                    biteMark.transform.SetParent(rightForeArm);
                    biteMark.transform.localPosition = new Vector3(0.04f, 0.12f, 0f);
                    biteMark.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    biteMark.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
                    biteMark.GetComponent<Renderer>().sharedMaterial = woundMat;
                    Object.DestroyImmediate(biteMark.GetComponent<Collider>());

                    biteSymptom = rightForeArm.gameObject.AddComponent<BiteMarkSymptom>();
                    typeof(BiteMarkSymptom).GetField("woundVisualObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(biteSymptom, biteMark);
                    typeof(BiteMarkSymptom).GetField("woundTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(biteSymptom, biteMark.transform);
                }

                if (spine1 != null)
                {
                    heartbeat = spine1.gameObject.GetComponent<HeartbeatSymptom>();
                    if (heartbeat == null) heartbeat = spine1.gameObject.AddComponent<HeartbeatSymptom>();
                    heartbeat.Initialize(false);
                }

                pupil = null;
                Transform head = null;
                foreach (var t in survivorRoot.GetComponentsInChildren<Transform>())
                {
                    if (t.name == "Head") { head = t; break; }
                }
                if (head != null)
                {
                    var col = head.GetComponent<Collider>();
                    if (col == null)
                    {
                        var sc = head.gameObject.AddComponent<SphereCollider>();
                        sc.radius = 0.16f;
                        sc.isTrigger = true;
                    }
                    pupil = head.GetComponent<PupilSymptom>();
                    if (pupil == null) pupil = head.gameObject.AddComponent<PupilSymptom>();
                    pupil.Initialize(false);
                }
                // Configuración de torso descubierto y ropa
                var torsoCtrl = survivorRoot.AddComponent<TorsoClothingController>();
                var mTorsoInit = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/npc_casual_set_00/Mesh/Mesh_Parts/npc_hmn_01m_torso1.fbx");
                if (mTorsoInit != null) torsoCtrl.ConfigureBareTorso(mTorsoInit);

                if (spine1 != null)
                {
                    rashSymptom = spine1.gameObject.GetComponent<RashSymptom>();
                    if (rashSymptom == null) rashSymptom = spine1.gameObject.AddComponent<RashSymptom>();
                    rashSymptom.Initialize(false);
                }
            }
            else
            {
                survivorRoot = new GameObject("Survivor_Subject");
                survivorRoot.transform.SetParent(boothRoot.transform);
                survivorRoot.transform.position = new Vector3(0f, 0f, 1.45f);
                survivorModel = survivorRoot.AddComponent<SurvivorModel>();
            }

            var sModelType = typeof(SurvivorModel);
            sModelType.GetField("biteSymptom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(survivorModel, biteSymptom);
            sModelType.GetField("heartbeatSymptom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(survivorModel, heartbeat);
            sModelType.GetField("pupilSymptom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(survivorModel, pupil);
            sModelType.GetField("rashSymptom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(survivorModel, rashSymptom);

            // --- 8. LINTERNA 3D REAL (Del Asset Importado) ---
            var flashlightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flashlight/Model/Flashlight.prefab");
            GameObject flashObj;
            if (flashlightPrefab != null)
            {
                flashObj = (GameObject)PrefabUtility.InstantiatePrefab(flashlightPrefab, boothRoot.transform);
                flashObj.name = "Tool_Flashlight";
                flashObj.transform.position = new Vector3(-0.38f, deskSurfaceY + 0.04f, 0.62f);
                flashObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Acostada apuntando al frente
            }
            else
            {
                flashObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                flashObj.name = "Tool_Flashlight";
                flashObj.transform.SetParent(boothRoot.transform);
                flashObj.transform.position = new Vector3(-0.38f, deskSurfaceY + 0.04f, 0.62f);
                flashObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                flashObj.transform.localScale = new Vector3(0.04f, 0.1f, 0.04f);
            }

            flashObj.tag = "Tool";
            var flashCol = flashObj.GetComponent<Collider>();
            if (flashCol == null)
            {
                var cap = flashObj.AddComponent<CapsuleCollider>();
                cap.radius = 0.035f;
                cap.height = 0.23f;
                cap.direction = 1; // Y axis
            }

            var flashRb = flashObj.GetComponent<Rigidbody>();
            if (flashRb == null) flashRb = flashObj.AddComponent<Rigidbody>();
            flashRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var flashGrab = flashObj.GetComponent<XRGrabInteractable>();
            if (flashGrab == null) flashGrab = flashObj.AddComponent<XRGrabInteractable>();

            // Waypoints permanentes para superviviente y documento (nunca se destruyen)
            GameObject standPointAnchor = new GameObject("Survivor_StandPoint_Anchor");
            standPointAnchor.transform.SetParent(boothRoot.transform);
            standPointAnchor.transform.position = new Vector3(0f, 0f, 1.45f);
            standPointAnchor.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            GameObject docSpawnAnchor = new GameObject("Document_SpawnPoint_Anchor");
            docSpawnAnchor.transform.SetParent(boothRoot.transform);
            docSpawnAnchor.transform.position = new Vector3(0f, deskSurfaceY + 0.01f, 0.62f);
            docSpawnAnchor.transform.rotation = Quaternion.identity;

            // Foco de luz Spot proyectado desde la punta de la linterna hacia el frente (+Z)
            GameObject fLightChild = new GameObject("SpotLight");
            fLightChild.transform.SetParent(flashObj.transform);
            fLightChild.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            fLightChild.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var fSpot = fLightChild.AddComponent<Light>();
            fSpot.type = LightType.Spot;
            fSpot.range = 12.0f;
            fSpot.spotAngle = 55f;
            fSpot.innerSpotAngle = 25f;
            fSpot.intensity = 28.0f;
            fSpot.color = new Color(0.88f, 0.95f, 1.0f);
            fSpot.shadows = LightShadows.None;
            fSpot.enabled = true;
            var addData = fLightChild.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();

            var flashTool = flashObj.GetComponent<FlashlightTool>();
            if (flashTool == null) flashTool = flashObj.AddComponent<FlashlightTool>();
            typeof(FlashlightTool).GetField("spotLight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(flashTool, fSpot);

            // --- 9. HERRAMIENTAS RESTANTES ---
            // Estetoscopio 3D Real
            var stethoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Stethoscope/Prefabs/Stethoscope.prefab");
            GameObject stetho;
            if (stethoPrefab != null)
            {
                stetho = (GameObject)PrefabUtility.InstantiatePrefab(stethoPrefab, boothRoot.transform);
                PrefabUtility.UnpackPrefabInstance(stetho, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                stetho.name = "Tool_Stethoscope";
                stetho.transform.position = new Vector3(-0.18f, deskSurfaceY + 0.02f, 0.62f);
                stetho.transform.rotation = Quaternion.Euler(0f, 35f, 0f);

                var boxCol = stetho.GetComponent<BoxCollider>();
                if (boxCol == null) boxCol = stetho.AddComponent<BoxCollider>();
                boxCol.center = new Vector3(0f, 0.05f, 0.08f);
                boxCol.size = new Vector3(0.22f, 0.12f, 0.35f);
            }
            else
            {
                stetho = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stetho.name = "Tool_Stethoscope";
                stetho.transform.SetParent(boothRoot.transform);
                stetho.transform.position = new Vector3(-0.18f, deskSurfaceY + 0.02f, 0.62f);
                stetho.transform.localScale = new Vector3(0.08f, 0.02f, 0.08f);
                stetho.GetComponent<Renderer>().sharedMaterial = toolMat;
            }

            stetho.tag = "Tool";
            var stethoRb = stetho.GetComponent<Rigidbody>();
            if (stethoRb == null) stethoRb = stetho.AddComponent<Rigidbody>();
            stethoRb.mass = 0.35f;
            stethoRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            stethoRb.interpolation = RigidbodyInterpolation.Interpolate;

            var stethoGrab = stetho.GetComponent<XRGrabInteractable>();
            if (stethoGrab == null) stethoGrab = stetho.AddComponent<XRGrabInteractable>();
            stethoGrab.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.Instantaneous; // Pegado a la mano (ver PalmGripProfile)
            stethoGrab.throwOnDetach = true;
            stethoGrab.smoothPosition = false;
            stethoGrab.smoothRotation = false;

            Transform bellTrans = null;
            foreach (var t in stetho.GetComponentsInChildren<Transform>())
            {
                if (t.name == "Bell") { bellTrans = t; break; }
            }
            SphereCollider bellCol = null;
            if (bellTrans != null)
            {
                bellCol = bellTrans.GetComponent<SphereCollider>();
                if (bellCol == null) bellCol = bellTrans.gameObject.AddComponent<SphereCollider>();
                bellCol.radius = 0.045f;
                bellCol.isTrigger = true;
            }

            var stethoToolComp = stetho.GetComponent<StethoscopeTool>();
            if (stethoToolComp == null) stethoToolComp = stetho.AddComponent<StethoscopeTool>();
            if (bellCol != null)
            {
                typeof(StethoscopeTool).GetField("bellCollider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(stethoToolComp, bellCol);
            }

            // Sello Aprobado (Diegético: Base con placa entintada, mango ergonómico y perilla)
            GameObject stampApp = CreateDiegeticStamp(boothRoot, "Tool_Stamp_Approved",
                new Vector3(0.18f, deskSurfaceY + 0.01f, 0.62f), VerdictType.ApprovedSafeZone,
                greenMat, stampWoodMat, stampBrassMat);

            // Sello Cuarentena (Diegético: Base con placa entintada, mango ergonómico y perilla)
            GameObject stampQuar = CreateDiegeticStamp(boothRoot, "Tool_Stamp_Quarantine",
                new Vector3(0.32f, deskSurfaceY + 0.01f, 0.62f), VerdictType.SendToQuarantine,
                redMat, stampWoodMat, stampBrassMat);

            // --- 10. DOCUMENTO SANITARIO ---
            GameObject docObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            docObj.name = "Document_Passport";
            docObj.transform.SetParent(boothRoot.transform);
            docObj.transform.position = new Vector3(0f, deskSurfaceY + 0.01f, 0.62f);
            docObj.transform.localScale = new Vector3(0.28f, 0.008f, 0.2f);
            docObj.GetComponent<Renderer>().sharedMaterial = paperMat;
            var docRb = docObj.AddComponent<Rigidbody>();
            docRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            docObj.AddComponent<XRGrabInteractable>();
            var docInteractable = docObj.AddComponent<DocumentInteractable>();

            GameObject canvasObj = new GameObject("DocumentCanvas");
            canvasObj.transform.SetParent(docObj.transform);
            canvasObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            canvasObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            canvasObj.transform.localScale = new Vector3(0.0018f, 0.0018f, 0.0018f);

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<CanvasScaler>();

            var docView = docObj.AddComponent<DocumentView>();

            GameObject textObj = new GameObject("DocText");
            textObj.transform.SetParent(canvasObj.transform, false);
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "PASE SANITARIO [VIGENTE]\n------------------------\nNOMBRE: Sujeto-001\nEDAD: 28 años\nID: BIO-4092-Z\nVENCE: 20/12/2026\nGRUPO: O+";
            tmp.fontSize = 18;
            tmp.color = Color.black;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            var rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(230, 150);

            GameObject appStampVis = new GameObject("Stamp_Approved_Vis");
            appStampVis.transform.SetParent(canvasObj.transform, false);
            var appTmp = appStampVis.AddComponent<TextMeshProUGUI>();
            appTmp.text = "┌───────────────────┐\n│     APROBADO      │\n│   ZONA SEGURA A   │\n│   PASE SANITARIO  │\n└───────────────────┘";
            appTmp.fontSize = 19;
            appTmp.lineSpacing = -10;
            appTmp.color = new Color(0f, 0.7f, 0.15f, 0.95f);
            appTmp.fontStyle = FontStyles.Bold;
            appTmp.alignment = TextAlignmentOptions.Center;
            appStampVis.SetActive(false);

            GameObject quarStampVis = new GameObject("Stamp_Quarantine_Vis");
            quarStampVis.transform.SetParent(canvasObj.transform, false);
            var quarTmp = quarStampVis.AddComponent<TextMeshProUGUI>();
            quarTmp.text = "┌───────────────────┐\n│    CUARENTENA     │\n│  AISLAMIENTO BIO  │\n│ ORDEN DETENCIÓN   │\n└───────────────────┘";
            quarTmp.fontSize = 19;
            quarTmp.lineSpacing = -10;
            quarTmp.color = new Color(0.9f, 0.12f, 0.12f, 0.95f);
            quarTmp.fontStyle = FontStyles.Bold;
            quarTmp.alignment = TextAlignmentOptions.Center;
            quarStampVis.SetActive(false);

            GameObject uvWatermarkVis = new GameObject("Stamp_UV_Watermark");
            uvWatermarkVis.transform.SetParent(canvasObj.transform, false);
            var uvTmp = uvWatermarkVis.AddComponent<TextMeshProUGUI>();
            uvTmp.text = "✦ SELLO FORENSE OFICIAL ✦\nMINISTERIO DE SALUD\n[BIO-SEGURIDAD CERTIFICADA]";
            uvTmp.fontSize = 17;
            uvTmp.lineSpacing = -10;
            uvTmp.color = new Color(0.15f, 1.0f, 0.75f, 0.95f);
            uvTmp.fontStyle = FontStyles.Bold;
            uvTmp.alignment = TextAlignmentOptions.Center;
            var uvRect = uvWatermarkVis.GetComponent<RectTransform>();
            uvRect.localPosition = new Vector3(0f, -25f, 0f);
            uvWatermarkVis.SetActive(false);

            // --- 11. BOTONES FÍSICOS DE MESA ---
            GameObject btnGreen = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            btnGreen.name = "Button_Approved";
            btnGreen.transform.SetParent(boothRoot.transform);
            btnGreen.transform.position = new Vector3(0.55f, deskSurfaceY + 0.02f, 0.62f);
            btnGreen.transform.localScale = new Vector3(0.1f, 0.03f, 0.1f);
            btnGreen.GetComponent<Renderer>().sharedMaterial = greenMat;
            btnGreen.AddComponent<XRSimpleInteractable>();
            var decBtnGreen = btnGreen.AddComponent<DecisionButton>();
            typeof(DecisionButton).GetField("buttonVerdict", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(decBtnGreen, VerdictType.ApprovedSafeZone);

            GameObject btnRed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            btnRed.name = "Button_Quarantine";
            btnRed.transform.SetParent(boothRoot.transform);
            btnRed.transform.position = new Vector3(-0.55f, deskSurfaceY + 0.02f, 0.58f);
            btnRed.transform.localScale = new Vector3(0.1f, 0.03f, 0.1f);
            btnRed.GetComponent<Renderer>().sharedMaterial = redMat;
            btnRed.AddComponent<XRSimpleInteractable>();
            var decBtnRed = btnRed.AddComponent<DecisionButton>();
            typeof(DecisionButton).GetField("buttonVerdict", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(decBtnRed, VerdictType.SendToQuarantine);

            // Botones de comando de examen físico (Affordance y control ergonómico)
            GameObject btnArms = CreateInspectionCommandButton(boothRoot, "Button_RaiseArms",
                new Vector3(-0.38f, deskSurfaceY + 0.01f, 0.48f),
                InspectionCommandType.ToggleRaiseArms, "<color=#33ccff>LEVANTE BRAZOS</color>\n<size=75%>[ V ]</size>",
                cyanMat, deskMat);

            GameObject btnTorso = CreateInspectionCommandButton(boothRoot, "Button_InspectTorso",
                new Vector3(-0.24f, deskSurfaceY + 0.01f, 0.48f),
                InspectionCommandType.ToggleExposeTorso, "<color=#ffaa33>DESCUBRA TORSO</color>\n<size=75%>[ C ]</size>",
                amberMat, deskMat);

            // --- 12. MONITOR DE SIGNOS VITALES Y ECG DIEGÉTICO (EN ESCRITORIO) ---
            GameObject vitalMonitor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vitalMonitor.name = "Vital_Signs_Monitor";
            vitalMonitor.transform.SetParent(boothRoot.transform);
            vitalMonitor.transform.position = new Vector3(-0.50f, deskSurfaceY + 0.14f, 0.85f);
            vitalMonitor.transform.rotation = Quaternion.Euler(0f, 24f, 0f);
            vitalMonitor.transform.localScale = new Vector3(0.28f, 0.22f, 0.18f);
            vitalMonitor.GetComponent<Renderer>().sharedMaterial = deskMat;

            GameObject vBezel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vBezel.name = "Screen_Bezel";
            vBezel.transform.SetParent(vitalMonitor.transform, false);
            vBezel.transform.localPosition = new Vector3(0f, 0.01f, -0.51f);
            vBezel.transform.localScale = new Vector3(0.92f, 0.84f, 0.04f);
            vBezel.GetComponent<Renderer>().sharedMaterial = new Material(urpShader) { color = new Color(0.06f, 0.08f, 0.07f) };
            Object.DestroyImmediate(vBezel.GetComponent<Collider>());

            GameObject vCanvasObj = new GameObject("MonitorCanvas");
            vCanvasObj.transform.SetParent(vitalMonitor.transform, false);
            vCanvasObj.transform.localPosition = new Vector3(0f, 0.01f, -0.54f);
            vCanvasObj.transform.localScale = new Vector3(0.0016f, 0.0016f, 0.0016f);

            var vCanvas = vCanvasObj.AddComponent<Canvas>();
            vCanvas.renderMode = RenderMode.WorldSpace;
            vCanvasObj.AddComponent<CanvasScaler>();

            GameObject ecgObj = new GameObject("ECG_Oscilloscope");
            ecgObj.transform.SetParent(vCanvasObj.transform, false);
            ecgObj.AddComponent<RawImage>();
            var ecgRect = ecgObj.GetComponent<RectTransform>();
            ecgRect.sizeDelta = new Vector2(150f, 60f);
            ecgRect.anchoredPosition = new Vector2(0f, 6f);

            GameObject bpmObj = new GameObject("BpmText");
            bpmObj.transform.SetParent(vCanvasObj.transform, false);
            var bpmTmp = bpmObj.AddComponent<TextMeshProUGUI>();
            bpmTmp.text = "<b>--</b> <size=60%>BPM</size>";
            bpmTmp.fontSize = 20;
            bpmTmp.color = new Color(0.3f, 0.95f, 0.6f);
            bpmTmp.alignment = TextAlignmentOptions.Center;
            var bpmRect = bpmObj.GetComponent<RectTransform>();
            bpmRect.sizeDelta = new Vector2(150f, 26f);
            bpmRect.anchoredPosition = new Vector2(0f, 44f);

            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(vCanvasObj.transform, false);
            var statusTmp = statusObj.AddComponent<TextMeshProUGUI>();
            statusTmp.text = "<color=#559988>○ TELEMETRÍA EN ESPERA</color>";
            statusTmp.fontSize = 9;
            statusTmp.color = Color.white;
            statusTmp.alignment = TextAlignmentOptions.Center;
            var statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.sizeDelta = new Vector2(150f, 18f);
            statusRect.anchoredPosition = new Vector2(0f, -28f);

            vitalMonitor.AddComponent<VitalSignsMonitor>();

            // --- 12. MONITOR DIEGÉTICO SUPERIOR ---
            GameObject monitor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monitor.name = "Diagnostic_Monitor";
            monitor.transform.SetParent(boothRoot.transform);
            monitor.transform.position = new Vector3(0f, 1.95f, 1.95f);
            monitor.transform.localScale = new Vector3(1.3f, 0.65f, 0.05f);
            monitor.GetComponent<Renderer>().sharedMaterial = deskMat;

            GameObject monitorCanvas = new GameObject("MonitorCanvas");
            monitorCanvas.transform.SetParent(monitor.transform);
            monitorCanvas.transform.localPosition = new Vector3(0f, 0f, -0.55f);
            monitorCanvas.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
            var mCanvas = monitorCanvas.AddComponent<Canvas>();
            mCanvas.renderMode = RenderMode.WorldSpace;

            GameObject monText = new GameObject("MonitorText");
            monText.transform.SetParent(monitorCanvas.transform, false);
            var mTmp = monText.AddComponent<TextMeshProUGUI>();
            mTmp.text = "<color=#33ccff>PUESTO DE CONTROL Y BIOSEGURIDAD VR</color>\n------------------------------------\n1. Ausculta el tórax con el estetoscopio.\n2. Enciende la linterna y examina el antebrazo.\n3. Lee el documento de identidad.\n4. Estampa el veredicto o pulsa el botón.";
            mTmp.fontSize = 14;
            mTmp.color = Color.white;
            mTmp.alignment = TextAlignmentOptions.Center;
            var mRect = monText.GetComponent<RectTransform>();
            mRect.sizeDelta = new Vector2(250, 120);

            // --- 13. MANAGERS ---
            GameObject managersRoot = new GameObject("[CHECKPOINT_MANAGERS]");
            managersRoot.transform.SetParent(boothRoot.transform);
            var flowMgr = managersRoot.AddComponent<CheckpointFlowManager>();
            var hapticMgr = managersRoot.AddComponent<HapticManager>();
            var audioMgr = managersRoot.AddComponent<SpatialAudioManager>();
            var metrics = managersRoot.AddComponent<UsabilityMetricsTracker>();

            var flowType = typeof(CheckpointFlowManager);
            flowType.GetField("currentSurvivor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(flowMgr, survivorModel);
            flowType.GetField("currentDocument", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(flowMgr, docInteractable);
            flowType.GetField("metricsTracker", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(flowMgr, metrics);
            flowType.GetField("monitorStatusText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(flowMgr, mTmp);
            flowType.GetField("survivorStandPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(flowMgr, standPointAnchor.transform);
            flowType.GetField("documentSpawnPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(flowMgr, docSpawnAnchor.transform);
            flowType.GetField("quarantineAlarmLight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(flowMgr, aLight);

            // Poblar roster de prefabs civiles para alternar en cada ronda
            var pfabList = new System.Collections.Generic.List<GameObject>();
            string[] cPaths = new string[] {
                "Assets/npc_casual_set_00/Prefabs/npc_csl_00_character_01m_01.prefab",
                "Assets/npc_casual_set_00/Prefabs/npc_csl_00_character_01f_01.prefab",
                "Assets/npc_casual_set_00/Prefabs/npc_csl_00_character_01m_02.prefab",
                "Assets/npc_casual_set_00/Prefabs/npc_csl_00_character_02f_01.prefab",
                "Assets/npc_casual_set_00/Prefabs/npc_csl_00_character_02m_01.prefab",
                "Assets/npc_casual_set_00/Prefabs/npc_csl_00_character_01f_02.prefab"
            };
            foreach (var cp in cPaths)
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(cp);
                if (pf != null) pfabList.Add(pf);
            }
            flowType.GetField("civilianPrefabs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(flowMgr, pfabList);

            var woundM = AssetDatabase.LoadAssetAtPath<Material>("Assets/Abandoned_Asylum/Materials/M_InfectedWound.mat");
            if (woundM != null)
            {
                flowType.GetField("woundMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(flowMgr, woundM);
            }

            var mTorsoPf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/npc_casual_set_00/Mesh/Mesh_Parts/npc_hmn_01m_torso1.fbx");
            var fTorsoPf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/npc_casual_set_00/Mesh/Mesh_Parts/npc_hmn_01f_torso1.fbx");
            if (mTorsoPf != null)
            {
                flowType.GetField("maleTorsoPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(flowMgr, mTorsoPf);
            }
            if (fTorsoPf != null)
            {
                flowType.GetField("femaleTorsoPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(flowMgr, fTorsoPf);
            }

            // --- 13.B MÓDULO DE COMANDOS POR MANOS (INTERACCIÓN SIN MANDOS NI BOTONES) ---
            HandCommandModuleInstaller.Install(boothRoot);

            // --- 13.C AGARRE NATURAL CON LA PALMA (HERRAMIENTAS EMPUÑADAS, NO PELLIZCADAS) ---
            NaturalHandGripInstaller.Install(boothRoot);

            // --- 14. SIMULADOR XR PARA DESARROLLO EN ESCRITORIO (PC / TECLADO + RATÓN) ---
            var simPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.5.1/XR Device Simulator/XR Device Simulator.prefab");
            if (simPrefab != null)
            {
                var simObj = (GameObject)PrefabUtility.InstantiatePrefab(simPrefab, boothRoot.transform);
                PrefabUtility.UnpackPrefabInstance(simObj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                simObj.name = "XR Device Simulator";
                var enhancer = simObj.GetComponent<XRSimulatorDesktopEnhancer>();
                if (enhancer == null) enhancer = simObj.AddComponent<XRSimulatorDesktopEnhancer>();
            }

            // Guardar escena
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/CheckpointBoothScene.unity");
            Debug.Log("¡Escena hospitalaria de cuarentena construida con éxito con assets reales y nueva iluminación!");
        }

        private static GameObject CreateDiegeticStamp(GameObject parent, string name, Vector3 position, VerdictType verdict, Material themeMat, Material woodMat, Material metalMat)
        {
            GameObject stampRoot = new GameObject(name);
            stampRoot.tag = "Tool";
            stampRoot.transform.SetParent(parent.transform);
            stampRoot.transform.position = position;

            var rb = stampRoot.AddComponent<Rigidbody>();
            rb.mass = 0.45f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var grabCol = stampRoot.AddComponent<BoxCollider>();
            grabCol.center = new Vector3(0f, 0.055f, 0f);
            grabCol.size = new Vector3(0.065f, 0.11f, 0.045f);

            var grabInteractable = stampRoot.AddComponent<XRGrabInteractable>();
            grabInteractable.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.Instantaneous; // Pegado a la mano (ver PalmGripProfile)
            grabInteractable.throwOnDetach = true;
            grabInteractable.smoothPosition = false;
            grabInteractable.smoothRotation = false;

            // 1. Placa base
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObj.name = "Stamp_Base";
            baseObj.transform.SetParent(stampRoot.transform, false);
            baseObj.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            baseObj.transform.localScale = new Vector3(0.065f, 0.024f, 0.042f);
            baseObj.GetComponent<Renderer>().sharedMaterial = metalMat;
            Object.DestroyImmediate(baseObj.GetComponent<Collider>());

            // 2. Almohadilla inferior de tinta
            GameObject inkPad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            inkPad.name = "Stamp_InkPad";
            inkPad.transform.SetParent(stampRoot.transform, false);
            inkPad.transform.localPosition = new Vector3(0f, 0.002f, 0f);
            inkPad.transform.localScale = new Vector3(0.062f, 0.005f, 0.038f);
            inkPad.GetComponent<Renderer>().sharedMaterial = themeMat;
            var padCol = inkPad.GetComponent<BoxCollider>();
            padCol.isTrigger = true;

            // 3. Vástago cilíndrico de madera
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "Stamp_Stem";
            stem.transform.SetParent(stampRoot.transform, false);
            stem.transform.localPosition = new Vector3(0f, 0.055f, 0f);
            stem.transform.localScale = new Vector3(0.022f, 0.035f, 0.022f);
            stem.GetComponent<Renderer>().sharedMaterial = woodMat;
            Object.DestroyImmediate(stem.GetComponent<Collider>());

            // 4. Perilla ergonómica esférica
            GameObject knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            knob.name = "Stamp_Knob";
            knob.transform.SetParent(stampRoot.transform, false);
            knob.transform.localPosition = new Vector3(0f, 0.095f, 0f);
            knob.transform.localScale = new Vector3(0.038f, 0.038f, 0.038f);
            knob.GetComponent<Renderer>().sharedMaterial = woodMat;
            Object.DestroyImmediate(knob.GetComponent<Collider>());

            // 5. Anillo de color de veredicto
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Stamp_ColorRing";
            ring.transform.SetParent(stampRoot.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.032f, 0f);
            ring.transform.localScale = new Vector3(0.035f, 0.006f, 0.035f);
            ring.GetComponent<Renderer>().sharedMaterial = themeMat;
            Object.DestroyImmediate(ring.GetComponent<Collider>());

            // Lógica de StampTool
            var stampTool = stampRoot.AddComponent<StampTool>();
            typeof(StampTool).GetField("stampVerdict", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(stampTool, verdict);
            typeof(StampTool).GetField("baseTriggerCollider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(stampTool, padCol);

            return stampRoot;
        }

        private static GameObject CreateInspectionCommandButton(GameObject parent, string name, Vector3 position, 
            InspectionCommandType commandType, string labelText, Material capMat, Material baseMat)
        {
            GameObject btnRoot = new GameObject(name);
            btnRoot.transform.SetParent(parent.transform);
            btnRoot.transform.position = position;

            // Base fija del pulsador
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Button_Base";
            baseObj.transform.SetParent(btnRoot.transform, false);
            baseObj.transform.localPosition = Vector3.zero;
            baseObj.transform.localScale = new Vector3(0.09f, 0.012f, 0.09f);
            baseObj.GetComponent<Renderer>().sharedMaterial = baseMat;
            Object.DestroyImmediate(baseObj.GetComponent<Collider>());

            // Capucha móvil pulsable
            GameObject capObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            capObj.name = "Button_MovingCap";
            capObj.transform.SetParent(btnRoot.transform, false);
            capObj.transform.localPosition = new Vector3(0f, 0.016f, 0f);
            capObj.transform.localScale = new Vector3(0.075f, 0.016f, 0.075f);
            capObj.GetComponent<Renderer>().sharedMaterial = capMat;

            var capCol = capObj.GetComponent<Collider>();
            if (capCol != null) capCol.isTrigger = true;

            // Canvas de texto informativo WorldSpace
            GameObject canvasObj = new GameObject("ButtonCanvas");
            canvasObj.transform.SetParent(btnRoot.transform, false);
            canvasObj.transform.localPosition = new Vector3(0f, 0.032f, 0.052f);
            canvasObj.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
            canvasObj.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            GameObject textObj = new GameObject("LabelText");
            textObj.transform.SetParent(canvasObj.transform, false);
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            var rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 55);

            // Box collider en la raíz para permitir interacción física de mandos y raycast
            var rootCol = btnRoot.AddComponent<BoxCollider>();
            rootCol.center = new Vector3(0f, 0.016f, 0f);
            rootCol.size = new Vector3(0.09f, 0.035f, 0.09f);

            // Componentes de interacción
            var interactable = btnRoot.AddComponent<XRSimpleInteractable>();
            var cmdComp = btnRoot.AddComponent<InspectionCommandButton>();

            cmdComp.Configure(commandType, capObj.transform);

            return btnRoot;
        }

        private static void ConfigureHandsRig(GameObject rig)
        {
            if (rig == null) return;

            // 1. Desactivar el salto en la cabina (elimina saltos accidentales al presionar B)
            var jumpObj = rig.transform.Find("Locomotion/Jump");
            if (jumpObj != null)
            {
                var jumpProvider = jumpObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Jump.JumpProvider>();
                if (jumpProvider != null) jumpProvider.enabled = false;
                jumpObj.gameObject.SetActive(false);
            }

            // 2. Asignar material opaco PBR de piel realista a todas las mallas de manos
            Material skinMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Hand_OpaqueSkin.mat");
            if (skinMat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                skinMat = new Material(shader)
                {
                    name = "M_Hand_OpaqueSkin",
                    color = new Color(0.86f, 0.73f, 0.63f, 1f)
                };
                skinMat.SetFloat("_Smoothness", 0.30f);
                skinMat.SetFloat("_Surface", 0.0f);
                skinMat.renderQueue = 2000;
                AssetDatabase.CreateAsset(skinMat, "Assets/Materials/M_Hand_OpaqueSkin.mat");
            }

            foreach (var r in rig.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (r.gameObject.name == "LeftHand" || r.gameObject.name == "RightHand")
                {
                    r.sharedMaterials = new Material[] { skinMat };
                }
            }

            // 3. Desactivar post-procesador de filtrado de manos para evitar amortiguamiento/lag en desktop
            var filter = rig.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Samples.Hands.HandsOneEuroFilterPostProcessor>(true);
            if (filter != null) filter.enabled = false;

            // 4. Optimizar interactores Near-Far para respuesta instantánea 1:1
            var nearFars = rig.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor>(true);
            foreach (var nf in nearFars)
            {
                var attach = nf.GetComponent<UnityEngine.XR.Interaction.Toolkit.Attachment.InteractionAttachController>();
                var caster = nf.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.Casters.CurveInteractionCaster>();
                if (attach != null)
                {
                    attach.angleStabilization = 0f;
                    attach.positionStabilization = 0f;
                    attach.smoothOffset = false;
                }
                if (caster != null)
                {
                    caster.enableStabilization = false;
                }
            }

            // 5. Configurar origen de tracking en modo Floor con altura de ojos ergonómica (1.65m)
            var xrOrigin = rig.GetComponent<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                xrOrigin.RequestedTrackingOriginMode = Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Floor;
                xrOrigin.CameraYOffset = 1.65f;
            }
        }
    }
}
