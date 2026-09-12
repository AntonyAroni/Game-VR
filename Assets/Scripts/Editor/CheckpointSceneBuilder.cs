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

            // Pared derecha
            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "Wall_Right";
            rightWall.transform.SetParent(roomRoot.transform);
            rightWall.transform.position = new Vector3(2.75f, 1.6f, 1.25f);
            rightWall.transform.localScale = new Vector3(0.1f, 3.2f, 6.5f);
            if (tiledWallZ != null) rightWall.GetComponent<Renderer>().sharedMaterial = tiledWallZ;

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
            // Estetoscopio
            GameObject stetho = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stetho.name = "Tool_Stethoscope";
            stetho.tag = "Tool";
            stetho.transform.SetParent(boothRoot.transform);
            stetho.transform.position = new Vector3(-0.18f, deskSurfaceY + 0.02f, 0.62f);
            stetho.transform.localScale = new Vector3(0.08f, 0.02f, 0.08f);
            stetho.GetComponent<Renderer>().sharedMaterial = toolMat;
            var stethoRb = stetho.AddComponent<Rigidbody>();
            stethoRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            stetho.AddComponent<XRGrabInteractable>();
            stetho.AddComponent<StethoscopeTool>();

            // Sello Aprobado
            GameObject stampApp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stampApp.name = "Tool_Stamp_Approved";
            stampApp.tag = "Tool";
            stampApp.transform.SetParent(boothRoot.transform);
            stampApp.transform.position = new Vector3(0.20f, deskSurfaceY + 0.03f, 0.62f);
            stampApp.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
            stampApp.GetComponent<Renderer>().sharedMaterial = greenMat;
            var appRb = stampApp.AddComponent<Rigidbody>();
            appRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            stampApp.AddComponent<XRGrabInteractable>();
            var stampAppTool = stampApp.AddComponent<StampTool>();
            typeof(StampTool).GetField("stampVerdict", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(stampAppTool, VerdictType.ApprovedSafeZone);

            // Sello Cuarentena
            GameObject stampQuar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stampQuar.name = "Tool_Stamp_Quarantine";
            stampQuar.tag = "Tool";
            stampQuar.transform.SetParent(boothRoot.transform);
            stampQuar.transform.position = new Vector3(0.35f, deskSurfaceY + 0.03f, 0.62f);
            stampQuar.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
            stampQuar.GetComponent<Renderer>().sharedMaterial = redMat;
            var quarRb = stampQuar.AddComponent<Rigidbody>();
            quarRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            stampQuar.AddComponent<XRGrabInteractable>();
            var stampQuarTool = stampQuar.AddComponent<StampTool>();
            typeof(StampTool).GetField("stampVerdict", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(stampQuarTool, VerdictType.SendToQuarantine);

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
            appTmp.text = "[ APROBADO ]";
            appTmp.fontSize = 32;
            appTmp.color = new Color(0f, 0.65f, 0.1f);
            appTmp.fontStyle = FontStyles.Bold;
            appTmp.alignment = TextAlignmentOptions.Center;
            appStampVis.SetActive(false);

            GameObject quarStampVis = new GameObject("Stamp_Quarantine_Vis");
            quarStampVis.transform.SetParent(canvasObj.transform, false);
            var quarTmp = quarStampVis.AddComponent<TextMeshProUGUI>();
            quarTmp.text = "[ CUARENTENA ]";
            quarTmp.fontSize = 32;
            quarTmp.color = new Color(0.85f, 0.1f, 0.1f);
            quarTmp.fontStyle = FontStyles.Bold;
            quarTmp.alignment = TextAlignmentOptions.Center;
            quarStampVis.SetActive(false);

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
            btnRed.transform.position = new Vector3(-0.55f, deskSurfaceY + 0.02f, 0.62f);
            btnRed.transform.localScale = new Vector3(0.1f, 0.03f, 0.1f);
            btnRed.GetComponent<Renderer>().sharedMaterial = redMat;
            btnRed.AddComponent<XRSimpleInteractable>();
            var decBtnRed = btnRed.AddComponent<DecisionButton>();
            typeof(DecisionButton).GetField("buttonVerdict", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(decBtnRed, VerdictType.SendToQuarantine);

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

            // Guardar escena
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/CheckpointBoothScene.unity");
            Debug.Log("¡Escena hospitalaria de cuarentena construida con éxito con assets reales y nueva iluminación!");
        }
    }
}
