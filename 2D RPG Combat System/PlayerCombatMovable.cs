using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Inhertics from CombatMovable. This component handles all player movement
// and attacks in combat.

// Core of the component is a State Machine that defines transitions between
// various states in combat. Makes calls to its Abstract Movable functions for
// the majority of the implementations.

public class PlayerCombatMovable : CombatMovable
{
    //Components
    private CombatManagerV2 combatManager;
    private EntityAnimator anim;
    private PlayerWeaponArtController weaponArtController;
    private SoundController soundController;

    //Constants
    private const float minAttackWindupAnimationDelay = 0.3f;

    //Internal values 
    protected float combatMoveSpeed = 6;
    protected float combatRollSpeed = 10;

    private PlayerCombatState currentState;
    private int comboHitID;

    private PlayerAttack currentAttack;

    private PlayerCombatAnimationParams animationParams;

    private bool animationSpriteFlipLock = false;

    private bool playerHardKnockdown = false;

    private Coroutine storedAnimationSequence; //keeps track of current (if applicable) animation sequence to be interrupted 

    public override void Awake()
    {
        base.Awake();
        combatManager = GameObject.Find("GameController").GetComponent<CombatManagerV2>();
        soundController = GameObject.Find("SoundController").GetComponent<SoundController>();
        anim = GetComponent<EntityAnimator>();
        weaponArtController = GetComponent<PlayerWeaponArtController>();
    }

    //Exit combat 
    public void ExitCombat()
    {
        playerHardKnockdown = false;
        animationSpriteFlipLock = false;
        StopWeaponShieldAnimMatch();
        HideWeaponShield();
        SetAutoSpriteFlipLock(false);
        ToggleGravity(true);
    }

    //Sets player up for combat
    public void InitializeForCombat()
    {
        playerHardKnockdown = false;
        animationSpriteFlipLock = false;
        currentAttack = null;
        comboHitID = 0;
        currentState = PlayerCombatState.standby;
        SetAutoSpriteFlipLock(true);
        ToggleGravity(false);
        SetCurrentAnimationParams(GetDefaultCombatParams());
    }

    //Resets player in combat tutorial
    public void CombatTutorialReset()
    {
        currentState = PlayerCombatState.standby;
    }

    //Ends the player's turn 
    private void EndTurn()
    {
        currentState = PlayerCombatState.standby;
        combatManager.ChangeCombatPhase(CombatManagerState.turnChange);
    }

    public override void Update()
    {
        base.Update();
        if (CombatManagerV2.inCombatV2)
        {
            HandleCombatFacingDirection();
            HandleBaseCombatAnimations();

            switch (currentState)
            {
                case PlayerCombatState.standby:
                    //Nothing happens here, used as a in between state for transitioning to other 
                    //actions in combat.
                    break;
                case PlayerCombatState.evaluate:
                    //Check if combat has been won
                    if (combatManager.CombatWon())
                    {
                        combatManager.WinCombat();
                        break;
                    }
                    //Check if there are more hits in your weapon combo 
                    if (comboHitID < GetCurrentAttack().comboHits.Length)
                    {
                        //Check if you can hit the target enemy from where you are
                        if (TargetEnemyInRangeForAttack())
                        {
                            StartCoroutine(IniateComboHit(GetCurrentAttack(), comboHitID));
                            currentState = PlayerCombatState.attackWindup;
                        } else
                        {
                            //If not, move until they are in range. 
                            SetMoveSpeed(combatMoveSpeed);
                            SetMovePosition(combatManager.GetTargetEnemy().CurrentPosition());
                            currentState = PlayerCombatState.moving;
                        }
                    } else
                    {
                        //Otherwise, player attack turn is over
                        combatManager.CheckCombatTutorialObjectiveTrigger(CombatTutorialStateTriggers.playerCompletedFullAttackCombo);
                        combatManager.SpecialTutorialSecondEnemyFullComboCheck();
                        EndTurn();
                        break;
                    }
                    break;
                case PlayerCombatState.moving:
                    //Move until you are actually in range of an enemy
                    if (TargetEnemyInRangeForAttack())
                    {
                        StopMovement();
                        currentState = PlayerCombatState.evaluate;
                    }
                    break;
                case PlayerCombatState.moveToAttack:
                    //Wait until you have arrived a attack move position
                    if (!IsMoving())
                    {
                        //End attack anim
                        StartCoroutine(FollowthroughAnimationEndDelaySequence());                     
                        //Check if you missed or hit the action command
                        if (combatManager.GetLastActionCommandSucess())
                        {                           
                            CalculateEnemyHit();
                            comboHitID++;

                            if (combatManager.CombatWon())
                            {
                                currentState = PlayerCombatState.evaluate;
                            } else
                            {
                                currentState = PlayerCombatState.enemyKnockbackWait;
                            }
                        } else
                        {
                            combatManager.CheckCombatTutorialObjectiveTrigger(CombatTutorialStateTriggers.playerMissedAttack);
                            EndTurn();
                            break;
                        }
                    }
                    break;
                case PlayerCombatState.enemyKnockbackWait:
                    //Wait until enemy is finished being knocked back 
                    if (!combatManager.GetTargetEnemy().IsMoving())
                    {
                        currentState = PlayerCombatState.evaluate;
                    }
                    break;
            }
        }
    }

