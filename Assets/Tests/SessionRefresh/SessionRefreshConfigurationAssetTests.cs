using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ToastedVr.SessionRefresh.Tests
{
    // El asset vive en Assembly-CSharp, que los tests no pueden referenciar: se revisa por sus campos serializados.
    public sealed class SessionRefreshConfigurationAssetTests
    {
        private const string ResourcePath = "SessionRefresh/SessionRefreshConfiguration";

        private static SerializedObject LoadConfiguration()
        {
            var asset = Resources.Load<ScriptableObject>(ResourcePath);
            Assert.IsNotNull(asset, "No se encontró la configuración en Resources");
            Assert.AreEqual("SessionRefreshConfiguration", asset.GetType().Name);
            return new SerializedObject(asset);
        }

        [Test]
        public void ReusesLoginBackendAddress()
        {
            var authentication = LoadConfiguration().FindProperty("authenticationConfiguration").objectReferenceValue;

            Assert.IsNotNull(authentication);
            Assert.AreEqual("UnityAuthenticationConfiguration", authentication.GetType().Name);
        }

        [Test]
        public void LoginSceneIsEnabledInBuildSettings()
        {
            var loginScene = LoadConfiguration().FindProperty("loginSceneName").stringValue;

            var enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => Path.GetFileNameWithoutExtension(scene.path));
            CollectionAssert.Contains(enabledScenes, loginScene);
        }

        [Test]
        public void TimingsArePositive()
        {
            var configuration = LoadConfiguration();

            Assert.Greater(configuration.FindProperty("refreshMarginSeconds").floatValue, 0f);
            Assert.Greater(configuration.FindProperty("requestTimeoutSeconds").intValue, 0);
            Assert.Greater(configuration.FindProperty("unavailableRetrySeconds").floatValue, 0f);
            Assert.GreaterOrEqual(configuration.FindProperty("returnToLoginDelaySeconds").floatValue, 0f);
            StringAssert.StartsWith("/", configuration.FindProperty("refreshPath").stringValue);
        }
    }
}
