using System;
using UnityEngine;

using VersusFight;

public class AttackArea : MonoBehaviour
{
    public EntityInfo playerInfo;


    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Entity")) return;

        Entity entity = other.GetComponentInParent<Entity>();
        entity.Hit(transform.position, playerInfo.Attack);

        // 네트워크로 공격 패킷 전송
        if (VNetworkManager.Instance == null) return;
        Vector3 attackAreaPos = transform.position;
        Debug.Log($"attackAreaPos : {attackAreaPos}");
        AttackPacket attackPacket = new AttackPacket()
        {
            networkId = VNetworkManager.Instance.NetworkId,

            attackDirectionX = attackAreaPos.x,
            attackDirectionY = attackAreaPos.y,
            attackDirectionZ = attackAreaPos.z,

            attack = playerInfo.Attack
        };
        VNetworkManager.Instance.SendPacket(PacketType.Attack,
                PacketSerializer.SerializeAttack(attackPacket));
    }

    public void SetInfo(ref EntityInfo info)
    {
        playerInfo = info;
    }
}