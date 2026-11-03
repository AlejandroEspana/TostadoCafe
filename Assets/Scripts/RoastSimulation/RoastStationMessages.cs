using System;
using UnityEngine;

namespace ToastedVr.RoastSimulation
{
    // Textos que ve el jugador y que no vienen del backend; los errores del backend ya traen su mensaje en español.
    [CreateAssetMenu(fileName = "RoastStationMessages", menuName = "Toasted VR/Roast Station Messages")]
    public sealed class RoastStationMessages : ScriptableObject
    {
        [Serializable]
        public struct KeyedText
        {
            public string key;
            public string text;
        }

        [Header("Controls")]
        [SerializeField] private string powerOnLabel;
        [SerializeField] private string powerOffLabel;
        [SerializeField] private string chargeLabel;
        [SerializeField] private string dischargeLabel;
        [Tooltip("{0} = gas percent.")]
        [SerializeField] private string gasKnobFormat;

        [Header("Status lines")]
        [Tooltip("{0} = phase name.")]
        [SerializeField] private string phaseFormat;
        [Tooltip("{0} = temperature in °C.")]
        [SerializeField] private string temperatureFormat;
        [Tooltip("{0} = roast time as mm:ss.")]
        [SerializeField] private string roastTimeFormat;
        [Tooltip("{0} = gas percent.")]
        [SerializeField] private string gasFormat;
        [Tooltip("{0} = rise per minute in °C.")]
        [SerializeField] private string riseFormat;
        [Tooltip("{0} = grain state name.")]
        [SerializeField] private string grainFormat;
        [Tooltip("{0} = roast time as mm:ss, or the pending text.")]
        [SerializeField] private string firstCrackFormat;
        [Tooltip("{0} = roast time as mm:ss, or the pending text.")]
        [SerializeField] private string secondCrackFormat;
        [SerializeField] private string crackPendingText;
        [Tooltip("{0} = final score, {1} = grade name.")]
        [SerializeField] private string scoreFormat;

        [Header("Names")]
        [SerializeField] private string phaseOffName;
        [SerializeField] private KeyedText[] phaseNames;
        [SerializeField] private KeyedText[] grainStateNames;
        [SerializeField] private KeyedText[] gradeNames;

        [Header("Alarms")]
        [SerializeField] private string burnedAlarm;
        [SerializeField] private string bakedAlarm;
        [SerializeField] private string burnWarningAlarm;
        [SerializeField] private string alarmLimitExceededAlarm;
        [SerializeField] private string chargeOvershootAlarm;

        [Header("Station messages")]
        [SerializeField] private string connectingMessage;
        [SerializeField] private string offlineMessage;
        [SerializeField] private string sessionExpiredMessage;
        [SerializeField] private string simulationLostMessage;
        [SerializeField] private string dischargingMessage;
        [SerializeField] private string evaluationPendingMessage;
        [SerializeField] private string unexpectedErrorMessage;

        [Header("Temperature chart")]
        [SerializeField] private string chartTitle;
        [SerializeField] private string chartTitleFinal;
        [SerializeField] private string chartAxisTitle;
        [Tooltip("{0} = temperature in °C of an axis mark.")]
        [SerializeField] private string chartTickFormat;

        public string PowerOnLabel => powerOnLabel;
        public string PowerOffLabel => powerOffLabel;
        public string ChargeLabel => chargeLabel;
        public string DischargeLabel => dischargeLabel;
        public string GasKnobFormat => gasKnobFormat;
        public string PhaseFormat => phaseFormat;
        public string TemperatureFormat => temperatureFormat;
        public string RoastTimeFormat => roastTimeFormat;
        public string GasFormat => gasFormat;
        public string RiseFormat => riseFormat;
        public string GrainFormat => grainFormat;
        public string FirstCrackFormat => firstCrackFormat;
        public string SecondCrackFormat => secondCrackFormat;
        public string CrackPendingText => crackPendingText;
        public string ScoreFormat => scoreFormat;
        public string PhaseOffName => phaseOffName;
        public string BurnedAlarm => burnedAlarm;
        public string BakedAlarm => bakedAlarm;
        public string BurnWarningAlarm => burnWarningAlarm;
        public string AlarmLimitExceededAlarm => alarmLimitExceededAlarm;
        public string ChargeOvershootAlarm => chargeOvershootAlarm;
        public string ConnectingMessage => connectingMessage;
        public string OfflineMessage => offlineMessage;
        public string SessionExpiredMessage => sessionExpiredMessage;
        public string SimulationLostMessage => simulationLostMessage;
        public string DischargingMessage => dischargingMessage;
        public string EvaluationPendingMessage => evaluationPendingMessage;
        public string UnexpectedErrorMessage => unexpectedErrorMessage;
        public string ChartTitle => chartTitle;
        public string ChartTitleFinal => chartTitleFinal;
        public string ChartAxisTitle => chartAxisTitle;
        public string ChartTickFormat => chartTickFormat;

        public string PhaseName(string phase) => Lookup(phaseNames, phase);
        public string GrainStateName(string grainState) => Lookup(grainStateNames, grainState);
        public string GradeName(string grade) => Lookup(gradeNames, grade);

        // Si el backend agrega un valor nuevo, se muestra tal cual en vez de dejar el texto vacío.
        private static string Lookup(KeyedText[] texts, string key)
        {
            if (texts != null)
            {
                foreach (var entry in texts)
                {
                    if (entry.key == key)
                    {
                        return entry.text;
                    }
                }
            }

            return key ?? string.Empty;
        }
    }
}