    //Calculates effect from hitting enemy sucessfully
    private void CalculateEnemyHit()
    {
        combatManager.GetTargetEnemy().KnockbackWithSpeed(TargetEnemyToLeft(), 0.5f);
        combatManager.ChangeEnemyHealth(combatManager.GetTargetEnemy(), 
            -CalculateCurrentAttackDamage(combatManager.GetEquippedWeapon(), comboHitID, combatManager.GetLastActionCommandRate()));
        combatManager.CombatShakeScreen(CombatScreenShakeAmmount.light);
    }

    //Given a direction and sucess rate, roll the player forward or back at a certain distance. 
    public void CombatRoll(bool rollLeft, float successRate)
    {
        soundController.player.PlaySoundRandomPitch("roll", 0.1f);
        SetMoveSpeed(combatRollSpeed);

        float baseRollDist = 4.2f; //4.2f
        float rollDistance = baseRollDist * (successRate / 100);
        float endPos = rollLeft ? CurrentPosition() - rollDistance : CurrentPosition() + rollDistance;

        SetMovePosition(AdjustMovePositionBasedOnWalls(endPos, CurrentPosition()));

        //Do animation stuff
        StopWeaponShieldAnimMatch();
        SetCurrentAnimationParams(new PlayerCombatAnimationParams(true, false, false));
        //Set facing direction to be the direction you rolled
        animationSpriteFlipLock = true;
        ren.flipX = rollLeft;
        //Start animation sequence
        StartCoroutine(RollAnimSequence(GetCurrentMovementTime()));

        combatManager.CheckCombatTutorialObjectiveTrigger(CombatTutorialStateTriggers.playerCombatRolled);
    }

    //Controlls animation timing of roll
    private IEnumerator RollAnimSequence(float time)
    {
        anim.PlayAnimOverTime("Roll", time);
        yield return new WaitForSecondsRealtime(time);
        //Sets anim params back to normal
        SetCurrentAnimationParams(GetDefaultCombatParams());
        animationSpriteFlipLock = false;
    }

    //Triggers an animation for blocking 
    public void TriggerBlockAnimation()
    {
        StopWeaponShieldAnimMatch();
        SetCurrentAnimationParams(new PlayerCombatAnimationParams(true, false, true));
        storedAnimationSequence = StartCoroutine(BlockAnimSequence(0.3f)); //GetCurrentMovementTime() * 0.6f
    }

    //Controlls timing of block sequence 
    private IEnumerator BlockAnimSequence(float time)
    {
        //Plays block anim over time
        anim.PlayAnimOverTime("Block", time);
        weaponArtController.shieldAnimator.StartMatchingAnim(anim, "Block");

        yield return new WaitForSecondsRealtime(time);

        //Sets anim params back to normal
        SetCurrentAnimationParams(GetDefaultCombatParams());
        storedAnimationSequence = null;
    }

    //Interrupts current animation and resets back to normal
    private void InterruptCurrentAnimation()
    {
        if (storedAnimationSequence != null)
        {
            StopCoroutine(storedAnimationSequence);
            SetCurrentAnimationParams(GetDefaultCombatParams());
        }
    }

    //Starts the player's attack logic
    public void InitiatePlayerAttack()
    {
        comboHitID = 0;
        SetCurrentAttack(combatManager.GetEquippedWeapon().attack);
        currentState = PlayerCombatState.evaluate;
    }

    //Starts player's counter attack logic
    public void InitatePlayerCounterAttack()
    {
        InterruptCurrentAnimation();
        comboHitID = 0;
        SetCurrentAttack(combatManager.GetEquippedWeapon().weapon.counterCombo);
        combatManager.SetCurrentRangeEffectiveness(1);
        currentState = PlayerCombatState.evaluate;
    }

