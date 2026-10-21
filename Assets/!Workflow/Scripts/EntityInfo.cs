using UnityEngine;
using VersusFight;

[System.Serializable]
public class EntityInfo
{
    [field: SerializeField]
    public int NetworkId { get; set; }
    
    [field: SerializeField]
    public EntityType Type { get; set; }
    
    [field: SerializeField]
    public int HP { get; set; } = 10;
    
    [field: SerializeField]
    public float Attack { get; set; } = 2;

    [field: SerializeField]
    public float Defense { get; set; } = 1;
}