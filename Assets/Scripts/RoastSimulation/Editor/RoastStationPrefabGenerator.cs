using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ToastedVr.RoastVoice;

namespace ToastedVr.RoastSimulation.Editor
{
    // Arma el prefab RoastStation desde cero para no editar su YAML a mano. Desde el menú o en batchmode:
    // Unity.exe -batchmode -quit -projectPath <proyecto> -executeMethod ToastedVr.RoastSimulation.Editor.RoastStationPrefabGenerator.Generate
    public static class RoastStationPrefabGenerator
    {
        public const string PrefabPath = "Assets/Prefabs/RoastStation/RoastStation.prefab";
        private const string ConfigurationPath = "Assets/Settings/RoastSimulation/RoastSimulationConfiguration.asset";
        private const string MessagesPath = "Assets/Settings/RoastSimulation/RoastStationMessages.asset";
        private const string VoiceConfigurationPath = "Assets/Settings/RoastVoice/RoastVoiceConfiguration.asset";
        private const string VoiceMessagesPath = "Assets/Settings/RoastVoice/RoastVoiceMessages.asset";

        // El bootstrap vive en Assembly-CSharp porque usa el login; este asmdef de editor no puede referenciarlo.
        private const string BootstrapTypeName = "ToastedVr.RoastSimulationBootstrap.RoastStationBootstrap, Assembly-CSharp";
        private const string AuthenticationConfigurationType = "UnityAuthenticationConfiguration";

        // Medidas en metros. El jugador mira la estación hacia +Z.
        private static readonly Vector3 BoardCenter = new Vector3(0f, 1.15f, 0.03f);
        private static readonly Vector3 BoardSize = new Vector3(1.2f, 1.2f, 0.02f);
        private static readonly Vector3 StatusCenter = new Vector3(0f, 1.48f, 0f);
        private static readonly Vector2 StatusSize = new Vector2(1.1f, 0.5f);
        private static readonly Vector3 MessageCenter = new Vector3(0f, 1.16f, 0f);
        private static readonly Vector2 MessageSize = new Vector2(1.1f, 0.1f);
        private const float ControlsHeight = 0.97f;
        private const float LabelsHeight = 0.86f;
        private static readonly float[] ControlColumns = { -0.48f, -0.24f, 0f, 0.24f, 0.48f };
        private static readonly Vector3 ButtonSize = new Vector3(0.08f, 0.08f, 0.04f);
        private static readonly Vector2 LabelSize = new Vector2(0.22f, 0.06f);
        private static readonly Vector3 SubtitleCenter = new Vector3(0f, 0.68f, 0f);
        private static readonly Vector2 SubtitleSize = new Vector2(1.1f, 0.22f);
        private const float VoiceSpatialBlend = 1f;
        private const float StatusFontSize = 0.3f;
        private const float LabelFontSize = 0.25f;
        private static readonly Vector3 KnobBodyScale = new Vector3(0.07f, 0.015f, 0.07f);
        private static readonly Vector3 KnobPointerScale = new Vector3(0.01f, 0.01f, 0.03f);
        private static readonly Vector3 KnobPointerOffset = new Vector3(0f, 0.016f, 0.022f);

        [MenuItem("Tools/Toasted VR/Generar RoastStation")]
        public static void Generate()
        {
            var prefab = Build(PrefabPath);
            Selection.activeObject = prefab;
            Debug.Log($"[RoastSimulation] Prefab generated at {PrefabPath}");
        }

