using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Inherits from CombatMovable. This component handles Enemy combat behavior, such as
// movement and attacking.

// The core of the component is a State Machine that determines enemy behavior and transition 
// between states. Makes calls to it's CombatMoveable code for the majority of functionality.

public class CombatEnemyV2 : CombatMovable
{
    [Header("Misc.")]
    public bool spritesFliped;

    [Header("Movement Values")]
    public float combatMoveSpeed;

    [Header("In Combat")]
    public CombatEnemy.AttackPossibility[] attackPossibilities;

    //Internal vars
    private const float staminaScale = 3f;

    private const float minAttackWindupAnimationDelay = 0.3f;

    public CombatEnemyState currentState;
    private bool isEnemysTurn;
    private bool enemyInCombat;

    private EnemyAttack currentAttack;
    private int currentAttackComboID;

    private int baseSortingLayer;

    private Vector2 startPosition;

    private bool lockEnemyDefaultAnims;
    private bool lockEnemySpriteFlip;

    private bool repositionMoveAdjustedByWalls; //a boolean flag that says if enemy reposition was adjusted by walls

    private Coroutine storedAnimationSequence;

    private bool alreadyHasStartLocation;

    //Components
    private EnemyHealth healthScript;
    private CombatManagerV2 combatManager;
    private EntityAnimator anim;
    private SoundController soundController;
    private OverworldEnemy overworldEnemy;

    //Attack data
    private float distanceTraveledForAttack;
    private float currentMoveDistance;
    private float currentMoveStaminaCost;
    private bool playerWasInRange;
    private float attackLeftBound;
    private float attackRightBound;
    private bool playerRollCatch;
    private float attackStartPosition;
    private float attackEndPosition;
    private float attackHitSpeed;
    private bool attackWasMovingLeft;

    public override void Awake()
    {
        base.Awake();
    }

    public void SetupStartLocation()
    {
        if (alreadyHasStartLocation)
        {
            return;
        }
        startPosition = transform.position;
        alreadyHasStartLocation = true;
    }

    protected override void EnsureComponents()
    {
        base.EnsureComponents();
        combatManager = GameObject.Find("GameController").GetComponent<CombatManagerV2>();
        healthScript = GetComponent<EnemyHealth>();
        anim = GetComponent<EntityAnimator>();
        combatManager = GameObject.Find("GameController").GetComponent<CombatManagerV2>();
        soundController = GameObject.Find("SoundController").GetComponent<SoundController>();
        healthScript = GetComponent<EnemyHealth>();
        anim = GetComponent<EntityAnimator>();
        ToggleGravity(true);
        ToggleColliderSolid(true);
        baseSortingLayer = ren.sortingOrder;
        SetupStartLocation();
        //If there is an overworld enemy component, ensure its componenets here as well
        try
        {
            overworldEnemy = GetComponent<OverworldEnemy>();
            if (overworldEnemy != null)
            {
                overworldEnemy.EnsureComponents();
            }
        } catch { }
    }

    //Exits combat (if this enemy is alive)
    public void ExitCombat()
    {
        repositionMoveAdjustedByWalls = false;
        lockEnemySpriteFlip = false;
        enemyInCombat = false;
        lockEnemyDefaultAnims = false;
        healthScript.FullHeal();
        SetSortingOrder(GetBaseSortingOrder());
        isEnemysTurn = false;
        SetAutoSpriteFlipLock(false);
        ToggleGravity(true);
        ToggleColliderSolid(true);
    }

    //Sets the sprite sorting order of this enemy.
    public void SetSortingOrder(int layer)
    {
        ren.sortingOrder = layer;
    }

    //Gets the base sprite sorting layer of this enemy.
    public int GetBaseSortingOrder()
    {
        return baseSortingLayer;
    }

