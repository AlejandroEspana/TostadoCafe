using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToastedVr.Authentication
{
    [DisallowMultipleComponent]
    public sealed class LoginVirtualKeyboard : MonoBehaviour
    {
        private const float KeyboardWidth = 820f;
        private const float KeyboardHeight = 280f;
        private const float RowHeight = 44f;
        private const float Spacing = 6f;

        private static readonly string[][] CharacterRows =
        {
            new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I" },
            new[] { "J", "K", "L", "M", "N", "O", "P", "Q", "R" },
            new[] { "S", "T", "U", "V", "W", "X", "Y", "Z", "-" },
            new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }
        };

        private TMP_InputField targetInput;
        private Coroutine hideRoutine;
        private bool isBuilt;

        public void ShowFor(TMP_InputField inputField)
        {
            if (inputField == null || !inputField.interactable)
            {
                return;
            }

            targetInput = inputField;
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (!isBuilt)
            {
                BuildKeyboard();
            }
        }

        public void RequestHideWhenFocusLeavesInput()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }

            hideRoutine = StartCoroutine(HideWhenFocusLeavesKeyboard());
        }

        public void AppendCharacter(string value)
        {
            if (targetInput == null || !targetInput.interactable || string.IsNullOrEmpty(value))
            {
                return;
            }

            targetInput.text += value;
            targetInput.ForceLabelUpdate();
        }

        public void DeleteLastCharacter()
        {
            if (targetInput == null || !targetInput.interactable || string.IsNullOrEmpty(targetInput.text))
            {
                return;
            }

            targetInput.text = targetInput.text.Substring(0, targetInput.text.Length - 1);
            targetInput.ForceLabelUpdate();
        }

        public void Hide()
        {
            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
                hideRoutine = null;
            }

            gameObject.SetActive(false);
        }

        private void Awake()
        {
            ConfigureRoot();
        }

        private void OnDisable()
        {
            hideRoutine = null;
        }

        private IEnumerator HideWhenFocusLeavesKeyboard()
        {
            yield return null;
            hideRoutine = null;

            var selectedObject = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selectedObject == null || !selectedObject.transform.IsChildOf(transform))
            {
                Hide();
            }
        }

        private void ConfigureRoot()
        {
            var rectTransform = GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, -390f);
            rectTransform.sizeDelta = new Vector2(KeyboardWidth, KeyboardHeight);

            var background = GetComponent<Image>();
            if (background == null)
            {
                background = gameObject.AddComponent<Image>();
            }

            background.color = new Color(0.13f, 0.09f, 0.07f, 0.97f);
            background.raycastTarget = true;

            var layout = GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.spacing = Spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private void BuildKeyboard()
        {
            foreach (var characterRow in CharacterRows)
            {
                var row = CreateRow("CharacterRow");
                foreach (var character in characterRow)
                {
                    CreateKey(row.transform, character, LoginVirtualKeyboardKey.Action.Append, character, 1f, KeyColor());
                }
            }

            var actionRow = CreateRow("ActionRow");
            CreateKey(actionRow.transform, "Espacio", LoginVirtualKeyboardKey.Action.Append, " ", 1.5f, KeyColor());
            CreateKey(actionRow.transform, "Borrar", LoginVirtualKeyboardKey.Action.Backspace, string.Empty, 1f, DeleteColor());
            CreateKey(actionRow.transform, "Listo", LoginVirtualKeyboardKey.Action.Confirm, string.Empty, 1f, ConfirmColor());
            isBuilt = true;
        }

        private GameObject CreateRow(string rowName)
        {
            var row = new GameObject(rowName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(transform, false);

            var layoutElement = row.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = RowHeight;

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = Spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return row;
        }

        private void CreateKey(Transform parent, string label, LoginVirtualKeyboardKey.Action action, string value, float width, Color color)
        {
            var key = new GameObject($"Key_{label}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(LoginVirtualKeyboardKey));
            key.transform.SetParent(parent, false);

            var layoutElement = key.GetComponent<LayoutElement>();
            layoutElement.flexibleWidth = width;

            var image = key.GetComponent<Image>();
            image.color = color;

            var button = key.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(key.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = label;
            text.fontSize = 25f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.96f, 0.88f, 1f);
            text.raycastTarget = false;

            key.GetComponent<LoginVirtualKeyboardKey>().Initialize(this, action, value);
        }

        private static Color KeyColor()
        {
            return new Color(0.34f, 0.22f, 0.14f, 1f);
        }

        private static Color DeleteColor()
        {
            return new Color(0.45f, 0.20f, 0.14f, 1f);
        }

        private static Color ConfirmColor()
        {
            return new Color(0.18f, 0.42f, 0.25f, 1f);
        }
    }
}
