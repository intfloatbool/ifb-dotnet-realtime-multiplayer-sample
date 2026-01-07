using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ENet;
using IRMServer.Protocol;
using IRMShared;
using MessagePack;
using R3;

namespace IRMServer
{
    public sealed class ServerInstance : IDisposable, IServer
    {
        private const int LOOP_MS = 33; // 30fps
        
        private const int BUFFER_SIZE = 1024;

        public ReadOnlyReactiveProperty<bool> IsReady => _isReady;
        private readonly ReactiveProperty<bool> _isReady = new ReactiveProperty<bool>();
        
        public Observable<ConnectedClientInstance> ClientConnected => _clientConnected;
        private readonly Subject<ConnectedClientInstance> _clientConnected = new Subject<ConnectedClientInstance>();
        public Observable<ConnectedClientInstance> ClientDisconnected => _clientDisconnected;
        private readonly Subject<ConnectedClientInstance> _clientDisconnected = new Subject<ConnectedClientInstance>();

        public IReadOnlyDictionary<uint, ConnectedClientInstance> ConnectedClientsMapMap => _connectedClientsMap;
        private readonly Dictionary<uint, ConnectedClientInstance> _connectedClientsMap = new Dictionary<uint, ConnectedClientInstance>();
        
        
        public Observable<Messages.RawMessage> MessageReceived => _messageReceived;
        private readonly Subject<Messages.RawMessage> _messageReceived = new Subject<Messages.RawMessage>();
        
        public Observable<(ConnectedClientInstance, Messages.RawMessage)> MessageReceivedFrom => _messageReceivedFrom;
        private readonly Subject<(ConnectedClientInstance, Messages.RawMessage)> _messageReceivedFrom = new Subject<(ConnectedClientInstance, Messages.RawMessage)>();

        private readonly BytesBufferArrayPool _bufferArrayPool = new BytesBufferArrayPool(BUFFER_SIZE, 10);
        
        private readonly ConcurrentQueue<(ConnectedClientInstance, Messages.RawMessage)> _messagesQueue = new ConcurrentQueue<(ConnectedClientInstance, Messages.RawMessage)>();

        private readonly List<IHandler> _handlers = new List<IHandler>();

        private readonly CancellationTokenSource _selfCts = new CancellationTokenSource();

        public ServerInstance AddHandlerBeforeStart(IHandler handler)
        {
            _handlers.Add(handler);
            return this;
        }
        
