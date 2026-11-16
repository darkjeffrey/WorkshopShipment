using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace WorkshopShipments
{
    /// <summary>One shipment in the side panel: a picture of its biggest cargo item and a few lines of text.</summary>
    public sealed class ShipmentCardVM : ViewModel
    {
        public ShipmentCardVM(ImageIdentifierVM image, string title, string cargo, string arrival, string status, string position)
        {
            Image = image;
            Title = title ?? "";
            Cargo = cargo ?? "";
            Arrival = arrival ?? "";
            Status = status ?? "";
            Position = position ?? "";
        }

        /// <summary>An item picture drawn by the game (null = no picture).</summary>
        [DataSourceProperty]
        public ImageIdentifierVM Image { get; }

        [DataSourceProperty]
        public bool HasImage => Image != null;

        [DataSourceProperty]
        public string Title { get; }

        [DataSourceProperty]
        public string Cargo { get; }

        [DataSourceProperty]
        public string Arrival { get; }

        [DataSourceProperty]
        public string Status { get; }

        [DataSourceProperty]
        public string Position { get; }

        [DataSourceProperty]
        public bool HasPosition => Position.Length > 0;

        public override void OnFinalize()
        {
            base.OnFinalize();
            Image?.OnFinalize();
        }
    }
}
