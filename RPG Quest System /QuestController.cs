using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class that handles the functionality of various quests in the game.
// Provides a suite of functions that other components may find relevant in regards to quests,
// such giving quests, stepping/completing quests, checking whether or not a quest is completed etc..

public class QuestController : MonoBehaviour
{
    // In game quest memory 
    public List<PlayerQuest> currentQuests;
    public List<string> completedQuests;

    // Component references 
    private GameObject questListItemPrefab;
    private StatusMenuController statusController;
    private InventoryController inventoryController;
    private InterfaceController interfaceController;
    private RoomController roomController;

    public PlayerQuest targetQuest;
    public QuestDetailsPannel targetQuestScreenInfo;
    public GameObject targetQuestScreenInfoParent;

    public GameObject questNotificationParent;
    private GameObject questNotificationPrefab;

    public QuestLocationArrow questArrow;

    [HideInInspector]
    public bool justCompletedQuest;

    [HideInInspector]
    public bool questLocationGenerated;
    private Vector2 storedQuestLocation;

    public void Awake()
    {
        questListItemPrefab = Resources.Load<GameObject>("UI/quest_prefab");
        questNotificationPrefab = Resources.Load<GameObject>("UI/quest_notification_prefab");
        statusController = GetComponent<StatusMenuController>();
    }

    void Start()
    {
        interfaceController = GetComponent<InterfaceController>();
        inventoryController = GetComponent<InventoryController>();     
        roomController = GetComponent<RoomController>();

        // This is a new game ie. not loaded from save 
        if (!SaveController.loadOnStart)
        {
            currentQuests = new List<PlayerQuest>();
            completedQuests = new List<string>();
            statusController.questList = new List<QuestListMenuOption>();
        }      
    }

    public void Update()
    {
        // Quest is 'active' and displayable, so show HUD info 
        if (canShowTargetQuestPannel())
        {
            targetQuestScreenInfo.UpdateCurrentQuest(targetQuest.data, targetQuest.stepID);
            targetQuestScreenInfoParent.SetActive(true);

            questArrow.targetRoom = roomController.getRoom(targetQuest.data.steps[targetQuest.stepID].roomLocation);
            questArrow.targetLocation = getQuestStepLocation(targetQuest.data, targetQuest.stepID);
            questArrow.gameObject.SetActive(canShowTargetQuestArrow(targetQuest.data.steps[targetQuest.stepID].roomLocation));
        } else
        {
            targetQuestScreenInfoParent.SetActive(false);
            questArrow.gameObject.SetActive(false);
        }
    }

    // Checks if on-screen quest HUD can be displayed 
    private bool canShowTargetQuestPannel()
    {
        return targetQuest.data != null &&
            InventoryController.inventoryOpen == false &&
            StatusMenuController.inStatMenu == false &&
            PauseController.gamePaused == false &&
            DialogueController.inDialogueSection == false &&
            DialogueController.inVNSection == false &&
            CutsceneController.inCutscene == false &&
            CampController.inCamp == false &&
            CombatManagerV2.inCombatV2 == false;
    }

    // Determines where the on-screen guide arrow should go based on a given quest and its step ID. 
    private Vector2 getQuestStepLocation(QuestData data, int step)
    {       
        for (int i = 0; i < data.steps.Length; i++)
        {
            if (i == step)
            {
                Room roomToCheck = roomController.getRoom(data.steps[i].roomLocation);
                //Check if the target position is in the current room
                if (roomToCheck == roomController.getGlobalRoom())
                {
                    if (data.steps[i].coordinate)
                    {
                        storedQuestLocation = data.steps[i].position;
                        return storedQuestLocation;
                    }
                    else if (data.steps[i].npcCoordinate)
                    {
                        //First solution, loops through the children of the room. Bad because it misses children of children and is slow. 
                        //for (int x = 0; x < roomToCheck.transform.childCount; x++)
                        //{
                        //    if (roomToCheck.transform.GetChild(x).name == data.steps[i].npcName)
                        //    {
                        //        storedQuestLocation = roomToCheck.transform.GetChild(x).transform.position + new Vector3(0, 1.5f, 0);
                        //        return storedQuestLocation;
                        //    }
                        //}

                        //Second solution, uses GameObject.Find. Bad because its inefficient, but probably wont have a huge impact
                        //on performance. Also, can't have duplicate named npc's or might bug out. 
                        storedQuestLocation = GameObject.Find(data.steps[i].npcName).transform.position + new Vector3(0, 1.5f, 0);
                        return storedQuestLocation;
                    }
                } else
                {
                    //Potentially demanding algorithm, so just don't run it if its already done for a room 
                    if (questLocationGenerated == false)
                    {
                        questLocationGenerated = true;
                    }
                    else
                    {
                        return storedQuestLocation;
                    }

                    storedQuestLocation = shortestLocationOnPathToTargetRoom(data.steps[i].roomLocation);
                    return storedQuestLocation;
                }           
            }
        }
        //error
        return new Vector2(0,0);
    }

