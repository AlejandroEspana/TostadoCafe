using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ToastedVr.RoastSimulation.Tests
{
    public static class RoastStationPrefabAssertions
    {
        public static void AssertFullyWired(GameObject prefab)
        {
            Assert.IsNotNull(prefab);
            var behaviours = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            Assert.IsFalse(behaviours.Any(b => b == null), "Hay un script faltante en el prefab");

            foreach (var behaviour in behaviours.Where(b => b.GetType().Namespace?.StartsWith("ToastedVr") == true))
            {
                var property = new SerializedObject(behaviour).GetIterator();
                while (property.NextVisible(true))
                {
                    // Los campos heredados de Unity y de XRI empiezan con m_ y son opcionales; solo se revisan los propios.
                    if (property.propertyType == SerializedPropertyType.ObjectReference && !property.name.StartsWith("m_"))
                    {
                        Assert.IsNotNull(property.objectReferenceValue, $"{behaviour.GetType().Name}.{property.name} está vacío");
                    }
                }
            }

            AssertCount(behaviours, "RoastStationController", 1);
            AssertCount(behaviours, "RoastStationDisplay", 1);
            AssertCount(behaviours, "RoastStationControls", 1);
            AssertCount(behaviours, "RoastStationBootstrap", 1);
            AssertCount(behaviours, "XRSimpleInteractable", 4);
            AssertCount(behaviours, "GasKnobInteractable", 1);
            AssertCount(behaviours, "VoiceAssistantController", 1);
            AssertCount(behaviours, "VoiceAssistantControls", 1);
            AssertCount(behaviours, "VoiceAnswerPlayer", 1);
            AssertCount(behaviours, "RoastAlertsController", 1);
            Assert.IsNotNull(prefab.GetComponent<AudioSource>(), "falta el AudioSource de la voz");
        }

        private static void AssertCount(MonoBehaviour[] behaviours, string typeName, int expected)
        {
            Assert.AreEqual(expected, behaviours.Count(b => b.GetType().Name == typeName), typeName);
        }
    }
}
