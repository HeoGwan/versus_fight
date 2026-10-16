using System;


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
        MaxConnect = 8,
        Disconnect = 255,
    }
    // 패킷 구조체
    public struct Packet
    {
        // Header
        public PacketType packetType;   // 1
        public EntityType entityType;   // 1
        public int networkId;           // 4
        // 6 bytes

        // Body
        public byte[] data;
    }
    
    public struct MovePacket
    {
        public float x;        // 4
        public float y;        // 4
        public float z;        // 4

        public float rotX;     // 4
        public float rotY;     // 4
        public float rotZ;     // 4
        public float rotW;     // 4
    } // 28 bytes
    public struct HitPacket
    {
        // Vector3
        public float knockbackDirectionX;   // 4
        public float knockbackDirectionY;   // 4
        public float knockbackDirectionZ;   // 4
    }; // 12 bytes
    public struct AttackPacket
    {
        // Vector3
        public float attackDirectionX;  // 4
        public float attackDirectionY;  // 4
        public float attackDirectionZ;  // 4

        // 공격력
        public float attack;            // 4
    }; // 16 bytes
    public struct DisconnectPacket
    {
        // public int networkId;   // 4
    }; // 4 bytes


    public class PacketSerializer
    {
        private const uint PACKET_HEADER_SIZE = 6;

        private const uint MOVE_PACKET_SIZE = 28;
        private const uint HIT_PACKET_SIZE = 12;
        private const uint ATTACK_PACKET_SIZE = 16;
        private const uint DISCONNECT_PACKET_SIZE = 1;


        // 패킷 직렬화 (전송 시)
        public static byte[] SerializePacket(Packet packet)
        {
            int dataLength = packet.data.Length;
            byte[] result = new byte[PACKET_HEADER_SIZE + dataLength]; // packetType + networkId + entityType + data
            result[0] = (byte)packet.packetType;
            result[1] = (byte)packet.entityType;
            Buffer.BlockCopy(BitConverter.GetBytes(packet.networkId), 0, result, 2, 4);

            for (int i = 0; i < dataLength; ++i)
            {
                result[i + PACKET_HEADER_SIZE] = packet.data[i];
            }

            return result;
        }

        // 패킷 역직렬화 (수신 시)
        public static Packet DeserializePacket(byte[] data)
        {
            byte[] packetData = new byte[data.Length - PACKET_HEADER_SIZE];
            Buffer.BlockCopy(data, (int)PACKET_HEADER_SIZE, packetData, 0, packetData.Length);

            return new Packet()
            {
                packetType = (PacketType)data[0],
                entityType = (EntityType)data[1],
                networkId = BitConverter.ToInt32(data, 2),

                data = packetData
            };
        }

        // 이동 패킷
        public static byte[] SerializeMove(MovePacket packet)
        {
            byte[] data = new byte[MOVE_PACKET_SIZE];

            // Buffer.BlockCopy(BitConverter.GetBytes(packet.networkId), 0, data, 0, 4);

            Buffer.BlockCopy(BitConverter.GetBytes(packet.x), 0, data, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(packet.y), 0, data, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(packet.z), 0, data, 8, 4);

            Buffer.BlockCopy(BitConverter.GetBytes(packet.rotX), 0, data, 12, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(packet.rotY), 0, data, 16, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(packet.rotZ), 0, data, 20, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(packet.rotW), 0, data, 24, 4);

            return data;
        }

        public static MovePacket DeserializeMove(byte[] data)
        {
            return new MovePacket
            {
                // networkId = BitConverter.ToInt32(data, 0),

                x = BitConverter.ToSingle(data, 0),
                y = BitConverter.ToSingle(data, 4),
                z = BitConverter.ToSingle(data, 8),

                rotX = BitConverter.ToSingle(data, 12),
                rotY = BitConverter.ToSingle(data, 16),
                rotZ = BitConverter.ToSingle(data, 20),
                rotW = BitConverter.ToSingle(data, 24),
            };
        }

        // 피격 패킷
        public static byte[] SerializeHit(HitPacket packet)
        {
            byte[] data = new byte[HIT_PACKET_SIZE];

            // Buffer.BlockCopy(BitConverter.GetBytes(packet.networkId), 0, data, 0, 4);

            Buffer.BlockCopy(BitConverter.GetBytes(packet.knockbackDirectionX), 0, data, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(packet.knockbackDirectionY), 0, data, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(packet.knockbackDirectionZ), 0, data, 8, 4);

            return data;
        }

        public static HitPacket DeserializeHit(byte[] data)
        {
            return new HitPacket()
            {
                // networkId = BitConverter.ToInt32(data, 0),

                knockbackDirectionX = BitConverter.ToSingle(data, 0),
                knockbackDirectionY = BitConverter.ToSingle(data, 4),
                knockbackDirectionZ = BitConverter.ToSingle(data, 8),
            };
        }

        // 공격 패킷
        public static byte[] SerializeAttack(AttackPacket packet)
        {
            byte[] data = new byte[ATTACK_PACKET_SIZE];

            // Buffer.BlockCopy(BitConverter.GetBytes(packet.networkId), 0, data, 0, 4);

            Buffer.BlockCopy(BitConverter.GetBytes(packet.attackDirectionX), 0, data, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(packet.attackDirectionY), 0, data, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(packet.attackDirectionZ), 0, data, 8, 4);

            Buffer.BlockCopy(BitConverter.GetBytes(packet.attack), 0, data, 12, 4);

            return data;
        }

        public static AttackPacket DeserializeAttack(byte[] data)
        {
            return new AttackPacket()
            {
                // networkId = BitConverter.ToInt32(data, 0),

                attackDirectionX = BitConverter.ToSingle(data, 0),
                attackDirectionY = BitConverter.ToSingle(data, 4),
                attackDirectionZ = BitConverter.ToSingle(data, 8),

                attack = BitConverter.ToSingle(data, 12),
            };
        }


        // 연결 종료 패킷
        public static byte[] SerializeDiconnect(DisconnectPacket packet)
        {
            byte[] data = new byte[DISCONNECT_PACKET_SIZE];

            // Buffer.BlockCopy(BitConverter.GetBytes(packet.networkId), 0, data, 0, 4);

            return data;
        }

        public static DisconnectPacket DeserializeDiconnect(byte[] data)
        {
            return new DisconnectPacket()
            {
                // networkId = BitConverter.ToInt32(data, 0),
            };
        }
    }
}