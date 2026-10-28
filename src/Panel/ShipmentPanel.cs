using System;
using System.Collections.Generic;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace WorkshopShipments
{
    /// <summary>
    /// The card panel beside the left game menu (game menus can only show text, not pictures). It is a Gauntlet layer on
    /// the map screen that closes itself once the menu it was opened for is no longer showing. When there are more cards
    /// than fit, the scroll buttons make the layer take mouse input (and only mouse input).
    /// </summary>
    internal static class ShipmentPanel
    {
        private const int GraceTicks = 10;

        private static GauntletLayer _layer;
        private static ScreenBase _screen;
        private static Action _release;
        private static string _menuId;
        private static int _mismatchTicks;
        private static bool _restricted;

        /// <summary>Shows the panel while the given game menu is open.</summary>
        public static void Show(string menuId, string title, List<ShipmentCardVM> cards)
        {
            Hide();
            if (cards.Count == 0 || !(ScreenManager.TopScreen is MapScreen screen)) return;

            var vm = new ShipmentPanelVM(title, cards);
            _screen = screen;
            _menuId = menuId;
            _layer = new GauntletLayer("GauntletLayer", 1001);
            var movie = _layer.LoadMovie("ShipmentPanel", vm);

            // A panel with scroll buttons has to see the mouse; a plain one takes no input.
            _restricted = cards.Count > ShipmentPanelVM.PageSize;
            if (_restricted) _layer.InputRestrictions.SetInputRestrictions(true, ClickMask);

            screen.AddLayer(_layer);
            _mismatchTicks = 0;

            var layer = _layer;
            _release = () =>
            {
                layer.ReleaseMovie(movie);
                vm.OnFinalize();
            };
        }

        public static void Hide()
        {
            if (_layer == null) return;
            try
            {
                _release?.Invoke();
                if (_restricted) _layer.InputRestrictions.ResetInputRestrictions();
                _screen.RemoveLayer(_layer);
            }
            catch (Exception)
            {
                // The screen may already be gone (e.g. back to the main menu); nothing to clean up.
            }
            Clear();
        }

        private static void Clear()
        {
            _layer = null;
            _screen = null;
            _release = null;
            _menuId = null;
            _restricted = false;
        }

        // Only the mouse buttons are needed; falls back to "all input" if the name is not found.
        private static InputUsageMask ClickMask =>
            Enum.TryParse("MouseButtons", out InputUsageMask mask) ? mask : InputUsageMask.All;

        /// <summary>Called every frame: closes the panel once its menu is gone.</summary>
        public static void Tick()
        {
            if (_layer == null) return;

            if (ScreenManager.TopScreen != _screen)
            {
                // Another screen is on top (battle, main menu). Take the layer off the map screen so it doesn't linger
                // there; if that screen is gone, Hide just forgets it.
                Hide();
                return;
            }

            if (Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId == _menuId)
            {
                _mismatchTicks = 0;
                return;
            }

            // A short grace period covers the moment the menu is still switching.
            if (++_mismatchTicks > GraceTicks) Hide();
        }
    }
}
