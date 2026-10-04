using System;
using Features.CameraModule.Scripts.Models;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using Features.InputModule.Realization.Scripts.Generated;
using Features.MvpModule;
using Features.PlayerLifeModule.Scripts;
using Features.ShopModule.Scripts.Data;
using Features.ShopModule.Scripts.Generated;
using Features.ShopModule.Scripts.UI;
using Zenject;

namespace Features.ShopModule.Scripts.Systems {
    public sealed class ShopWindowSystem : IInitializable, IDisposable {
        private readonly IInputService _inputService;
        private readonly IWindowsService _windowsService;
        private readonly IGameFlowStateMachineService _gameFlowStateMachineService;
        private readonly IReadOnlyWalletModel _walletModel;
        private readonly ShopModel _shopModel;
        private readonly CursorModel _cursorModel;
        private readonly IPlayerLifeQuery _playerLifeQuery;

        public ShopWindowSystem(
            IInputService inputService,
            IWindowsService windowsService,
            IGameFlowStateMachineService gameFlowStateMachineService,
            IReadOnlyWalletModel walletModel,
            ShopModel shopModel,
            CursorModel cursorModel,
            IPlayerLifeQuery playerLifeQuery) {
            _inputService = inputService;
            _windowsService = windowsService;
            _gameFlowStateMachineService = gameFlowStateMachineService;
            _walletModel = walletModel;
            _shopModel = shopModel;
            _cursorModel = cursorModel;
            _playerLifeQuery = playerLifeQuery;
        }

        public void Initialize() {
            _inputService.Shop.Performed += OnShopPressed;
            _inputService.CloseWindow.Performed += OnCloseWindowPressed;
            _gameFlowStateMachineService.StateExited += OnGameFlowStateExited;
            _walletModel.OnBalanceChanged += OnWalletChanged;
            _walletModel.OnAvailableChanged += OnWalletChanged;
            _shopModel.OnOpenChanged += OnShopOpenChanged;
            _playerLifeQuery.OnLocalStateChanged += OnLocalLifeStateChanged;
        }

        public void Dispose() {
            _inputService.Shop.Performed -= OnShopPressed;
            _inputService.CloseWindow.Performed -= OnCloseWindowPressed;
            _gameFlowStateMachineService.StateExited -= OnGameFlowStateExited;
            _walletModel.OnBalanceChanged -= OnWalletChanged;
            _walletModel.OnAvailableChanged -= OnWalletChanged;
            _shopModel.OnOpenChanged -= OnShopOpenChanged;
            _playerLifeQuery.OnLocalStateChanged -= OnLocalLifeStateChanged;
            _shopModel.Close();
        }

        private bool CanOpenShop() =>
            _gameFlowStateMachineService.CurrentStateType == typeof(SessionGameFlowState)
            && _gameFlowStateMachineService.IsTransitioning == false
            && _walletModel.IsAvailable
            && _playerLifeQuery.LocalState == PlayerLifeState.Alive;

        private void OpenWindow() {
            _windowsService.OpenWindow<ShopWindow>();
            _inputService.DisableMovementMap();
            _cursorModel.RequestFreeCursor();
        }

        private void CloseWindow() {
            IWindow window = _windowsService.GetWindow(typeof(ShopWindow));
            if (window.WindowStatus != WindowStatus.Closed)
                _windowsService.CloseWindow<ShopWindow>();

            _inputService.EnableMovementMap();
            _cursorModel.ReleaseFreeCursor();
        }

        private void OnShopPressed() {
            if (_shopModel.IsOpen) {
                _shopModel.Close();
                return;
            }

            if (CanOpenShop())
                _shopModel.Open();
        }

        private void OnCloseWindowPressed() =>
            _shopModel.Close();

        private void OnGameFlowStateExited(Type stateType) {
            if (stateType == typeof(SessionGameFlowState))
                _shopModel.Close();
        }

        private void OnWalletChanged() {
            if (_walletModel.IsAvailable == false)
                _shopModel.Close();
        }

        private void OnLocalLifeStateChanged(PlayerLifeState state) {
            if (state == PlayerLifeState.Dead)
                _shopModel.Close();
        }

        private void OnShopOpenChanged() {
            if (_shopModel.IsOpen)
                OpenWindow();
            else
                CloseWindow();
        }
    }
}
