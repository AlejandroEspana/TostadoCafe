using System;
using System.IO;
using ToastedVr.RoastSimulation;

namespace ToastedVr.RoastAlerts
{
    public sealed class StoredAlertPackage
    {
        public StoredAlertPackage(AlertPackage package, string eTag)
        {
            Package = package;
            ETag = eTag;
        }

        public AlertPackage Package { get; }
        public string ETag { get; }
    }

    // Guarda el último paquete descargado y su ETag, para no bajar varios MB en cada tueste.
    public sealed class AlertPackageStore
    {
        private const string PackageFile = "package.json";
        private const string ETagFile = "package.etag";

        private readonly string directory;

        public AlertPackageStore(string directory)
        {
            this.directory = directory;
        }

        // null si no hay paquete, está dañado o tiene un schemaVersion que este build no entiende.
        public StoredAlertPackage Load()
        {
            try
            {
                var packagePath = Path.Combine(directory, PackageFile);
                if (!File.Exists(packagePath))
                {
                    return null;
                }

                var package = RoastJson.TryDeserialize<AlertPackage>(File.ReadAllText(packagePath));
                if (!AlertPackage.IsUsable(package))
                {
                    return null;
                }

                var eTagPath = Path.Combine(directory, ETagFile);
                return new StoredAlertPackage(package, File.Exists(eTagPath) ? File.ReadAllText(eTagPath) : null);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return null;
            }
        }

        public bool Save(string json, string eTag)
        {
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, PackageFile), json);
                var eTagPath = Path.Combine(directory, ETagFile);
                if (string.IsNullOrEmpty(eTag))
                {
                    File.Delete(eTagPath);
                }
                else
                {
                    File.WriteAllText(eTagPath, eTag);
                }

                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
