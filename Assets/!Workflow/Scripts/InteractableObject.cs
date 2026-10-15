using UnityEngine;

namespace VersusFight
{
    public class InteractableObject : Entity
    {
        [SerializeField] private float hitPower = 10f;

        public override void Hit(Vector3 attackAreaPosition, float attack)
        {
            Vector3 hitDirection = transform.position - attackAreaPosition;
            float distance = Vector3.Distance(attackAreaPosition, transform.position);

            _rigid.AddForce(hitDirection * distance * hitPower);
        }
    }
}