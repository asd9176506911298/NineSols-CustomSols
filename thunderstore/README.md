# Nine Sols Custom Sols

# Custom Sprite
- Player
- Parry
- Arrow
- Sword
- Foo
- UI
- TalismanBall
- Air Dash
- Dash
- Menu Logo
- YingZhao
- Other If you know SpriteName Put into any folder
- Dialogue Atlas

# Preview
![](https://github.com/asd9176506911298/NineSols-CustomSols/blob/Optimized/Source/img/CustomSolsPreview.png?raw=true)
![](https://github.com/asd9176506911298/NineSols-CustomSols/blob/MoreUISprite/Source/img/UIPreview.png?raw=true)

# Open Custom Sols Folder
- F1 have a checkbox can open folder

# Create Your Skin
- Copy Paste Default Folder and Rename Folder into correspond Folder put edited image
- ![](https://github.com/asd9176506911298/NineSols-CustomSols/blob/ConfigManagerOption/Source/img/CreateSkinFolder.png?raw=true)
 
# How to Custom Sprite
- Change Image
- Put changed image to correspond Folder
- ![](https://github.com/asd9176506911298/NineSols-CustomSols/blob/ConfigManagerOption/Source/img/LogoExample.png?raw=true)
- Ctrl + H Reload

# How to Custom Player Sprite
- F1 Toast you want Sprite Name
- https://drive.google.com/drive/folders/102UGxf7OyI4CTQCI0H8iiOCntImi7jFD
- Download Origin Sprite Sprite include Player Sprite
- Put Changed Player Sprite into Player Folder
- ![](https://github.com/asd9176506911298/NineSols-CustomSols/blob/ConfigManagerOption/Source/img/PlayerExample.png?raw=true)
- Ctrl + H Reload

# How to replace Player All Sprite 
- put your image into folder PlayerSpriteAllUseThis
- and name 1 2 3 4 5....
- ![](https://github.com/asd9176506911298/NineSols-CustomSols/blob/PlayerAllSpriteUseThis/Source/img/AllUse.png?raw=true)

# Dialogue Atlas
- Atlas Folder
- Toast Dialogue will toast Portrait_{name}
- Portrait_Yee
- ![](https://github.com/asd9176506911298/NineSols-CustomSols/blob/main/Source/img/Dialogue.png?raw=true)

# SkinConfig.json Color
if you don't want change color use "NormalHpColor": "#"  
"NormalHpColor": "#FF926EFF" Hex is RGBA Red 255, Green 146, Blue 110, Alpha 255
```
{
  "Colors": {
    "NormalHpColor": "#",
    "InternalHpColor": "#",
    "ExpRingOuterColor": "#",
    "ExpRingInnerColor": "#",
    "RageBarColor": "#",
    "RageBarFrameColor": "#",
    "ArrowLineBColor": "#",
    "ArrowGlowColor": "#",
    "ChiBallLeftLineColor": "#",
    "ButterflyRightLineColor": "#",
    "CoreCColor": "#",
    "CoreDColor": "#",    
    "DashColor": "#",
    "PerfectParryColor": "#",
    "ImperfectParryColor": "#",
    "SwordCharingCirlceColor": "#",
    "SwordCharingAbsorbColor": "#",
    "SwordCharingGlowColor": "#",
    "ParticlesFooColor": "#",
    "FooLightColor": "#",
    "DrawFooBallColor1": "#",
    "DrawFooBallColor2": "#",
    "DrawFooBallColor3": "#",
    "DrawFooBallColor4": "#",
    "DrawFooBallColor5": "#",
    "DrawFooLightColor": "#",
    "DrawFooBottomLightColor": "#"
  },
  "Parry": {
    "UCCharging1Color": "#",
    "UCCharging2Color": "#",
    "UCSuccess1Color": "#",
    "UCSuccess2Color": "#",
    "AirParryColor": "#",
    "UCParryColor": "#"
  },
  "Bow": {
    "NormalArrowLv1": [-15.0, 0.0, 0.0],
    "NormalArrowLv2": [-15.0, 0.0, 0.0],
    "NormalArrowLv3": [-15.0, 0.0, 0.0]
  }
}
```
![](https://github.com/asd9176506911298/NineSols-CustomSols/blob/main/Source/img/UCParryColor.png?raw=true)
![](https://github.com/asd9176506911298/NineSols-CustomSols/blob/main/Source/img/SwordCharingAbsorbColor.png?raw=true)
![](https://github.com/asd9176506911298/NineSols-CustomSols/blob/main/Source/img/SwrodCharingCircleAndGlow.png?raw=true)
![](https://github.com/asd9176506911298/NineSols-CustomSols/blob/main/Source/img/DrawFooBall.png?raw=true)
![](https://github.com/asd9176506911298/NineSols-CustomSols/blob/main/Source/img/DrawFooLight.png?raw=true)
![](https://github.com/asd9176506911298/NineSols-CustomSols/blob/main/Source/img/FooLight.png?raw=true)
![](https://github.com/asd9176506911298/NineSols-CustomSols/blob/main/Source/img/PerfectParry.png?raw=true)
![](https://github.com/asd9176506911298/NineSols-CustomSols/blob/main/Source/img/ImperfectParry.png?raw=true)


# Normal Arrow Light Adjust Position
![](https://github.com/asd9176506911298/NineSols-CustomSols/blob/main/Source/img/BowPosition.png?raw=true)
default is -15.0, 0.0, 0.0
```
{
  "NormalArrowLv1": [-15.0, 0.0, 0.0],
  "NormalArrowLv2": [-15.0, 0.0, 0.0],
  "NormalArrowLv3": [-15.0, 0.0, 0.0]
}
```

# Arrow not work problem
- reload will work
- cause when you apply skin object haven't created

# Mod UnNeed Sprite
- When Mod Update some UnNeed Sprite Sprite Still inside folder
- If you want latest clean folder
- remove `BepInEx\config\CustomSols` this folder ReInstall will get latest clean folder

# Notice
- If you use PlayerSpriteAllUseThis folder your Player will not work only PlayerSpriteAllUseThis work 

# Origin Sprite Example Skin
- https://drive.google.com/drive/folders/102UGxf7OyI4CTQCI0H8iiOCntImi7jFD
- YingZhao
- https://drive.google.com/file/d/1ruhRussKrtjLR8uJol32EkoVr34S_AOI

# If not found want sprite
- use Asset Studio
- https://github.com/asd9176506911298/StudioDev/releases/latest
- ask in Modding Discord
- https://discord.gg/NYT4vQpweS