        public static GameObject Build(string prefabPath)
        {
            var configuration = LoadRequired<RoastSimulationConfiguration>(ConfigurationPath);
            var messages = LoadRequired<RoastStationMessages>(MessagesPath);
            var voiceConfiguration = LoadRequired<RoastVoiceConfiguration>(VoiceConfigurationPath);
            var voiceMessages = LoadRequired<RoastVoiceMessages>(VoiceMessagesPath);
            var authenticationConfiguration = FindSingleAsset(AuthenticationConfigurationType);
            var bootstrapType = Type.GetType(BootstrapTypeName)
                ?? throw new InvalidOperationException($"{BootstrapTypeName} was not found.");

            var root = new GameObject("RoastStation");
            try
            {
                var controller = root.AddComponent<RoastStationController>();
                SetReferences(controller, ("configuration", configuration), ("messages", messages));

                CreatePrimitive("Board", PrimitiveType.Cube, root.transform, BoardCenter, BoardSize);
                var statusText = CreateText("Status", root.transform, StatusCenter, StatusSize, StatusFontSize, TextAlignmentOptions.TopLeft);
                var messageText = CreateText("Message", root.transform, MessageCenter, MessageSize, StatusFontSize, TextAlignmentOptions.Center);

                var power = CreateButton("PowerButton", root.transform, ControlColumns[0]);
                var charge = CreateButton("ChargeButton", root.transform, ControlColumns[1]);
                var discharge = CreateButton("DischargeButton", root.transform, ControlColumns[2]);
                var knob = CreateKnob(root.transform, ControlColumns[3]);
                var talk = CreateButton("TalkButton", root.transform, ControlColumns[4]);
                var subtitleText = CreateText("Subtitle", root.transform, SubtitleCenter, SubtitleSize, StatusFontSize, TextAlignmentOptions.Top);

                var display = root.AddComponent<RoastStationDisplay>();
                SetReferences(display, ("controller", controller), ("statusText", statusText), ("messageText", messageText));

                var controls = root.AddComponent<RoastStationControls>();
                SetReferences(controls,
                    ("controller", controller),
                    ("powerButton", power.interactable), ("chargeButton", charge.interactable),
                    ("dischargeButton", discharge.interactable), ("gasKnob", knob.interactable),
                    ("powerLabel", power.label), ("chargeLabel", charge.label),
                    ("dischargeLabel", discharge.label), ("gasLabel", knob.label));

                var audioSource = root.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = VoiceSpatialBlend;
                var player = root.AddComponent<VoiceAnswerPlayer>();

                var voiceAssistant = root.AddComponent<VoiceAssistantController>();
                SetReferences(voiceAssistant,
                    ("configuration", voiceConfiguration), ("messages", voiceMessages),
                    ("station", controller), ("player", player));

                var voiceControls = root.AddComponent<VoiceAssistantControls>();
                SetReferences(voiceControls,
                    ("assistant", voiceAssistant), ("talkButton", talk.interactable),
                    ("talkLabel", talk.label), ("subtitleText", subtitleText));

                var bootstrap = root.AddComponent(bootstrapType);
                SetReferences(bootstrap,
                    ("authenticationConfiguration", authenticationConfiguration), ("controller", controller),
                    ("voiceAssistant", voiceAssistant));

                EnsureFolder(Path.GetDirectoryName(prefabPath)?.Replace('\\', '/'));
                return PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out var saved) && saved
                    ? AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)
                    : throw new InvalidOperationException($"Could not save {prefabPath}.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static (XRSimpleInteractable interactable, TMP_Text label) CreateButton(string name, Transform parent, float column)
        {
            var button = CreatePrimitive(name, PrimitiveType.Cube, parent, new Vector3(column, ControlsHeight, -ButtonSize.z / 2f), ButtonSize);
            var interactable = button.AddComponent<XRSimpleInteractable>();
            var label = CreateText(name + "Label", parent, new Vector3(column, LabelsHeight, 0f), LabelSize, LabelFontSize, TextAlignmentOptions.Center);
            return (interactable, label);
        }

        // La raíz no gira y su eje Y apunta al jugador; solo gira la pieza visible.
        private static (GasKnobInteractable interactable, TMP_Text label) CreateKnob(Transform parent, float column)
        {
            var knob = new GameObject("GasKnob");
            knob.transform.SetParent(parent, false);
            knob.transform.localPosition = new Vector3(column, ControlsHeight, -KnobBodyScale.y);
            knob.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            var visual = new GameObject("KnobVisual").transform;
            visual.SetParent(knob.transform, false);
            CreatePrimitive("Body", PrimitiveType.Cylinder, visual, Vector3.zero, KnobBodyScale);
            CreatePrimitive("Pointer", PrimitiveType.Cube, visual, KnobPointerOffset, KnobPointerScale);

            var interactable = knob.AddComponent<GasKnobInteractable>();
            SetReferences(interactable, ("knobVisual", visual));
            var label = CreateText("GasKnobLabel", parent, new Vector3(column, LabelsHeight, 0f), LabelSize, LabelFontSize, TextAlignmentOptions.Center);
            return (interactable, label);
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localScale = localScale;

            var pipelineMaterial = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.defaultMaterial : null;
            if (pipelineMaterial != null)
            {
                primitive.GetComponent<MeshRenderer>().sharedMaterial = pipelineMaterial;
            }

            return primitive;
        }

        private static TMP_Text CreateText(string name, Transform parent, Vector3 localPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<TextMeshPro>();
            text.rectTransform.localPosition = localPosition;
            text.rectTransform.sizeDelta = size;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.black;
            text.text = string.Empty;
            return text;
        }

        private static void SetReferences(UnityEngine.Object target, params (string field, UnityEngine.Object value)[] references)
        {
            var serialized = new SerializedObject(target);
            foreach (var (field, value) in references)
            {
                var property = serialized.FindProperty(field)
                    ?? throw new InvalidOperationException($"{target.GetType().Name} has no field {field}.");
                property.objectReferenceValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException($"{path} was not found.");
        }

        private static UnityEngine.Object FindSingleAsset(string typeName)
        {
            var guids = AssetDatabase.FindAssets("t:" + typeName);
            if (guids.Length != 1)
            {
                throw new InvalidOperationException($"Expected one {typeName} asset and found {guids.Length}.");
            }

            return AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
