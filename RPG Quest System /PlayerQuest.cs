using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// An object representing the in-game data version of a quest, created and managed
// at run time to store quest progress. 

[System.Serializable]
public class PlayerQuest
{
    public QuestData data; // Data pertaining to the quest.
    public int stepID; // What step is it on? 

    public PlayerQuest(QuestData data, int id)
    {
        this.data = data;
        this.stepID = id;
    }
}
