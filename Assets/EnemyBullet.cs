using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public int damage = 10;

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject);
    }

    private void HandleHit(GameObject hitObject)
    {
        if (hitObject == null || !hitObject.CompareTag("Player")) return;

        PlayerStatus status = hitObject.GetComponent<PlayerStatus>();
        if (status == null)
        {
            status = hitObject.GetComponentInParent<PlayerStatus>();
        }

        if (status != null)
        {
            status.TakeDamage(damage);
        }
        else
        {
            Debug.LogWarning("EnemyBullet: PlayerStatus was not found on the Player.", hitObject);
        }

        Destroy(gameObject);
    }
}
