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

        private static WorkshopShipmentsMcmSettings S
        {
            get
            {
                try { return WorkshopShipmentsMcmSettings.Instance ?? Defaults; }
                catch (Exception) { return Defaults; }
            }
        }

        public static bool Enabled => S.Enabled;
        public static bool DebugMessages => S.DebugMessages;
        public static int BaseFee => S.BaseFee;
        public static float FeePerWeight => S.FeePerWeight;
        public static int BasicSize => S.BasicSize;
        public static int ArmedSize => S.ArmedSize;
        public static int ArmedFee => S.ArmedFee;
        public static int VeteranSize => S.VeteranSize;
        public static int VeteranFee => S.VeteranFee;
        public static int MaxActive => S.MaxActive;
        public static bool ShowPosition => S.ShowPosition;
    }
}
