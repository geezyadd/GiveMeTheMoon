using UnityEngine;

namespace Features.PointOfInterestModule.Scripts.Configuration {
    // The bug nest content: the bugs that fly out from behind the container when the ship enters the zone and the
    // optional entry cutscene.
    [CreateAssetMenu(fileName = nameof(BugNestConfiguration), menuName = "Configurations/Points of interest/" + nameof(BugNestConfiguration))]
    public sealed class BugNestConfiguration : ScriptableObject {
        [SerializeField] private PointOfInterestContentDefinition _content;
        [Header("Bugs")]
        [SerializeField, Min(1)] private int _bugCount = 6;
        [Tooltip("The bugs appear this far behind the container, seen from the ship.")]
        [SerializeField, Min(0f)] private float _spawnBehindMeters = 12f;
        [Tooltip("Shown bugs this close to the nest make the flock the cutscene camera follows.")]
        [SerializeField, Min(1f)] private float _flockSearchMeters = 160f;
        [Header("Entry cutscene")]
        [Tooltip("Off: no stop and no cutscene, the bugs just fly out.")]
        [SerializeField] private bool _playEntryCutscene = true;
        [Tooltip("The camera pulls this far back from the ship, away from the nest.")]
        [SerializeField, Min(0f)] private float _cameraPullBackMeters = 22f;
        [SerializeField, Min(0f)] private float _cameraPullUpMeters = 12f;
        [Tooltip("For the follow shot the camera flies toward the nest: it stands this share of the way from the nest to the ship.")]
        [SerializeField, Range(0.1f, 1f)] private float _cameraFollowNestShare = 0.4f;
        [Tooltip("During the follow shot the camera stands this far to the side.")]
        [SerializeField, Min(0f)] private float _cameraFollowSideMeters = 10f;
        [Tooltip("The first shot: the camera pulls away and looks at the container while the bugs come out.")]
        [SerializeField, Min(0.1f)] private float _lookAtNestSeconds = 1.6f;
        [Tooltip("The second shot: the camera follows the middle of the flock.")]
        [SerializeField, Min(0.1f)] private float _followFlockSeconds = 2f;

        public PointOfInterestContentDefinition Content => _content;
        public int BugCount => _bugCount;
        public float SpawnBehindMeters => _spawnBehindMeters;
        public float FlockSearchMeters => _flockSearchMeters;
        public bool PlayEntryCutscene => _playEntryCutscene;
        public float CameraPullBackMeters => _cameraPullBackMeters;
        public float CameraPullUpMeters => _cameraPullUpMeters;
        public float CameraFollowNestShare => _cameraFollowNestShare;
        public float CameraFollowSideMeters => _cameraFollowSideMeters;
        public float LookAtNestSeconds => _lookAtNestSeconds;
        public float FollowFlockSeconds => _followFlockSeconds;
    }
}
