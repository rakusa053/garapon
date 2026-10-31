using System.Collections.Generic;
using Garapon.Core;
using Garapon.Game;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Garapon.Editor
{
    public static class GaraponWorldBuilder
    {
        const string DataPath = "Assets/Data/GameData.asset";
        const string FontSourcePath = "Assets/Fonts/NotoSansJP-Regular.otf";
        const string FontAssetPath = "Assets/Fonts/NotoSansJP-Regular SDF.asset";
        const string InputPath = "Assets/InputSystem_Actions.inputactions";
        const string MaterialFolder = "Assets/Materials";
        const string TownPath = "Assets/Scenes/Town.unity";
        const string EndingPath = "Assets/Scenes/Ending.unity";

        const float DrumHeight = 2.3f;
        const float DrumApothem = 1f;
        const float DrumDepth = 1.2f;
        const float PanelThickness = 0.08f;
        const float HoleSize = 0.42f;

        sealed class Palette
        {
            public Material Ground, Wall, Wood, Glass, Ball, Trash, Player, Shelf, Station, Water;
        }

        [MenuItem("Garapon/Build World")]
        public static void BuildWorld()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            CreateGameData();
            var font = CreateFontAsset();
            var palette = CreatePalette();

            BuildTown(font, palette);
            BuildEnding(font, palette);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(TownPath, true),
                new EditorBuildSettingsScene(EndingPath, true)
            };
            AssetDatabase.SaveAssets();
        }

        static void CreateGameData()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }

            var asset = AssetDatabase.LoadAssetAtPath<GameDataAsset>(DataPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<GameDataAsset>();
                asset.Data = GameData.CreateDefault();
                AssetDatabase.CreateAsset(asset, DataPath);
                AssetDatabase.SaveAssets();
            }
        }

        static TMP_FontAsset CreateFontAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null)
            {
                return existing;
            }

            var source = AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
            var fontAsset = TMP_FontAsset.CreateFontAsset(source, 64, 6, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048);
            fontAsset.name = "NotoSansJP-Regular SDF";
            fontAsset.isMultiAtlasTexturesEnabled = true;
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

            fontAsset.material.name = fontAsset.name + " Material";
            fontAsset.atlasTexture.name = fontAsset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        static Palette CreatePalette()
        {
            return new Palette
            {
                Ground = CreateMaterial("Ground", new Color(0.45f, 0.55f, 0.42f)),
                Wall = CreateMaterial("Wall", new Color(0.82f, 0.82f, 0.78f)),
                Wood = CreateMaterial("Wood", new Color(0.55f, 0.33f, 0.16f)),
                Glass = CreateTransparentMaterial("SmokedGlass", new Color(0.08f, 0.08f, 0.1f, 0.6f)),
                Ball = CreateMaterial("Ball", Color.white),
                Trash = CreateMaterial("Trash", Color.white),
                Player = CreateMaterial("Player", new Color(0.2f, 0.45f, 0.85f)),
                Shelf = CreateMaterial("Shelf", new Color(0.92f, 0.72f, 0.3f)),
                Station = CreateMaterial("Station", new Color(0.3f, 0.65f, 0.6f)),
                Water = CreateMaterial("Water", new Color(0.35f, 0.75f, 0.85f))
            };
        }

        static Material CreateMaterial(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, $"{MaterialFolder}/{name}.mat");
            return material;
        }

        static Material CreateTransparentMaterial(string name, Color color)
        {
            var material = CreateMaterial(name, color);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
            EditorUtility.SetDirty(material);
            return material;
        }

        static void BuildTown(TMP_FontAsset font, Palette palette)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateLight();

            // NewScene unloads assets that nothing references yet, so load this one afterwards.
            var data = AssetDatabase.LoadAssetAtPath<GameDataAsset>(DataPath);

            var session = new GameObject("GameSession").AddComponent<GameSession>();
            Wire(session, ("dataAsset", data));

            var ground = Box("Ground", null, new Vector3(0f, -0.5f, 0f), new Vector3(90f, 1f, 50f), palette.Ground);
            ground.isStatic = true;

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, 11f, -9f);
            var followCamera = cameraObject.AddComponent<FollowCamera>();

            var playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObject.name = "Player";
            playerObject.transform.position = new Vector3(0f, 1.1f, 0f);
            playerObject.GetComponent<Renderer>().sharedMaterial = palette.Player;
            Object.DestroyImmediate(playerObject.GetComponent<CapsuleCollider>());
            playerObject.AddComponent<CharacterController>();
            var player = playerObject.AddComponent<PlayerController>();
            Wire(player,
                ("actions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath)),
                ("cameraTransform", cameraObject.transform));
            Wire(followCamera, ("target", playerObject.transform));

            var trashField = new GameObject("TrashField").AddComponent<TrashField>();
            trashField.transform.position = new Vector3(0f, 0f, 0f);
            Wire(trashField, ("player", playerObject.transform), ("material", palette.Trash));

            var recycle = Box("RecycleStation", null, new Vector3(-6f, 0.75f, 7f), new Vector3(3f, 1.5f, 2f), palette.Station);
            recycle.AddComponent<RecycleStation>();
            WorldLabel("換金所", recycle.transform.position + Vector3.up * 1.4f, font);

            BuildSupermarket(data.Data, font, palette);
            BuildGaraponBooth(font, palette);
            BuildHud(player, font);

            EditorSceneManager.SaveScene(scene, TownPath);
        }

        static void BuildSupermarket(GameData data, TMP_FontAsset font, Palette palette)
        {
            var root = new GameObject("Supermarket");
            root.transform.position = new Vector3(30f, 0f, 0f);

            Box("BackWall", root.transform, new Vector3(0f, 0.75f, 12f), new Vector3(24f, 1.5f, 0.4f), palette.Wall);
            Box("RightWall", root.transform, new Vector3(12f, 0.75f, 0f), new Vector3(0.4f, 1.5f, 24f), palette.Wall);
            Box("FrontWallLeft", root.transform, new Vector3(-7f, 0.75f, -12f), new Vector3(10f, 1.5f, 0.4f), palette.Wall);
            Box("FrontWallRight", root.transform, new Vector3(7f, 0.75f, -12f), new Vector3(10f, 1.5f, 0.4f), palette.Wall);
            Box("LeftWallNorth", root.transform, new Vector3(-12f, 0.75f, 7f), new Vector3(0.4f, 1.5f, 10f), palette.Wall);
            Box("LeftWallSouth", root.transform, new Vector3(-12f, 0.75f, -7f), new Vector3(0.4f, 1.5f, 10f), palette.Wall);

            const int perRow = 5;
            for (var i = 0; i < data.Products.Count; i++)
            {
                var row = i / perRow;
                var column = i % perRow;
                var local = new Vector3(-8f + column * 4f, 0.6f, 8f - row * 5f);
                var shelf = Box($"Shelf {data.Products[i].Id}", root.transform, local, new Vector3(2.2f, 1.2f, 1f), palette.Shelf);
                var label = WorldLabel(data.Products[i].DisplayName, shelf.transform.position + Vector3.up * 1.2f, font);
                var product = shelf.AddComponent<ProductShelf>();
                Wire(product, ("productId", data.Products[i].Id), ("label", label));
            }

            var register = Box("CashRegister", root.transform, new Vector3(6f, 0.6f, -9f), new Vector3(3f, 1.2f, 1.2f), palette.Station);
            register.AddComponent<CashRegister>();
            WorldLabel("レジ", register.transform.position + Vector3.up * 1.2f, font);
        }

        static void BuildGaraponBooth(TMP_FontAsset font, Palette palette)
        {
            var root = new GameObject("Garapon");
            root.transform.position = new Vector3(-28f, 0f, 2f);

            var center = new Vector3(0f, DrumHeight, 0f);
            Box("Table", root.transform, new Vector3(0f, 0.2f, -0.8f), new Vector3(4f, 0.4f, 2.4f), palette.Wall);
            Box("LegLeft", root.transform, new Vector3(-1.5f, DrumHeight / 2f, 0f), new Vector3(0.2f, DrumHeight, 0.2f), palette.Wood);
            Box("LegRight", root.transform, new Vector3(1.5f, DrumHeight / 2f, 0f), new Vector3(0.2f, DrumHeight, 0.2f), palette.Wood);

            var drumObject = new GameObject("Drum");
            drumObject.transform.SetParent(root.transform, false);
            drumObject.transform.localPosition = center;
            var drumBody = drumObject.AddComponent<Rigidbody>();
            drumBody.isKinematic = true;
            drumBody.interpolation = RigidbodyInterpolation.Interpolate;

            var panelWidth = 2f * DrumApothem * Mathf.Tan(22.5f * Mathf.Deg2Rad) + 0.02f;
            for (var i = 0; i < 8; i++)
            {
                var degrees = i * 45f;
                var radians = degrees * Mathf.Deg2Rad;
                var position = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * (DrumApothem + PanelThickness / 2f);
                var rotation = Quaternion.Euler(0f, 0f, degrees + 90f);
                var panel = new GameObject($"Panel {i}").transform;
                panel.SetParent(drumObject.transform, false);
                panel.localPosition = position;
                panel.localRotation = rotation;

                if (i == 6)
                {
                    BuildExitPanel(panel, panelWidth, palette);
                }
                else
                {
                    Box("Plank", panel, Vector3.zero, new Vector3(panelWidth, PanelThickness, DrumDepth), palette.Wood);
                }
            }

            var capOffset = DrumDepth / 2f + 0.03f;
            ColliderOnly("BackCap", drumObject.transform, new Vector3(0f, 0f, capOffset), new Vector3(2.4f, 2.4f, 0.06f));
            ColliderOnly("FrontCap", drumObject.transform, new Vector3(0f, 0f, -capOffset), new Vector3(2.4f, 2.4f, 0.06f));
            Disc("BackGlass", drumObject.transform, new Vector3(0f, 0f, capOffset), palette.Wood);
            Disc("FrontGlass", drumObject.transform, new Vector3(0f, 0f, -capOffset), palette.Glass);

            var shutter = drumObject.transform.Find("Panel 6/Shutter").GetComponent<BoxCollider>();

            var sensorObject = new GameObject("ExitSensor");
            sensorObject.transform.SetParent(root.transform, false);
            sensorObject.transform.localPosition = new Vector3(0f, DrumHeight - 1.25f, 0f);
            var sensorCollider = sensorObject.AddComponent<BoxCollider>();
            sensorCollider.isTrigger = true;
            sensorCollider.size = new Vector3(1.1f, 0.1f, 1.2f);
            var sensor = sensorObject.AddComponent<ExitSensor>();

            BuildChute(root.transform, palette);

            var machine = root.AddComponent<GaraponMachine>();
            Wire(machine,
                ("drum", drumBody),
                ("shutter", shutter),
                ("exitSensor", sensorObject.transform),
                ("ballMaterial", palette.Ball));
            Wire(sensor, ("machine", machine));
            WorldLabel("ガラポン抽選会", root.transform.position + new Vector3(0f, 4.2f, 0f), font);
        }

        static void BuildExitPanel(Transform panel, float panelWidth, Palette palette)
        {
            var sideWidth = (panelWidth - HoleSize) / 2f;
            var sideOffset = (HoleSize + sideWidth) / 2f;
            var endDepth = (DrumDepth - HoleSize) / 2f;
            var endOffset = (HoleSize + endDepth) / 2f;

            Box("SideA", panel, new Vector3(-sideOffset, 0f, 0f), new Vector3(sideWidth, PanelThickness, DrumDepth), palette.Wood);
            Box("SideB", panel, new Vector3(sideOffset, 0f, 0f), new Vector3(sideWidth, PanelThickness, DrumDepth), palette.Wood);
            Box("EndA", panel, new Vector3(0f, 0f, -endOffset), new Vector3(HoleSize, PanelThickness, endDepth), palette.Wood);
            Box("EndB", panel, new Vector3(0f, 0f, endOffset), new Vector3(HoleSize, PanelThickness, endDepth), palette.Wood);
            Box("Shutter", panel, Vector3.zero, new Vector3(HoleSize, PanelThickness, HoleSize), palette.Wood);
        }

        static void BuildChute(Transform root, Palette palette)
        {
            var ramp = new GameObject("Chute").transform;
            ramp.SetParent(root, false);
            ramp.localPosition = new Vector3(0f, 0.72f, -0.75f);
            ramp.localRotation = Quaternion.Euler(-12f, 0f, 0f);

            Box("Slide", ramp, Vector3.zero, new Vector3(1.2f, 0.05f, 2.2f), palette.Wall);
            Box("SideLeft", ramp, new Vector3(-0.62f, 0.1f, 0f), new Vector3(0.05f, 0.2f, 2.2f), palette.Wall);
            Box("SideRight", ramp, new Vector3(0.62f, 0.1f, 0f), new Vector3(0.05f, 0.2f, 2.2f), palette.Wall);
            Box("Stopper", ramp, new Vector3(0f, 0.1f, -1.1f), new Vector3(1.2f, 0.2f, 0.05f), palette.Wall);
            Box("BackStop", ramp, new Vector3(0f, 0.1f, 1.1f), new Vector3(1.2f, 0.2f, 0.05f), palette.Wall);
        }

        static void BuildHud(PlayerController player, TMP_FontAsset font)
        {
            var canvas = CreateCanvas();
            var status = ScreenText("Status", canvas, font, 46, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(800f, 400f));
            var inventory = ScreenText("Inventory", canvas, font, 42, TextAlignmentOptions.TopRight,
                new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(600f, 500f));
            var prompt = ScreenText("Prompt", canvas, font, 52, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(1500f, 90f));
            var message = ScreenText("Message", canvas, font, 60, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1500f, 110f));

            var hud = canvas.gameObject.AddComponent<GameHud>();
            Wire(hud,
                ("player", player),
                ("statusText", status),
                ("inventoryText", inventory),
                ("promptText", prompt),
                ("messageText", message));
        }

        static void BuildEnding(TMP_FontAsset font, Palette palette)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateLight();

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 6f, -9f), Quaternion.Euler(30f, 0f, 0f));

            Box("Ground", null, new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 40f), palette.Wood);
            var bath = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bath.name = "HotSpring";
            bath.transform.position = new Vector3(0f, 0.05f, 2f);
            bath.transform.localScale = new Vector3(10f, 0.1f, 7f);
            bath.GetComponent<Renderer>().sharedMaterial = palette.Water;

            var canvas = CreateCanvas();
            var result = ScreenText("Result", canvas, font, 44, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 800f));
            var ending = canvas.gameObject.AddComponent<EndingController>();
            Wire(ending, ("resultText", result));

            EditorSceneManager.SaveScene(scene, EndingPath);
        }

        static void CreateLight()
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);
        }

        static RectTransform CreateCanvas()
        {
            var canvasObject = new GameObject("Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            return canvas.GetComponent<RectTransform>();
        }

        static TMP_Text ScreenText(string name, RectTransform parent, TMP_FontAsset font, float size,
            TextAlignmentOptions alignment, Vector2 anchor, Vector2 position, Vector2 dimensions)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.outlineWidth = 0.15f;
            text.outlineColor = Color.black;
            var rect = text.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            return text;
        }

        static TMP_Text WorldLabel(string content, Vector3 position, TMP_FontAsset font)
        {
            var labelObject = new GameObject($"Label {content}");
            labelObject.transform.SetPositionAndRotation(position, Quaternion.LookRotation(new Vector3(0f, -11f, 9f)));
            var text = labelObject.AddComponent<TextMeshPro>();
            text.font = font;
            text.text = content;
            text.fontSize = 6f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.black;
            text.rectTransform.sizeDelta = new Vector2(5f, 2f);
            return text;
        }

        static GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        static void ColliderOnly(string name, Transform parent, Vector3 localPosition, Vector3 size)
        {
            var cap = new GameObject(name);
            cap.transform.SetParent(parent, false);
            cap.transform.localPosition = localPosition;
            cap.AddComponent<BoxCollider>().size = size;
        }

        static void Disc(string name, Transform parent, Vector3 localPosition, Material material)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = name;
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.transform.SetParent(parent, false);
            disc.transform.localPosition = localPosition;
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            disc.transform.localScale = new Vector3(2.16f, 0.03f, 2.16f);
            disc.GetComponent<Renderer>().sharedMaterial = material;
        }

        static void Wire(Object target, params (string Field, object Value)[] values)
        {
            var serialized = new SerializedObject(target);
            foreach (var (field, value) in values)
            {
                var property = serialized.FindProperty(field);
                if (value is string text)
                {
                    property.stringValue = text;
                }
                else
                {
                    property.objectReferenceValue = (Object)value;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