    private IEnumerator IniateComboHit(PlayerAttack attack, int comboHitID)
    {
        //Determine attack end position
        float endPos = combatManager.GetTargetEnemy().GetPointBlankPosition(this);
        endPos = AdjustMovePositionBasedOnWalls(endPos, CurrentPosition());
        //Create action command now 
        combatManager.CreateNewActionCommand(GenerateActionCommandData(attack, comboHitID, endPos));

        //Start windup animation
        StopWeaponShieldAnimMatch();
        SetCurrentAnimationParams(new PlayerCombatAnimationParams(true, true, false));
        StartCoroutine(AttackWindupAnimationDelaySequence(attack.comboHits[comboHitID].hitDelay));

        yield return new WaitForSecondsRealtime(attack.comboHits[comboHitID].hitDelay);

        soundController.player.PlaySoundRandomPitch("enemy_attack", 0.1f);

        //Resets combat camera to roll back zoom after counter zoom in
        combatManager.ResetCombatCamera();

        //Set attack movement and begin 
        SetMoveSpeed(combatMoveSpeed * attack.comboHits[comboHitID].hitSpeedMultiplier);
        SetMovePosition(endPos);
        currentState = PlayerCombatState.moveToAttack;

        //Start followthrough animation
        anim.PlayAnimOverTime("followthrough", GetCurrentMovementTime());
        weaponArtController.weaponAnimator.StartMatchingAnim(anim, "Followthrough_Neutral");
    }

    //Determines how soon to start the windup animation for this attack
    private IEnumerator AttackWindupAnimationDelaySequence(float windup)
    {
        float delayTime = (windup < minAttackWindupAnimationDelay) ? 0 : windup - minAttackWindupAnimationDelay;
        float animTime = (windup < minAttackWindupAnimationDelay) ? windup : windup - delayTime;

        yield return new WaitForSecondsRealtime(delayTime);

        anim.PlayAnimOverTime("windup", animTime);
        weaponArtController.weaponAnimator.StartMatchingAnim(anim, "Windup_Neutral");
    }

