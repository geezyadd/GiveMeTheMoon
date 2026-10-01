using System;
using Features.CameraModule.Scripts.Models;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using Features.InputModule.Realization.Scripts.Generated;
using Features.MvpModule;
using Features.ShopModule.Scripts.Data;
using Features.ShopModule.Scripts.UI;
using Zenject;

namespace Features.ShopModule.Scripts.Systems {
    public sealed class ShopWindowSystem : IInitializable, IDisposable {
        private readonly IInputService _inputService;
        private readonly IWindowsService _windowsService;
        private readonly IGameFlowStateMachineService _gameFlowStateMachineService;
        private readonly GameFlowStateLifecycleEventClass _gameFlowStateLifecycleEventClass;
        private readonly IWalletModel _walletModel;
        private readonly ShopModel _shopModel;
        private readonly CursorModel _cursorModel;

        public ShopWindowSystem(
            IInputService inputService,
            IWindowsService windowsService,
            IGameFlowStateMachineService gameFlowStateMachineService,
            GameFlowStateLifecycleEventClass gameFlowStateLifecycleEventClass,
            IWalletModel walletModel,
            ShopModel shopModel,
            CursorModel cursorModel) {
            _inputService = inputService;
            _windowsService = windowsService;
            _gameFlowStateMachineService = gameFlowStateMachineService;
            _gameFlowStateLifecycleEventClass = gameFlowStateLifecycleEventClass;
            _walletModel = walletModel;
            _shopModel = shopModel;
            _cursorModel = cursorModel;
        }

        public void Initialize() {
            _inputService.Shop.Performed += OnShopPressed;
            _inputService.CloseWindow.Performed += OnCloseWindowPressed;
            _gameFlowStateLifecycleEventClass.OnStateExited += OnGameFlowStateExited;
            _walletModel.OnBalanceChanged += OnWalletChanged;
            _shopModel.OnOpenChanged += OnShopOpenChanged;
        }

        public void Dispose() {
            _inputService.Shop.Performed -= OnShopPressed;
            _inputService.CloseWindow.Performed -= OnCloseWindowPressed;
            _gameFlowStateLifecycleEventClass.OnStateExited -= OnGameFlowStateExited;
            _walletModel.OnBalanceChanged -= OnWalletChanged;
            _shopModel.OnOpenChanged -= OnShopOpenChanged;
            _shopModel.Close();
        }

        private bool CanOpenShop() =>
            _gameFlowStateMachineService.CurrentStateType == typeof(SessionGameFlowState)
            && _gameFlowStateMachineService.IsTransitioning == false
            && _walletModel.IsAvailable;

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

        private void OnShopOpenChanged() {
            if (_shopModel.IsOpen)
                OpenWindow();
            else
                CloseWindow();
        }
    }
}
