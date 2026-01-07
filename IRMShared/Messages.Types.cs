using System;
using MessagePack;

namespace IRMShared
{
    
    public static partial class Messages
    {
        [MessagePackObject]
        public class RawMessage
        {

            [IgnoreMember] 
            public string DebugFrom { get; set; } = string.Empty;
            
            [IgnoreMember]
            public EChannel Channel { get; set; }
            
            [Key(0)] 
            public EMessageFlag MessageFlag { get; set; } = EMessageFlag.RELIABLE;
            
            /// <summary>
            /// Header
            /// </summary>
            [Key(1)]
            public EMessageType MessageType { get; set; }

            /// <summary>
            /// Body to deserialize
            /// </summary>
            [Key(2)] 
            public byte[]? BodyData { get; set; }
            
            [Key(3)]
            public EMessageTarget Target { get; set; }
        }
        

        [MessagePackObject]
        public class ResponseUserInfoBody 
        {
            [Key(0)]
            public int Id { get; set; }
            [Key(1)]
            public bool IsMaster { get; set; }
        }

        [MessagePackObject]
        public class ClientStateMessage
        {
            [Key(0)]
            public int ClientId { get; set; }
            
            [Key(1)]
            public ulong Tick { get; set; }

            [Key(2)] 
            public float PosX { get; set; }
            
            [Key(3)] 
            public float PosY { get; set; }
            
            [Key(4)] 
            public float PosZ { get; set; }
            
            [Key(5)]
            public DateTime SentTimestamp { get; set; }
        }

        [MessagePackObject]
        public class AllClientsInfo
        {
            [Key(0)] 
            public ResponseUserInfoBody[] UserInfoBodyCollection { get; set; } = Array.Empty<ResponseUserInfoBody>() ;
        }
    }
}