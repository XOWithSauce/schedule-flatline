# 1.0.3
- Ensured mod works in the latest 0.4.7f12 game version, continuing work from the community patch;
- Added logic for the player energy to start closing the eyelids (vignette) when below 15% energy. This feature was broken since 1.0.2 but now the mod provides similiar logic standalone.
- Added logic to allow the game default audio settings to sync with the mod related audio players when changed (all mod audio uses the FX audio volume)
- Changed pass out first person ragdoll to disable HUD during the entire duration
- Changed all of the clothing related IDs to match the latest patch + added a few "warm" clothing options that help with staying warm; long sleeved tie-dyed shirt and leather jacket.
- Changed the hospital related healing logic:
    - Added Jack Knight (doctor) to the npcs who allow to heal player diseases at hospital
    - Removed Irene from the list of npcs who allow to heal player diseases at hospital
    - The charge nurse (Lisa) remains the same so she offers healing options while at the hospital during her shift
- Removed the death screen text animation that changed the text to be "Flatlined" because yes

#### 1.0.2.48
- Merged PR [1.0.2.48 Community Patch by LeonardoDCB on GitHub](https://github.com/XOWithSauce/schedule-flatline/pull/4) with following changes merged to repository pre1.0.3:
    - Fixed a bug with accessing Sleep Controller before it exists on mod load
    - Fixed bugs related to player sleeping state being evaluated incorrectly
    - Fixed bugs with the death screen mod related buttons
    - Fixed a bug caused by changed set time and sleep start function names
    - Fixed a bug where avatar head object was inaccessible in the mod ragdoll logic
    - Added safeguard for scoreboard formatting in the death screen

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