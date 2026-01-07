using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ENet;
using IRMClient.Protocol;
using IRMClient.State;
using IRMShared;
using MessagePack;
using R3;

namespace IRMClient
{
    public sealed class ClientInstance : IDisposable, IClientStateHolder
    {
        private const int LOOP_MS = 33; // 30fps
        public ClientState ClientState { get; } = new ClientState();
        
        public ReadOnlyReactiveProperty<bool> IsReady => _isReady;
        private readonly ReactiveProperty<bool> _isReady = new ReactiveProperty<bool>();
        
        private readonly ConcurrentQueue<Messages.RawMessage> _messagesQueue = new ConcurrentQueue<Messages.RawMessage>();

        public Observable<Messages.RawMessage> MessageReceived => _messageReceived;
        private readonly Subject<Messages.RawMessage> _messageReceived = new Subject<Messages.RawMessage>();

        private readonly BytesBufferArrayPool _bufferArrayPool = new BytesBufferArrayPool(1024, 10);

        private readonly List<IClientMessagesHandler> _messagesHandlers = new List<IClientMessagesHandler>();

        private readonly CancellationTokenSource _selfCts = new CancellationTokenSource();


        private readonly object peerLock = new object();

        public void AddMessageHandler(IClientMessagesHandler messagesHandler)
        {
            messagesHandler.HandleSetup(_messageReceived, EnqueueMessageToBeSend);
            _messagesHandlers.Add(messagesHandler);
        }
        
