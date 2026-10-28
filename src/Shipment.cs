using TaleWorlds.CampaignSystem.Party;

namespace WorkshopShipments
{
    /// <summary>One shipment on the road. Saved as a single "|"-separated line so the save needs no custom types.</summary>
    internal sealed class Shipment
    {
        public string PartyId;
        public string OriginId = "";
        public string DestId;
        public int Tier;
        public bool SellOnArrival;
        public bool Delivered;

        // Not saved.
        public MobileParty Party;
        public float LastDistanceLeft = -1f;

        public string ToSaveString() =>
            string.Join("|", PartyId, DestId, SellOnArrival ? "1" : "0", Delivered ? "1" : "0", OriginId, Tier.ToString());

        /// <summary>Reads a line written by ToSaveString; null if it is unreadable. Early saves had no origin or tier.</summary>
        public static Shipment FromSaveString(string line)
        {
            string[] parts = (line ?? "").Split('|');
            if (parts.Length < 4 || parts[0].Length == 0) return null;

            var shipment = new Shipment
            {
                PartyId = parts[0],
                DestId = parts[1],
                SellOnArrival = parts[2] == "1",
                Delivered = parts[3] == "1"
            };
            if (parts.Length >= 6)
            {
                shipment.OriginId = parts[4];
                int.TryParse(parts[5], out shipment.Tier);
            }
            return shipment;
        }
    }
}
