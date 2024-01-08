using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Anything that can be moved (on a flat plane) in the overworld.
// Used for enemies & player in combat as well as NPCs in the overworld
// and anything else that is moved on a flat plane. 
public abstract class AbstractMovable : MonoBehaviour
{
    //Private variables
    protected float currentMoveSpeed;

    protected bool LockAutoSpriteFlip; //Turn this on if you want something external to control sprite flipping

    protected bool SpritesFlipped; //if the sprites are facing left by default instead of right (the normal way)

    //Info about current movmenet
    protected bool isMoving;
    private bool movingLeft;
    private float targetMovePosition;
    private float startMovePosition;

    protected float colliderExtent; //the distance from the center of the movable to the edge (on either side) of its collider

    protected float gravityscale;

    //Components
    protected Rigidbody2D rb;
    protected SpriteRenderer ren;
    protected BoxCollider2D col;

    private bool compsLoaded = false;

    public virtual void Awake()
    {
        //Determines all component references are valid. 
        EnsureComponents();

        //Define the true "width" of this collider as a value
        if (col)
        {
            colliderExtent = col.size.x / 2;
        } else
        {
            colliderExtent = 0.5f; //some default size 
        }     
    }

    // Determines if all component references are valid. 
    protected virtual void EnsureComponents()
    {
        if (compsLoaded) { return; }

        //Required
        rb = GetComponent<Rigidbody2D>();
        gravityscale = rb.gravityScale;

        //Technically optional, but probably will always have these two components
        try { col = GetComponent<BoxCollider2D>(); } catch { }
        try { ren = GetComponent<SpriteRenderer>(); } catch { }

        compsLoaded = true;
    }

    // Determines if component should handle this as a left-facing default or right-facing
    // default sprite. 
    public void SetSpritesFlipped(bool flipped)
    {
        SpritesFlipped = flipped;
    }

    // Determines if the component should automatically flip the sprite left/right based on movement or not. 
    public void SetAutoSpriteFlipLock(bool turnOn)
    {
        LockAutoSpriteFlip = turnOn;
    }

    public virtual void SetMoveSpeed(float speed) //Set the movement speed
    {
        currentMoveSpeed = speed;
    }

    public virtual void SetMovePosition(float xPos) //Initiate a move to given position with current speed
    {
        movingLeft = (transform.position.x > xPos);
        targetMovePosition = xPos;
        startMovePosition = transform.position.x;

        isMoving = true;
        SetMovementVectorFromCurrentMove();
    }

    public virtual void StopMovement() //Stops the current movement. (Not a pause, forgets old move information.) 
    {
        targetMovePosition = transform.position.x;
        movingLeft = false;

        isMoving = false;
        rb.velocity = Vector2.zero;
    }

    //Check for info about this moving thing
    public bool IsMoving() //Is this movable currently doing a move
    {
        return isMoving;
    }

    public float GetCurrentMovementTime() //How long will this current movement take? (Returns 0 if not moving)
    {
        return IsMoving() ? CalculateMoveTime(startMovePosition, targetMovePosition, currentMoveSpeed) : 0;
    }

    public float CalculateMoveTime(float startXPos, float endXPos, float speed) //given a start pos, end pos, and movement speed, calculate move time 
    {
        return (Mathf.Abs(endXPos - startXPos) / speed);
    }

    public bool MoveWillBeLeft(float xStart, float xEnd) //Given a start and end x pos, determine if this will be a left or right movement
    {
        return xStart >= xEnd;
    }

    public float CurrentPosition() // Returns world transform x position. 
    {
        return transform.position.x;
    }

    public float GetColliderExtent() // Returns extent of collider from center of sprite. 
    {
        return colliderExtent;
    }

    public float GetMoveStartingPosition() //used in stamina and distance traveled calculations
    {
        return startMovePosition;
    }

    public bool PositionWithinRange(float posToCheck, float leftBound, float rightBound) //checks if a given position is within range 
    {
        return (posToCheck >= leftBound) && (posToCheck <= rightBound);
    }
    
    public float GetMovementEndPosition() //gets the current target positon
    {
        return targetMovePosition;
    }

    //Misc.
    private void SetMovementVectorFromCurrentMove()
    {
        if (isMoving)
        {
            //Set movement vector based on which way it should be going
            Vector2 movementVector = new Vector2();
            if (movingLeft)
            {
                movementVector = new Vector2(-currentMoveSpeed, 0);
            }
            else
            {
                movementVector = new Vector2(currentMoveSpeed, 0);
            }

            rb.velocity = movementVector;
        }     
    }


    //Actually do the movement
    public virtual void FixedUpdate()
    {
        if (isMoving)
        {
            SetMovementVectorFromCurrentMove();
        }
    }

    public virtual void Update()
    {
        if (isMoving)
        {
            //Handle sprite flipping
            if (!LockAutoSpriteFlip && ren != null)
            {
                if (rb.velocity.x < 0.1f)
                {
                    ren.flipX = !SpritesFlipped;
                }
                else if (rb.velocity.x > 0.1f)
                {
                    ren.flipX = SpritesFlipped;
                }
            }

            //Stop movement
            if (movingLeft)
            {
                if (transform.position.x < targetMovePosition)
                {
                    transform.position = new Vector2(targetMovePosition, transform.position.y);
                    StopMovement();
                }
            }
            else
            {
                if (transform.position.x > targetMovePosition)
                {
                    transform.position = new Vector2(targetMovePosition, transform.position.y);
                    StopMovement();
                }
            }
        }
    }
}
