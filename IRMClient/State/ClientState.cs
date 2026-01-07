using ObservableCollections;
using R3;

namespace IRMClient.State
{
    public class ClientState
    {
        public UserInfo UserInfo { get; set; } = new UserInfo();
        public ReactiveProperty<IRMVec3> Position { get; } = new ReactiveProperty<IRMVec3>(new IRMVec3());
        
        public ObservableList<OthersState> OthersStatesCollection { get; } = new ObservableList<OthersState>();
        public OthersStateMessagesQueue OthersStateMessagesQueue { get; } = new OthersStateMessagesQueue(100);
    }
}