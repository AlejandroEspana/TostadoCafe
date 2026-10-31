namespace ToastedVr.RoastSimulation
{
    public static class RoastApiFailureClassifier
    {
        // reachedServer es false cuando no hubo respuesta HTTP (sin red, timeout o servidor apagado).
        public static RoastApiFailure Classify(bool reachedServer, long statusCode)
        {
            if (!reachedServer)
            {
                return RoastApiFailure.Unavailable;
            }

            if (statusCode >= 200 && statusCode < 300)
            {
                return RoastApiFailure.None;
            }

            switch (statusCode)
            {
                case 400: return RoastApiFailure.BadRequest;
                case 401: return RoastApiFailure.Unauthorized;
                case 404: return RoastApiFailure.NotFound;
                case 409: return RoastApiFailure.InvalidPhase;
            }

            return statusCode >= 500 ? RoastApiFailure.Unavailable : RoastApiFailure.Unexpected;
        }
    }
}
