using System;
using System.Threading.Tasks;

namespace Features.GameFlowStateMachineModule.Scripts {
    public interface IGameFlowStateMachineService {
        event Action<Type> StateEntered;
        event Action<Type> StateExited;

        Type CurrentStateType { get; }
        bool IsTransitioning { get; }

        Task EnterAsync<TState>() where TState : class, IGameFlowState;
    }
}