    //Takes in a target room and returns a Vector2 position of the loading zone in the current
    //room that is the shortest path to that room from the current path.
    private Vector2 shortestLocationOnPathToTargetRoom(string targetRoom)
    {
        Room currentRoom = roomController.getGlobalRoom();

        //Go through all the doorways and loading zones in the current room, keeping track of their positions
        //as well as how many rooms of distance between where they connect to and the target room.
        Vector2[] posArray = new Vector2[currentRoom.loadingZones.Length + currentRoom.doorways.Length];
        int[] distArray = new int[currentRoom.loadingZones.Length + currentRoom.doorways.Length];

        for (int i = 0; i < currentRoom.loadingZones.Length; i++)
        {
            posArray[i] = currentRoom.loadingZones[i].transform.position;
            distArray[i] = shortestPathToTargetRoom(targetRoom, currentRoom.loadingZones[i].roomID, currentRoom.roomID);
        }

        for (int i = 0; i < currentRoom.doorways.Length; i++)
        {
            posArray[currentRoom.loadingZones.Length + i] = currentRoom.doorways[i].transform.position;
            distArray[currentRoom.loadingZones.Length + i] = shortestPathToTargetRoom(targetRoom, currentRoom.doorways[i].roomID, currentRoom.roomID);
        }

        //Loop through the distance array and find the shortest distance, this is the location we want to point to 
        int shortest = 9999;
        int shortestID = 0;
        for (int i = 0; i < distArray.Length; i++)
        {
            if (distArray[i] < shortest)
            {
                shortest = distArray[i];
                shortestID = i;
            }
        }

        test = distArray;

        return posArray[shortestID];
    }

    //Gets the shortest path afrom a given room to another
    private int shortestPathToTargetRoom(string targetRoom, string currentRoom, string previousRoom)
    {
        if (currentRoom == targetRoom) //this will work assuming the room 1.) exists somewhere, 2.) is connected
        {
            return 0;
        } else if (previousRoom == currentRoom) //If this is a path that leads to the same room, ignore it. 
            //KNOWN ISSUE: If there is a room where the path forward is accessible only by an in room path, this will fail. 
        {
            return 9999;
        }

        Room currentRoomScript = roomController.getRoom(currentRoom);
        currentRoomScript.GenerateEntraceData();
        int shortest = 9999; //some high number, represents the room not being present in this connection?

        for (int i = 0; i < currentRoomScript.roomConnections.Count; i++)
        {
            //Only process recursive algorithm on unexplored rooms (don't go back ever)
            if (currentRoomScript.roomConnections[i] != previousRoom)
            {
                //Make sure the connections are actually set up so we dont overflow
                Room neighbor = roomController.getRoom(currentRoomScript.roomConnections[i]);
                neighbor.GenerateEntraceData();

                int dist = shortestPathToTargetRoom(targetRoom, currentRoomScript.roomConnections[i], currentRoom);
                if (dist < shortest)
                {
                    shortest = dist;
                }
            }
        }

        if (shortest == 9999)
        {
            return 9999;
        } else
        {
            return shortest + 1;
        }
    }

    // Given a specific quest ID and progress, gives the player that quest with that progress level
    // if they don't already have it. 
    public void GiveQuest(string questID, int progress)
    {
        //Check if you already have the quest?
        for (int i = 0; i < currentQuests.Count; i++)
        {
            if (currentQuests[i].data.internalQuestName == questID)
            {
                return;
            }
        }

        QuestData loadedData = Resources.Load<QuestData>("Quests/" + questID);
        PlayerQuest newQuest = new PlayerQuest(loadedData, progress);
        currentQuests.Add(newQuest);

        //If this is the first quest, auto select it
        if (currentQuests.Count == 1)
        {
            targetQuest = new PlayerQuest(loadedData, progress);
        }

        GenerateQuestPrefab(newQuest);

        GameObject newNotif = Instantiate(questNotificationPrefab);
        newNotif.transform.SetParent(questNotificationParent.transform);
        QuestNotification script = newNotif.GetComponent<QuestNotification>();
        script.quest = loadedData;
        script.type = QuestNotificationType.newQuest;
    }

