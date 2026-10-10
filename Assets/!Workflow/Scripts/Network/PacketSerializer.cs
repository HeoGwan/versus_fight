using System;
using System.Runtime.InteropServices;

namespace VersusFight
{
    // 패킷 타입
    public enum PacketType : byte
    {
        None = 0,
        Connect = 1,
        Initialize = 2,
        Move = 3,
        Run = 4,
        Sprint = 5,
        Attack = 6,
        Hit = 7,
        DisConnect = 255,
    }
    // 패킷 구조체
    public struct Packet
    {
        public PacketType packetType;   // 1
        public byte[] data;
    }
    // public struct Packet
    // {
    //     public PacketType packetType;   // 1
    //     public int networkId;   // 4

    //     public float x;        // 4
    //     public float y;        // 4
    //     public float z;        // 4

    //     public float rotX;     // 4
    //     public float rotY;     // 4
    //     public float rotZ;     // 4
    //     public float rotW;     // 4
    // }  // 33 bytes
    public struct MovePacket
    {
        public int networkId;   // 4

        public float x;        // 4
        public float y;        // 4
        public float z;        // 4

        public float rotX;     // 4
        public float rotY;     // 4
        public float rotZ;     // 4
        public float rotW;     // 4
    } // 32 bytes
    public struct HitPacket
    {
        public int networkId;               // 4
        
        // Vector3
        public float knockbackDirectionX;   // 4
        public float knockbackDirectionY;   // 4
        public float knockbackDirectionZ;   // 4
    }; // 16 bytes
    public struct AttackPacket
    {
        public int networkId;           // 4

        // Vector3
        public float attackDirectionX;  // 4
        public float attackDirectionY;  // 4
        public float attackDirectionZ;  // 4

        // 공격력
        public float attack;            // 4
    }; // 20 bytes

    public class PacketSerializer
    {
        private const uint MOVE_PACKET_SIZE = 32;
        private const uint HIT_PACKET_SIZE = 16;
        private const uint ATTACK_PACKET_SIZE = 20;


        // 패킷 직렬화 (전송 시)
        public static byte[] SerializePacket(Packet packet)
        {
            int dataLength = packet.data.Length;
            byte[] result = new byte[dataLength + 1];
            result[0] = (byte)packet.packetType;

            for (int i = 0; i < dataLength; ++i)
            {
                result[i + 1] = packet.data[i];
            }

            return result;
        }

        // 패킷 역직렬화 (수신 시)
        public static Packet DeserializePacket(byte[] data)
        {
            byte[] packetData = new byte[data.Length - 1];
            Buffer.BlockCopy(data, 1, packetData, 0, packetData.Length);

            return new Packet()
            {
                packetType = (PacketType)data[0],
                data = packetData
            };
        }

        // 이동 패킷
        public static byte[] SerializeMove(MovePacket packet)
        {
            byte[] data = new byte[MOVE_PACKET_SIZE];

            Buffer.BlockCopy(BitConverter.GetBytes(packet.networkId), 0, data, 0, 4);

            Buffer.BlockCopy(BitConverter.GetBytes(packet.x), 0, data, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(packet.y), 0, data, 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(packet.z), 0, data, 12, 4);

            Buffer.BlockCopy(BitConverter.GetBytes(packet.rotX), 0, data, 16, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(packet.rotY), 0, data, 20, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(packet.rotZ), 0, data, 24, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(packet.rotW), 0, data, 28, 4);

            return data;
        }

        public static MovePacket DeserializeMove(byte[] data)
        {
            return new MovePacket
            {
                networkId = BitConverter.ToInt32(data, 0),

                x = BitConverter.ToSingle(data, 4),
                y = BitConverter.ToSingle(data, 8),
                z = BitConverter.ToSingle(data, 12),

                rotX = BitConverter.ToSingle(data, 16),
                rotY = BitConverter.ToSingle(data, 20),
                rotZ = BitConverter.ToSingle(data, 24),
                rotW = BitConverter.ToSingle(data, 28),
            };
        }

        // 피격 패킷
        public static byte[] SerializeHit(HitPacket packet)
        {
            byte[] data = new byte[HIT_PACKET_SIZE];

            Buffer.BlockCopy(BitConverter.GetBytes(packet.networkId), 0, data, 0, 4);

            Buffer.BlockCopy(BitConverter.GetBytes(packet.knockbackDirectionX), 0, data, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(packet.knockbackDirectionY), 0, data, 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(packet.knockbackDirectionZ), 0, data, 12, 4);

            return data;
        }

        public static HitPacket DeserializeHit(byte[] data)
        {
            return new HitPacket()
            {
                networkId = BitConverter.ToInt32(data, 0),

                knockbackDirectionX = BitConverter.ToSingle(data, 4),
                knockbackDirectionY = BitConverter.ToSingle(data, 8),
                knockbackDirectionZ = BitConverter.ToSingle(data, 12),
            };
        }

        // 공격 패킷
        public static byte[] SerializeAttack(AttackPacket packet)
        {
            byte[] data = new byte[ATTACK_PACKET_SIZE];

            Buffer.BlockCopy(BitConverter.GetBytes(packet.networkId), 0, data, 0, 4);

            Buffer.BlockCopy(BitConverter.GetBytes(packet.attackDirectionX), 0, data, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(packet.attackDirectionY), 0, data, 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(packet.attackDirectionZ), 0, data, 12, 4);

            Buffer.BlockCopy(BitConverter.GetBytes(packet.attack), 0, data, 16, 4);

            return data;
        }

        public static AttackPacket DeserializeAttack(byte[] data)
        {
            return new AttackPacket()
            {
                networkId = BitConverter.ToInt32(data, 0),

                attackDirectionX = BitConverter.ToSingle(data, 4),
                attackDirectionY = BitConverter.ToSingle(data, 8),
                attackDirectionZ = BitConverter.ToSingle(data, 12),

                attack = BitConverter.ToSingle(data, 16),
            };
        }
    }
}