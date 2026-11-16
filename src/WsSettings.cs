using System;

namespace WorkshopShipments
{
    /// <summary>
    /// Read-only view of the current settings. Values come from the MCM page (WorkshopShipmentsMcmSettings);
    /// the defaults there are used if MCM isn't loaded yet.
    /// </summary>
    public static class WsSettings
    {
        private static readonly WorkshopShipmentsMcmSettings Defaults = new WorkshopShipmentsMcmSettings();

        private static WorkshopShipmentsMcmSettings Current
        {
            get
            {
                try { return WorkshopShipmentsMcmSettings.Instance ?? Defaults; }
                catch (Exception) { return Defaults; }
            }
        }

        public static bool Enabled => Current.Enabled;
        public static bool DebugMessages => Current.DebugMessages;
        public static int BaseFee => Current.BaseFee;
        public static float FeePerWeight => Current.FeePerWeight;
        public static int BasicSize => Current.BasicSize;
        public static int ArmedSize => Current.ArmedSize;
        public static int ArmedFee => Current.ArmedFee;
        public static int VeteranSize => Current.VeteranSize;
        public static int VeteranFee => Current.VeteranFee;
        public static int MaxActive => Current.MaxActive;
        public static bool ShowPosition => Current.ShowPosition;
    }
}