    // Creates a new quest notification popup. 
    public void GenerateQuestPrefab(PlayerQuest quest)
    {
        GameObject newPrefab = Instantiate(questListItemPrefab);
        newPrefab.transform.SetParent(statusController.questListParent.transform);
        QuestListMenuOption script = newPrefab.GetComponent<QuestListMenuOption>();
        script.quest = quest;
        statusController.questList.Add(script);
    }

    //Checks if the player is current doing quest given by id
    public bool isQuestActive(string questID)
    {
        for (int i = 0; i < currentQuests.Count; i++)
        {
            if (currentQuests[i].data.internalQuestName == questID)
            {
                return true;
            }
        }

        return false;
    }

    //Checks if the player has completed this quest on this save file
    public bool hasPlayerCompletedQuest(string questID)
    {
        for (int i = 0; i < completedQuests.Count; i++)
        {
            if (completedQuests[i] == questID)
            {
                return true;
            }
        }

        return false;
    }

    //Check if given step of given quest is completed
    public bool isQuestStepComplete(string questID, int step)
    {
        for (int i = 0; i < currentQuests.Count; i++)
        {
            if (currentQuests[i].data.internalQuestName == questID)
            {
                //The value that needs to be completed for a quest step to be 'done'
                //Set up this value to check if step is complete
                int completenessScoreMax = 0;
                if (currentQuests[i].data.steps[step].itemsRequired)
                {
                    completenessScoreMax += currentQuests[i].data.steps[step].itemRequirements.Length;
                }

                //

                //Go through all the possible requirements and check if they've been met
                int completenessScoreCurrent = 0;
                if (currentQuests[i].data.steps[step].itemsRequired)
                {
                    for (int x = 0; x < currentQuests[i].data.steps[step].itemRequirements.Length; x++)
                    {
                        if (inventoryController.itemCount(currentQuests[i].data.steps[step].itemRequirements[x].item) >=
                            currentQuests[i].data.steps[step].itemRequirements[x].count)
                        {
                            completenessScoreCurrent++;
                        }
                    }
                }

                return completenessScoreCurrent == completenessScoreMax;
            }
        }

        return false;
    }

    //Gets the current internal step of a given quest
    public int getCurrentQuestStep(string questID)
    {
        for (int i = 0; i < currentQuests.Count; i++)
        {
            if (currentQuests[i].data.internalQuestName == questID)
            {
                return currentQuests[i].stepID;
            }
        }

        return 0;
    }

    //Move a quest to the next step, if it is on the last step, it's complete
    public void AdvanceQuest(string questID)
    {
        for (int i = 0; i < currentQuests.Count; i++)
        {
            if (currentQuests[i].data.internalQuestName == questID)
            {
                currentQuests[i].stepID++;

                if (targetQuest.data == currentQuests[i].data)
                {
                    targetQuest.stepID++;
                }

                if (currentQuests[i].stepID == currentQuests[i].data.steps.Length)
                {
                    currentQuests[i].stepID = 0;
                    if (targetQuest.data == currentQuests[i].data)
                    {
                        targetQuest.stepID = 0;
                    }

                    justCompletedQuest = true;

                    ////Give quest rewards
                    if (currentQuests[i].data.memoryIronReward > 0)
                    {
                        InterfaceController.scrap += currentQuests[i].data.memoryIronReward;
                        interfaceController.ShowScrapAddVisual(currentQuests[i].data.memoryIronReward);
                    }

                    for (int x = 0; x < currentQuests[i].data.itemRewards.Length; x++)
                    {
                        for (int z = 0; z < currentQuests[i].data.itemRewardCounts[x]; z++)
                        {
                            inventoryController.GiveItem(currentQuests[i].data.itemRewards[x]);
                        }
                    }


                    GameObject newNotif = Instantiate(questNotificationPrefab);
                    newNotif.transform.SetParent(questNotificationParent.transform);
                    QuestNotification script = newNotif.GetComponent<QuestNotification>();
                    script.quest = currentQuests[i].data;
                    script.type = QuestNotificationType.completedQuest;


                    statusController.DestroyQuestPrefab(currentQuests[i].data.internalQuestName);
                    completedQuests.Add(currentQuests[i].data.internalQuestName);
                    if (targetQuest.data == currentQuests[i].data)
                    {
                        targetQuest.data = null;
                    }
                    currentQuests.Remove(currentQuests[i]);
                    i--;

                } else
                {
                    GameObject newNotif = Instantiate(questNotificationPrefab);
                    newNotif.transform.SetParent(questNotificationParent.transform);
                    QuestNotification script = newNotif.GetComponent<QuestNotification>();
                    script.quest = currentQuests[i].data;
                    script.type = QuestNotificationType.updatedQuest;
                }
            }
        }
    }
}