    //Set up starting values for combat
    public void InitializeForCombat()
    {
        enemyInCombat = true;

        currentState = CombatEnemyState.evaluate;
        repositionMoveAdjustedByWalls = false;
        isEnemysTurn = false;
        playerWasInRange = false;
        currentAttack = null;
        distanceTraveledForAttack = 0;
        currentAttackComboID = 0;
        attackLeftBound = 0;
        attackRightBound = 0;
        playerRollCatch = false;
        attackWasMovingLeft = false;
        attackStartPosition = 0;
        attackEndPosition = 0;
        attackHitSpeed = 0;
        lockEnemyDefaultAnims = false;
        lockEnemySpriteFlip = false;
        healthScript.Init();
        SetAutoSpriteFlipLock(true);
        ToggleGravity(false);
        ToggleColliderSolid(false);
        StopMovement();
    }

    //Tells the enemy to begin its turn 
    public void StartTurn()
    {
        isEnemysTurn = true;
        //Reset stamina
        healthScript.stamina = healthScript.maxStamina;
        currentState = CombatEnemyState.evaluate;
    }

    //'Resets' this enemy, stopping current attack and healing 
    public void CombatTutorialReset()
    {
        healthScript.stamina = healthScript.maxStamina;
        lockEnemyDefaultAnims = false;
        isEnemysTurn = false;
        healthScript.Init();
    }

    //Here we use a state machine to figure out what the enemy should be doing in combat & out of combat
    public override void Update()
    {
        base.Update();
        //Handle in-combat state machine
        if (CombatManagerV2.inCombatV2 && enemyInCombat)
        {
            HandleCombatFacingDirection();
            HandleCombatDefaultAnimation();

            if (isEnemysTurn)
            {
                switch (currentState)
                {
                    case CombatEnemyState.evaluate:
                        //Check if player counter has been activated
                        if (combatManager.PlayerCounterActivated())
                        {
                            isEnemysTurn = false;
                            combatManager.StartPlayerCounterAttack();
                            break;
                        }
                        //Check for player death
                        if (combatManager.PlayerDead())
                        {
                            isEnemysTurn = false;
                            combatManager.InitiatePlayerDeath();
                            break;
                        }
                        //Check if the player has gone outside combat bounds
                        if (combatManager.PlayerOutOfCombatBounds())
                        {
                            combatManager.EscapeCombat();
                            break;
                        }
                        //Check if you have any remaining stamina.
                        if (healthScript.stamina <= 0.1f)
                        {
                            EndTurn();
                            break;
                        }
                        //Make a decision about what to do (attack/move)
                        if (CanInitiateAttack(GetCurrentAttack(), currentAttackComboID))
                        {
                            //Attack can be initiated from here, therefore attack
                            currentState = CombatEnemyState.attackWindup;
                            StartCoroutine(InitateComboHit(GetCurrentAttack(), currentAttackComboID));
                        } else
                        {
                            //Attack not currently possible, therefore move until you can 
                            float endPos = DetermineBestMovePosition(GetCurrentAttack());
                            float adjustedEndPos = AdjustMovePositionBasedOnWalls(endPos, CurrentPosition());

                            //Determine how much stamina it would have cost to walk extra distance,
                            //and determine how much extra move pos that would have given the enemy
                            if (endPos != adjustedEndPos)
                            {
                                float gainedDistance = Mathf.Abs(endPos - adjustedEndPos);
                                float lostStamina = gainedDistance * staminaScale;

                                healthScript.stamina -= lostStamina;
                                distanceTraveledForAttack += gainedDistance;
                            }

                            SetMoveSpeed(combatMoveSpeed);
                            SetMovePosition(adjustedEndPos);
                            currentState = CombatEnemyState.moving;
                        }
                        break;
                    case CombatEnemyState.movingToAttack:
                        //Check to see if you have arrived at attack position
                        if (!IsMoving())
                        {
                            //End current attack animation
                            //StartCoroutine(FollowthroughAnimationEndDelaySequence());
                            //Calculate damage vs. defence of the player 
                            if (playerWasInRange)
                            {
                                CalculatePlayerHit(GetCurrentAttack(), currentAttackComboID);
                            }

                            currentAttackComboID++;
                            currentState = CombatEnemyState.playerKnockbackWait;
                        }
                        break;
                    case CombatEnemyState.playerKnockbackWait:
                        //This state is always entered after an attack lands, waiting
                        //until the player is done being knocked back (or rolling) to comtinue with its turn 
                        if (!GetPlayer().IsMoving() && !GetPlayer().PlayerInHardKnockdown())
                        {
                            currentState = CombatEnemyState.evaluate;
                        }
                        break;
                    case CombatEnemyState.moving:
                        //Calculate how much stamina the current move is costing
                        //and what the distance traveled addition would be 
                        currentMoveStaminaCost = Mathf.Abs(GetMoveStartingPosition() - CurrentPosition()) * staminaScale;
                        currentMoveDistance = distanceTraveledForAttack + Mathf.Abs(GetMoveStartingPosition() - CurrentPosition());

                        //If you arrive at endpoint without ever attacking, end turn 
                        if (!IsMoving())
                        {
                            //Update however much stamina/dist 
                            healthScript.stamina -= currentMoveStaminaCost;
                            distanceTraveledForAttack = currentMoveDistance;
                            //Go back to evaluate (likely to end turn due to empty stamina)
                            currentState = CombatEnemyState.evaluate;
                        }
                        //While moving, check if at any point you are able to attack
                        if (CanInitiateAttack(GetCurrentAttack(), currentAttackComboID))
                        {
                            //Update however much stamina/dist 
                            healthScript.stamina -= currentMoveStaminaCost;
                            distanceTraveledForAttack = currentMoveDistance;
                            //Go back to evaluate to initate the attack
                            StopMovement();
                            currentState = CombatEnemyState.evaluate;
                        }
                        break;
                }
            }
        }
    }

