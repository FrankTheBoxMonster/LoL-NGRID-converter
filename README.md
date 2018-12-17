# LoL-NGRID-converter

This tool can convert .AIMESH_NGRID files into .BMP files in order to expose navgrid cell flags.

[Download links](https://github.com/FrankTheBoxMonster/LoL-NGRID-converter/releases/tag/v1.0)


### General explanation of .AIMESH_NGRID files

Essentially, the game sees the map as a giant chess board, made up of several tiles or cells.  On Summoner's Rift, this chess board is 295 cells along the X axis and 296 cells along the Z axis (Riot uses a Y-up axis), and each of these cells represents a 50-unit square.  Pathfinding is a relatively expensive operation, and pathfinding algorithms work based on nodes.  To improve performance, many pathfinding systems break up the navigable area into nodes, but by instead thinking of these nodes as the center points of cells, you are able to define different subregions that represent different properties about where a character is currently standing, which allows the game to know things such as "this character is currently in the river" or "the closest lane to this location would be top lane".

Vision checks adapt this same logic, but instead of trying to find a way to get around walls in order to get to a location, it's checking if it *would* have to get around walls, because if a wall is in the way then you cannot see a unit standing in that location.  It also add rules for things such as "you can see out of a brush, but not into a brush, but you also share vision with any allied unit that is currently in that brush, such as a ward".


### Generated files

The conversion will generate 9 files, each representing a different navgrid cell flag layer, described below.  Note that these descriptions are only accurate for Summoner's Rift, and some maps will use the layers differently.

Many maps, especially older maps, will generate single-color images for some layers, either due to those layers being unused or not existing at the time.

Also note that only VisionPathing.bmp has any consistent meaning assigned to each color value, with all other layers just using whatever the next available color was for that flag value.


### VisionPathing.bmp

![VisionPathing.bmp](https://raw.githubusercontent.com/FrankTheBoxMonster/LoL-NGRID-converter/master/test%20files/SummonersRift/SummonersRift.visionPathing.bmp)

These flags control walls, brush, walkable areas, team-specific walls, etc.  Pathing rules are directly linked with vision rules.

Most of the colors are self-explanatory, but here's some rules for the ones that are not:

 * The cyan color that represents structures and occurs along wall diagonal edges, as well as the sea green color that occurs where brush borders a wall is a "transparent wall".  You can see through it, but cannot walk through it.  This was added to the diagonal walls shortly after Season 5 Worlds, where some line of sight oddities with regards to these diagonals surfaced.  The structure walls were added some time later after it was discovered that you could hide behind them.
 * The yellow color (seen on the Crystal Scar, Odyssey, and Project maps, not used on Summoner's Rift) marks areas that are "always visible" to both teams.
 * The base gates for each team are made up of two colors.  Both colors represent areas that only one team can walk on, but the inner color additionally represents an area where you can see into and out of the area, but not *through* the area (if two opposing units stand on opposite sides of a base gate, they can't see each other until one of them moves into the base gate).  On some maps, the entire base gate has this property, creating a single-color base gate, but on Summoner's Rift both sides of the base gate have a small strip without this property.


### RiverRegions.bmp

![RiverRegions.bmp](https://raw.githubusercontent.com/FrankTheBoxMonster/LoL-NGRID-converter/master/test%20files/SummonersRift/SummonersRift.riverRegions.bmp)

These flags roughly define different parts of the river.  Specifically, it defines jungle vs non-jungle, the main river area, river entrances, and Baron pit (but not Dragon pit).

These flags are rather sloppy and not as clean as some of the other layers.  They overall appear to be unused and superceded by combining checks for other flag layers (for example, Hunter's Talisman passive ignores the flags found in this file and instead uses the jungle quadrant markers found in MainRegions.bmp, which is proven particularly at locations where the jungle borders the lanes).


### JungleQuadrants.bmp

![JungleQuadrants.bmp](https://raw.githubusercontent.com/FrankTheBoxMonster/LoL-NGRID-converter/master/test%20files/SummonersRift/SummonersRift.jungleQuadrants.bmp)

Similar to RiverRegions.bmp, these flags are rather sloppy (specifically note the south quadrant bleeding into mid lane) and appear to go unused in favor of combinations of other flag checks (you can check which jungle quadrant you are in by instead using MainRegions.bmp for getting top side vs bot side and Rings.bmp or NearestLane.bmp for getting red side vs blue side).


### MainRegions.bmp

![MainRegions.bmp](https://raw.githubusercontent.com/FrankTheBoxMonster/LoL-NGRID-converter/master/test%20files/SummonersRift/SummonersRift.mainRegions.bmp)

Pretty self-explanatory.  Note that the Odyssey map repurposed this layer to mark each of its different zones (this wasn't done on the Star Guardian map), the Project map used this layer to mark each "always visible" area with its own flag, and Twisted Treeline lacks a mid lane and defines Vilemaw pit as top-side jungle and the rest of the jungle as bot-side jungle.

Examples of this being used include:

 * Hunter's Talisman's extra mana regen while in jungle or river
 * Staff of Flowing Water
 * Waterwalking rune
 * Pyke's +1 movespeed while in river
 * Yorick Mist Walkers/Maiden knowing if they are in a lane or not to start pushing
 * Corki Package's bonus speed falling off outside of the base area


### NearestLane.bmp

![NearestLane.bmp](https://raw.githubusercontent.com/FrankTheBoxMonster/LoL-NGRID-converter/master/test%20files/SummonersRift/SummonersRift.nearestLane.bmp)

Also pretty self-explanatory.  Note that each area is distinct, so "blue top side neutral zone" doesn't count as "blue top lane".  You'd have to check all four top-side flags in order to definitively say if your closest lane would be top lane or not.  Also note that the Project map reused this layer to separate each of contiguous "lane".

Examples of this being used include pathing for Zz'rot minions and Rift Herald.


### POI.bmp

![POI.bmp](https://raw.githubusercontent.com/FrankTheBoxMonster/LoL-NGRID-converter/master/test%20files/SummonersRift/SummonersRift.POI.bmp)

**P**oints **O**f **I**nterest include base gates, turrets, and monster camps.

Examples of this being used include the Point Runner passive.  Not sure if there is a use for jungle camp POI flags, since the only thing that would really make sense is for monster patience, but their patience doesn't start to fall off until a decent bit outside of the POI area (blue-side Red Buff's POI area stops shortly in front of its spawn position but it doesn't start losing patience until it gets between the Krug wall and the brush going towards blue-side base, and the distance isn't consistent across any camps, so it seems like patience triggers using a per-camp distance value instead).


### Rings.bmp

![Rings.bmp](https://raw.githubusercontent.com/FrankTheBoxMonster/LoL-NGRID-converter/master/test%20files/SummonersRift/SummonersRift.rings.bmp)

There's probably a better name for these but this is just what I call them.  Examples of this being used include the Homestart bonus falling off once you cross your outer turret (and previously fell off once you crossed the middle of the map).
 
The different rings correspond to these areas (note that each ring stops at the outer edge of the physical turret object, not the turret's attack range):
 
 * Fountain to the outer edge of the nexus turrets
 * Nexus turrets to inhib turrets (stops shortly inside of your base walls)
 * Inhib turrets to inner turrets
 * Inner turrets to outer turrets
 * Outer turrets to the middle of the map


### HeightSamples.bmp

![HeightSamples.bmp](https://raw.githubusercontent.com/FrankTheBoxMonster/LoL-NGRID-converter/master/test%20files/SummonersRift/SummonersRift.heightSamples.bmp)

Represents the ground height of the map.  Darker values are low ground, brighter values are high ground.  Some projectiles (mainly globals) are set to follow this ground height (if you shoot a Jinx ult over the walls for Baron or Dragon pit then this becomes more noticeable).

You can use Kayn E on the few spots on Summoner's Rift where you see pure black and see that he will dip pretty far down (specifically near blue side bot inhib turret and the one pixel in the wall just above it).  While using his E, Kayn will always be rendered on top of the map mesh, so there's no visible clipping, but you can still see him move disproportionately with the rest of the map.


### .LSGNGRID file

This is the last of the files generated by the converter.  It's just an intermediary file format that I use for other stuff, and cuts out roughly 80% of the original .AIMESH_NGRID file that isn't useful for anything.  It's not meant to be readable, but the format is in the source code.  Unless for some reason you want to make a program to read it then you can completely ignore this file since it doesn't have any information that the other files don't have already.