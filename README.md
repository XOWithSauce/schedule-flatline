# Schedule I Flatline Mod

## Features

- Permanent Death: You have only 1 life. After you die and return to menu your save will be **permanently deleted**!
- Survival system: Keep your food, water and temperature levels high enough or you will perish!
- Diseases: Every choice has a consequence. Consuming drugs makes you depressed. Living in the sewers gives you cancer. Cure and manage your diseases or you will perish!
- Temperature system: World temperature rises and falls throughout the day. Keeping your property doors open causes them to become colder. Buy AC Units to keep your properties warm and dress up in warmer clothing to prevent dying from hypothermia!
- Bed Rotting: You can now lay in the bed while you scroll through your phone. That's it.
- Fall damage and first person ragdolling
- Customized drug effects and bad side effects
- Custom WAV file audios and PNG Icons

## Installation

> If you are using Thunderstore Mod Manager you can skip these steps and just install the mod through the manager and it will work.

### Manual installation: 

1. Install **Melon Loader** from a trusted source like the official [MelonWiki](https://melonwiki.xyz/) and follow their setup instructions.
2. Download the **IL2CPP** version if you are using the default game backend. If you have opted into alternate or alternate-beta, download **MONO**.
3. Unzip the downloaded folder, here you will find the **Mods** folder containing the mod .dll file and **UserData** folder containing the mod data folder
4. Copy the contents **Mods** folder into the **Steam/steamapps/common/Schedule I/Mods** folder
5. Copy the contents of **UserData** folder into the **Steam/steamapps/common/Schedule I/UserData** folder


## Configuration

After starting the game with the mod enabled, the MelonPreferences configuration file will write a new category which you can use to modify basic mod settings.

The Flatline mod settings can be configured from the **Steam/steamapps/common/Schedule I/UserData/MelonPreferences.cfg** file:

- Open the file and Press CTRL + F to find `Flatline_XOWithSauce`

There are following configurations available through the file:

- **PermanentDeath**
    - **true** (default): Disable respawning and after death you can only return to menu, **after which the last played save is permanently deleted!**.
    - **false**: Enable respawning (multiplayer) OR Loading last save (singleplayer).

- **DrugSideEffects**
    - **true** (default): Enables overdosing on drugs and medicine and adds effects to drugs.
    - **false**: Disables the effects of consuming drugs and medications.

- **PropertyTemperatureChanges**
    - **true** (default): Properties get cold if door is kept open and outside is colder.
    - **false**: Property temperatures do not change.

- **WorldTemperatureChanges**
    - **true** (default): World temperature changes based on time and weather.
    - **false**: World temperature stays at 20 celsius.

- **FahrenheitTemp**
    - **true**: Display temperatures as Fahrenheit.
    - **false** (default): Display temperatures as Celsius.

- **DiseasesEnabled**
    - **true** (default): Enables all diseases.
    - **false**: Disables all diseases.

- **BleedingEnabled**
    - **true** (default): Enable Bleeding disease.
    - **false**: Disable Bleeding disease.

- **BoneBreakEnabled**
    - **true** (default): Enable Bone Break disease.
    - **false**: Disable Bone Break disease.

- **CancerEnabled**
    - **true** (default): Enable Cancer disease.
    - **false**: Disable Cancer disease.

- **DepressionEnabled**
    - **true** (default): Enable Depression disease.
    - **false**: Disable Depression disease.

- **FeverEnabled**
    - **true** (default): Enable Fever disease.
    - **false**: Disable Fever disease.

- **WaterRequired**
    - **true** (default): Player needs to drink to survive.
    - **false**: Player does not need to drink to survive.

- **FoodRequired**
    - **true** (default): Player needs to eat to survive.
    - **false**: Player does not need to eat to survive.

- **EnergyRequired**
    - **true** (default): Player needs to rest and manage energy.
    - **false**: Player does not need to rest or manage energy.

- **TemperatureRequired**
    - **true** (default): Player needs to stay warm to survive.
    - **false**: Player does not need to manage temperature to survive.

- **WaterConsumption**
    - **0.00087958** (default): Amount of water consumed per minute.

- **FoodConsumption**
    - **0.0015** (default): Amount of food consumed per minute.

- **EnergyConsumption**
    - **0.0007** (default): Amount of energy consumed per minute.

- **TemperatureConsumption**
    - **0.00022** (default): Amount of temperature lost per each degree difference.
---

>    - 


---
## Survival system

Battle against depleting water, food and energy levels while maintaining sufficient body temperature.


### Water
<img src="https://i.imgur.com/QAykR37.png">

> Drinking Cuke, Energy drinks or Tap water increases your water levels!

### Food
<img src="https://i.imgur.com/vnErE7T.png">

> Consume food like Bananas or Donuts to stay satiated!

### Energy
<img src="https://i.imgur.com/MVQUT6j.png">

> Rot in the bed, consume beverages or sleep through the night to reset your energy levels! Running out of energy causes you to pass out for multiple hours. If you're not inside your property you will get robbed!

### Temperature
<img src="https://i.imgur.com/Pr2CAMM.png">

> Your body temperature changes based on the temperature of your local environment and current clothing. Keep your property doors closed on colder days and dress up in warm clothing to stay alive!

---

>    - 

---
## Diseases

All diseases come with varying severity and effects. Each of the diseases can be cured for a hefty price by visiting the charge nurse at the Hospital! Some of the diseases heal naturally on their own.

### Cancer
<img src="https://i.imgur.com/0NWymFd.png">

> Based on your predisposition and other health statistics you might develop Cancer!

- Curing cancer costs $80,000. Cancer cannot be cured at a terminal stage.
- Cancer will be lethal if not cured at the hospital within 3-5 in-game weeks based on the severity
- At later stages cancer starts to lower the Max HP amount


### Bone Break
<img src="https://i.imgur.com/JiJQvYl.png">

> Falling down from higher elevation or taking impact damage has a chance to break your bones!

- Fixing broken bones costs $18,000
- Broken bones in the legs will cause you to occasionally stumble down while running and prevent jumping
- Broken bones will result in slower movement speed
- Sprinting causes the disease to heal slower while rotting in the bed causes the healing to speed up


### Bleeding
<img src="https://i.imgur.com/Z8v3LKb.png">

> Taking damage from slashing or piercing objects can cause you to bleed profusely!

- Curing a bleed costs $30,000
- Stem the bleeding by looking down and holding the interact button to prevent bleeding out and heal the disease
- Lower severity bleed will heal on its own, while more severe bleeding will kill you quickly


### Fever
<img src="https://i.imgur.com/jU6R0Th.png">

> Seasonal flu, man flu, whatever you call it, you can now contract it! Occurance of fever is decided by many factors like predisposition and body temperature!

- Curing a fever costs $8,000
- Rot in the bed or consume some medicine to speed up the heal progress
- Fever will cause your energy, food and water levels to deplete more quickly overtime
- Fever will also cause your Max HP to lower overtime if not healed quickly enough


### Depression
<img src="https://i.imgur.com/V0xknI5.png">

> It's not all sunshine and rainbows... Consuming drugs, being too poor, being too hungry and just about anything negative will make you depressed. Touch some grass and don't take drugs to keep your mind at ease.

- Curing depression costs $4,000
- Rot in the bed and do nothing to heal depression quicker
- You can consume drugs to get rid of the effects of Depression, but only temporarily
- Depression will cause you to not be able to go outside or answer messages on your phone

---

>    - 

---

## Drug Side Effects

### Meth and Cocaine
> Consuming large doses causes you to tweak and change hotbar slots. Consuming too much will cause an overdose.

### Shrooms
> The visuals will be amplified to extreme based on dosage. Consuming too much will cause an overdose.

### Weed

> Causes you to become more hungry and thirsty.

---

## Ingestible Items Effects
| Category | Item ID | Food | Energy | Thirst | HP Regen | Special Effects |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Food** | `banana` | 18 | 1.5 | 0 | 2 | - |
| **Food** | `chili` | 5 | 3 | 0 | 5 | Toxicity: 0.05 |
| **Food** | `megabean` | 8 | 10 | 0 | 3 | - |
| **Food** | `donut` | 15 | 2 | 0 | 1 | - |
| **Drink** | `cuke` | 0 | 1.5 | 14 | 3 | - |
| **Drink** | `energydrink` | 0 | 5 | 10 | 1 | + Sanity |
| **Medicine** | `flumedicine` | 0 | 0 | 5 | 3 | Heals Illness |
| **Medicine** | `paracetamol` | 0 | 0 | 0 | 8 | Heals Illness, Toxicity: 0.17 |
| **Medicine** | `addy` | 0 | 18 | 0 | 1 | + Sanity, Toxicity: 0.22 |


---

>    - 

---


### Console support

The Flatline mod supports using in-game console commands to change the mod events and state.

`flatline help` - Show all available commands, command targets, target members and command usage info.

`flatline stop` - Stop the mod from updating events and state.

`flatline start` - If stopped, start the mod update again.

`flatline enable logs` - Enable all debug logs.

---
#### List Command Examples
> Use the list command to display current states and changeable parameters

`flatline list player` - Print into MelonLoader Console the current values and state for local player

`flatline list disease` - Print into MelonLoader Console the current diseases values and state for local player

`flatline list consumption` - Print into MelonLoader Console the current ingested items and drugs amounts

---
#### Set Command Examples

*Player Set Command examples*

`flatline set player energy 1.0` - Replenishes energy to full (at 0.0 you pass out)

`flatline set player thirst 1.0` - Replenishes your water levels to full (at 0.0 you die)

`flatline set player hunger 1.0` - Replenishes food levels to full (at 0.0 you die)

`flatline set player temperature 1.0` - Replenishes food levels to full (at 0.0 you die)

*Disease Set Command examples*

`flatline set disease fever healstate 1.0` - Completely heals fever after which it will deactivate

`flatline set disease cancer severity 0.15` - Sets the cancer severity (range 0.0 - 0.3)

`flatline set disease bleed progression 4` - Sets the bleed disease progression to 4 (at 5 you will die, range 1-5)

---
#### Add Command Examples

`flatline add disease fever` - Adds a new fever disease instantly

`flatline add disease bonebreak` - Adds a new bone break disease instantly

`flatline add disease cancer` - Adds a new cancer disease instantly

`flatline add disease depression` - Adds a new depression disease instantly

`flatline add disease bleed` - Adds a new bleeding disease instantly


---

>    - 

---


## Save Data

The Flatline mod will save mod related data into one of the following folders based on the install type (manual or mod manager) and mod version:

Thunderstore Mod Manager:

`UserData/XO_WithSauce-Flatline_MONO/XO_WithSauce-Flatline/PlayerData/(name).json`

OR 

`UserData/XO_WithSauce-Flatline_IL2CPP/XO_WithSauce-Flatline/PlayerData/(name).json`

Manual installs (MONO and IL2CPP use the same):

`UserData/XO_WithSauce-Flatline/PlayerData/(name).json`


The save data .json file is named for example "2_factory.json" if your save file slot is 2 and save name is Factory

The save data consists of the players current state, the active diseases states, and the consumption data of drugs and foods all in readable format which can be modified by editing the file.

## Images and Audios data

The Flatline mod will load mod related images and audios from one of the following folders based on mod version:

Thunderstore Mod Manager:

`UserData/XO_WithSauce-Flatline_MONO/XO_WithSauce-Flatline/ModResources`

OR 

`UserData/XO_WithSauce-Flatline_IL2CPP/XO_WithSauce-Flatline/ModResources`

Manual installs (MONO and IL2CPP use the same):

`UserData/XO_WithSauce-Flatline/ModResources`

The Mod Resources directory contains 2 folders, one for audios and one for images. 

Audios are from Pixabay and the audio creators are credited accordingly in the metadata with links to their pages.

Images are customized and edited from icons downloaded from Flaticon and Icons8. See `ModResources/Images/CREDITS.txt` for full legal disclaimer, license usage and creator credits.


The mod comes with an [audio loader (on GitHub)](https://github.com/XOWithSauce/schedule-flatline/blob/main/Source/Config/AudioLoader.cs) which is a stripped copy of the [deadlyfingers UnityWav project on GitHub](https://github.com/deadlyfingers/UnityWav)


---

>    - 

---


### Contribute, Build from Source or Verify Integrity -> [GitHub](https://github.com/XOWithSauce/schedule-flatline/)