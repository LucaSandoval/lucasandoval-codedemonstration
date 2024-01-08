# 2D RPG Combat System Code Snippet taken from "A Song of Sunlight"

Link to [STEAM PAGE](https://store.steampowered.com/app/2263250/A_Song_Of_Sunlight/?curator_clanid=42575477).

The following is a small portion of the code that handles the combat system in my 2D RPG "A Song of Sunlight." I have chosen the following files to demonstrate 
an example of Object Oriented Design in the game code. The game is side-scrolling RPG, and thus much of the game ocurrs on flat planes. Given this, I wanted to 
design a highly reusable component "AbstractMovable.cs" that any and all movable entities could use in the game world. This was used for NPCs in the overworld 
as well as the combat system, which is displayed here. 
