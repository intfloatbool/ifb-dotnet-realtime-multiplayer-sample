using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IRMClient;
using IRMClient.State;
using IRMShared;
using NetworkPlugin.Scripts;
using ObservableCollections;
using R3;
using UnityEngine;
using Object = UnityEngine.Object;

public class IRMSetup : MonoBehaviour
{
    [SerializeField] private IRMSyncTransform _syncTransformPrefab;
    [SerializeField] private string _hostName = "127.0.0.1";
    [SerializeField] private int _port = 7777;

    private CancellationTokenSource _clientCts = new CancellationTokenSource();
    private ClientInstance _clientInstance;
    private Task _clientStartTask;

    private Dictionary<int, IRMSyncTransform> _othersSyncMap = new Dictionary<int, IRMSyncTransform>();

    private bool _isClientReady = false;

    private async Awaitable Start()
    {
        IRMLogger.Setup(Debug.Log, Debug.LogError);
        IRMLogger.IsLoggingEnabled = true;
        
        Debug.Log($"Setup ClientInstance...");
        _clientInstance = await CreateAndSetupClientInstanceAsync(_clientCts.Token);
        Debug.Log($"Setup ClientInstance is DONE.");
        
        
        var registerTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await _clientInstance.WaitForRegistrationSuccessAsync(registerTimeoutCts.Token);
            await Awaitable.MainThreadAsync();
        }
        catch (OperationCanceledException)
        {
            Debug.LogError("WaitForRegistrationSuccessAsync timeout!");
        }
        finally
        {
            registerTimeoutCts.Dispose();
        }
        
        Debug.Log($"My User data: {GetMyUserInfoString()}");


        var mySync = Instantiate(_syncTransformPrefab, Vector3.zero, Quaternion.identity);
        mySync.Setup(() => true, GetMyUserInfo().Id, (pos) =>
        {
            //Debug.Log($"MINE_XXX Send pos: {pos}");
            _clientInstance.ClientState.Position.Value = new IRMVec3(pos.x, pos.y, pos.z);
        });

        
        _clientInstance.ClientState.OthersStatesCollection.ObserveChanged().ObserveOnCurrentSynchronizationContext()
            .Subscribe((_) =>
            {
                foreach (var other in _clientInstance.ClientState.OthersStatesCollection)
                {
                    if (_othersSyncMap.TryGetValue(other.UserId.Value, out var existed))
                    {
                        Object.Destroy(existed.gameObject);
                    }
                
                    var otherSync = Instantiate(_syncTransformPrefab, Vector3.zero, Quaternion.identity);
                
                    otherSync.Setup(() => false, other.UserId.Value, (_) => {});
                    _othersSyncMap[other.UserId.Value] = otherSync;
                    Debug.Log($"FFF_XXXX Other Client connected: {other.UserId} , thread: {Thread.CurrentThread.ManagedThreadId}");
                }
               
            });

        _isClientReady = true;
    }

    private void Update()
    {
        OthersUpdateLoop();
    }

    private void OthersUpdateLoop()
    {
        if (!_isClientReady)
        {
            return;
        }

        foreach (var otherState in _clientInstance.ClientState.OthersStateMessagesQueue.DequeueAll())
        {
            if (_othersSyncMap.TryGetValue(otherState.UserId, out var syncTransform) && !syncTransform.IsMine)
            {
                syncTransform.SetRemotePos(new Vector3(otherState.Pos.X, otherState.Pos.Y, otherState.Pos.Z));

                var diff = DateTime.UtcNow - otherState.SentTimestamp;
                Debug.Log($"<color=red>OTHER_MSG_XXX</color> delay seconds: {diff.TotalSeconds}");
            }
        }
    }

    private void OnDestroy()
    {
        _clientCts?.Cancel();
        _clientCts?.Dispose();
        _clientInstance?.Dispose();
    }

    private async Awaitable<ClientInstance> CreateAndSetupClientInstanceAsync(CancellationToken clientToken)
    {
        var clientInstance = new ClientInstance();
        clientInstance.SetupDefaultClientHandlers();
        using var clientReadyCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); 
        _clientStartTask = clientInstance.StartAsync(new ClientConfiguration(), "127.0.0.1", (ushort) _port, clientToken, (ex) =>
        {
            if (ex is not OperationCanceledException)
            {
                Debug.LogError($"ClientInstance.StartAsync() ERR! {ex}");
            }
        });
        
        while (!clientInstance.IsReady.CurrentValue)
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(0.1f, clientReadyCts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new Exception("client isReady timeout");
            }
        }

        await Awaitable.MainThreadAsync();
        
        return clientInstance;
    }

    private UserInfo GetMyUserInfo()
    {
        return _clientInstance.ClientState.UserInfo;
    }

    private string GetMyUserInfoString()
    {
        var userInfo = _clientInstance.ClientState.UserInfo;
        return $"(id: {userInfo.Id}, isMaster: {userInfo.IsMaster})";
    }
    
    
}
