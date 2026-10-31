using ToastedVr.RoastSimulation;

namespace ToastedVr.RoastAlerts
{
    public enum AlertPackageSource
    {
        None,
        Downloaded,
        Stored
    }

    // El build no trae paquete de respaldo: sin red tampoco hay simulador, así que basta con el último guardado.
    public static class AlertPackageChooser
    {
        public static (AlertPackage package, AlertPackageSource source) Choose(StoredAlertPackage stored,
            RoastApiResult<AlertPackageDownload> result)
        {
            if (result != null && result.IsSuccess && !result.Value.IsNotModified)
            {
                return (result.Value.Package, AlertPackageSource.Downloaded);
            }

            return stored != null ? (stored.Package, AlertPackageSource.Stored) : (null, AlertPackageSource.None);
        }
    }
}
