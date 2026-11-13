# Agritech v0.4.5

An agritech layer for Stardew Valley 1.6: tech, breeds, leather and meat chains, luxury grading with provenance, contracts with fashion and restaurant houses, freight, waste-to-fuel, drones, horse husbandry and farm visitors. One mod, no hard dependencies.

## Keys (change them in config.json)
F7 contract board · F8 farm data terminal · F9 heat-map overlay · F10 draw a fence zone · F11 cycle fence zones. Clicking the Hub opens the terminal; clicking the Freight Depot opens the board.

## Agritech skill
Levels 0-10, earned by collaring (25 xp), grading, retiring animals, grooming, drone work, hosting buyers and delivering contracts.

| Level | Unlocks |
| --- | --- |
| 0 | Sensor Post, Smart Collar, Butcher's Tag, Grooming Brush; Merino sheep at Marnie's (level 1) |
| 2 | Grading Table, Oat Feed; Cashmere Goat, Angora Rabbit and the herd horses (bay, black, grey) at Marnie's |
| 3 | Tanning Vat, Ageing Locker, Drying Rack, Saddle; Wagyu Steak and Beef Stew (cooking); Wagyu at Marnie's; Hub tier 2; restaurant houses start trading |
| 4 | Cold Store; Freight Depot and Cold Warehouse at Robin's; fashion houses start trading (with Hub tier 2) |
| 5 | Fischer-Tropsch Unit, Fuel Cell; Wagyu Tasting (cooking) |
| 6 | Drone Dock and the four drones; Hub tier 3 |
| 9 | Hub tier 4 |

## Hub tiers
1 coverage and locations · 2 soil moisture and nutrients, health and heat cycles, morning alerts · 3 overlay, virtual fences, drones · 4 forecasts, health trends, certified Estate/Couture grades, air freight.

## Chains
- **Fibre**: Merino wool, raw cashmere, raw angora (our breeds, sheared), vanilla wool, plus alpaca and llama wool from Mizu's Alpaca & Llama and cashmere and silk from Cloths and Colors (matched by item ID, with a raw-fibre name fallback for other mods) → Grading Table → Fine / Prime / Estate / Couture lots with a provenance certificate.
- **Grade** = input quality + collared-herd health bonus + skill bonus. Estate and Couture need a tier-4 Hub and a collared herd of that species.
- **Leather**: Butcher's Tag on a cow, Wagyu, buffalo, yak, goat or sheep that has been an adult for a full season (28 days) → Raw Hide (+ beef or Wagyu cuts for cattle: 5 after one season, up to 7 for older, healthy animals) → Drying Rack, 1 day → Dried Hide → Tanning Vat (+1 oak resin) → Tanned Leather → Grading Table.
- **Wagyu**: Wagyu cut (3-day shelf life) → Ageing Locker, 3 days → Dry-Aged Wagyu (6 days) → Grading Table.
- **Kitchen**: Wagyu Steak (Wagyu cut + garlic), Beef Stew (2 beef cuts + potato + tomato), Wagyu Tasting (dry-aged + fresh Wagyu + horseradish). Kōen, The Brass Pheasant and Gus order them.
- **Waste to energy**: barns produce manure each morning → Fischer-Tropsch Unit (manure, fibre, sap or hardwood) → Synthetic Fuel → drones, trucks, or Fuel Cell → Battery Pack.

## Drones (Hub tier 3)
Place a Drone Dock with a chest beside it. Put drones and battery packs or fuel in the chest. Every morning:
Survey (sensor coverage for the whole farm) · Spray (fertilises depleted plots from the chest, waters dry ones) · Herding (enforces virtual fences) · Delivery (ships contracts the chest can fill, free, overnight). You'll see the docked drones hovering over the dock.

## Trade
Contracts are posted on Mondays. Buyers: Maison Vellier (Paris), Casa Ardenti (Milan), Halvorsen & Lund (Stockholm), Kōen and The Brass Pheasant (Zuzu City), Gus and Emily. Relationship grows with on-time deliveries and visits: higher tiers, more orders, then standing orders.
Shipping: Cart 3 days free · Rail 2 days, 2%, needs Freight Depot · Truck 1 day, 1 fuel · Air 1 day, 10%, Hub tier 4. Perishables that arrive past their shelf life pay half; late deliveries pay 70%. A Cold Store keeps perishables in the chests beside it from ageing; the Cold Warehouse (Robin, skill 4) has its own storage where nothing ages.
The fashion and restaurant houses visit on their day each season (8th, 15th, 22nd, 12th, 19th): talk to them to be inspected and open their contracts. Tourists come on weekends and pay for a farm tour.

## Horses
**Herd**: bay, black and grey horses (generated at load time from the game's own horse sprite) live in a barn and breed there like cattle (foals grow up in a week). Oat Feed once a day (+friendship, fills them), the Grooming Brush once a day (+friendship). At 3 hearts an adult takes a Saddle and becomes trained: trained horses sell for double. The terminal lists the herd.
**Your riding horse**: Groom with the brush once a day (bond), ride to train (speed bonus, up to level 5). Two horses with bond 80+ and a tier-2 Hub have a foal every 28 days; build another Stable and it moves in.

## Look
Sensor posts gain probes, a second antenna and a solar panel as the Hub tier rises. The terminal is a green-screen monitor; the contract board is a corkboard of pinned orders.

## Testing
- `agritech` shows your skill level and XP; `agritech_xp <amount>` adds XP.
- `agritech_selftest run` (on a throwaway save: it adds buildings, items, money and XP) drives every feature once and prints PASS/FAIL for each check; results also go to `selftest.txt` in this folder.
- While you play, a balance diary is written to `balance/<save>.csv` in this folder: one line per day with skill progress, where the XP came from, and contract income next to shipping-bin income.

## Build
.NET 6 SDK or newer, then `dotnet build -c Release` in this folder. The build finds your Stardew Valley install automatically (or set `GamePath` in `Agritech.csproj`) and copies the mod into its Mods folder.
