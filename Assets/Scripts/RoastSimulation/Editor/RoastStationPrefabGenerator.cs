using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ToastedVr.RoastAlerts;
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
        private const string AlertsConfigurationPath = "Assets/Settings/RoastAlerts/RoastAlertsConfiguration.asset";
        private const string ChartConfigurationPath = "Assets/Settings/RoastSimulation/TemperatureChartConfiguration.asset";
        private const string ChartMaterialPath = "Assets/Materials/RoastStation/TemperatureChartLine.mat";
        private const string ResultPanelConfigurationPath = "Assets/Settings/RoastSimulation/RoastResultPanelConfiguration.asset";

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
        // La gráfica va a la derecha del tablero y a su altura, para no mirar hacia arriba en VR.
        private const float ChartGap = 0.05f;

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
            var alertsConfiguration = LoadRequired<RoastAlertsConfiguration>(AlertsConfigurationPath);
            var chartConfiguration = LoadRequired<TemperatureChartConfiguration>(ChartConfigurationPath);
            var chartMaterial = LoadRequired<Material>(ChartMaterialPath);
            var resultPanelConfiguration = LoadRequired<RoastResultPanelConfiguration>(ResultPanelConfigurationPath);
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

                var visualEvents = root.AddComponent<RoastStationVisualEvents>();
                SetReferences(visualEvents, ("controller", controller));

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

                var alerts = root.AddComponent<RoastAlertsController>();
                SetReferences(alerts,
                    ("configuration", alertsConfiguration), ("station", controller),
                    ("voiceAssistant", voiceAssistant), ("player", player));

                var bootstrap = root.AddComponent(bootstrapType);
                SetReferences(bootstrap,
                    ("authenticationConfiguration", authenticationConfiguration), ("controller", controller),
                    ("voiceAssistant", voiceAssistant), ("alerts", alerts));

                CreateTemperatureChart(root.transform, controller, chartConfiguration, chartMaterial);
                CreateResultPanel(root.transform, controller, resultPanelConfiguration, chartConfiguration, chartMaterial,
                    voiceConfiguration, voiceAssistant, player);

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

        // Pieza nueva aparte: no cambia nada de las piezas que ya existían en el prefab.
        private static void CreateTemperatureChart(Transform parent, RoastStationController controller,
            TemperatureChartConfiguration configuration, Material material)
        {
            var chartObject = new GameObject("TemperatureChart");
            chartObject.transform.SetParent(parent, false);
            chartObject.transform.localPosition = new Vector3(
                BoardCenter.x + BoardSize.x / 2f + ChartGap + configuration.PanelWidth / 2f, BoardCenter.y, 0f);

            var content = new GameObject("ChartContent").transform;
            content.SetParent(chartObject.transform, false);

            var background = CreatePrimitive("ChartBackground", PrimitiveType.Quad, content, Vector3.zero,
                new Vector3(configuration.PanelWidth, configuration.PanelHeight, 1f));
            UnityEngine.Object.DestroyImmediate(background.GetComponent<Collider>());
            var backgroundRenderer = background.GetComponent<MeshRenderer>();
            PrepareChartRenderer(backgroundRenderer, material);

            var spacing = configuration.LayerSpacing;
            var grid = CreateChartLayer("ChartGrid", content, -spacing, material);
            var baseline = CreateChartLayer("ChartBaseline", content, -spacing, material);
            var floorReference = CreateChartLayer("ChartFloorReference", content, -2f * spacing, material);
            var targetReference = CreateChartLayer("ChartTargetReference", content, -3f * spacing, material);
            var curve = CreateChartLayer("ChartCurve", content, -4f * spacing, material);

            var textDepth = -5f * spacing;
            var title = CreateText("ChartTitle", content, new Vector3(0f, configuration.PlotHeight / 2f, textDepth),
                new Vector2(configuration.PanelWidth, configuration.TitleHeight), configuration.TitleFontSize, TextAlignmentOptions.Center);
            var axisTitle = CreateText("ChartAxisTitle", content, new Vector3(0f, 0f, textDepth),
                new Vector2(configuration.PlotHeight, configuration.TickLabelSize.y), configuration.LabelFontSize, TextAlignmentOptions.Center);
            axisTitle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var tickLabel = CreateText("ChartTickLabel", content, new Vector3(0f, 0f, textDepth),
                configuration.TickLabelSize, configuration.LabelFontSize, TextAlignmentOptions.Right);
            tickLabel.gameObject.SetActive(false);

            var chart = chartObject.AddComponent<TemperatureChart>();
            SetReferences(chart,
                ("controller", controller), ("configuration", configuration), ("content", content.gameObject),
                ("background", backgroundRenderer), ("grid", grid), ("floorReference", floorReference),
                ("targetReference", targetReference), ("curve", curve), ("baseline", baseline),
                ("title", title), ("axisTitle", axisTitle), ("tickLabelTemplate", tickLabel));
        }

        // Pieza nueva aparte, a la izquierda del tablero como la gráfica a la derecha; lleva también la voz del resultado.
        private static void CreateResultPanel(Transform parent, RoastStationController controller, RoastResultPanelConfiguration configuration,
            TemperatureChartConfiguration style, Material material, RoastVoiceConfiguration voiceConfiguration,
            VoiceAssistantController voiceAssistant, VoiceAnswerPlayer player)
        {
            var panelObject = new GameObject("RoastResultPanel");
            panelObject.transform.SetParent(parent, false);
            panelObject.transform.localPosition = new Vector3(
                BoardCenter.x - BoardSize.x / 2f - configuration.Gap - configuration.PanelWidth / 2f, BoardCenter.y, 0f);

            var content = new GameObject("ResultContent").transform;
            content.SetParent(panelObject.transform, false);

            var background = CreatePrimitive("ResultBackground", PrimitiveType.Quad, content, Vector3.zero,
                new Vector3(configuration.PanelWidth, configuration.PanelHeight, 1f));
            UnityEngine.Object.DestroyImmediate(background.GetComponent<Collider>());
            var backgroundRenderer = background.GetComponent<MeshRenderer>();
            PrepareChartRenderer(backgroundRenderer, material);

            var textDepth = -configuration.LayerSpacing;
            var innerWidth = configuration.PanelWidth - 2f * configuration.Padding;
            var top = configuration.PanelHeight / 2f - configuration.Padding;
            var bodyTop = top - configuration.TitleHeight;
            var bodyBottom = -configuration.PanelHeight / 2f + configuration.Padding;
            var title = CreateText("ResultTitle", content, new Vector3(0f, top - configuration.TitleHeight / 2f, textDepth),
                new Vector2(innerWidth, configuration.TitleHeight), style.TitleFontSize, TextAlignmentOptions.Center);
            var body = CreateText("ResultBody", content, new Vector3(0f, (bodyTop + bodyBottom) / 2f, textDepth),
                new Vector2(innerWidth, bodyTop - bodyBottom), configuration.BodyFontSizeMax, TextAlignmentOptions.TopLeft);

            var panel = panelObject.AddComponent<RoastResultPanel>();
            SetReferences(panel,
                ("controller", controller), ("style", style), ("configuration", configuration), ("content", content.gameObject),
                ("background", backgroundRenderer), ("title", title), ("body", body));

            var speech = panelObject.AddComponent<FeedbackSpeechPlayer>();
            SetReferences(speech,
                ("configuration", voiceConfiguration), ("station", controller),
                ("voiceAssistant", voiceAssistant), ("player", player));
        }

        private static MeshFilter CreateChartLayer(string name, Transform parent, float depth, Material material)
        {
            var layer = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            layer.transform.SetParent(parent, false);
            layer.transform.localPosition = new Vector3(0f, 0f, depth);
            PrepareChartRenderer(layer.GetComponent<MeshRenderer>(), material);
            return layer.GetComponent<MeshFilter>();
        }

        private static void PrepareChartRenderer(MeshRenderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
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
