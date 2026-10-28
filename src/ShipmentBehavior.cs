using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace WorkshopShipments
{
    /// <summary>
    /// Sends, tracks and delivers shipments. A shipment is a plain CustomPartyComponent party (saveable without a type
    /// definer), not a caravan, so the caravan trading AI and the "trade with caravan" option never see it.
    /// The menus are in ShipmentBehavior.Menus.cs.
    /// </summary>
    public sealed partial class ShipmentBehavior : CampaignBehaviorBase
    {
        // Map distance per hour used for travel times until a real shipment has been measured.
        private const float DefaultDistancePerHour = 4.5f;
        // Hourly progress outside this range means the shipment stopped, was rerouted or the reading is bad.
        private const float MinDistancePerHour = 0.5f;
        private const float MaxDistancePerHour = 50f;

        private static ShipmentBehavior _instance;

        private readonly List<Shipment> _shipments = new List<Shipment>();
        private ItemRoster _pendingCargo = new ItemRoster();
        private Settlement _pendingDest;
        private int _pendingTier = Escorts.Basic;
        private float _measuredDistancePerHour;

        public ShipmentBehavior()
        {
            _instance = this;
        }

        /// <summary>True for a party this mod created (used by the food patch).</summary>
        public static bool IsShipment(MobileParty party)
        {
            return party != null && _instance != null
                && _instance._shipments.Any(s => s.Party == party || (s.Party == null && s.PartyId == party.StringId));
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnPartyDestroyed);
        }

        public override void SyncData(IDataStore dataStore)
        {
            List<string> saved = _shipments.Select(s => s.ToSaveString()).ToList();
            dataStore.SyncData("wsh_shipments", ref saved);
            dataStore.SyncData("wsh_pending_cargo", ref _pendingCargo);
            dataStore.SyncData("wsh_average_speed", ref _measuredDistancePerHour);

            if (dataStore.IsLoading)
            {
                _shipments.Clear();
                _shipments.AddRange((saved ?? new List<string>()).Select(Shipment.FromSaveString).Where(s => s != null));
                _pendingCargo ??= new ItemRoster();
            }
        }

        // ---------------- Sending ----------------

        private int CurrentCost()
        {
            int cost = WsSettings.BaseFee + Escorts.ExtraCost(_pendingTier);
            if (_pendingDest == null || Settlement.CurrentSettlement == null) return cost;

            float distance = Distance(Settlement.CurrentSettlement, _pendingDest);
            return cost + (int)Math.Ceiling(Warehouses.Weight(_pendingCargo) * WsSettings.FeePerWeight * distance / 100f);
        }

        /// <summary>Warehouse room left in a town, counting shipments already on their way there.</summary>
        private float FreeSpace(Settlement town)
        {
            float used = Warehouses.Weight(Warehouses.GetRoster(town))
                + ActiveShipments().Where(s => s.DestId == town.StringId).Sum(s => Warehouses.Weight(s.Party.ItemRoster));
            return Math.Max(0f, Warehouses.Capacity - used);
        }

        private void Dispatch()
        {
            try
            {
                Settlement origin = Settlement.CurrentSettlement;
                Settlement dest = _pendingDest;
                if (origin == null || dest == null) return;

                int cost = CurrentCost();
                int itemCount = Warehouses.ItemCount(_pendingCargo);
                MobileParty party = CreateShipmentParty(origin, dest);
                if (party == null) return;

                Warehouses.MoveAll(_pendingCargo, party.ItemRoster);
                SendTo(party, dest);
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, cost);

                _shipments.Add(new Shipment
                {
                    PartyId = party.StringId,
                    OriginId = origin.StringId,
                    DestId = dest.StringId,
                    Tier = _pendingTier,
                    Party = party
                });
                _pendingDest = null;

                Log.Info(new TextObject("{=wsh_sent}Your shipment of {COUNT} items has left {ORIGIN} for {DEST}. It should get there in {TIME}.")
                    .SetTextVariable("COUNT", itemCount)
                    .SetTextVariable("ORIGIN", origin.Name)
                    .SetTextVariable("DEST", dest.Name)
                    .SetTextVariable("TIME", TravelTime(Distance(origin, dest))), Colors.Green);
                Log.Debug("Created " + party.StringId + " with " + party.MemberRoster.TotalManCount + " men, cost " + cost);

                GameMenu.SwitchToMenu("town");
            }
            catch (Exception e)
            {
                Log.Error("Dispatch", e);
            }
        }

        private MobileParty CreateShipmentParty(Settlement origin, Settlement dest)
        {
            TroopRoster escort = Escorts.Build(_pendingTier, origin.Culture ?? Hero.MainHero.Culture);
            TextObject name = new TextObject("{=wsh_party_name}Shipment to {DEST}").SetTextVariable("DEST", dest.Name);

            MobileParty party = CustomPartyComponent.CreateCustomPartyWithTroopRoster(
                origin.GatePosition, 1f, dest, name, Clan.PlayerClan, escort, TroopRoster.CreateDummyTroopRoster(),
                Hero.MainHero, "mule", "mule_load_a", avoidHostileActions: true);
            if (party == null) Log.Debug("CreateCustomPartyWithTroopRoster returned null");
            return party;
        }

        private static void SendTo(MobileParty party, Settlement dest)
        {
            party.Ai.SetDoNotMakeNewDecisions(true);
            party.SetMoveGoToSettlement(dest, MobileParty.NavigationType.Default, false);
        }

        private void ReturnPendingCargo()
        {
            if (_pendingCargo == null || _pendingCargo.Count == 0 || MobileParty.MainParty == null) return;
            Warehouses.MoveAll(_pendingCargo, MobileParty.MainParty.ItemRoster);
            Log.Debug("Returned the unsent cargo to the party");
        }

        // Covers every way out of town, including ones that skip the "Back to town" option.
        private void OnSettlementLeft(MobileParty party, Settlement settlement)
        {
            if (party != null && party == MobileParty.MainParty) ReturnPendingCargo();
        }

        // ---------------- Travel ----------------

        private static MobileParty FindParty(Shipment s)
        {
            return s.Party ??= MobileParty.All.FirstOrDefault(p => p.StringId == s.PartyId);
        }

        private List<Shipment> ActiveShipments()
        {
            return _shipments.Where(s => !s.Delivered && FindParty(s) != null && s.Party.IsActive).ToList();
        }

        private void OnHourlyTick()
        {
            try
            {
                foreach (Shipment s in _shipments.ToList()) UpdateShipment(s);
            }
            catch (Exception e)
            {
                Log.Error("OnHourlyTick", e);
            }
        }

        private void UpdateShipment(Shipment s)
        {
            MobileParty party = FindParty(s);
            if (party == null || !party.IsActive)
            {
                _shipments.Remove(s);
                return;
            }
            if (party.MapEvent != null) return;

            // Delivered parties are removed here rather than inside the settlement-entered event.
            if (s.Delivered)
            {
                _shipments.Remove(s);
                DestroyPartyAction.Apply(null, party);
                return;
            }

            Settlement dest = Settlement.Find(s.DestId);
            if (!s.SellOnArrival && !CanDeliverTo(dest)) dest = Redirect(s, party);
            if (dest == null) return;

            if (party.CurrentSettlement == dest)
            {
                Arrive(s, party, dest);
                return;
            }

            MeasureSpeed(s, party, dest);
            if (party.TargetSettlement != dest || party.DefaultBehavior != AiBehavior.GoToSettlement)
            {
                SendTo(party, dest);
                s.LastDistanceLeft = -1f;
                Log.Debug(party.StringId + " sent back on the road to " + dest.StringId);
            }
        }

        private static bool CanDeliverTo(Settlement town) =>
            town != null && Warehouses.IsReachable(town) && Warehouses.GetRoster(town) != null;

        /// <summary>
        /// The destination can't take the shipment any more (war, or the workshop is gone). Goes to the nearest other
        /// warehouse; if there is none, to the nearest friendly town to sell the cargo there.
        /// </summary>
        private Settlement Redirect(Shipment s, MobileParty party)
        {
            Settlement old = Settlement.Find(s.DestId);
            Settlement next = Warehouses.ValidDestinations(null).OrderBy(t => PartyDistance(party, t)).FirstOrDefault();
            if (next == null)
            {
                next = Town.AllTowns.Select(t => t.Settlement).Where(Warehouses.IsReachable)
                    .OrderBy(t => PartyDistance(party, t)).FirstOrDefault();
                s.SellOnArrival = true;
            }
            if (next == null) return null;

            s.DestId = next.StringId;
            s.LastDistanceLeft = -1f;
            party.SetMoveGoToSettlement(next, MobileParty.NavigationType.Default, false);

            TextObject message = s.SellOnArrival
                ? new TextObject("{=wsh_redirect_sell}Your shipment can't reach a warehouse of yours any more. It is heading to {DEST} to sell the cargo.")
                : new TextObject("{=wsh_redirect}Your shipment to {OLD} can't deliver there any more. It is heading to your warehouse in {DEST} instead.");
            Log.Info(message
                .SetTextVariable("OLD", old?.Name ?? TextObject.GetEmpty())
                .SetTextVariable("DEST", next.Name), Colors.Yellow);
            return next;
        }

        /// <summary>
        /// Keeps a running average of how much closer a shipment gets each hour, in the same map distance the travel
        /// times use, so the estimates match how fast shipments really move.
        /// </summary>
        private void MeasureSpeed(Shipment s, MobileParty party, Settlement dest)
        {
            bool travelling = party.CurrentSettlement == null && party.TargetSettlement == dest;
            float distanceLeft = travelling ? PartyDistance(party, dest) : -1f;
            float last = s.LastDistanceLeft;
            s.LastDistanceLeft = distanceLeft;
            if (last <= 0f || distanceLeft <= 0f || last == float.MaxValue || distanceLeft == float.MaxValue) return;

            float perHour = last - distanceLeft;
            if (perHour <= MinDistancePerHour || perHour > MaxDistancePerHour) return;

            _measuredDistancePerHour = _measuredDistancePerHour <= MinDistancePerHour
                ? perHour
                : _measuredDistancePerHour * 0.9f + perHour * 0.1f;
            Log.Debug(party.StringId + " moved " + perHour.ToString("0.0") + " this hour, average " + _measuredDistancePerHour.ToString("0.0"));
        }

        /// <summary>"about 1 day 5 hours" for a map distance.</summary>
        private TextObject TravelTime(float distance)
        {
            float speed = _measuredDistancePerHour > MinDistancePerHour ? _measuredDistancePerHour : DefaultDistancePerHour;
            int totalHours = Math.Max(1, (int)Math.Round(distance / speed));
            int days = totalHours / 24;
            int hours = totalHours % 24;

            TextObject dayText = days == 1 ? new TextObject("{=wsh_one_day}1 day") : new TextObject("{=wsh_days}{DAYS} days").SetTextVariable("DAYS", days);
            TextObject hourText = hours == 1 ? new TextObject("{=wsh_one_hour}1 hour") : new TextObject("{=wsh_hours}{HOURS} hours").SetTextVariable("HOURS", hours);
            TextObject time = days == 0 ? hourText
                : hours == 0 ? dayText
                : new TextObject("{=wsh_days_hours}{DAYS} {HOURS}").SetTextVariable("DAYS", dayText).SetTextVariable("HOURS", hourText);
            return new TextObject("{=wsh_about}about {TIME}").SetTextVariable("TIME", time);
        }

        private static float Distance(Settlement from, Settlement to)
        {
            try
            {
                return Campaign.Current.Models.MapDistanceModel.GetDistance(from, to, false, false, MobileParty.NavigationType.Default);
            }
            catch (Exception)
            {
                return 0f;
            }
        }

        private static float PartyDistance(MobileParty party, Settlement town)
        {
            try
            {
                return Campaign.Current.Models.MapDistanceModel.GetDistance(party, town, false, MobileParty.NavigationType.Default, out float _);
            }
            catch (Exception)
            {
                return float.MaxValue;
            }
        }

        // ---------------- Arrival and loss ----------------

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            try
            {
                if (party == null || settlement == null) return;
                Shipment s = _shipments.FirstOrDefault(x => !x.Delivered && FindParty(x) == party);
                if (s != null && s.DestId == settlement.StringId) Arrive(s, party, settlement);
            }
            catch (Exception e)
            {
                Log.Error("OnSettlementEntered", e);
            }
        }

        private void Arrive(Shipment s, MobileParty party, Settlement town)
        {
            var (stored, sold, gold) = Warehouses.Unload(party.ItemRoster, town, s.SellOnArrival);
            s.Delivered = true;

            TextObject text = sold == 0
                ? new TextObject("{=wsh_arrived}Your shipment reached {DEST}. {STORED} items were put in your warehouse.")
                : stored == 0
                    ? new TextObject("{=wsh_arrived_sold}Your shipment reached {DEST}. The cargo ({SOLD} items) was sold for {GOLD}{GOLD_ICON}.")
                    : new TextObject("{=wsh_arrived_partial}Your shipment reached {DEST}. {STORED} items were put in your warehouse; {SOLD} didn't fit and were sold for {GOLD}{GOLD_ICON}.");
            MBInformationManager.AddQuickInformation(text
                .SetTextVariable("DEST", town.Name)
                .SetTextVariable("STORED", stored)
                .SetTextVariable("SOLD", sold)
                .SetTextVariable("GOLD", gold));
            Log.Debug(party.StringId + " delivered: stored " + stored + ", sold " + sold + " for " + gold);
        }

        private void OnPartyDestroyed(MobileParty party, PartyBase destroyer)
        {
            try
            {
                Shipment s = _shipments.FirstOrDefault(x => x.Party == party || x.PartyId == party?.StringId);
                if (s == null) return;
                _shipments.Remove(s);
                if (s.Delivered) return;

                Settlement dest = Settlement.Find(s.DestId);
                TextObject text = destroyer != null
                    ? new TextObject("{=wsh_lost_to}Your shipment to {DEST} was taken by {ENEMY}.").SetTextVariable("ENEMY", destroyer.Name)
                    : new TextObject("{=wsh_lost}Your shipment to {DEST} was lost.");
                MBInformationManager.AddQuickInformation(text.SetTextVariable("DEST", dest?.Name ?? TextObject.GetEmpty()));
            }
            catch (Exception e)
            {
                Log.Error("OnPartyDestroyed", e);
            }
        }
    }
}
