using UnityEngine;

public class BombTile : ATile
{
    private (float, float) ExplosionDamageParams = (0f, 0.8f);
    private float ExplosionRadius = 3f;
    private bool exploding;

    public override void DamageTile(TileStat<TileDamageCategory> damage)
    {
        if (exploding) return;
        exploding = true;
        if (damage.Ammount >= 0)
        {
            // Explode
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, ExplosionRadius);
            foreach(Collider2D hit in hits)
            {
                ATile otherTile = hit.gameObject.GetComponent<ATile>();
                if (otherTile == null) continue;
                // Ignore self 
                if (otherTile == this) continue;

                // Also ignore tiles with the same collision catagory
                if (GetCollisionCatagory() != TileCollisionCatagory.none)
                {
                    if (GetCollisionCatagory() == otherTile.GetCollisionCatagory()) continue;
                    if (GetCollisionCatagory() == TileCollisionCatagory.enemyBullet
                        && otherTile.GetCollisionCatagory() == TileCollisionCatagory.enemyBody) continue;
                    if (GetCollisionCatagory() == TileCollisionCatagory.enemyBody
                        && otherTile.GetCollisionCatagory() == TileCollisionCatagory.enemyBullet) continue;
                }


                // Deal random range of damage
                otherTile.DamageTile(new TileStat<TileDamageCategory>(TileDamageCategory.ExplosionDamage,
                    Random.Range(ExplosionDamageParams.Item1, ExplosionDamageParams.Item2)));
            }
            CameraController.Instance?.ShakeCamera(0.2f, 0.1f);
            VFXFactory.Instance?.SpawnExplosionTileEffect(transform.position);
            SoundController.Instance.PlaySoundOneShotRandomPitch(SoundBank.Instance.BombTileExplosion, 0.05f);
            DestroyTile();
        }
    }
}
