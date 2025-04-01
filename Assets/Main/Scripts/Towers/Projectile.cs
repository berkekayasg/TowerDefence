using UnityEngine;
using TowerDefence.Core;

public class Projectile : MonoBehaviour
{
    [Header("Attributes")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float damage = 10f;

    private Transform target;

    public void Seek(Transform _target)
    {
        target = _target;
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = target.position - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

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
            enemy.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
