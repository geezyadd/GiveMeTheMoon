using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Ore {
    // A piece appears at a random spot within the scatter radius of the middle and drifts straight away from it at a
    // speed between the configuration's bounds, so the pieces of one rock spread out from the break.
    public sealed class FieldOreDropService : IFieldOreDropService {
        private const float FULL_TURN_RADIANS = 2f * Mathf.PI;

        private readonly IFieldOreDropRule _fieldOreDropRule;
        private readonly IFieldOreSpawner _fieldOreSpawner;
        private readonly FieldOreCatalog _fieldOreCatalog;
        private readonly FieldOreConfiguration _fieldOreConfiguration;

        public FieldOreDropService(IFieldOreDropRule fieldOreDropRule, IFieldOreSpawner fieldOreSpawner, FieldOreCatalog fieldOreCatalog,
                                   FieldOreConfiguration fieldOreConfiguration) {
            _fieldOreDropRule = fieldOreDropRule;
            _fieldOreSpawner = fieldOreSpawner;
            _fieldOreCatalog = fieldOreCatalog;
            _fieldOreConfiguration = fieldOreConfiguration;
        }

        public int ServerDropFromRock(in FieldRockDescription rock, IFieldRandom random) {
            int count = _fieldOreDropRule.GetCount(rock, random.Value(), random.Value());
            return ServerDropAt(rock.Center, rock.Radius * _fieldOreConfiguration.DropScatterShare, count, random);
        }

        public int ServerDropAt(Vector3 position, float scatterRadius, int count, IFieldRandom random) {
            int dropped = 0;
            for (int i = 0; i < count; i++) {
                if (TryDropOne(position, scatterRadius, random))
                    dropped++;
            }

            return dropped;
        }

        private bool TryDropOne(Vector3 position, float scatterRadius, IFieldRandom random) {
            FieldOreDefinition kind = FieldWeightedPick.Pick(_fieldOreCatalog.Ores, random);
            Vector3 direction = CreateDirection(random);
            float speed = Mathf.Lerp(_fieldOreConfiguration.MinDriftSpeedMetersPerSecond, _fieldOreConfiguration.MaxDriftSpeedMetersPerSecond, random.Value());
            Vector3 spot = position + direction * (scatterRadius * random.Value());
            return _fieldOreSpawner.TrySpawn(kind, spot, direction * speed, out _);
        }

        // Even over the sphere: an even height and an even turn around it.
        private static Vector3 CreateDirection(IFieldRandom random) {
            float height = random.Value() * 2f - 1f;
            float azimuth = random.Value() * FULL_TURN_RADIANS;
            float ring = Mathf.Sqrt(1f - height * height);
            return new Vector3(ring * Mathf.Cos(azimuth), height, ring * Mathf.Sin(azimuth));
        }
    }
}
