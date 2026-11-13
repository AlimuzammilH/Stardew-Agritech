# Changelog

## 0.4.3
- Balance: an animal must have been an adult for a full season (28 days) before the Butcher's Tag works, and meat cuts grow with its time as an adult (5 after one season, up to 7). Buying calves and retiring them days later no longer prints money.
- Balance: breed prices brought in line with vanilla payback times: Merino and Angora 12,000g, Cashmere Goat 10,000g, Wagyu 8,000g.
- Balance: the top skill levels need less XP (level 10 at 6,800 XP instead of 15,000), so Hub tier 4 and the Estate/Couture grades arrive in year two instead of year three.
- `agritech_selftest run`: runs every feature once in the real game and reports PASS/FAIL per check.
- Balance diary: one line per in-game day in `balance/<save>.csv`.

## 0.4.2
- Herd horses are now generated at load time from the game's own horse sprite, so the mod ships no game art.

## 0.4.1
- Fibre from other mods is matched by item ID, not by name: llama wool no longer grades as alpaca, and Mizu's meat, dishes and yarn and Cloths and Colors' silkworms are no longer accepted by the Grading Table. Same fix for the herd bonus.
- Herding no longer pushes animals through fences when there's no path into the zone.
- Docked drones are drawn from a cached dock list instead of scanning every location each frame.
- Console commands `agritech` (show skill) and `agritech_xp <amount>`.

## 0.4.0
- Horse herd as barn livestock (bay, black, grey) with foals; Oat Feed, grooming, Saddle training (trained horses sell for double).
- Drying Rack step before tanning; sensor posts change look with the Hub tier; Cold Warehouse building; Wagyu Steak, Beef Stew and Wagyu Tasting as cooking recipes ordered by the restaurant buyers.
- Green-screen terminal and corkboard contract board.

## 0.3.1
- Cloths and Colors link: its spinner and loom take Merino wool, raw cashmere and raw angora; its Sewing skill boosts fibre grading and grading earns Sewing XP.

## 0.3.0
- Item tooltips show provenance and freshness; real herding with pathfinding; tractor fuel; hand delivery to Gus and Emily.

## 0.2.0
- First complete version: Hub and skill, sensors, collars, breeds, leather and Wagyu chains, grading with provenance, contracts, freight, waste-to-fuel, drones, horses and visitors.