    //Respawns this enemy after resting at a campfire 
    public void Respawn()
    {
        EnsureComponents();
        transform.position = startPosition;
        ExitCombat();
    }

    //Called when this enemy is killed
    public void EnemyDeath()
    {
        isEnemysTurn = false;
        healthScript.DeathEffect();
        healthScript.DropItems();
        healthScript.GiveDeathScrap();
        gameObject.SetActive(false);
    }

    //End this enemies turn and continue combat
    private void EndTurn()
    {
        isEnemysTurn = false;
        combatManager.ChangeCombatPhase(CombatManagerState.turnChange);
        combatManager.CheckCombatTutorialObjectiveTrigger(CombatTutorialStateTriggers.enemyTurnEnded);
    }

    //Assuming you cannot attack from current position, determine the best position to walk to in order to be able to attack
    private float DetermineBestMovePosition(EnemyAttack attack)
    {
        switch (attack.type)
        {
            case attackType.Backup:
                return PlayerToLeft()
                    ? CurrentPosition() + determineMaxPossibleMoveDist() : CurrentPosition() - determineMaxPossibleMoveDist();
            case attackType.Chase:
                return PlayerToLeft() 
                    ? CurrentPosition() - determineMaxPossibleMoveDist() : CurrentPosition() + determineMaxPossibleMoveDist();
        }

        return CurrentPosition();
    }

    //With remaining stamina, determine how far this enemy could possible move
    private float determineMaxPossibleMoveDist()
    {
        return healthScript.stamina / staminaScale;
    }

