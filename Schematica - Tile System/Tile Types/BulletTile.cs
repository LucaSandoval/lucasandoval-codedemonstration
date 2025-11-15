using UnityEngine;
using System;

public class BulletTile : ATile
{
    // Base shoot timer for all bullet tiles, modified by fire rate from entity
    private const float baseShootTimerMax = 1.65f;

    private float shootTimer;
    private float shootTimerMax;

    private ITileEntity ownerEntity;
    protected BulletFireType fireType;

    protected override void Start()
    {
        base.Start();
        SetShootTimerMax();
        shootTimer = 0;
    }

    private void SetShootTimerMax()
    {
        if (ownerEntity == null)
        {
            ownerEntity = GetComponentInParent<ITileEntity>();
        }
        if (ownerEntity != null)
        {
            float entityFireRate = ownerEntity.GetEntityAttribute(TileStatAttribute.EntityFireRate);
            float shootTimerReduction = (1 + (entityFireRate / 100));
            shootTimerMax = baseShootTimerMax / shootTimerReduction;
        }
    }
        
    protected override void Update()
    {
        base.Update();
        // Shoot timer
        if (shootTimer > 0)
        {
            shootTimer -= Time.deltaTime;
        }
        // Keep max fire rate updated
        SetShootTimerMax();
    }

    // Can bullet tile fire this bullet
    public bool CanFire()
    {
        return shootTimer <= 0;
    }

    public (float, float) GetFireTimers()
    {
        return (shootTimer, shootTimerMax);
    }

    // Resets the shoot timer
    private void ResetShootTimer()
    {
        shootTimer = shootTimerMax;
    }

    public override void RecieveEntityActionEvent(EntityActionEvent actionEvent)
    {
        BulletFireEvent bulletFireEvent = (BulletFireEvent)actionEvent;
        if (bulletFireEvent != null && CanFire())
        {
            EntityFactory.Instance?.SpawnBullet(bulletFireEvent.bulletTileData, bulletFireEvent.bulletMovementParams,
                transform.position, GetFireDirectionForFireEvent(bulletFireEvent), bulletFireEvent.bulletRotation,
                bulletFireEvent.collisionCatagory, bulletFireEvent.CalculateBulletLifetime());
            PlayDamageEffect();
            ResetShootTimer();

            // Only shake the camera if we're close enough
            if (player)
            {
                if (Vector2.Distance(transform.position, player.transform.position) <= 15f)
                {
                    SoundController.Instance?.PlaySoundOneShotRandomPitch(SoundBank.Instance.PlayerShootSound, 0.05f);
                    CameraController.Instance?.ShakeCamera(0.1f, 0.07f);
                }
            } else
            {
                CameraController.Instance?.ShakeCamera(0.1f, 0.07f);
            }
        }
    }

    private Vector2 GetFireDirectionForFireEvent(BulletFireEvent bulletFireEvent)
    {
        switch(fireType)
        {
            default:
            case BulletFireType.up:
                return transform.up;
            case BulletFireType.down:
                return -transform.up;
            case BulletFireType.left:
                return -transform.right;
            case BulletFireType.right:
                return transform.right;
            case BulletFireType.followmouse:
                return bulletFireEvent.bulletDirection;
        }
    }

    public override void InitTile(TileData tileData, TileCollisionCatagory collisionCatagory, string arguments)
    {
        base.InitTile(tileData, collisionCatagory, arguments);
        // use argument to determine fire type
        if (arguments != "")
        {
            fireType = (BulletFireType)Enum.Parse(typeof(BulletFireType), arguments.ToLower());
        } else
        {
            fireType = BulletFireType.followmouse;
        }
    }
}

/// <summary>
/// Fire direction for this bullet
/// </summary>
public enum BulletFireType
{
    up, down, left, right, followmouse
}
