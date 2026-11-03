using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ToastedVr.RoastSimulation
{
    public static class RoastJson
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        public static string Serialize(object value) => JsonConvert.SerializeObject(value, Settings);

        // Devuelve null si el cuerpo no es JSON válido, para tratarlo como respuesta inesperada y no romper la corrutina.
        public static T TryDeserialize<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(json, Settings);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
