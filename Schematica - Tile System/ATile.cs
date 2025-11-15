using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents the base functionality of all tiles in the game.
/// </summary>
public abstract class ATile : MonoBehaviour, ITile
{
    protected ITileOwner owner;
    protected TileData tileData;

    protected SpriteTileVisualizationComponent visualizationComponent;
    protected BoxCollider2D col;
    protected GameObject tileVisualObject;

    private bool componentsRegistered;
    private const float damageTimerMax = 0.2f;
    private float damageTimer;

    private float tileVisualScale;
    private const float tileVisualScaleMax = 1.25f;

    private const float baseDamageKnockback = 10f;

    protected TileCollisionCatagory collisionCatagory;

    // Mutable stats
    protected float currentHealth;
    protected GameObject player;


    private void Awake()
    {
        EnsureComponents();
        currentHealth = 1;
        tileVisualScale = 1;
        damageTimer = damageTimerMax;
    }

    // Ensures all dependant components have been located already
    protected void EnsureComponents()
    {
        if (!componentsRegistered)
        {
            visualizationComponent = GetComponentInChildren<SpriteTileVisualizationComponent>();
            if (visualizationComponent) tileVisualObject = visualizationComponent.gameObject;
            col = GetComponent<BoxCollider2D>();

            componentsRegistered = true;
        }
    }

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    protected virtual void Update()
    {
        // Check for death
        if (currentHealth <= 0)
        {
            DestroyTile();
        }
        // Damage i-frame timer
        if (damageTimer > 0)
        {
            damageTimer -= Time.deltaTime;
        }
        // Render tile
        RenderTile();
    }

    // Returns whether or not this tile can current take damage. 
    private bool CanTakeDamage()
    {
        return damageTimer <= 0;
    }

    // Makes tile invulnerable from damage for a short moment.
    private void TriggerHitInvulnerability()
    {
        damageTimer = damageTimerMax;
    }

    // Script to initialize tile with tile data.
    public virtual void InitTile(TileData tileData, TileCollisionCatagory collisionCatagory, string arguments)
    {
        EnsureComponents();

        this.tileData = tileData;
        this.collisionCatagory = collisionCatagory;
        gameObject.name = tileData.TileName;

        // Initalize stats
        currentHealth = tileData.GetAttributeValue(TileStatAttribute.TileHealth);

        // If player owned, give player tile layer for special collision properties
        gameObject.layer = GetLayerFromCollisionCatagory(collisionCatagory);

        // Initialize rendering
        visualizationComponent.SetData(tileData, collisionCatagory);
        visualizationComponent.CreateTileVisualization();

        RenderTile();
    }

    /// <summary>
    /// Returns the collision catagory of this tile.
    /// </summary>
    public TileCollisionCatagory GetCollisionCatagory()
    {
        return collisionCatagory;
    }

    /// <summary>
    /// Gets the correct layer name from given tile collision catagory.
    /// </summary>
    /// <param name="collisionCatagory">Given collision catagory.</param>
    /// <returns>String of layer name</returns>
    private int GetLayerFromCollisionCatagory(TileCollisionCatagory collisionCatagory)
    {
        string layerName = "Default";
        switch(collisionCatagory)
        {
            case TileCollisionCatagory.playerOwned:
                layerName = "PlayerOwnedTile";
                break;
            case TileCollisionCatagory.enemyBody:
                layerName = "EnemyBodyTile";
                break;
            case TileCollisionCatagory.enemyBullet:
                layerName = "EnemyBulletTile";
                break;
        }
        return LayerMask.NameToLayer(layerName);
    }

    public void RemoveOwner()
    {
        if (owner != null)
        {
            owner.RemoveTile(this);
            owner = null;
        }
    }

    public void SetOwner(ITileOwner owner)
    {
        if (this.owner != owner)
        {
            RemoveOwner();
            this.owner = owner;
            owner.AddTile(this);
        }
    }

    /// <summary>
    /// Renders the visual aspect of this tile. 
    /// </summary>
    protected virtual void RenderTile()
    {
        if (visualizationComponent)
        {
            if (tileData != null)
            {
                // Control damage 'bounce' effect
                if (tileVisualScale > 1)
                {
                    tileVisualScale -= Time.deltaTime;
                }
                if (tileVisualScale > tileVisualScaleMax)
                {
                    tileVisualScale = tileVisualScaleMax;
                }
                tileVisualObject.transform.localScale = new Vector2(tileVisualScale, tileVisualScale);
            }
        }
    }

    /// <summary>
    /// Effect to play when tile takes damage.
    /// </summary>
    protected virtual void PlayDamageEffect()
    {
        tileVisualScale = tileVisualScaleMax;
    }

    public GameObject GetTileObject()
    {
        return gameObject;
    }