        public async Task StartAsync(ServerConfiguration configuration, CancellationToken token, Action<Exception> exHandler = null)
        {
            try
            {
                _isReady.Value = false;
                if (!Library.Initialize())
                {
                    throw new Exception($"{GetType().Name}.StartAsync(): Enet Library.Initialize failed.");
                }

                using var host = new Host();

                Address address = new Address
                {
                    Port = configuration.Port
                };
                if (configuration.BufferSize.HasValue)
                {
                    host.Create(address, configuration.MaxClients, configuration.ChannelLimit,
                        configuration.Bandwidth.Item1,
                        configuration.Bandwidth.Item2, configuration.BufferSize.Value);
                }
                else
                {
                    host.Create(address, configuration.MaxClients, configuration.ChannelLimit,
                        configuration.Bandwidth.Item1,
                        configuration.Bandwidth.Item2);
                }

                int hostServiceTimeoutMs = configuration.HostServiceTimeoutMs;
                int loopDelayMs = configuration.LoopFrequencyDelayMs;

                _isReady.Value = true;
                ENet.Event netEvent = default;

                foreach (var handler in _handlers)
                {
                    handler.Handle(this);
                }

                while (!token.IsCancellationRequested)
                {
                    
                    if (_messagesQueue.TryDequeue(out var msgTuple))
                    {
                       
                        var peer = msgTuple.Item1.Peer;
                        if (_connectedClientsMap.ContainsKey(peer.ID))
                        {
                            HandleDequeuedMessage(msgTuple.Item2, ref peer);
                        }
                        else
                        {
                            Console.Error.Write($"[{GetType().Name}].Messages TryDequeue() -> clientInstance is not found!");
                        }
                        
                    }
                    
                    bool isPolled = false;
                    while (!isPolled)
                    {
                        if (host.CheckEvents(out netEvent) <= 0)
                        {
                            if (host.Service(hostServiceTimeoutMs, out netEvent) <= 0)
                            {
                                break;
                            }

                            isPolled = true;
                        }

                        HandleEvent(ref netEvent);
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
                Library.Deinitialize();
            }
            
        }

        private void HandleEvent(ref ENet.Event netEvent)
        {
            var peerId = netEvent.Peer.ID;
            switch (netEvent.Type)
            {
                case EventType.Connect:
                {
                    Console.WriteLine($"\tSERV_XXX  EV Connect.");
                    var peer = netEvent.Peer;
                    var clientInstance = new ConnectedClientInstance(ref peer);
                    _connectedClientsMap.Add(peerId, clientInstance);
                    _clientConnected.OnNext(clientInstance);
                    break;
                }
                case EventType.Disconnect:
                {
                    Console.WriteLine($"\tSERV_XXX  EV Disconnect.");

                    if (TryGetClientByPeer(ref netEvent, out var clientInstance))
                    {
                        _connectedClientsMap.Remove(clientInstance.ID);
                    }
                    else
                    {
                        Console.Error.Write($"[{GetType().Name}].EventType.Disconnect TryGetClientByPeer() clientInstance is not found!");
                    }
                    
                    _clientDisconnected.OnNext(clientInstance);
                    break;
                }
                case EventType.Timeout:
                {
                    Console.WriteLine($"\tSERV_XXX  EV Timeout.");
                    break;
                }
                case EventType.Receive:
                {
                   
                    Console.WriteLine($"\tSERV_XXX EV Message RECV from ID:  {peerId}");

                    byte[] buffer = _bufferArrayPool.Rent();
                    netEvent.Packet.CopyTo(buffer);

                    if (Messages.TryDeserializeRawMessage(buffer, out var rawMsg))
                    {
                        _messageReceived.OnNext(rawMsg);

                        if (TryGetClientByPeer(ref netEvent, out var clientInstance))
                        {
                            _messageReceivedFrom.OnNext((clientInstance, rawMsg));
                        }
                        else
                        {
                            Console.Error.Write($"[{GetType().Name}].EventType.Receive TryGetClientByPeer() clientInstance is not found!");
                        }
                    }
                    _bufferArrayPool.Return(buffer);
                    netEvent.Packet.Dispose();
                    break;
                }
            }
        }

        private bool TryGetClientByPeer(ref Event netEvent, out ConnectedClientInstance clientInstance)
        {
            var clientId = netEvent.Peer.ID;
            clientInstance = null;
            
            if (_connectedClientsMap.TryGetValue(clientId, out clientInstance))
            {
                return true;
            }
            
            return false;
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
            
            var peerIn = peer;
            Func<bool> sendFunc = () => peerIn.Send((byte)msg.Channel, ref packet);
            SendAttemptsAsync(sendFunc, msg.MessageType, 10, _selfCts.Token).ContinueWith((res) =>
            {
                if (!res.Result)
                {
                    packet.Dispose();
                    Console.Error.WriteLine(
                        $"[{GetType().Name}].HandleDequeuedMessage() , send() failed! {msg.MessageType} after 10 attempts!");
                }
            });

        }
        
        private async Task<bool> SendAttemptsAsync(Func<bool> sendFunc, EMessageType msgType, int attemptsCount, CancellationToken token)
        {
            for (int i = 0; i < attemptsCount; i++)
            {
                bool sendRes = sendFunc.Invoke();
                if (sendRes)
                {
                    return true;
                }

                await Task.Delay(LOOP_MS, token).ConfigureAwait(false);
            }

            return false;
        }
        
        public void EnqueueMessageToBeSend(ConnectedClientInstance target, Messages.RawMessage message) 
        {
            _messagesQueue.Enqueue((target, message));
        }

        public void Dispose()
        {
            _selfCts?.Cancel();
            _selfCts?.Dispose();
            _clientConnected.Dispose();
            foreach (var handler in _handlers)
            {
                handler.Dispose();
            }
        }
    }
}