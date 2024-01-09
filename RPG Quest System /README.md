# RPG Quest System Code Snippet taken from "A Song of Sunlight"

Link to [STEAM PAGE](https://store.steampowered.com/app/2263250/A_Song_Of_Sunlight/?curator_clanid=42575477).

The following is a small portion of the code that manages the Quest System in my current RPG progect. I wanted the system to be as flexible as possible, and so ease of creation and editing of
quest data was emphasized. All Quests are stored as ScriptableObjects, which made editing their parameters very time efficient. The main controller is a singleton that provides functions for other
components to reference, such as the giving of quests and checking of their statuses. 
