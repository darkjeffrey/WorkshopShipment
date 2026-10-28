using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace WorkshopShipments
{
    /// <summary>The panel itself: a title and one page of cards, with scroll buttons for the rest.</summary>
    public sealed class ShipmentPanelVM : ViewModel
    {
        /// <summary>How many cards are shown at once; the rest are reached with the scroll buttons.</summary>
        public const int PageSize = 4;

        private readonly List<ShipmentCardVM> _all;
        private int _offset;
        private bool _canUp;
        private bool _canDown;
        private string _pageText = "";

        public ShipmentPanelVM(string title, List<ShipmentCardVM> cards)
        {
            Title = title;
            _all = cards;
            Cards = new MBBindingList<ShipmentCardVM>();
            Refresh();
        }

        [DataSourceProperty]
        public string Title { get; }

        [DataSourceProperty]
        public string ScrollUpText => new TextObject("{=wsh_scroll_up}^  Scroll up").ToString();

        [DataSourceProperty]
        public string ScrollDownText => new TextObject("{=wsh_scroll_down}v  Scroll down").ToString();

        /// <summary>The cards on the current page.</summary>
        [DataSourceProperty]
        public MBBindingList<ShipmentCardVM> Cards { get; }

        [DataSourceProperty]
        public bool CanScrollUp
        {
            get => _canUp;
            private set
            {
                if (_canUp == value) return;
                _canUp = value;
                OnPropertyChangedWithValue(value, nameof(CanScrollUp));
            }
        }

        [DataSourceProperty]
        public bool CanScrollDown
        {
            get => _canDown;
            private set
            {
                if (_canDown == value) return;
                _canDown = value;
                OnPropertyChangedWithValue(value, nameof(CanScrollDown));
            }
        }

        /// <summary>"1-4 of 9", shown under the cards.</summary>
        [DataSourceProperty]
        public string PageText
        {
            get => _pageText;
            private set
            {
                if (_pageText == value) return;
                _pageText = value;
                OnPropertyChangedWithValue(value, nameof(PageText));
            }
        }

        public void ExecuteScrollUp() => Scroll(-1);

        public void ExecuteScrollDown() => Scroll(1);

        /// <summary>Moves the visible window by whole cards (negative is up).</summary>
        private void Scroll(int cards)
        {
            int max = Math.Max(0, _all.Count - PageSize);
            int next = Math.Max(0, Math.Min(max, _offset + cards));
            if (next == _offset) return;
            _offset = next;
            Refresh();
        }

        private void Refresh()
        {
            Cards.Clear();
            foreach (ShipmentCardVM card in _all.Skip(_offset).Take(PageSize)) Cards.Add(card);

            CanScrollUp = _offset > 0;
            CanScrollDown = _offset + PageSize < _all.Count;
            PageText = _all.Count > PageSize
                ? new TextObject("{=wsh_page}{FIRST}-{LAST} of {TOTAL}")
                    .SetTextVariable("FIRST", _offset + 1)
                    .SetTextVariable("LAST", Math.Min(_all.Count, _offset + PageSize))
                    .SetTextVariable("TOTAL", _all.Count)
                    .ToString()
                : "";
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            Cards.Clear();
            foreach (ShipmentCardVM card in _all) card.OnFinalize();
        }
    }
}
