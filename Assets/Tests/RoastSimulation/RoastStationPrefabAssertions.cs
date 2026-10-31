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
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.name != "m_Script")
                    {
                        Assert.IsNotNull(property.objectReferenceValue, $"{behaviour.GetType().Name}.{property.name} está vacío");
                    }
                }
            }

            AssertCount(behaviours, "RoastStationController", 1);
            AssertCount(behaviours, "RoastStationDisplay", 1);
            AssertCount(behaviours, "RoastStationControls", 1);
            AssertCount(behaviours, "RoastStationBootstrap", 1);
            AssertCount(behaviours, "XRSimpleInteractable", 3);
            AssertCount(behaviours, "GasKnobInteractable", 1);
        }

        private static void AssertCount(MonoBehaviour[] behaviours, string typeName, int expected)
        {
            Assert.AreEqual(expected, behaviours.Count(b => b.GetType().Name == typeName), typeName);
        }
    }
}