    //Upon an in-range attack landing, calculate the response. If the player blocked,
    //subtract stamina, knockback, fill counter depending on sucess rate
    private void CalculatePlayerHit(EnemyAttack attack, int comboHitID)
    {
        //If both of these conditions are true, the player sucesfully 
        if (combatManager.GetLastActionCommandSucess() && playerRollCatch == false)
        {
            //Evaluate response to player block
            if (combatManager.GetLastActionCommandResponseData().playerBlocked)
            {
                //Base values
                float counterMeterFillScaling = 1;
                float knockBackScaling = 1;
                float staminaDamageMultiplier = 1;

                //Calculate result of perfect command 
                if (combatManager.LastActionCommandWasPerfect())
                {
                    counterMeterFillScaling = 2;
                    knockBackScaling = 0.2f;
                    staminaDamageMultiplier = 0;
                }
                //Calulate result of counter
                if (combatManager.PlayerWillCounterAttackAfterValue(combatManager.GetBlockCounterMeterFill() * counterMeterFillScaling))
                {
                    knockBackScaling = 0.2f;
                }
                //Calculate stamina damage
                float staminaDamage = CalculateBlockStaminaDamage(attack, comboHitID,
                    healthScript.stamina_damage, combatManager.GetLastActionCommandRate()) * staminaDamageMultiplier;
                //Check for shield brake 
                if (combatManager.GetCurrentStamina() <= staminaDamage)
                {
                    //Sheild brake costs all remaining stamina
                    combatManager.ChangePlayerStamina(-Mathf.Round(combatManager.GetCurrentStamina()));
                    combatManager.SpawnPlayerShieldBreakEffect();
                    //Player damage is increased slightly 
                    DamagePlayer(attack, comboHitID, 1.2f);
                } else
                {
                    //Deal knockback, stamina damage, and add to counter meter
                    GetPlayer().KnockbackWithSpeed(PlayerToLeft(), attack.knockBack * knockBackScaling);
                    GetPlayer().TriggerBlockAnimation();

                    combatManager.ChangePlayerStamina(-staminaDamage);
                    combatManager.FillCounterMeter(combatManager.GetBlockCounterMeterFill() * counterMeterFillScaling);
                    combatManager.CombatShakeScreen(CombatScreenShakeAmmount.light);
                    DealStatusDamageToPlayer(healthScript.throughBlockSpecialTypes, healthScript.status_damage_through_block);

                    combatManager.CheckCombatTutorialObjectiveTrigger(CombatTutorialStateTriggers.attackBlocked);

                    soundController.player.PlaySoundRandomPitch("wooden_shield", 0.1f);
                }     
            }
        } else
        {
            //Otherwise, the player will be hit. 
            DamagePlayer(attack, comboHitID, 1);
        }
    }

    //Deals damage to player, including knockback and potential hard knockdown
    private void DamagePlayer(EnemyAttack attack, int comboHitID, float damageMultiplier)
    {
        //Determine if attack was heavy
        float knockbackMultiplier = 1f;
        CombatScreenShakeAmmount screenShakeMultiplier = CombatScreenShakeAmmount.medium;
        bool hardKnockdown = false;

        if (hardKnockdown = attack.combo[comboHitID].direction == attackDirection.HEAVY)
        {
            knockbackMultiplier = 2f;
            screenShakeMultiplier = CombatScreenShakeAmmount.heavy;
        }

        GetPlayer().KnockbackWithSpeed(PlayerToLeft(), attack.knockBack * knockbackMultiplier);
        combatManager.ChangePlayerHealth(-(Mathf.Round(healthScript.physical_damage * attack.damageMultiplier * damageMultiplier)));
        combatManager.CombatShakeScreen(screenShakeMultiplier);
        if (hardKnockdown) { GetPlayer().TriggerHardKnockDownAnimation(); }
        DealStatusDamageToPlayer(healthScript.damageSpecialTypes, healthScript.status_damage);
    }

    //Calculates stamina damage dealt from combo hit based on sucessrae 
    public float CalculateBlockStaminaDamage(EnemyAttack attack, int comboHitID, float staminaDamage, float successRate)
    {
        //convert to a game usable numebr
        staminaDamage = staminaDamage / 10;
        float finalDamage = staminaDamage * attack.combo[comboHitID].blockDamageMult * (1 + ((1 - (successRate / 100)) * 2));

        return (float)Math.Round(finalDamage, 1);
    }

