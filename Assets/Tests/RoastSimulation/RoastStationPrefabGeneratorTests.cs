using System.Linq;
using NUnit.Framework;
using ToastedVr.RoastSimulation.Editor;
using UnityEditor;
using UnityEngine;

namespace ToastedVr.RoastSimulation.Tests
{
    public sealed class RoastStationPrefabGeneratorTests
    {
        private const string TemporaryFolder = "Assets/Tests/RoastSimulation/GeneratedByTest";
        private const string TemporaryPrefab = TemporaryFolder + "/RoastStation.prefab";

        [TearDown]
        public void DeleteGeneratedPrefab()
        {
            AssetDatabase.DeleteAsset(TemporaryFolder);
        }

        [Test]
        public void BuildsAFullyWiredStation()
        {
            var prefab = RoastStationPrefabGenerator.Build(TemporaryPrefab);

            RoastStationPrefabAssertions.AssertFullyWired(prefab);
        }

        [Test]
        public void RebuildingReplacesThePrefab()
        {
            RoastStationPrefabGenerator.Build(TemporaryPrefab);
            var guid = AssetDatabase.AssetPathToGUID(TemporaryPrefab);

            var rebuilt = RoastStationPrefabGenerator.Build(TemporaryPrefab);

            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(TemporaryPrefab));
            RoastStationPrefabAssertions.AssertFullyWired(rebuilt);
        }

        [Test]
        public void PlacesTheTemperatureChartBesideTheBoard()
        {
            var prefab = RoastStationPrefabGenerator.Build(TemporaryPrefab);
            var configuration = AssetDatabase.LoadAssetAtPath<TemperatureChartConfiguration>(
                "Assets/Settings/RoastSimulation/TemperatureChartConfiguration.asset");

            var chart = prefab.transform.Find("TemperatureChart");
            var board = prefab.transform.Find("Board");

            Assert.IsNotNull(chart);
            Assert.AreEqual(1, prefab.GetComponentsInChildren<TemperatureChart>(true).Length);
            Assert.IsNotNull(chart.GetComponent<TemperatureChart>());
            Assert.Greater(chart.localPosition.x - configuration.PanelWidth / 2f, board.localPosition.x + board.localScale.x / 2f);
            Assert.AreEqual(board.localPosition.y, chart.localPosition.y, 1e-6);
            Assert.IsNull(chart.GetComponentInChildren<Collider>(true), "la gráfica no debe tapar los rayos de los botones");
            Assert.IsFalse(chart.Find("ChartContent/ChartTickLabel").gameObject.activeSelf);
        }

        [Test]
        public void PlacesTheResultPanelLeftOfTheBoard()
        {
            var prefab = RoastStationPrefabGenerator.Build(TemporaryPrefab);
            var configuration = AssetDatabase.LoadAssetAtPath<RoastResultPanelConfiguration>(
                "Assets/Settings/RoastSimulation/RoastResultPanelConfiguration.asset");

            var panel = prefab.transform.Find("RoastResultPanel");
            var board = prefab.transform.Find("Board");

            Assert.IsNotNull(panel);
            Assert.AreEqual(1, prefab.GetComponentsInChildren<RoastResultPanel>(true).Length);
            Assert.IsNotNull(panel.GetComponent<RoastResultPanel>());
            Assert.IsTrue(panel.GetComponents<MonoBehaviour>().Any(b => b.GetType().Name == "FeedbackSpeechPlayer"));
            Assert.Less(panel.localPosition.x + configuration.PanelWidth / 2f, board.localPosition.x - board.localScale.x / 2f);
            Assert.AreEqual(board.localPosition.y, panel.localPosition.y, 1e-6);
            Assert.IsNull(panel.GetComponentInChildren<Collider>(true), "el panel no debe tapar los rayos de los botones");
            Assert.AreEqual(prefab.transform.childCount - 1, panel.GetSiblingIndex(), "pieza nueva al final, sin mover las demás");
        }
    }
}