    //Lets the last frame of the followthrough anim end before interrupting animation for idle 
    private IEnumerator FollowthroughAnimationEndDelaySequence()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        SetCurrentAnimationParams(GetDefaultCombatParams());
    }

    //Given an attack, combo id, and current movetime, generates an action command 
    private CombatManager.ActionCommandData GenerateActionCommandData(PlayerAttack attack, int comboHitID, float endPosition)
    {
        CombatManager.ActionCommandData newData = new CombatManager.ActionCommandData(
            CalculateMoveTime(CurrentPosition(), endPosition, combatMoveSpeed * attack.comboHits[comboHitID].hitSpeedMultiplier),
            attack.comboHits[comboHitID].hitDelay,
            false,
            action_command_type.playerPhysicalAttack,
            0,
            combatManager.GetAdjustedAttackDirection(attack.comboHits[comboHitID].direction, CurrentPosition(), endPosition));
        return newData;
    }

    //Determines if the targeted enemy is in range of your weapon 
    private bool TargetEnemyInRangeForAttack()
    {
        return PositionWithinRange(combatManager.GetTargetEnemy().CurrentPosition(),
            CurrentPosition() - combatManager.GetEquippedWeapon().weapon.attack_startup_range,
            CurrentPosition() + combatManager.GetEquippedWeapon().weapon.attack_startup_range);
    }

    //Determines if target enemy is to the left of the player currently
    private bool TargetEnemyToLeft()
    {
        return combatManager.GetTargetEnemy().CurrentPosition() <= CurrentPosition();
    }

    //Gets the current player attack
    private PlayerAttack GetCurrentAttack()
    {
        return currentAttack;
    }

    //Set the current player attack 
    private void SetCurrentAttack(PlayerAttack attack)
    {
        currentAttack = attack;
    }

    //Handles facing direction during combat
    private void HandleCombatFacingDirection()
    {
        if (animationSpriteFlipLock) { return; }
        ren.flipX = (combatManager.GetCurrentCombatState() == CombatManagerState.enemyTurns)
            ? (combatManager.GetCurrentTurnEnemy().CurrentPosition() <= CurrentPosition()) 
            : (combatManager.GetTargetEnemy().CurrentPosition() <= CurrentPosition());
    }

    //Controlls player default anims and weapon/shield visibility
    private struct PlayerCombatAnimationParams
    {
        public bool lockPlayerDefaultAnims;
        public bool showPlayerWeapon;
        public bool showPlayerShield;

        public PlayerCombatAnimationParams(bool lockAnims, bool weapon, bool shield)
        {
            lockPlayerDefaultAnims = lockAnims;
            showPlayerWeapon = weapon;
            showPlayerShield = shield;
        }
    }

    //Gets current combat animation params
    private PlayerCombatAnimationParams GetCurrentAnimationParams()
    {
        return animationParams;
    }

    //Sets current combat animation params
    private void SetCurrentAnimationParams(PlayerCombatAnimationParams prms)
    {
        animationParams = prms;
    }

    //Default animation params for idle 
    private PlayerCombatAnimationParams GetDefaultCombatParams()
    {
        return new PlayerCombatAnimationParams(false, true, true);
    }

    //Handles default combat animations (idle/walk, controlls weapon and sheild visibility)
    private void HandleBaseCombatAnimations()
    {
        if (!GetCurrentAnimationParams().lockPlayerDefaultAnims)
        {
            //Check if player is moving
            if (Mathf.Abs(rb.velocity.x) > 0.1f)
            {
                anim.PlayAnimBase("Run_Combat");
                StopWeaponShieldAnimMatch();
            } else
            {
                anim.PlayAnimBase("CombatIdle");
                weaponArtController.weaponAnimator.StartMatchingAnim(anim, "Idle");
                weaponArtController.shieldAnimator.StartMatchingAnim(anim, "Idle");
            }            
        }
        weaponArtController.showWeapon = GetCurrentAnimationParams().showPlayerWeapon;
        weaponArtController.showShield = GetCurrentAnimationParams().showPlayerShield;

        weaponArtController.SetWeaponShieldFlip(ren.flipX);
    }

    //Stops weapon/sheild anim matching for trigger animations 
    private void StopWeaponShieldAnimMatch()
    {
        weaponArtController.weaponAnimator.StopMatchingAnim();
        weaponArtController.shieldAnimator.StopMatchingAnim();
    }

    //Hides weapon and shield out of combat
    private void HideWeaponShield()
    {
        weaponArtController.showWeapon = false;
        weaponArtController.showShield = false;
    }

    //Trigger player death effect
    public void PlayerDeathAnimation()
    {
        StopWeaponShieldAnimMatch();
        SetCurrentAnimationParams(new PlayerCombatAnimationParams(true, false, false));
        anim.PlayAnimBase("Death");
    }

    //Checks if the player is in hard knockdown state
    public bool PlayerInHardKnockdown()
    {
        return playerHardKnockdown;
    }

    //Activates animation for player getting hard knocked down by an attack
    public void TriggerHardKnockDownAnimation()
    {
        playerHardKnockdown = true;
        StopWeaponShieldAnimMatch();
        SetCurrentAnimationParams(new PlayerCombatAnimationParams(true, false, false));
        StartCoroutine(HardKnockdownSequence(GetCurrentMovementTime()));
    }

    //Controlls timing of knockdown sequence 
    private IEnumerator HardKnockdownSequence(float time)
    {
        float minKnockDownTime = 0.7f;
        if (time < minKnockDownTime)
        {
            time = minKnockDownTime;
        }

        //Plays stagger anim over time
        anim.PlayAnimOverTime("Stagger", time);

        //Wait for knockback to end + small delay
        yield return new WaitForSecondsRealtime(time + 0.5f);

        //Sets anim params back to normal
        SetCurrentAnimationParams(GetDefaultCombatParams());
        playerHardKnockdown = false;
    }

    //Calculates damage from a given weapon/attack/sucessrate 
    private float CalculateCurrentAttackDamage(CombatManager.PlayerCombatWeapon weapon, int comboHitID, float successRate)
    {
        float dam = Mathf.Round(combatManager.playerStatController.GetWeaponDamageRaw(weapon.weapon) 
            * weapon.attack.comboHits[comboHitID].damageMultiplier * combatManager.GetCurrentRangeEffectiveness())
            + Random.Range(-1, 1);

        dam = Mathf.Round(dam * (successRate / 100));

        if (DebugMenu.debug_AremDamageMax) { dam = 999999; }

        if (dam > 0)
        {
            return dam;
        }
        return 1;
    }
}

[System.Serializable]
public enum PlayerCombatState
{
    evaluate,
    moving,
    moveToAttack,
    enemyKnockbackWait,
    standby,
    attackWindup
}
