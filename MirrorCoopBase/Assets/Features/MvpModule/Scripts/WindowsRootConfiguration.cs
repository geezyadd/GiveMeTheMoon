using UnityEngine;
using UnityEngine.EventSystems;

namespace Features.MvpModule {
    // The prefabs the windows live under: the screen-space canvas and the UI event system.
    [CreateAssetMenu(menuName = "Game/Windows Root Configuration", fileName = "WindowsRootConfiguration")]
    public sealed class WindowsRootConfiguration : ScriptableObject {
        [Tooltip("The overlay canvas every window is put under (scaler, raycaster, sorting order).")]
        [SerializeField] private Canvas _windowsRootPrefab;
        [Tooltip("The UI event system with the Input System module; made only when no event system exists yet.")]
        [SerializeField] private EventSystem _eventSystemPrefab;

        public Canvas WindowsRootPrefab => _windowsRootPrefab;
        public EventSystem EventSystemPrefab => _eventSystemPrefab;
    }
}
