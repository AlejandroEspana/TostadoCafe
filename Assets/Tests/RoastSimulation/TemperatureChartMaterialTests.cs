using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class TemperatureChartMaterialTests
    {
        private const string MaterialPath = "Assets/Materials/RoastStation/TemperatureChartLine.mat";

        // Sin luces para que el color sea el mismo de la web, y de dos caras para que la malla se vea desde cualquier lado.
        [Test]
        public void IsAnUnlitDoubleSidedMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            Assert.IsNotNull(material, MaterialPath);
            Assert.AreEqual("Universal Render Pipeline/Unlit", material.shader.name);
            Assert.AreEqual(0f, material.GetFloat("_Cull"));
            Assert.IsTrue(material.HasProperty("_BaseColor"));
        }
    }
}