    //Deals status damage to player given a list of status effects
    private void DealStatusDamageToPlayer(statusType[] types, float ammount)
    {
        foreach(statusType type in types)
        {
            combatManager.DealStatusDamageToPlayer(type, ammount);
        }
    }

    private IEnumerator InitateComboHit(EnemyAttack attack, int comboHitID)
    {
        //Set intial attack vars
        combatManager.ClearActionCommandData();
        playerWasInRange = false;
        playerRollCatch = false;
        attackWasMovingLeft = false;
        repositionMoveAdjustedByWalls = false;

        //Subtract stamina from combo hit
        healthScript.stamina -= (healthScript.maxStamina * (attack.combo[comboHitID].staminaPercentCost / 100));

        float endPoint = 0f;
        if (attack is ChaseAttack)
        {
            //If this is a chase attack, we must have been in range to do this hit. 
            //Therefore, the end point will be guaranteed to be in front of the player. 
            playerWasInRange = true;
            endPoint = GetPlayer().GetPointBlankPosition(this);
            endPoint = AdjustMovePositionBasedOnWalls(endPoint, CurrentPosition());
        }

        if (attack is BackupAttack)
        {
            //If this is a backup attack, we are either in range or out of range of the player.
            //If in range, end point will be in front of the player.
            if (playerWasInRange = MovableInRange(GetPlayer(), attack.combo[comboHitID].range))
            {
                endPoint = GetPlayer().GetPointBlankPosition(this);
            } else
            {
                float comboHitRange = attack.combo[comboHitID].range;
                endPoint = MoveWillBeLeft(CurrentPosition(), GetPlayer().CurrentPosition())
                    ? CurrentPosition() - comboHitRange : CurrentPosition() + comboHitRange;
                //Check for possible wall interaction here as well
                endPoint = AdjustMovePositionBasedOnWalls(endPoint, CurrentPosition());
            }
        }

        //Calculate left and right bound of attack
        attackLeftBound = MoveWillBeLeft(CurrentPosition(), GetPlayer().CurrentPosition())
            ? CurrentPosition() - attack.combo[comboHitID].range : CurrentPosition();
        attackRightBound = MoveWillBeLeft(CurrentPosition(), GetPlayer().CurrentPosition())
            ? CurrentPosition() : CurrentPosition() + attack.combo[comboHitID].range;
        attackStartPosition = CurrentPosition();
        attackEndPosition = endPoint;
        attackHitSpeed = attack.combo[comboHitID].speedMultiplier * combatMoveSpeed;
        attackWasMovingLeft = MoveWillBeLeft(CurrentPosition(), GetPlayer().CurrentPosition());

        Debug.DrawLine(new Vector3(attackLeftBound, transform.position.y), new Vector3(attackLeftBound, transform.position.y + 2), Color.red, 2f);
        Debug.DrawLine(new Vector3(attackRightBound, transform.position.y), new Vector3(attackRightBound, transform.position.y + 2), Color.red, 2f);

        //Create the action command, determine where to land
        if (playerWasInRange)
        {
            combatManager.CreateNewActionCommand(GenerateActionCommandData(attack, comboHitID, endPoint));
        }

        //Start windup animation
        lockEnemyDefaultAnims = true;
        lockEnemySpriteFlip = true;
        StartCoroutine(AttackWindupAnimationDelaySequence(attack.combo[comboHitID].windup));

        yield return new WaitForSecondsRealtime(attack.combo[comboHitID].windup); //wait for the windup of the move

        soundController.player.PlaySoundRandomPitch("enemy_attack", 0.1f);

        SetMoveSpeed(attackHitSpeed);
        //We should only go to the original move position if the player hasnt rolled. 
        if (!combatManager.GetLastActionCommandResponseData().playerRolled) { SetMovePosition(endPoint); }

        currentState = CombatEnemyState.movingToAttack;

        //Start followthrough animation
        float minFollowthroughAnimTime = 0.2f;
        InterruptCurrentAnimation();
        storedAnimationSequence = StartCoroutine(FollowthroughAnimationSequence((GetCurrentMovementTime() < minFollowthroughAnimTime)
            ? minFollowthroughAnimTime : GetCurrentMovementTime()));
    }

