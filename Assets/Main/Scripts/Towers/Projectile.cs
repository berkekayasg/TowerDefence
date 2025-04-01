using UnityEngine;
using TowerDefence.Core;

public class Projectile : MonoBehaviour
{
    // Attributes are now set via Initialize method
    private float _speed;
    private float _damage;
    private Transform target;

    // Initialize the projectile with necessary data from the tower
    public void Initialize(Transform _target, float projectileSpeed, float projectileDamage)
    {
        target = _target;
        _speed = projectileSpeed;
        _damage = projectileDamage;
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = target.position - transform.position;
        float distanceThisFrame = _speed * Time.deltaTime; // Use initialized speed

        if (dir.magnitude <= distanceThisFrame)
        {
            HitTarget();
            return;
        }

        transform.Translate(dir.normalized * distanceThisFrame, Space.World);
    }

    void HitTarget()
    {
        // Play impact VFX using VFXManager
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayVFX("ProjectileImpact", transform.position, transform.rotation);
        }

        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(_damage); // Use initialized damage
        }

        Destroy(gameObject);
    }
}