    protected virtual void OnCollisionStay2D(Collision2D collision)
    {
        ITile otherTile = collision.collider.gameObject.GetComponent<ITile>();
        if (otherTile != null)
        {
            // Other tile takes collisionDamage of this tile
            otherTile.DamageTile(new TileStat<TileDamageCategory>(TileDamageCategory.CollisionDamage, 
                tileData.GetAttributeValue(TileStatAttribute.CollisionDamage)));

            // This tile takes collisionDamage of collided tile
            DamageTile(new TileStat<TileDamageCategory>(TileDamageCategory.CollisionDamage, 
                otherTile.GetTileData().GetAttributeValue(TileStatAttribute.CollisionDamage)));

            // If damage was dealt on either side, try a knockback
            if (tileData.GetAttributeValue(TileStatAttribute.CollisionDamage) > 0 
                || otherTile.GetTileData().GetAttributeValue(TileStatAttribute.CollisionDamage) > 0)
            {
                // Apply nockback to other tile if damage is dealt
                ITileEntity otherEntity = collision.collider.gameObject.GetComponent<ITileEntity>();
                if (otherEntity != null) otherEntity.KnockbackEntity(baseDamageKnockback);

                // Apply nockback to this tile
                ITileEntity thisEntity = GetComponentInParent<ITileEntity>();
                if (thisEntity != null) thisEntity.KnockbackEntity(baseDamageKnockback);
            }
        }
    }

    public void ChangeTileHealth(float ammount)
    {
        currentHealth += ammount;
        // Make sure you don't overflow 'max' health
        if (currentHealth > tileData.GetAttributeValue(TileStatAttribute.TileHealth))
        {
            currentHealth = tileData.GetAttributeValue(TileStatAttribute.CollisionDamage);
        }
    }

    public virtual void DamageTile(TileStat<TileDamageCategory> damage)
    {
        // Calculate damage taking into account defence
        float dealtDamage = damage.Ammount;
        if (damage.Attribute == TileDamageCategory.CollisionDamage)
        {
            dealtDamage -= tileData.GetAttributeValue(TileStatAttribute.CollisionDefence);
            dealtDamage = (dealtDamage < 0) ? 0 : dealtDamage; 
        }

        // Play damage effect, trigger invulnerability time
        if (CanTakeDamage())
        {
            if (dealtDamage > 0) 
            {
                TriggerHitInvulnerability();
                SoundController.Instance?.PlaySoundOneShotRandomPitch(SoundBank.Instance.DamageTileSound, 0.05f);
            }
            if (dealtDamage > 0) PlayDamageEffect();
        }
        else return;

        ChangeTileHealth(-dealtDamage);
        // Spawn health bar
        if (dealtDamage > 0)
        {
            UIManager.Instance?.CreateTileHealthBar(this);
            UIManager.Instance?.CreateTileDamagePopupNumber(dealtDamage, transform.position);
            VFXFactory.Instance?.SpawnTileDamageEffect(tileData, transform.position);
        }
    }

    /// <summary>
    /// Given an entity, applys a random radial force outwards from its current position.
    /// </summary>
    /// <param name="entity">Entity to apply radial force to.</param>
    /// <param name="variableForce">Whether or not to apply a random ammount of force up to force ammount.</param>
    /// <param name="force">Force ammount</param>
    protected void ApplyRandomRadialSpawnForce(IMovingEntity entity, bool variableForce, float force)
    {
        // Apply force
        int randDegree = Random.Range(0, 360);
        Vector2 forceVector = new Vector2(Mathf.Cos(randDegree), Mathf.Sin(randDegree)).normalized;
        float forceScalar = (variableForce) ? Random.Range(force / 10, force) : force;
        entity.MoveEntity(forceVector * forceScalar);
    }

    public virtual void DestroyTile()
    {
        if (Vector2.Distance(transform.position, player.transform.position) <= 15f)
        {
            SoundController.Instance?.PlaySoundOneShotRandomPitch(SoundBank.Instance.TileDestroySound, 0.05f);
        }

        VFXFactory.Instance?.SpawnTileDeathEffect(tileData, transform.position);
        UIManager.Instance?.RemoveTileHealthBar(this);
        RemoveOwner();

        // If this is an enemy body tile, drop XP based on progression level
        if (gameObject.layer == GetLayerFromCollisionCatagory(TileCollisionCatagory.enemyBody))
        {
            int xpOrbsToDrop = Mathf.RoundToInt(Mathf.Lerp(2, 12, 
                Mathf.InverseLerp(1, 15, PlayerProgressionComponent.maxProgressionDifficulty)));
            List<ExpirienceOrbCollectable> spawnedXP =
            LootFactory.Instance?.SpawnDistributedValueExpirienceOrbs(20, xpOrbsToDrop, transform.position);
            foreach (var orb in spawnedXP)
            {
                ApplyRandomRadialSpawnForce(orb, true, 5f);
            }
        }

        Destroy(gameObject);
    }

    public virtual TileData GetTileData()
    {
        return tileData;
    }

    public ITileOwner GetTileOwner()
    {
        return owner;
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public virtual void RecieveEntityActionEvent(EntityActionEvent actionEvent) { }
}
