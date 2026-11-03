using UnityEngine;
using UnityEngine.UI;

namespace ToastedVr.Authentication
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class LoginVirtualKeyboardKey : MonoBehaviour
    {
        public enum Action
        {
            Append,
            Backspace,
            Confirm
        }

        private LoginVirtualKeyboard keyboard;
        private Button button;
        private Action action;
        private string value;

        public void Initialize(LoginVirtualKeyboard owner, Action keyAction, string keyValue)
        {
            keyboard = owner;
            action = keyAction;
            value = keyValue;
            button = GetComponent<Button>();
            button.onClick.AddListener(Press);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(Press);
            }
        }

        private void Press()
        {
            if (keyboard == null)
            {
                return;
            }

            switch (action)
            {
                case Action.Append:
                    keyboard.AppendCharacter(value);
                    break;
                case Action.Backspace:
                    keyboard.DeleteLastCharacter();
                    break;
                case Action.Confirm:
                    keyboard.Hide();
                    break;
            }
        }
    }
}
