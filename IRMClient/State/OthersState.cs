using R3;

namespace IRMClient.State
{
    public class OthersState
    {
        public Subject<Unit> Updated { get; set; } = new Subject<Unit>();
        public ReactiveProperty<int> UserId { get; set; } = new ReactiveProperty<int>(-1);
        public ReactiveProperty<ulong> Tick { get; set; } = new ReactiveProperty<ulong>(0);
        public ReactiveProperty<IRMVec3> Pos { get; set; } = new ReactiveProperty<IRMVec3>(new IRMVec3(-2599, -2599, -2599));
    }
}