    //Determines how soon to start the windup animation for this attack
    private IEnumerator AttackWindupAnimationDelaySequence(float windup)
    {
        if (storedAnimationSequence != null)
        {
            yield break;
        }

        float delayTime = (windup < minAttackWindupAnimationDelay) ? 0 : windup - minAttackWindupAnimationDelay;
        float animTime = (windup < minAttackWindupAnimationDelay) ? windup : windup - delayTime;

        yield return new WaitForSecondsRealtime(delayTime);

        anim.PlayAnimOverTime("windup", animTime);
    }

    //Animation sequence for enemy attack followthrough based on given followthrough time
    //in real world seconds. 
    private IEnumerator FollowthroughAnimationSequence(float time)
    {
        anim.AllowDuplicateAnimationInterrupt();
        anim.PlayAnimOverTime("followthrough", time);
        yield return new WaitForSecondsRealtime(time);
        lockEnemyDefaultAnims = false;
        lockEnemySpriteFlip = false;
        storedAnimationSequence = null;
    }

    //Interupt the current enemy animation coroutine. 
    private void InterruptCurrentAnimation()
    {
        if (storedAnimationSequence != null)
        {
            StopCoroutine(storedAnimationSequence);
        }
    }

    //Should be called mid attack movement. When a player rolls, adjust end position to either go past where player
    //was, or if the player is still in range, perform the roll catch 
    public void AdjustToPlayerRoll()
    {
        //First, check if the player's new end position after rolling is still within the attack range. 
        if (PositionWithinRange(combatManager.playerCombatMovable.GetMovementEndPosition(), attackLeftBound, attackRightBound))
        {
            bool rollcatch = true;

            //If the player rolled past this attack, we can ignore this part of the code. 
            if ((attackWasMovingLeft ^ combatManager.GetLastActionCommandResponseData().playerRolledLeft))
            {
                rollcatch = false;
            }

            if (rollcatch)
            {
                //Your new end position will be directly in front of the player's roll ending position.
                float rollCatchEndPos = attackWasMovingLeft ?
                    combatManager.playerCombatMovable.GetMovementEndPosition() + 1
                    : combatManager.playerCombatMovable.GetMovementEndPosition() - 1;
                SetMovePosition(AdjustMovePositionBasedOnWalls(rollCatchEndPos, attackStartPosition));
                playerRollCatch = true;
                return;
            }
        }

        //If enemy didn't roll catch the player, they evaded it so the enemy needs to adjust their
        //end point to reflect the full extent of the original attack.
        float followthroughEndPos = attackWasMovingLeft ?
            attackLeftBound : attackRightBound;
        //SetMoveSpeed(attackHitSpeed);
        SetMovePosition(AdjustMovePositionBasedOnWalls(followthroughEndPos, attackStartPosition));
        playerRollCatch = false;
    }

    //Given an attack, combo id, and current movetime, generates an action command 
    private CombatManager.ActionCommandData GenerateActionCommandData(EnemyAttack attack, int comboHitID, float endPosition)
    {
        CombatManager.ActionCommandData newData = new CombatManager.ActionCommandData(
            CalculateMoveTime(CurrentPosition(), endPosition, combatMoveSpeed * attack.combo[comboHitID].speedMultiplier),
            attack.combo[comboHitID].windup,
            PlayerToLeft(),
            (attack.combo[comboHitID].direction != attackDirection.HEAVY) ? action_command_type.enemyStandardAttack : action_command_type.enemyHeavyAttack,
            attack.combo[comboHitID].hiddenTime,
            combatManager.GetAdjustedAttackDirection(attack.combo[comboHitID].direction, endPosition, CurrentPosition()));
        return newData;
    }

