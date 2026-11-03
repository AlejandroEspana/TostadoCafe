using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ToastedVr.Authentication
{
    public sealed class UnityLoginMouseGraphicRaycaster : GraphicRaycaster
    {
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (eventData is TrackedDeviceEventData)
            {
                return;
            }

            base.Raycast(eventData, resultAppendList);
        }
    }
}
