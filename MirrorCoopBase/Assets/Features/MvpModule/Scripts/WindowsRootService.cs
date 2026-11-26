using UnityEngine;
using UnityEngine.EventSystems;

namespace Features.MvpModule {
    // The canvas the windows live under, made from its prefab on first use and kept across scenes, with a UI event
    // system from its prefab when the scenes bring none.
    public sealed class WindowsRootService {
        private const string DontDestroyOnLoadScene = "DontDestroyOnLoad";

        private readonly WindowsRootConfiguration _configuration;
        private Transform _root;

        public WindowsRootService(WindowsRootConfiguration configuration) {
            _configuration = configuration;
        }

        public Transform Root {
            get {
                if (_root == null)
                    _root = CreateRoot();

                return _root;
            }
        }

        private Transform CreateRoot() {
            EnsureEventSystem();

            Canvas canvas = Object.Instantiate(_configuration.WindowsRootPrefab);
            canvas.name = _configuration.WindowsRootPrefab.name;
            Object.DontDestroyOnLoad(canvas.gameObject);
            return canvas.transform;
        }

        private void EnsureEventSystem() {
            EventSystem[] eventSystems =
                Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (EventSystem eventSystem in eventSystems) {
                if (eventSystem == null)
                    continue;

                if (eventSystem.gameObject.scene.name != DontDestroyOnLoadScene)
                    Object.DontDestroyOnLoad(eventSystem.gameObject);

                return;
            }

            EventSystem created = Object.Instantiate(_configuration.EventSystemPrefab);
            created.name = _configuration.EventSystemPrefab.name;
            Object.DontDestroyOnLoad(created.gameObject);
        }
    }
}
