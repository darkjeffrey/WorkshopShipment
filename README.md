# Workshop Shipments

A Mount & Blade II: Bannerlord mod (v1.4.8) that lets you send goods from any town to one of your workshops. You load the cargo, pick the workshop town and pay a carter. A guarded caravan then carries the goods across the map and puts them into your workshop warehouse there. The shipment travels like a trade caravan but never trades your goods on the way. Bandits and anyone at war with you can still attack it and take the cargo.

## How it works

- In any town, choose **Arrange a shipment to one of your workshops** in the town menu. It shows up once you own a workshop, and is only clickable when you own one in another town that isn't at war with you.
- **Load the cargo** opens a stash screen. Move whatever you want to send from your inventory to the shipment side; buy goods at the market first if you need to.
- **Choose the destination** lists your workshop towns with how long the trip takes (in days and hours). Hover over a town to see how much room is left in its warehouse.
- **Choose the escort** picks one of three escorts: Basic guard (12 caravan guards, no extra cost), Armed escort (25 men, half of them trained soldiers) or Veteran escort (40 men, mostly veteran elite troops). Bigger escorts cost more and make the caravan a harder target.
- **Send the shipment** charges the fee (base fee, plus weight times distance, plus the escort's extra cost) and the caravan sets out from the town gate.
- **Check shipments on the road** opens a panel beside the menu with a card for every shipment that is travelling: a picture of its biggest cargo item, where it started and where it is going, what it carries (the three biggest stacks, item count and value), when it should arrive, its escort and how many men are left, and its status (on the road, under attack, or going to sell the cargo). With the "Show where shipments are" option on, each card also says which settlement the shipment is near. The panel shows 4 cards at a time; use Scroll up and Scroll down for the rest.
- When it arrives, the cargo goes into that town's warehouse. Anything that doesn't fit is sold at that town's market and you get the money.
- If the destination stops being valid on the way (war, or you lost the workshop), the caravan heads to your nearest other warehouse. If there isn't one, it goes to the nearest friendly town and sells the cargo there.
- If the caravan is defeated, the cargo is lost to the attackers and you get a message.
- The escorts don't eat the food in the cargo.
- Travel times start as an estimate and get more accurate as your shipments travel, because the mod measures how fast they really move.
- Shipments are saved with the game. Cargo you loaded but didn't send goes back to your inventory when you leave the menu or the town.

## Options (Mod Options, MCM)

| Option | Default | What it does |
|---|---|---|
| Enable | On | Turns the mod on or off. |
| Base fee (denars) | 50 | What every shipment costs before weight and distance. |
| Fee per weight unit per 100 distance | 0.30 | Added for each unit of cargo weight, per 100 units of map distance. |
| Shipments on the road at once | 5 | How many of your shipments can travel at the same time. |
| Show where shipments are | Off | Adds the settlement each shipment is near to the cards in "Check shipments on the road". |
| Basic guard: men | 12 | Caravan guards in the cheapest escort, plus one caravan master. |
| Armed escort: men | 25 | Men in the middle escort, plus one caravan master. |
| Armed escort: extra cost | 300 | Added to the cost for the armed escort. |
| Veteran escort: men | 40 | Men in the strongest escort, plus one caravan master. |
| Veteran escort: extra cost | 900 | Added to the cost for the veteran escort. |
| Debug messages | Off | Shows chat messages about what the mod is doing, for testing. |

## Requirements

Bannerlord.Harmony, ButterLib, UIExtenderEx and Mod Configuration Menu v5. Load it after them.

## Building

With the `build.bat` in the `source` folder, run `build.bat repos\WorkshopShipments\src\WorkshopShipments.csproj` (or set its PROJECT line to that path). Without it, run `dotnet build -c Release` from the `src` folder. The build copies the dll, `SubModule.xml`, `ModuleData` and `GUI` into `Modules\WorkshopShipments` in the game folder. If the game is installed somewhere else, pass `/p:GameFolder="..."`.

## Translations

All English text is in the code as `{=id}English text`. `Module/ModuleData/Languages/std_WorkshopShipments.xml` lists every string and is the template for translations.

To add a language, for example German, run `python BannerlordModTools/bltools.py loc add-language WorkshopShipments DE` from the `repos` folder. It creates `Module/ModuleData/Languages/DE/` with `language_data.xml` and a translation file where every line is marked NEW; translate each `text` attribute, keeping the ids and every `{VARIABLE}` unchanged. Running `loc update` after the code changes marks new, changed and obsolete lines.

## Support

If you enjoy the mod, you can support me on [Ko-fi](https://ko-fi.com/darkjeffrey).