    //Given the current hit of the attack combo, can the enemy attack from here 
    private bool CanInitiateAttack(EnemyAttack attack, int comboHitID)
    {
        float curStamina = ((healthScript.stamina / healthScript.maxStamina) * 100);
        switch (attack.type)
        {
            case attackType.Chase:
                //Chase attacks are possible if the player is in range and enemy has enough stamina
                return MovableInRange(GetPlayer(), attack.combo[comboHitID].range) &&
                    curStamina >= attack.combo[comboHitID].staminaPercentCost;
            case attackType.Backup:
                //Backup attacks are possible if enemy has traveled enough distance (or bumped into a wall)
                //and enemy has enough stamina.
                BackupAttack newMedAttack = (BackupAttack)attack;
                return (currentMoveDistance >= newMedAttack.distRequirment) &&
                    curStamina >= attack.combo[comboHitID].staminaPercentCost;
        }

        return false;
    }

    //Gets the current attack. If no attack is chosen, picks a new one and resets the combo. 
    private EnemyAttack GetCurrentAttack()
    {
        //No current attack, so pick a new one and reset combo progress
        if (currentAttack == null)
        {
            distanceTraveledForAttack = 0;
            currentMoveDistance = 0;
            currentAttackComboID = 0;
            return PickNewAttack();
        } else
        {
            //Combo has been exhausted, therefore set current attack to null and pick a new one 
            if (currentAttackComboID == currentAttack.combo.Length)
            {
                currentAttack = null;
                return GetCurrentAttack();
            } else
            {
                return currentAttack;
            }
        }
    }

    //Picks the next attack based on various factors defined in the editor
    private EnemyAttack PickNewAttack()
    {
        Dictionary<int, CombatEnemy.AttackPossibility> attackIndexMap = new Dictionary<int, CombatEnemy.AttackPossibility>();
        List<int> pool = new List<int>();
        for (int i = 0; i < attackPossibilities.Length; i++)
        {
            attackIndexMap.Add(i, attackPossibilities[i]);
            for(int x = 0; x < attackPossibilities[i].chance; x++)
            {
                pool.Add(i);
            }
        }

        int rand = UnityEngine.Random.Range(0, pool.Count);

        return currentAttack = attackIndexMap[pool[rand]].attack;
    }

    //Returns true if the player is current to the left of this enemy (false == to the right)
    private bool PlayerToLeft()
    {
        return MoveWillBeLeft(CurrentPosition(), GetPlayer().CurrentPosition());
    }

    //Saves some typing 
    private PlayerCombatMovable GetPlayer()
    {
        return combatManager.playerCombatMovable;
    }

    //Handles facing direction during combat
    private void HandleCombatFacingDirection()
    {
        if (lockEnemySpriteFlip) { return; }

        if (spritesFliped)
        {
            ren.flipX = !(GetPlayer().CurrentPosition() <= CurrentPosition());
        } else
        {
            ren.flipX = GetPlayer().CurrentPosition() <= CurrentPosition();
        }      
    }

    //Handles enemy basic combat animations
    private void HandleCombatDefaultAnimation()
    {
        if (!lockEnemyDefaultAnims && storedAnimationSequence == null)
        {
            if (Mathf.Abs(rb.velocity.x) > 0.1f)
            {
                anim.PlayAnimBase("run");
            } else
            {
                anim.PlayAnimBase("idle");
            }          
        }
    }

    //Picks a specific attack
    public void PickSpecificAttack(EnemyAttack attack)
    {
        distanceTraveledForAttack = 0;
        currentMoveDistance = 0;
        currentAttackComboID = 0;
        currentAttack = attack;
    }

    //Gets this enemy health script
    public EnemyHealth GetHealthScript()
    {
        return healthScript;
    }

    //Is the enemy currently in a battle?
    public bool IsEnemyInCombat()
    {
        return enemyInCombat;
    }
}

[System.Serializable]
public enum CombatEnemyState
{
    evaluate,
    moving,
    attackWindup,
    movingToAttack,
    knockback,
    playerKnockbackWait
}
