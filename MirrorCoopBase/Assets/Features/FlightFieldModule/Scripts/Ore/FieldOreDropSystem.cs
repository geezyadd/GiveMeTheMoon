using System;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Generation;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Ore {
    // Server: every broken rock (a turret or pistol bullet, the hull flying into it) may drop ore. The rolls come from
    // one sequence of the session; what drops is the drop service's.
    public sealed class FieldOreDropSystem : IInitializable, IDisposable {
        private readonly IFieldRockEvents _fieldRockEvents;
        private readonly IFieldOreDropService _fieldOreDropService;
        private readonly IFieldRandom _random;

        public FieldOreDropSystem(IFieldRockEvents fieldRockEvents, IFieldOreDropService fieldOreDropService, IFieldRandomFactory fieldRandomFactory) {
            _fieldRockEvents = fieldRockEvents;
            _fieldOreDropService = fieldOreDropService;
            _random = fieldRandomFactory.Create(Environment.TickCount);
        }

        public void Initialize() =>
            _fieldRockEvents.RockDestroyed += OnRockDestroyed;

        public void Dispose() =>
            _fieldRockEvents.RockDestroyed -= OnRockDestroyed;

        private void OnRockDestroyed(FieldRockDestroyed destroyed) =>
            _fieldOreDropService.ServerDropFromRock(destroyed.Rock, _random);
    }
}
