using System.Collections.Generic;
using Features.ParcelModule.Scripts.Crate;
using Features.ParcelModule.Scripts.Stand;
using Features.SonarModule.Contracts;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Configuration {
    // The delivery parcels: what a station's stand offers and pays, when a parcel counts as on board, and the theft:
    // how far and how long a thief or a drifting parcel may get before it is lost, and how it comes back.
    [CreateAssetMenu(fileName = nameof(ParcelConfiguration) + "_Default", menuName = "Configurations/Parcel/" + nameof(ParcelConfiguration))]
    public sealed class ParcelConfiguration : ScriptableObject {
        [SerializeField] private ParcelCrate _cratePrefab;
        [Tooltip("The delivery stand the server puts beside the pad the ship stands at.")]
        [SerializeField] private ParcelStand _standPrefab;
        [Tooltip("Where the stand stands in the pad's space, metres: beside the shop kiosk, before the player spawn.")]
        [SerializeField] private Vector3 _standPadOffset = new(7.5f, 0.4f, 3.5f);
        [Tooltip("The sonar kind of a stolen parcel (carried off or drifting): the navigator leads the chase by it.")]
        [SerializeField] private SonarContactKind _stolenSonarKind;

        [Header("Offers")]
        [Tooltip("The chance weight of a station offering 0, 1, 2 parcels: the index is the count, at most the stand's slots.")]
        [SerializeField] private float[] _offerCountWeights = { 0.15f, 0.45f, 0.4f };
        [Tooltip("The lowest reward of a parcel; a good flight's ore pays about 150-250.")]
        [SerializeField, Min(1)] private int _minReward = 350;
        [SerializeField, Min(1)] private int _maxReward = 550;
        [Tooltip("Rewards are rounded to this step.")]
        [SerializeField, Min(1)] private int _rewardStep = 10;
        [Tooltip("A stand offers its parcels only while the ship stands at a station this close to it, metres.")]
        [SerializeField, Min(1f)] private float _standShipMaxMeters = 80f;

        [Header("On board")]
        [Tooltip("A crate over the deck up to this height above its surface is on board, metres.")]
        [SerializeField, Min(0f)] private float _aboardMaxHeightMeters = 6f;
        [Tooltip("A crate down to this depth below the deck surface is still on board, metres.")]
        [SerializeField, Min(0f)] private float _aboardBelowMeters = 1f;
        [Tooltip("A loose crate this far below the ship's deck fell overboard and is lost, metres.")]
        [SerializeField, Min(1f)] private float _fallLoseMeters = 30f;

        [Header("Theft and chase")]
        [Tooltip("The crate hangs this far under the bug that carries it, metres.")]
        [SerializeField, Min(0f)] private float _carrierHangMeters = 0.7f;
        [Tooltip("A stolen or drifting parcel this far from the ship is lost, metres.")]
        [SerializeField, Min(1f)] private float _loseDistanceMeters = 250f;
        [Tooltip("A parcel carried off this long is lost, seconds.")]
        [SerializeField, Min(1f)] private float _loseCarrySeconds = 75f;

        [Header("Drift")]
        [Tooltip("A parcel its shot-down thief lets go drifts on at this speed, metres per second.")]
        [SerializeField, Min(0f)] private float _driftSpeed = 1.2f;
        [Tooltip("A parcel left drifting this long is lost, seconds.")]
        [SerializeField, Min(1f)] private float _loseDriftSeconds = 60f;
        [Tooltip("The ship flies into a drifting parcel when it comes this close to the deck's edge, metres.")]
        [SerializeField, Min(0f)] private float _flyIntoMarginMeters = 1.5f;
        [Tooltip("... and the parcel is between this height below and the max height above the deck surface, metres.")]
        [SerializeField, Min(0f)] private float _flyIntoBelowMeters = 3f;

        [Header("Harpoon")]
        [Tooltip("A parcel pulled in this close to its rope's holder is on board, metres.")]
        [SerializeField, Min(0.1f)] private float _pullArriveMeters = 2.5f;
        [Tooltip("A reported harpoon hit counts when it lies this close to the crate, metres.")]
        [SerializeField, Min(0f)] private float _hitPointToleranceMeters = 1.5f;
        [Tooltip("A parcel brought aboard is let go this high over the deck surface and falls onto it, metres.")]
        [SerializeField, Min(0.1f)] private float _deckDropHeightMeters = 1f;

        public ParcelCrate CratePrefab => _cratePrefab;
        public ParcelStand StandPrefab => _standPrefab;
        public Vector3 StandPadOffset => _standPadOffset;
        public SonarContactKind StolenSonarKind => _stolenSonarKind;
        public IReadOnlyList<float> OfferCountWeights => _offerCountWeights;
        public int MinReward => _minReward;
        public int MaxReward => _maxReward;
        public int RewardStep => _rewardStep;
        public float StandShipMaxMeters => _standShipMaxMeters;
        public float AboardMaxHeightMeters => _aboardMaxHeightMeters;
        public float AboardBelowMeters => _aboardBelowMeters;
        public float FallLoseMeters => _fallLoseMeters;
        public float CarrierHangMeters => _carrierHangMeters;
        public float LoseDistanceMeters => _loseDistanceMeters;
        public float LoseCarrySeconds => _loseCarrySeconds;
        public float DriftSpeed => _driftSpeed;
        public float LoseDriftSeconds => _loseDriftSeconds;
        public float FlyIntoMarginMeters => _flyIntoMarginMeters;
        public float FlyIntoBelowMeters => _flyIntoBelowMeters;
        public float PullArriveMeters => _pullArriveMeters;
        public float HitPointToleranceMeters => _hitPointToleranceMeters;
        public float DeckDropHeightMeters => _deckDropHeightMeters;
    }
}
