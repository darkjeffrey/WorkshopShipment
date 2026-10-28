using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace WorkshopShipments
{
    /// <summary>One shipment in the side panel: a picture of its biggest cargo item and a few lines of text.</summary>
    public sealed class ShipmentCardVM : ViewModel
    {
        public ShipmentCardVM(ImageIdentifierVM image, string title, string line1, string line2, string line3, string description)
        {
            Image = image;
            Title = title ?? "";
            Line1 = line1 ?? "";
            Line2 = line2 ?? "";
            Line3 = line3 ?? "";
            Description = description ?? "";
        }

        /// <summary>An item picture drawn by the game (null = no picture).</summary>
        [DataSourceProperty]
        public ImageIdentifierVM Image { get; }

        [DataSourceProperty]
        public bool HasImage => Image != null;

        [DataSourceProperty]
        public string Title { get; }

        [DataSourceProperty]
        public string Line1 { get; }

        [DataSourceProperty]
        public string Line2 { get; }

        [DataSourceProperty]
        public string Line3 { get; }

        [DataSourceProperty]
        public string Description { get; }

        [DataSourceProperty]
        public bool HasDescription => Description.Length > 0;

        public override void OnFinalize()
        {
            base.OnFinalize();
            Image?.OnFinalize();
        }
    }
}
