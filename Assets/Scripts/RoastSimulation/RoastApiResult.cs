namespace ToastedVr.RoastSimulation
{
    public enum RoastApiFailure
    {
        None,
        BadRequest,
        Unauthorized,
        NotFound,
        InvalidPhase,
        Unavailable,
        Unexpected
    }

    public sealed class RoastApiResult<T> where T : class
    {
        public RoastApiResult(T value, RoastApiFailure failure, long statusCode, ApiError error)
        {
            Value = value;
            Failure = failure;
            StatusCode = statusCode;
            Error = error;
        }

        public T Value { get; }
        public RoastApiFailure Failure { get; }
        public long StatusCode { get; }

        // Mensaje del backend en español, listo para el jugador; puede faltar en 401 o sin conexión.
        public ApiError Error { get; }

        public bool IsSuccess => Failure == RoastApiFailure.None;
    }
}
