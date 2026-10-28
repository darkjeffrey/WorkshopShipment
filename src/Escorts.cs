using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Localization;

namespace WorkshopShipments
{
    /// <summary>The three escorts a shipment can hire: size, extra cost and who is in them.</summary>
    internal static class Escorts
    {
        public const int Basic = 0, Armed = 1, Veteran = 2, Count = 3;

        public static TextObject Name(int tier)
        {
            switch (tier)
            {
                case Armed: return new TextObject("{=wsh_tier_armed}Armed escort");
                case Veteran: return new TextObject("{=wsh_tier_veteran}Veteran escort");
                default: return new TextObject("{=wsh_tier_basic}Basic guard");
            }
        }

        public static TextObject Description(int tier)
        {
            switch (tier)
            {
                case Armed: return new TextObject("{=wsh_tier_armed_desc}Half caravan guards, half trained soldiers. Puts off most bandits.");
                case Veteran: return new TextObject("{=wsh_tier_veteran_desc}Caravan guards backed by veteran elite troops. Only a real army will take it on.");
                default: return new TextObject("{=wsh_tier_basic_desc}Local caravan guards. Enough to scare off looters, but not much more.");
            }
        }

        /// <summary>Men in the escort, not counting the caravan master.</summary>
        public static int Size(int tier)
        {
            switch (tier)
            {
                case Armed: return Math.Max(1, WsSettings.ArmedSize);
                case Veteran: return Math.Max(1, WsSettings.VeteranSize);
                default: return Math.Max(1, WsSettings.BasicSize);
            }
        }

        public static int ExtraCost(int tier)
        {
            switch (tier)
            {
                case Armed: return Math.Max(0, WsSettings.ArmedFee);
                case Veteran: return Math.Max(0, WsSettings.VeteranFee);
                default: return 0;
            }
        }

        public static TroopRoster Build(int tier, CultureObject culture)
        {
            CharacterObject guard = culture?.CaravanGuard ?? culture?.BasicTroop;
            CharacterObject master = culture?.CaravanMaster ?? guard;
            int size = Size(tier);

            int soldiers = 0;
            CharacterObject soldier = null;
            if (tier == Armed)
            {
                soldier = Upgrade(culture?.BasicTroop, 2);
                soldiers = size / 2;
            }
            else if (tier == Veteran)
            {
                soldier = Upgrade(culture?.EliteBasicTroop ?? culture?.BasicTroop, 2);
                soldiers = size - size / 3;
            }
            if (soldier == null) soldiers = 0;

            TroopRoster troops = TroopRoster.CreateDummyTroopRoster();
            if (master != null) troops.AddToCounts(master, 1);
            if (guard != null && size - soldiers > 0) troops.AddToCounts(guard, size - soldiers);
            if (soldiers > 0) troops.AddToCounts(soldier, soldiers);
            return troops;
        }

        /// <summary>Follows the first upgrade path up to the given number of steps.</summary>
        private static CharacterObject Upgrade(CharacterObject troop, int steps)
        {
            for (int i = 0; i < steps && troop != null; i++)
            {
                CharacterObject[] targets = troop.UpgradeTargets;
                if (targets == null || targets.Length == 0 || targets[0] == null) break;
                troop = targets[0];
            }
            return troop;
        }
    }
}
