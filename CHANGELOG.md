# 1.0.2
- Compiled the mod for game version 0.4.6f12 and ensured that everything works
- Fixed a bug in the latest game version where if the mod has a coloured dash in the custom dialogue options text, the game would crash when opening dialogue
- Removed logic that allowed bed rotting module to take into account multiplayer preventing multiple players from bed rotting in the same bed, due to Player class no longer containing networking logic for the current bed field.
- Temporarily removed logic that allowed user to change mod audio volumes using game settings menu due to action no longer being just a system action and now being a custom action that warrants further testing and inspecting, hopefully back working in next update.
- Game no longer tracks Players energy by default for the eyelid closing with decreasing energy and mod will not in this version do that eyelid closing anymore until it will implemented back by the mod hopefully by next update.

# 1.0.1
- Compiled the mod for game version 0.4.5f2 and ensured that everything works with latest patch, addressing 2 GH issues.
 
- Added more preferences options that generate in the MelonPreferences.cfg file. These new preferences allow disablement of Diseases, Temperature system, Drugs side effects, and more.
- Added tracking of days spent without a Fever disease to accumulate higher chance overtime 
- Added tracking of minutes when weather is sunny to increase chance of Depression disease whenever the weather hasn't been sunny

- Increased the healing amount of Bleeding disease when the bleeding is being stemmed, from requiring around 3-5 minutes of continuous stemming, down to around 1-2 minutes.
    - Note: In the first mod version bleeding disease was basically a death sentence and felt unsurvivable most of the time
- Decreased the players temperature passive decrease when parasympathetic system is active (after eating to full) by ~60%
- Decreased the players temperature decrease per degree difference by ~60%
- Increased the players temperature increase when staying in warm buildings by ~30%

- Decreased the minimum possible world temperature from +1 celsius to -1 celsius
- Lowered the chance of Fever disease and reworked its chance calculation
- Changed temperature system to account for active weather
- Slightly raised the laying down position of player when they are Bed Rotting to make the pillow 3d model not clip into the camera so often
- Simplified the code that is used for checking if player is inside a warm building (shops, dark market, etc.) since the weather update implements a function in game source that handles same logic
- Fixed a bug in the Cancer disease chance calculation and adjusted the chance calculation logic
- Fixed a bug where the world temperature display in the phone does not update instantly on save load, causing it to display 20 celsius instead of the current temperature, for the first loaded ingame hour