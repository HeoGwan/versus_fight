using UnityEngine;

public class Entity : MonoBehaviour
{
    public void Hit()
    {
        Debug.Log($"{gameObject.name} Hit!");
    }
}
