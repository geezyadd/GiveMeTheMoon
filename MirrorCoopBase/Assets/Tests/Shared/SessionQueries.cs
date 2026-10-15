#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using Features.MenuModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Game.Connection;
using Mirror;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Tests.Shared {
    // What the loop tests and the coop test client read from a running session: the menu, the game at a station, the
    // deck. Shared by the editor tests and the development-build client, so both wait for the same states.
    public static class SessionQueries {
        private const float DECK_EDGE_TOLERANCE = 0.5f;
        // A standing player's feet are above the deck's middle; this far below it they fell through. The middle, not the
        // top of the bounds: a banking deck raises its bounds' top above the walkable surface.
        private const float BELOW_DECK_CENTER_TOLERANCE = 0.5f;

        public static ConnectionNetworkManager NetworkManager =>
            ProjectContext.Instance.Container.Resolve<ConnectionSessionModel>().NetworkManager;

        public static NetworkIdentity LocalPlayer =>
            NetworkClient.localPlayer;

        public static T Resolve<T>() {
            if (ProjectContext.Instance.Container.HasBinding<T>())
                return ProjectContext.Instance.Container.Resolve<T>();

            foreach (SceneContext context in Object.FindObjectsByType<SceneContext>(FindObjectsSortMode.None)) {
                if (context.Container.HasBinding<T>())
                    return context.Container.Resolve<T>();
            }

            throw new InvalidOperationException($"No context binds {typeof(T).Name}.");
        }

        public static bool IsMenuReady() {
            MenuSessionViewBase menu = Object.FindAnyObjectByType<MenuSessionViewBase>();
            return menu != null && menu.OnHostClicked != null;
        }

        public static bool IsBackInMenu() {
            if (NetworkServer.active || NetworkClient.active || IsMenuReady() == false)
                return false;

            IGameFlowStateMachineService flow = Resolve<IGameFlowStateMachineService>();
            return flow.IsTransitioning == false && flow.CurrentStateType == typeof(MenuGameFlowState);
        }

        // GameScene is loaded with the ship, the local player exists and the ship stands at a station.
        public static bool IsGameAtStation() {
            if (LocalPlayer == null || Object.FindAnyObjectByType<ShipRunDirector>() == null)
                return false;

            if (NetworkManager.IsMapLoaded == false)
                return false;

            return Object.FindAnyObjectByType<ShipBase>() != null && Resolve<IReadOnlyShipRunModel>().Phase == ShipRunPhase.Build;
        }

        // In flight the loop index still counts the flights finished before the current one.
        public static bool IsAtStationAfterLoop(int loopIndex) {
            IReadOnlyShipRunModel run = Resolve<IReadOnlyShipRunModel>();
            return run.Phase == ShipRunPhase.Build && run.LoopIndex == loopIndex;
        }

        // Over the deck (with a little edge tolerance) and not fallen through it.
        public static bool IsOnDeck(ShipBase ship, Vector3 position) {
            Bounds deck = ship.DeckGeometry.Bounds;
            return ContainsXZ(deck, position, DECK_EDGE_TOLERANCE) && position.y > deck.center.y - BELOW_DECK_CENTER_TOLERANCE;
        }

        public static bool ContainsXZ(Bounds bounds, Vector3 point, float tolerance) =>
            point.x >= bounds.min.x - tolerance && point.x <= bounds.max.x + tolerance
            && point.z >= bounds.min.z - tolerance && point.z <= bounds.max.z + tolerance;
    }
}
#endif
