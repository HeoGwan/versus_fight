using UnityEngine;

public class AttackArea : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Entity")) return;

        Entity entity = other.GetComponentInParent<Entity>();
        entity.Hit(transform.position);
    }
}