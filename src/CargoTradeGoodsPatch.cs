using System;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;

namespace WorkshopShipments
{
    /// <summary>
    /// Shipments go into a workshop warehouse, whose screen only moves trade goods, so only trade goods may be loaded:
    /// in the "Load the cargo" screen every other item loses its transfer arrows.
    /// </summary>
    [HarmonyPatch(typeof(SPItemVM), MethodType.Constructor, new[]
    {
        typeof(InventoryLogic), typeof(bool), typeof(bool), typeof(InventoryScreenHelper.InventoryMode),
        typeof(ItemRosterElement), typeof(InventoryLogic.InventorySide), typeof(int), typeof(EquipmentIndex?)
    })]
    internal static class CargoTradeGoodsPatch
    {
        private static void Postfix(SPItemVM __instance, InventoryScreenHelper.InventoryMode usageType, ItemRosterElement newItem)
        {
            try
            {
                if (!ShipmentBehavior.LoadingCargo || usageType != InventoryScreenHelper.InventoryMode.Stash
                    || !__instance.IsTransferable || ShipmentBehavior.CanShip(newItem.EquipmentElement)) return;

                __instance.IsTransferable = false;
                if (__instance.TradeData != null) __instance.TradeData.IsTradeable = false;
                __instance.UpdateHintTexts();
            }
            catch (Exception e)
            {
                Log.Error("CargoTradeGoodsPatch", e);
            }
        }
    }
}