        public async Task StartAsync(ClientConfiguration configuration, string serverHostname, ushort port, CancellationToken token, Action<Exception> exHandler = null)
        {
            
            if (!Library.Initialize())
            {
                throw new Exception($"{GetType().Name}.StartAsync(): Enet Library.Initialize failed.");
            }

            _isReady.Value = false;

            Peer createdPeer = default;
            using var host = new Host();
            try
            {
                Address address = new Address();
                if (!address.SetHost(serverHostname))
                {
                    throw new Exception($"[{GetType().Name}] address.SetHost() failed.");
                }
                address.Port = port;
                host.Create();
             
                createdPeer = host.Connect(address, configuration.ChannelLimit);

                Event netEvent = default;

                //_ = InputCommandsLoopAsync(createdPeer, token);

                _ = Task.Run(() => InputCommandsLoopAsync(createdPeer, token), token);
                
                _isReady.Value = true;
                while (!token.IsCancellationRequested)
                {
                    bool isPolled = false;
                    while (!isPolled)
                    {
                        if (host.CheckEvents(out netEvent) <= 0)
                        {
                            if (host.Service(configuration.HostServiceTimeoutMs, out netEvent) <= 0)
                            {
                                break;
                            }

                            isPolled = true;
                        }
                    }

                    switch (netEvent.Type)
                    {
                        case EventType.None:
                        {
                            break;
                        }

                        case EventType.Connect:
                        {
                            IRMLogger.LogMsg("Client connected to server");
                            break;
                        }

                        case EventType.Disconnect:
                        {
                            IRMLogger.LogMsg("Client disconnected from server");
                            break;
                        }

                        case EventType.Timeout:
                        {
                            IRMLogger.LogMsg("Client connection timeout");
                            break;
                        }
                        
                        case EventType.Receive:
                        {
                            IRMLogger.LogMsg("Packet received from server - Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                            lock (peerLock)
                            {
                                CopyPayloadFromPacket(ref netEvent);
                            }
                            netEvent.Packet.Dispose();
                            break;
                        }
                            
                    }
                    
                    host.Flush();
                    await Task.Delay(LOOP_MS, token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                exHandler?.Invoke(ex);
                throw;
            }
            finally
            {
                if (createdPeer.IsSet)
                {
                    createdPeer.Disconnect(0);

                    while (host.Service(1000, out var netEvent) > 0)
                    {
                        if (netEvent.Type == EventType.Disconnect)
                        {
                            break;
                        }
                    }
                }
                Library.Deinitialize();
            }
        }

        private async Task InputCommandsLoopAsync(Peer peer, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                while (!_messagesQueue.IsEmpty && peer.IsSet)
                {
                    if (_messagesQueue.TryDequeue(out var msg))
                    {
                        HandleDequeuedMessage(msg, ref peer);
                    }

                    await Task.Delay(LOOP_MS, token).ConfigureAwait(false);
                }
                
                await Task.Delay(LOOP_MS, token).ConfigureAwait(false);
            }
            
        }

        private void HandleDequeuedMessage(Messages.RawMessage msg, ref Peer peer)
        {
            Packet packet = default(Packet);
            var data = MessagePackSerializer.Serialize(msg);

            PacketFlags? packetFlags = null;

            switch (msg.MessageFlag)
            {
                case EMessageFlag.INSTANT:
                {
                    packetFlags = PacketFlags.Instant;
                    break;
                }
                case EMessageFlag.RELIABLE:
                {
                    packetFlags = PacketFlags.Reliable;
                    break;
                }
                case EMessageFlag.UNRELIABLE:
                {
                    packetFlags = PacketFlags.UnreliableFragmented;
                    break;
                }
                case EMessageFlag.UNSEQUENCED:
                {
                    packetFlags = PacketFlags.Unsequenced;
                    break;
                }
            }

            if (packetFlags.HasValue)
            {
                packet.Create(data, packetFlags.Value);
            }
            else
            {
                packet.Create(data);
            }
            //Console.WriteLine($"CLIENT_XXX {peer.ID} send packet");
            var peerIn = peer;
            Func<bool> sendFunc = () => peerIn.Send((byte)msg.Channel, ref packet);

            int attempts = 10;
            SendAttemptsAsync(sendFunc, msg.MessageType, attempts, _selfCts.Token).ContinueWith((res) =>
            {
                if (!res.Result)
                {
                    packet.Dispose();
                    IRMLogger.LogErr(
                        $"[{GetType().Name}].HandleDequeuedMessage() , send() failed! {msg.MessageType} , debugFrom?: {msg.DebugFrom} after {attempts} attempts!");
                }
                else
                {
                    IRMLogger.LogMsg($"[{GetType().Name}].HandleDequeuedMessage() msg sent success.");
                }
            }).ConfigureAwait(false);


        }

        private async Task<bool> SendAttemptsAsync(Func<bool> sendFunc, EMessageType msgType, int attemptsCount, CancellationToken token)
        {
            for (int i = 0; i < attemptsCount; i++)
            {
                lock (peerLock)
                {
                    bool sendRes = sendFunc.Invoke();
                    if (sendRes)
                    {
                        return true;
                    }
                }
                await Task.Delay(LOOP_MS, token).ConfigureAwait(false);
            }

            return false;
        }

        private void CopyPayloadFromPacket(ref Event netEvent)
        {
            byte[] buffer = _bufferArrayPool.Rent();
            netEvent.Packet.CopyTo(buffer);

            if (Messages.TryDeserializeRawMessage(buffer, out var rawMessage))
            {
                _messageReceived.OnNext(rawMessage);
            }
            else
            {
                IRMLogger.LogErr($"[{GetType().Name}].CopyPayloadFromPacket() can't deserialize message!");
            }
            
            _bufferArrayPool.Return(buffer);
        }

        public void EnqueueMessageToBeSend(Messages.RawMessage message) 
        {
            //Console.WriteLine($"CLIENT_XXX EnqueueMessageToBeSend() msg: {message.MessageType}");
            _messagesQueue.Enqueue(message);
        }

        public async Task WaitForRegistrationSuccessAsync(CancellationToken timeoutToken)
        {
            timeoutToken.ThrowIfCancellationRequested();
            
            while (!ClientState.UserInfo.IsReady)
            {
                await Task.Delay(100, timeoutToken).ConfigureAwait(false);
            }
            
            timeoutToken.ThrowIfCancellationRequested();
        }

        public void Dispose()
        {
            _selfCts?.Cancel();
            _selfCts?.Dispose();
            foreach (var msgHandler in _messagesHandlers)
            {
                msgHandler.Dispose();
            }
            _messageReceived.Dispose();
        }
    }
}