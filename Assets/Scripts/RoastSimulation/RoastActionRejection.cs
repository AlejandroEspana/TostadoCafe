namespace ToastedVr.RoastSimulation
{
    public enum RoastAction
    {
        Charge,
        Discharge
    }

    // Por qué no se puede cargar o descargar en este momento, con el texto que ve el jugador.
    // Usa las mismas reglas de fase de la web (RoastPhaseRules): si la acción vale, no hay rechazo.
    public static class RoastActionRejection
    {
        public static string MessageFor(RoastAction action, bool isPowered, string phase, RoastStationMessages texts)
        {
            if (!isPowered)
            {
                return texts.PowerOnFirstMessage;
            }

            if (IsAllowed(action, phase))
            {
                return null;
            }

            if (phase == RoastPhaseRules.Finished)
            {
                return texts.RoastFinishedMessage;
            }

            if (action == RoastAction.Discharge)
            {
                if (phase == RoastPhaseRules.Preheat || phase == RoastPhaseRules.Ready)
                {
                    return texts.NotChargedYetMessage;
                }

                if (phase == RoastPhaseRules.ChargeDip)
                {
                    return texts.WaitForChargeDipMessage;
                }
            }
            else if (phase == RoastPhaseRules.ChargeDip || phase == RoastPhaseRules.Roasting)
            {
                return texts.AlreadyChargedMessage;
            }

            // Sin estado todavía o una fase que el backend agregue después.
            return texts.ActionNotAvailableMessage;
        }

        private static bool IsAllowed(RoastAction action, string phase)
        {
            return action == RoastAction.Charge ? RoastPhaseRules.CanCharge(phase) : RoastPhaseRules.CanDischarge(phase);
        }
    }
}
