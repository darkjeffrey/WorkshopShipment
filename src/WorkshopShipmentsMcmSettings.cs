using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Localization;

namespace WorkshopShipments
{
    /// <summary>The "Workshop Shipments" page in Mod Options (MCM). Saved to Documents\Mount and Blade II Bannerlord\Configs\ModSettings.</summary>
    public sealed class WorkshopShipmentsMcmSettings : AttributeGlobalSettings<WorkshopShipmentsMcmSettings>
    {
        public override string Id => "WorkshopShipments";
        // MCM shows DisplayName as-is (it doesn't translate it like setting names and hints) and stores it the first
        // time it asks, so resolve it here. If the game's text system isn't ready yet, TextObject hands the raw
        // "{=id}text" back; drop the id then so the English name still shows.
        public override string DisplayName
        {
            get
            {
                string name = new TextObject("{=wsh_mcm_title}Workshop Shipments").ToString();
                if (name.StartsWith("{=", System.StringComparison.Ordinal))
                {
                    int close = name.IndexOf('}');
                    if (close >= 0) name = name.Substring(close + 1);
                }
                return name;
            }
        }
        public override string FolderName => "WorkshopShipments";
        public override string FormatType => "json2";

        // ---------------- General ----------------
        private const string General = "{=wsh_mcm_group_general}General";

        [SettingPropertyBool("{=wsh_mcm_Enabled}Enable", Order = 0, RequireRestart = false,
            HintText = "{=wsh_mcm_Enabled_hint}Turn the mod on or off.")]
        [SettingPropertyGroup(General, GroupOrder = 0)]
        public bool Enabled { get; set; } = true;

        // ---------------- Shipments ----------------
        private const string Shipments = "{=wsh_mcm_group_shipments}Shipments";

        [SettingPropertyInteger("{=wsh_mcm_BaseFee}Base fee (denars)", 0, 2000, "0", Order = 0, RequireRestart = false,
            HintText = "{=wsh_mcm_BaseFee_hint}What every shipment costs before weight and distance are added.")]
        [SettingPropertyGroup(Shipments, GroupOrder = 1)]
        public int BaseFee { get; set; } = 50;

        [SettingPropertyFloatingInteger("{=wsh_mcm_FeePerWeight}Fee per weight unit per 100 distance", 0f, 5f, "0.00", Order = 1, RequireRestart = false,
            HintText = "{=wsh_mcm_FeePerWeight_hint}Added to the cost for each unit of cargo weight, for every 100 units of map distance to the workshop.")]
        [SettingPropertyGroup(Shipments, GroupOrder = 1)]
        public float FeePerWeight { get; set; } = 0.3f;

        [SettingPropertyInteger("{=wsh_mcm_MaxActive}Shipments on the road at once", 1, 20, "0", Order = 2, RequireRestart = false,
            HintText = "{=wsh_mcm_MaxActive_hint}How many of your shipments can be travelling at the same time.")]
        [SettingPropertyGroup(Shipments, GroupOrder = 1)]
        public int MaxActive { get; set; } = 5;

        [SettingPropertyBool("{=wsh_mcm_ShowPosition}Show where shipments are", Order = 3, RequireRestart = false,
            HintText = "{=wsh_mcm_ShowPosition_hint}In \"Check shipments on the road\", also show which settlement each shipment is near.")]
        [SettingPropertyGroup(Shipments, GroupOrder = 1)]
        public bool ShowPosition { get; set; } = false;

        // ---------------- Escorts ----------------
        private const string Escorts = "{=wsh_mcm_group_escorts}Escorts";

        [SettingPropertyInteger("{=wsh_mcm_BasicSize}Basic guard: men", 1, 100, "0", Order = 0, RequireRestart = false,
            HintText = "{=wsh_mcm_BasicSize_hint}Caravan guards in the cheapest escort, plus one caravan master. It costs nothing on top of the fee.")]
        [SettingPropertyGroup(Escorts, GroupOrder = 2)]
        public int BasicSize { get; set; } = 12;

        [SettingPropertyInteger("{=wsh_mcm_ArmedSize}Armed escort: men", 1, 100, "0", Order = 1, RequireRestart = false,
            HintText = "{=wsh_mcm_ArmedSize_hint}Men in the middle escort, plus one caravan master. Half are caravan guards, half are trained soldiers of the town's culture.")]
        [SettingPropertyGroup(Escorts, GroupOrder = 2)]
        public int ArmedSize { get; set; } = 25;

        [SettingPropertyInteger("{=wsh_mcm_ArmedFee}Armed escort: extra cost (denars)", 0, 10000, "0", Order = 2, RequireRestart = false,
            HintText = "{=wsh_mcm_ArmedFee_hint}Added to the shipment cost when you hire the armed escort.")]
        [SettingPropertyGroup(Escorts, GroupOrder = 2)]
        public int ArmedFee { get; set; } = 300;

        [SettingPropertyInteger("{=wsh_mcm_VeteranSize}Veteran escort: men", 1, 100, "0", Order = 3, RequireRestart = false,
            HintText = "{=wsh_mcm_VeteranSize_hint}Men in the strongest escort, plus one caravan master. A third are caravan guards, the rest are veteran elite troops of the town's culture.")]
        [SettingPropertyGroup(Escorts, GroupOrder = 2)]
        public int VeteranSize { get; set; } = 40;

        [SettingPropertyInteger("{=wsh_mcm_VeteranFee}Veteran escort: extra cost (denars)", 0, 20000, "0", Order = 4, RequireRestart = false,
            HintText = "{=wsh_mcm_VeteranFee_hint}Added to the shipment cost when you hire the veteran escort.")]
        [SettingPropertyGroup(Escorts, GroupOrder = 2)]
        public int VeteranFee { get; set; } = 900;

        // ---------------- Testing ----------------
        private const string Testing = "{=wsh_mcm_group_testing}Testing";

        [SettingPropertyBool("{=wsh_mcm_DebugMessages}Debug messages", Order = 0, RequireRestart = false,
            HintText = "{=wsh_mcm_DebugMessages_hint}Show chat messages about what the mod is doing. Useful for checking the mod works; turn it off for normal play.")]
        [SettingPropertyGroup(Testing, GroupOrder = 3)]
        public bool DebugMessages { get; set; } = false;
    }
}
