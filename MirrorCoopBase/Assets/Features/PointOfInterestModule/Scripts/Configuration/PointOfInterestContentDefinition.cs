using Features.PointOfInterestModule.Scripts.Client;
using UnityEngine;

namespace Features.PointOfInterestModule.Scripts.Configuration {
    // One kind of point of interest content (the bug nest): its id, how often a point gets it, the look every peer
    // builds at the point and its reward container in the middle. What happens on entering is the content's class.
    [CreateAssetMenu(fileName = nameof(PointOfInterestContentDefinition), menuName = "Configurations/Points of interest/" + nameof(PointOfInterestContentDefinition))]
    public sealed class PointOfInterestContentDefinition : ScriptableObject {
        [Tooltip("Stable id sent over the network with a point: never rename it once it is used.")]
        [SerializeField] private string _id;
        [Tooltip("How often a point gets this content against the other contents.")]
        [SerializeField, Min(0f)] private float _weight = 1f;
        [SerializeField] private PointOfInterestView _viewPrefab;
        [Header("Reward container")]
        [Tooltip("The container's radius: a hit within it (plus the hit tolerance) counts, the hull within it (plus the ship's ram reach) rams it.")]
        [SerializeField, Min(0.1f)] private float _containerRadiusMeters = 4f;
        [Tooltip("The damage that shoots the container open.")]
        [SerializeField, Min(1f)] private float _containerHitPoints = 60f;
        [Tooltip("Paid into the crew wallet once when the container is opened.")]
        [SerializeField, Min(0)] private int _reward = 150;

        public string Id => _id;
        public float Weight => _weight;
        public PointOfInterestView ViewPrefab => _viewPrefab;
        public float ContainerRadiusMeters => _containerRadiusMeters;
        public float ContainerHitPoints => _containerHitPoints;
        public int Reward => _reward;
    }
}
