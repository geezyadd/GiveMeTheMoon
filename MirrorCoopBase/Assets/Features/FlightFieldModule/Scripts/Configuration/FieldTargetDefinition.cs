using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Targets;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // One kind of target balloon in the flight field: what it looks like, how big its sphere is, the money it gives,
    // how many weapon hit points it has and how often the generator picks it. A new kind is a new asset in the
    // FieldTargetCatalog.
    [CreateAssetMenu(menuName = "Game/Flight Field/Target Definition", fileName = "FieldTarget_")]
    public sealed class FieldTargetDefinition : ScriptableObject, IFieldWeighted {
        [Tooltip("Stable id: the network carries it and the pop event names the kind with it.")]
        [SerializeField] private string _id;
        [Tooltip("The balloon every peer builds; authored for a sphere of radius 1 m and scaled by the radius.")]
        [SerializeField] private FieldTargetView _prefab;
        [Tooltip("Radius of the balloon's sphere, meters: the look, the weapon hit check and the ship's fly-in check.")]
        [SerializeField, Min(0.1f)] private float _radiusMeters = 3f;
        [Tooltip("Money the crew gets when it pops.")]
        [SerializeField, Min(1)] private int _reward = 20;
        [Tooltip("Weapon damage it takes to pop (the minigun deals 1 per hit).")]
        [SerializeField, Min(0.01f)] private float _hitPoints = 1f;
        [Tooltip("Relative chance the generator picks this kind for a balloon.")]
        [SerializeField, Min(0f)] private float _spawnWeight = 1f;
        [Tooltip("The colour of its pop: confetti, scraps and the '+N' text.")]
        [SerializeField] private Color _popColor = Color.white;

        public string Id => _id;
        public FieldTargetView Prefab => _prefab;
        public float RadiusMeters => _radiusMeters;
        public int Reward => _reward;
        public float HitPoints => _hitPoints;
        public float Weight => _spawnWeight;
        public Color PopColor => _popColor;
    }
}
