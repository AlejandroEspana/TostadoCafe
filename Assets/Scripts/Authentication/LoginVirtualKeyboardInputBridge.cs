using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ToastedVr.Authentication
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_InputField))]
    public sealed class LoginVirtualKeyboardInputBridge : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerClickHandler
    {
        [SerializeField] private LoginVirtualKeyboard virtualKeyboard;

        private TMP_InputField inputField;

        private void Awake()
        {
            inputField = GetComponent<TMP_InputField>();
            inputField.shouldHideSoftKeyboard = true;
        }

        public void OnSelect(BaseEventData eventData)
        {
            virtualKeyboard?.ShowFor(inputField);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            virtualKeyboard?.ShowFor(inputField);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            virtualKeyboard?.RequestHideWhenFocusLeavesInput();
        }
    }
}
