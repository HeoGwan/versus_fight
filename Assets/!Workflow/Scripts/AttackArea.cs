using UnityEngine;

using VersusFight;


public class AttackArea : MonoBehaviour
{
    private Weapon weapon;
    public EntityInfo playerInfo;

    void Start()
    {
        weapon = GetComponentInParent<Weapon>();
        if (weapon == null)
        {
            Debug.Log("no has weapon in parent");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Entity")) return;

        Debug.Log($"object : {other.gameObject.name}");

        // 엔티티 공격
        Entity entity = other.GetComponentInParent<Entity>();
        entity.Hit(transform.position, playerInfo.Attack);

        // 플레이어 공격 멈춤
        weapon.EndAttack();

        // 네트워크로 공격 패킷 전송
        if (VNetworkManager.Instance == null) return;
        Vector3 attackAreaPos = transform.position;
        
        AttackPacket attackPacket = new AttackPacket()
        {
            // networkId = VNetworkManager.Instance.NetworkId,

            attackDirectionX = attackAreaPos.x,
            attackDirectionY = attackAreaPos.y,
            attackDirectionZ = attackAreaPos.z,

            // attack = playerInfo.Attack
        };
        VNetworkManager.Instance.SendPacket(PacketType.Attack, EntityType.PLAYABLE,
                PacketSerializer.SerializeAttack(attackPacket));
    }

    public void SetInfo(ref EntityInfo info)
    {
        playerInfo = info;
    }
}