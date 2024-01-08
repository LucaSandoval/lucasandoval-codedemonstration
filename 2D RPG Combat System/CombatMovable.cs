using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Inherits from Abstract Movable. This component is suited for entities used in combat. 
// Defines a series of functions related to combat constraints, ie. Knockback, Handling movement 
// around obstacles etc..

public class CombatMovable : AbstractMovable
{
    //Constant numbers
    private const float minWallDistance = 0.2f;
    private const float minApproachDistance = 0.1f;
    private const float knockBackSpeed = 6f;

    //Toggles the gravityscale on the rigidbody of this combat movable, ensuring perfect speed calculations
    public void ToggleGravity(bool turnOn)
    {
        rb.gravityScale = turnOn ? gravityscale : 0f;
    }

    //Toddles the solidity of the colider. Turned off during battle to avoid sutations where the enemy can get 'stuck'
    public void ToggleColliderSolid(bool solid)
    {
        col.isTrigger = !solid;
    }

    //Given another movable, determines if its in a given range of this movable 
    public bool MovableInRange(AbstractMovable otherMovable, float magnitude)
    {
        //Calculate which edge of collider to do this check from 
        float edgePos = (otherMovable.CurrentPosition() <= CurrentPosition())
            ? CurrentPosition() - GetColliderExtent() : CurrentPosition() + GetColliderExtent();
        return Mathf.Abs(otherMovable.CurrentPosition() - edgePos) <= magnitude;
    }

    //Given another movable, calculate a position 'in front' of this movable (for attacks etc..)
    public float GetPointBlankPosition(AbstractMovable otherMovable)
    {
        return MoveWillBeLeft(otherMovable.CurrentPosition(), CurrentPosition()) 
            ? (CurrentPosition() + colliderExtent + minApproachDistance + otherMovable.GetColliderExtent()) 
            : (CurrentPosition() - colliderExtent - minApproachDistance - otherMovable.GetColliderExtent());
    }

    //Given a direction and magnitude 'knocks back' this movable 
    public void Knockback(bool left, float magnitude)
    {
        float direction = left ? -1 : 1;
        float endPoint = CurrentPosition() + (magnitude * direction);
        SetMovePosition(AdjustMovePositionBasedOnWalls(endPoint, CurrentPosition()));
    }

    //Called externally to conviniently knock back without applying a seperate spped
    public void KnockbackWithSpeed(bool left, float magnitude)
    {
        SetMoveSpeed(knockBackSpeed);
        Knockback(left, magnitude);
    }

    //Given a target move position and a move direction, return an adjusted postion thats in front of a wall 
    public float AdjustMovePositionBasedOnWalls(float xEndPos, float xStartPos)
    {
        //From the origin of the movable, raycast in the direction of your movmement
        //TODO: could possibly miss a wall thats lower than this cast point. Probably won't
        //happen but something to look into.
        Vector2 directionVector = MoveWillBeLeft(xStartPos, xEndPos) ? Vector2.left : Vector2.right;
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, directionVector, Mathf.Abs(xStartPos - xEndPos) + colliderExtent);

        for(int i = 0; i < hits.Length; i++)
        {
            //Check if you have hit a wall in this direction
            if (hits[i].transform.tag == "Wall")
            {
                //Calculate the position of the wall, and then apply an offset based on moving direction
                float wallHitXPosition = hits[i].point.x;

                return MoveWillBeLeft(xStartPos, xEndPos) ? (wallHitXPosition + colliderExtent + minWallDistance) : (wallHitXPosition - colliderExtent - minWallDistance);
            }
        }

        return xEndPos;
    }
}
