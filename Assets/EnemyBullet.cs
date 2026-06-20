using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public int damage = 10;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerStatus status = other.GetComponent<PlayerStatus>();
        if (status == null)
        {
            status = other.GetComponentInParent<PlayerStatus>();
        }

        if (status != null)
        {
            status.TakeDamage(damage);
        }
        else
        {
            Debug.LogWarning("EnemyBullet: PlayerStatus was not found on the Player.", other);
        }

        Destroy(gameObject);
    }
}
