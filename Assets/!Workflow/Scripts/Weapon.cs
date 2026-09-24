using UnityEngine;

public class Weapon : MonoBehaviour
{
    private PlayerController playerController;

    void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
    }

    public void EndAttack()
    {
        playerController.EndAttack();
    }
}
