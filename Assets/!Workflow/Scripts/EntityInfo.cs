using UnityEngine;
using VersusFight;

public class EntityInfo : MonoBehaviour
{
    public int NetworkId { get; set; }
    public EntityType Type { get; set; }
    public int HP { get; set; } = 10;
    public float Attack { get; set; } = 2;
    public float Defense { get; set; } = 1;
}