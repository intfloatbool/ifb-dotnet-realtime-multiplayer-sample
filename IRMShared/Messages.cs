using System;
using MessagePack;

namespace IRMShared
{
    public static partial class Messages
    {
        
        public static bool TryDeserializeRawMessage(byte[] rawBytes, out RawMessage result)
        {
            result = default;
            try
            {
                result = MessagePackSerializer.Deserialize<RawMessage>(rawBytes);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryDeserializeMessageBody(RawMessage rawMessage, out object inner)
        {
            inner = null;
            if (rawMessage.BodyData == null)
            {
                return false;
            }
            try
            {
                switch (rawMessage.MessageType)
                {
                    case EMessageType.REQ_USER_INFO:
                    {
                        inner = MessagePackSerializer.Deserialize<ResponseUserInfoBody>(rawMessage.BodyData!);
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }
    }
}