using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Object that stores the data related to an in-game quest, such as its steps,
// the position of key locations in a quest, and the rewards. 

[CreateAssetMenu(fileName = "New Quest", menuName = "Quests/Quest")]
public class QuestData : ScriptableObject
{
    public string internalQuestName; //name used to refrence object internally
    public int externalQuestNameID; //The in-game quest name and step descriptions
    public QuestType type; //determines priority 
    public int timeLimit; //time limit to complete quest (in-game days)

    public QuestStep[] steps; //steps for the quest, ID in class represents current step

    [Header("Reward")]
    public float memoryIronReward;
    public InventoryItem[] itemRewards;
    public int[] itemRewardCounts;
}

// The steps that compose a quest. Might have requirements, such as a specific
// number of items that need to be gathered. 

[System.Serializable]
public struct QuestStep
{
    //If all requirments are left blank, step is considered complete automatically 
    public bool itemsRequired;
    public QuestStepItemRequirment[] itemRequirements;

    [Header("Location Info")]
    public string roomLocation;
    public bool coordinate;
    public Vector2 position;
    public bool npcCoordinate;
    public string npcName;
}

// Details of which item and how many a quest step might require. 

[System.Serializable]
public struct QuestStepItemRequirment
{
    public InventoryItem item;
    public int count;
}

// What type of quest is it?

public enum QuestType
{
    main,
    side
}
