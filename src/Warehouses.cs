using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace WorkshopShipments
{
    /// <summary>
    /// Access to the vanilla workshop warehouses. There is one warehouse per town where the player owns a workshop; its
    /// roster lives in WorkshopsCampaignBehavior and is only reachable through the private GetWarehouseRoster.
    /// </summary>
    internal static class Warehouses
    {
        private static readonly MethodInfo GetWarehouseRosterMethod =
            AccessTools.Method(typeof(WorkshopsCampaignBehavior), "GetWarehouseRoster", new[] { typeof(Settlement) });

        /// <summary>The town's warehouse roster, or null if the player has no warehouse there.</summary>
        public static ItemRoster GetRoster(Settlement town)
        {
            if (town == null || !town.IsTown || !PlayerOwnsWorkshopIn(town)) return null;
            try
            {
                var behavior = Campaign.Current?.GetCampaignBehavior<WorkshopsCampaignBehavior>();
                if (behavior == null || GetWarehouseRosterMethod == null) return null;
                return GetWarehouseRosterMethod.Invoke(behavior, new object[] { town }) as ItemRoster;
            }
            catch (Exception e)
            {
                Log.Debug("GetWarehouseRoster failed for " + town.StringId + ": " + (e.InnerException ?? e).Message);
                return null;
            }
        }

        public static bool PlayerOwnsWorkshopIn(Settlement town)
        {
            var workshops = Hero.MainHero?.OwnedWorkshops;
            return workshops != null && workshops.Any(w => w != null && w.Settlement == town);
        }

        /// <summary>Towns that can receive a shipment now: the player has a warehouse there and isn't at war with it.</summary>
        public static List<Settlement> ValidDestinations(Settlement except)
        {
            var result = new List<Settlement>();
            var workshops = Hero.MainHero?.OwnedWorkshops;
            if (workshops == null) return result;
            foreach (var town in workshops.Select(w => w?.Settlement).Where(s => s != null).Distinct())
            {
                if (town == except || !IsReachable(town)) continue;
                if (GetRoster(town) != null) result.Add(town);
            }
            return result;
        }

        public static bool IsReachable(Settlement town)
        {
            var playerFaction = Clan.PlayerClan?.MapFaction;
            return town != null && town.IsTown && (playerFaction == null || !FactionManager.IsAtWarAgainstFaction(playerFaction, town.MapFaction));
        }

        public static int Capacity => Campaign.Current?.Models?.WorkshopModel?.WarehouseCapacity ?? 0;

        public static float Weight(ItemRoster roster)
        {
            if (roster == null) return 0f;
            float total = 0f;
            for (int i = 0; i < roster.Count; i++) total += roster.GetElementCopyAtIndex(i).GetRosterElementWeight();
            return total;
        }

        public static int ItemCount(ItemRoster roster)
        {
            if (roster == null) return 0;
            int total = 0;
            for (int i = 0; i < roster.Count; i++) total += roster.GetElementCopyAtIndex(i).Amount;
            return total;
        }

        /// <summary>Moves every item from one roster to another and empties the source.</summary>
        public static void MoveAll(ItemRoster from, ItemRoster to)
        {
            if (from == null || to == null) return;
            for (int i = 0; i < from.Count; i++)
            {
                ItemRosterElement element = from.GetElementCopyAtIndex(i);
                if (element.Amount > 0) to.AddToCounts(element.EquipmentElement, element.Amount);
            }
            from.Clear();
        }

        /// <summary>
        /// Puts as much of the cargo into the town's warehouse as fits by weight. What doesn't fit is sold at the town
        /// market and the money goes to the player. Returns (items stored, items sold, gold earned).
        /// </summary>
        public static (int stored, int sold, int gold) Unload(ItemRoster cargo, Settlement town, bool sellEverything)
        {
            ItemRoster warehouse = sellEverything ? null : GetRoster(town);
            float free = warehouse == null ? 0f : Math.Max(0f, Capacity - Weight(warehouse));
            int stored = 0, sold = 0, gold = 0;

            for (int i = 0; i < cargo.Count; i++)
            {
                ItemRosterElement element = cargo.GetElementCopyAtIndex(i);
                EquipmentElement item = element.EquipmentElement;
                int amount = element.Amount;
                if (amount <= 0 || item.Item == null) continue;

                int fits = amount;
                if (warehouse != null)
                {
                    float unitWeight = item.GetEquipmentElementWeight();
                    if (unitWeight > 0f) fits = Math.Min(amount, (int)Math.Floor(free / unitWeight));
                    if (fits > 0)
                    {
                        warehouse.AddToCounts(item, fits);
                        free -= fits * unitWeight;
                        stored += fits;
                    }
                }
                else
                {
                    fits = 0;
                }

                int rest = amount - fits;
                if (rest > 0 && town?.Town != null)
                {
                    int price = town.Town.GetItemPrice(item, null, true);
                    town.ItemRoster.AddToCounts(item, rest);
                    gold += price * rest;
                    sold += rest;
                }
            }
            cargo.Clear();

            if (gold > 0)
            {
                town.Town.ChangeGold(-Math.Min(gold, town.Town.Gold));
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, gold, true);
            }
            return (stored, sold, gold);
        }
    }
}
