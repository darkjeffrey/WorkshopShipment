using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace WorkshopShipments
{
    /// <summary>
    /// The shipment menus: the shipment menu with its destination and escort pickers, and "Check shipments on the road",
    /// which opens a small menu with a card per shipment in a panel beside it.
    /// </summary>
    public sealed partial class ShipmentBehavior
    {
        private const string MenuId = "wsh_shipment";
        private const string RoadMenuId = "wsh_road";

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                // A save made while the shipment menu was open still holds the loaded cargo; give it back.
                ReturnPendingCargo();

                starter.AddGameMenuOption("town", "wsh_town_shipment",
                    "{=wsh_town_option}Arrange a shipment to one of your workshops",
                    TownOptionCondition,
                    args =>
                    {
                        _pendingDest = null;
                        GameMenu.SwitchToMenu(MenuId);
                    });

                starter.AddGameMenu(MenuId,
                    "{=wsh_menu_text}A local carter offers to haul goods to one of your workshops under guard. The caravan won't stop to trade on the way, but anyone at war with you can still attack it.\n \nCargo: {WSH_CARGO}\nDestination: {WSH_DEST}\nEscort: {WSH_ESCORT}\nCost: {WSH_COST}{GOLD_ICON}\n{WSH_NOTE}\nYour shipments on the road: {WSH_ACTIVE}/{WSH_MAX}",
                    args => RefreshTexts(),
                    GameMenu.MenuOverlayType.SettlementWithBoth);

                starter.AddGameMenuOption(MenuId, "wsh_load", "{=wsh_load}Load the cargo",
                    args =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                        RefreshTexts();
                        return true;
                    },
                    args => InventoryScreenHelper.OpenScreenAsStash(_pendingCargo));

                starter.AddGameMenuOption(MenuId, "wsh_destination", "{=wsh_destination}Choose the destination",
                    args =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                        return true;
                    },
                    args => ShowDestinationPicker());

                starter.AddGameMenuOption(MenuId, "wsh_escort", "{=wsh_escort}Choose the escort",
                    args =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                        return true;
                    },
                    args => ShowEscortPicker());

                starter.AddGameMenuOption(MenuId, "wsh_status", "{=wsh_status}Check shipments on the road",
                    args =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                        if (ActiveShipments().Count == 0)
                        {
                            args.IsEnabled = false;
                            args.Tooltip = new TextObject("{=wsh_status_none}You have no shipments on the road.");
                        }
                        return true;
                    },
                    args => GameMenu.SwitchToMenu(RoadMenuId));

                starter.AddGameMenuOption(MenuId, "wsh_send", "{=wsh_send}Send the shipment ({WSH_COST}{GOLD_ICON})",
                    SendCondition,
                    args => Dispatch());

                starter.AddGameMenuOption(MenuId, "wsh_back", "{=wsh_back}Back to town",
                    args =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                        return true;
                    },
                    args =>
                    {
                        ReturnPendingCargo();
                        GameMenu.SwitchToMenu("town");
                    },
                    true);

                // Shipments on the road: the cards are in the panel beside this menu.
                starter.AddGameMenu(RoadMenuId, "{WSH_ROAD_TEXT}", args => RoadInit(args), GameMenu.MenuOverlayType.SettlementWithBoth);
                starter.AddGameMenuOption(RoadMenuId, "wsh_road_back", "{=wsh_back_shipment}Back to the shipment",
                    args =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                        return true;
                    },
                    args => GameMenu.SwitchToMenu(MenuId),
                    true);
            }
            catch (Exception e)
            {
                Log.Error("OnSessionLaunched", e);
            }
        }

        private bool TownOptionCondition(MenuCallbackArgs args)
        {
            if (!WsSettings.Enabled) return false;
            var workshops = Hero.MainHero?.OwnedWorkshops;
            if (workshops == null || workshops.Count == 0) return false;

            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            if (Warehouses.ValidDestinations(Settlement.CurrentSettlement).Count == 0)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=wsh_no_destination}You don't own a workshop in another town that isn't at war with you.");
            }
            return true;
        }

        private bool SendCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            RefreshTexts();

            TextObject reason = null;
            if (Warehouses.ItemCount(_pendingCargo) == 0)
                reason = new TextObject("{=wsh_need_cargo}Load some cargo first.");
            else if (_pendingDest == null)
                reason = new TextObject("{=wsh_need_dest}Choose a destination first.");
            else if (!Warehouses.ValidDestinations(Settlement.CurrentSettlement).Contains(_pendingDest))
                reason = new TextObject("{=wsh_dest_invalid}You can't send a shipment to {DEST} any more.").SetTextVariable("DEST", _pendingDest.Name);
            else if (ActiveShipments().Count >= WsSettings.MaxActive)
                reason = new TextObject("{=wsh_too_many}You already have {MAX} shipments on the road.").SetTextVariable("MAX", WsSettings.MaxActive);
            else if (Hero.MainHero.Gold < CurrentCost())
                reason = new TextObject("{=wsh_no_gold}You can't afford the carter's fee.");

            if (reason != null)
            {
                args.IsEnabled = false;
                args.Tooltip = reason;
            }
            return true;
        }

        private void RefreshTexts()
        {
            int count = Warehouses.ItemCount(_pendingCargo);
            MBTextManager.SetTextVariable("WSH_CARGO", count == 0
                ? new TextObject("{=wsh_cargo_empty}nothing loaded yet")
                : new TextObject("{=wsh_cargo_summary}{COUNT} items, weight {WEIGHT}, worth about {VALUE}{GOLD_ICON}")
                    .SetTextVariable("COUNT", count)
                    .SetTextVariable("WEIGHT", (int)Math.Ceiling(Warehouses.Weight(_pendingCargo)))
                    .SetTextVariable("VALUE", _pendingCargo.TotalValue));
            MBTextManager.SetTextVariable("WSH_DEST", _pendingDest == null
                ? new TextObject("{=wsh_dest_none}not chosen")
                : new TextObject("{=wsh_dest_summary}{TOWN} ({TIME} away)")
                    .SetTextVariable("TOWN", _pendingDest.Name)
                    .SetTextVariable("TIME", TravelTime(Distance(Settlement.CurrentSettlement, _pendingDest))));
            MBTextManager.SetTextVariable("WSH_ESCORT", EscortSummary(_pendingTier));
            MBTextManager.SetTextVariable("WSH_COST", CurrentCost());
            MBTextManager.SetTextVariable("WSH_ACTIVE", ActiveShipments().Count);
            MBTextManager.SetTextVariable("WSH_MAX", WsSettings.MaxActive);

            TextObject note = TextObject.GetEmpty();
            if (_pendingDest != null && count > 0 && Warehouses.Weight(_pendingCargo) > FreeSpace(_pendingDest))
                note = new TextObject("{=wsh_overflow_note}The warehouse in {DEST} doesn't have room for all of it. Whatever doesn't fit will be sold at the market there.\n")
                    .SetTextVariable("DEST", _pendingDest.Name);
            MBTextManager.SetTextVariable("WSH_NOTE", note);
        }

        private void ShowDestinationPicker()
        {
            Settlement origin = Settlement.CurrentSettlement;
            float cargoWeight = Warehouses.Weight(_pendingCargo);
            var elements = new List<InquiryElement>();
            foreach (Settlement town in Warehouses.ValidDestinations(origin).OrderBy(t => Distance(origin, t)))
            {
                float free = FreeSpace(town);
                TextObject title = new TextObject("{=wsh_pick_line}{TOWN}: {TIME} away")
                    .SetTextVariable("TOWN", town.Name)
                    .SetTextVariable("TIME", TravelTime(Distance(origin, town)));
                if (cargoWeight > free)
                    title = new TextObject("{=wsh_pick_line_full}{LINE} (warehouse too full)").SetTextVariable("LINE", title);
                string hint = new TextObject("{=wsh_pick_hint}Warehouse space left: {FREE} of {CAPACITY}.\nYour cargo weighs {WEIGHT}.")
                    .SetTextVariable("FREE", (int)Math.Floor(free))
                    .SetTextVariable("CAPACITY", Warehouses.Capacity)
                    .SetTextVariable("WEIGHT", (int)Math.Ceiling(cargoWeight))
                    .ToString();
                elements.Add(new InquiryElement(town, title.ToString(), null, true, hint));
            }

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                new TextObject("{=wsh_pick_title}Send the shipment to").ToString(),
                new TextObject("{=wsh_pick_desc}Choose the workshop town. The cargo goes into your warehouse there. Hover over a town to see how much room its warehouse has.").ToString(),
                elements, true, 1, 1,
                GameTexts.FindText("str_done").ToString(),
                GameTexts.FindText("str_cancel").ToString(),
                selected =>
                {
                    if (selected != null && selected.Count > 0) _pendingDest = selected[0].Identifier as Settlement;
                    GameMenu.SwitchToMenu(MenuId);
                },
                null));
        }

        private void ShowEscortPicker()
        {
            var elements = new List<InquiryElement>();
            for (int tier = 0; tier < Escorts.Count; tier++)
            {
                elements.Add(new InquiryElement(tier, EscortSummary(tier).ToString(), null, true, Escorts.Description(tier).ToString()));
            }

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                new TextObject("{=wsh_escort_title}Hire an escort").ToString(),
                new TextObject("{=wsh_escort_desc}A bigger escort costs more but makes the caravan a harder target. Hover over an escort to see who is in it.").ToString(),
                elements, true, 1, 1,
                GameTexts.FindText("str_done").ToString(),
                GameTexts.FindText("str_cancel").ToString(),
                selected =>
                {
                    if (selected != null && selected.Count > 0 && selected[0].Identifier is int tier) _pendingTier = tier;
                    GameMenu.SwitchToMenu(MenuId);
                },
                null));
        }

        // ---------------- Shipments on the road ----------------

        /// <summary>Opens the road menu and puts a card for each shipment in the panel beside it.</summary>
        private void RoadInit(MenuCallbackArgs args)
        {
            args.MenuTitle = new TextObject("{=wsh_status_title}Shipments on the road");
            var cards = new List<ShipmentCardVM>();

            List<Shipment> active = ActiveShipments();
            int number = 0;
            foreach (Shipment s in active)
            {
                MobileParty party = s.Party;
                Settlement dest = Settlement.Find(s.DestId);
                number++;

                ItemObject biggest = Stacks(party.ItemRoster).Select(x => x.Key).FirstOrDefault();
                cards.Add(new ShipmentCardVM(
                    biggest == null ? null : new ItemImageIdentifierVM(biggest),
                    new TextObject("{=wsh_card_route}{NUMBER}. {ROUTE}").SetTextVariable("NUMBER", number).SetTextVariable("ROUTE", Route(s)).ToString(),
                    new TextObject("{=wsh_card_cargo}Cargo: {CARGO}").SetTextVariable("CARGO", CargoSummary(party.ItemRoster)).ToString(),
                    new TextObject("{=wsh_card_arrives}Arrives: {ARRIVAL}").SetTextVariable("ARRIVAL", Arrival(party, dest)).ToString(),
                    new TextObject("{=wsh_card_escort_status}{ESCORT}, {MEN} men. {STATUS}")
                        .SetTextVariable("ESCORT", Escorts.Name(s.Tier))
                        .SetTextVariable("MEN", party.MemberRoster.TotalHealthyCount)
                        .SetTextVariable("STATUS", State(s, party)).ToString(),
                    Position(party)));
            }

            MBTextManager.SetTextVariable("WSH_ROAD_TEXT", active.Count == 0
                ? new TextObject("{=wsh_status_none}You have no shipments on the road.")
                : new TextObject("{=wsh_road_text}{ACTIVE} of {MAX} shipments on the road.")
                    .SetTextVariable("ACTIVE", active.Count)
                    .SetTextVariable("MAX", WsSettings.MaxActive));
            ShipmentPanel.Show(RoadMenuId, new TextObject("{=wsh_status_title}Shipments on the road").ToString(), cards);
        }

        /// <summary>"Pravend to Sanala".</summary>
        private static TextObject Route(Shipment s)
        {
            Settlement origin = string.IsNullOrEmpty(s.OriginId) ? null : Settlement.Find(s.OriginId);
            Settlement dest = Settlement.Find(s.DestId);
            return new TextObject("{=wsh_route}{ORIGIN} to {DEST}")
                .SetTextVariable("ORIGIN", origin?.Name ?? new TextObject("{=wsh_unknown}unknown"))
                .SetTextVariable("DEST", dest?.Name ?? new TextObject("{=wsh_unknown}unknown"));
        }

        private static TextObject State(Shipment s, MobileParty party)
        {
            if (party.MapEvent != null) return new TextObject("{=wsh_state_fight}Under attack!");
            if (s.SellOnArrival) return new TextObject("{=wsh_state_sell}On the road, going to sell the cargo");
            return new TextObject("{=wsh_state_road}On the road");
        }

        private TextObject Arrival(MobileParty party, Settlement dest)
        {
            if (dest == null) return new TextObject("{=wsh_unknown}unknown");
            if (party.CurrentSettlement == dest) return new TextObject("{=wsh_arrival_now}unloading now");
            return new TextObject("{=wsh_arrival_in}in {TIME}").SetTextVariable("TIME", TravelTime(PartyDistance(party, dest)));
        }

        /// <summary>"Position: near Epicrotea" when the "Show where shipments are" option is on, otherwise empty.</summary>
        private static string Position(MobileParty party)
        {
            if (!WsSettings.ShowPosition) return "";
            Settlement near = SettlementHelper.FindNearestSettlementToMobileParty(party, MobileParty.NavigationType.Default,
                x => x.IsTown || x.IsCastle || x.IsVillage);
            return new TextObject("{=wsh_status_position}Position: near {NEAR}")
                .SetTextVariable("NEAR", near?.Name ?? new TextObject("{=wsh_unknown}unknown"))
                .ToString();
        }

        /// <summary>Cargo amounts per item, biggest stacks first.</summary>
        private static List<KeyValuePair<ItemObject, int>> Stacks(ItemRoster roster)
        {
            var stacks = new Dictionary<ItemObject, int>();
            for (int i = 0; i < roster.Count; i++)
            {
                ItemRosterElement element = roster.GetElementCopyAtIndex(i);
                ItemObject item = element.EquipmentElement.Item;
                if (item == null || element.Amount <= 0) continue;
                stacks.TryGetValue(item, out int amount);
                stacks[item] = amount + element.Amount;
            }
            return stacks.OrderByDescending(x => x.Value).ToList();
        }

        /// <summary>"40 Grain, 12 Hides, 5 Iron Ore and 2 more (59 items, worth about 1840)": the biggest stacks first.</summary>
        private static TextObject CargoSummary(ItemRoster roster)
        {
            List<KeyValuePair<ItemObject, int>> stacks = Stacks(roster);
            if (stacks.Count == 0) return new TextObject("{=wsh_cargo_none}nothing");

            const int shown = 3;
            List<string> parts = stacks.Take(shown)
                .Select(x => new TextObject("{=wsh_cargo_stack}{AMOUNT} {ITEM}")
                    .SetTextVariable("AMOUNT", x.Value)
                    .SetTextVariable("ITEM", x.Key.Name)
                    .ToString())
                .ToList();
            string list = string.Join(", ", parts);
            if (stacks.Count > shown)
                list = new TextObject("{=wsh_cargo_more}{LIST} and {MORE} more")
                    .SetTextVariable("LIST", list)
                    .SetTextVariable("MORE", stacks.Count - shown)
                    .ToString();

            return new TextObject("{=wsh_cargo_contents}{LIST} ({COUNT} items, worth about {VALUE}{GOLD_ICON})")
                .SetTextVariable("LIST", list)
                .SetTextVariable("COUNT", Warehouses.ItemCount(roster))
                .SetTextVariable("VALUE", roster.TotalValue);
        }

        private static TextObject EscortSummary(int tier)
        {
            int extra = Escorts.ExtraCost(tier);
            TextObject text = extra > 0
                ? new TextObject("{=wsh_tier_line_cost}{NAME}: {MEN} men, +{COST}{GOLD_ICON}").SetTextVariable("COST", extra)
                : new TextObject("{=wsh_tier_line}{NAME}: {MEN} men");
            return text.SetTextVariable("NAME", Escorts.Name(tier)).SetTextVariable("MEN", Escorts.Size(tier) + 1);
        }
    }
}
