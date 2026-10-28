using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace WorkshopShipments
{
    /// <summary>
    /// Shipment escorts bring their own rations: without this they would eat any food in the cargo, and starve and
    /// desert when the cargo has none.
    /// </summary>
    [HarmonyPatch(typeof(DefaultMobilePartyFoodConsumptionModel), nameof(DefaultMobilePartyFoodConsumptionModel.DoesPartyConsumeFood))]
    internal static class FoodPatch
    {
        private static void Postfix(MobileParty mobileParty, ref bool __result)
        {
            try
            {
                if (__result && ShipmentBehavior.IsShipment(mobileParty)) __result = false;
            }
            catch (Exception e)
            {
                Log.Error("FoodPatch", e);
            }
        }
    }
}
