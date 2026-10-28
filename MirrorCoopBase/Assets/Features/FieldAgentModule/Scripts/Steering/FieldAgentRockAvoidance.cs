using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FlightFieldModule.Contracts;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Steering {
    // Rock avoidance from the field data, no physics. A rock sphere (widened by the agent's radius and clearance) that
    // the agent's course runs into within its look-ahead bends the course onto the sphere's tangent, harder the nearer
    // the agent is, so the agent slides round the rock toward its goal instead of stopping at it (a plain push away from
    // a 300 m giant would hold it in front of the giant). A course straight at the middle turns to the agent's own side.
    // Inside the widened sphere the agent is pushed straight out.
    public sealed class FieldAgentRockAvoidance : IFieldAgentRockAvoidance {
        private const float MIN_SPEED = 0.05f;
        private const float MIN_LENGTH = 0.001f;
        private const float MIN_SHELL = 0.01f;

        public float CalculateLookAheadMeters(Vector3 velocity, FieldAgentProfile profile) =>
            Mathf.Max(profile.MinLookAheadMeters, velocity.magnitude * profile.LookAheadSeconds);

        public Vector3 CalculateAvoidance(Vector3 position, Vector3 velocity, Vector3 course, IReadOnlyList<FieldRockDescription> rocks, FieldAgentProfile profile, float sideSign) {
            Vector3 direction = ReadCourseDirection(velocity, course);
            float lookAhead = CalculateLookAheadMeters(velocity, profile);
            Vector3 steer = Vector3.zero;
            for (int i = 0; i < rocks.Count; i++)
                steer += CalculateRockPush(position, direction, rocks[i], lookAhead, profile, sideSign);

            return steer;
        }

        public bool PushOutOfRocks(ref Vector3 position, ref Vector3 velocity, IReadOnlyList<FieldRockDescription> rocks, FieldAgentProfile profile) {
            bool isPushed = false;
            for (int i = 0; i < rocks.Count; i++) {
                FieldRockDescription rock = rocks[i];
                float minDistance = rock.Radius + profile.Radius;
                Vector3 fromCenter = position - rock.Center;
                float distance = fromCenter.magnitude;
                if (distance >= minDistance)
                    continue;

                Vector3 outward = distance > MIN_LENGTH ? fromCenter / distance : Vector3.up;
                position = rock.Center + outward * minDistance;
                float inward = Vector3.Dot(velocity, outward);
                if (inward < 0f)
                    velocity -= outward * inward;

                isPushed = true;
            }

            return isPushed;
        }

        private static Vector3 ReadCourseDirection(Vector3 velocity, Vector3 course) {
            if (course.magnitude > MIN_SPEED)
                return course.normalized;

            return velocity.magnitude > MIN_SPEED ? velocity.normalized : Vector3.zero;
        }

        private static Vector3 CalculateRockPush(Vector3 position, Vector3 direction, FieldRockDescription rock, float lookAhead, FieldAgentProfile profile, float sideSign) {
            float reach = rock.Radius + profile.Radius + profile.RockClearance;
            Vector3 fromCenter = position - rock.Center;
            float distance = fromCenter.magnitude;
            Vector3 outward = distance > MIN_LENGTH ? fromCenter / distance : Vector3.up;
            if (distance < reach)
                return CalculateEscape(outward, distance, reach, rock.Radius + profile.Radius, profile.MaxSpeed);

            float gap = distance - reach;
            if (IsRockOffCourse(position, direction, rock.Center, gap, lookAhead, reach))
                return Vector3.zero;

            Vector3 tangent = direction - outward * Vector3.Dot(direction, outward);
            tangent = tangent.magnitude > MIN_LENGTH ? tangent.normalized : CalculateSide(outward, sideSign);
            float urgency = 1f - Mathf.Clamp01(gap / lookAhead);
            return (tangent - direction) * (profile.MaxSpeed * urgency);
        }

        private static bool IsRockOffCourse(Vector3 position, Vector3 direction, Vector3 center, float gap, float lookAhead, float reach) =>
            direction == Vector3.zero || gap > lookAhead || IsCourseClear(position, direction, center, reach);

        private static bool IsCourseClear(Vector3 position, Vector3 direction, Vector3 center, float reach) {
            Vector3 toCenter = center - position;
            float along = Vector3.Dot(toCenter, direction);
            return along <= 0f || (toCenter - direction * along).magnitude >= reach;
        }

        private static Vector3 CalculateEscape(Vector3 outward, float distance, float reach, float contact, float maxSpeed) {
            float depth = (reach - distance) / Mathf.Max(reach - contact, MIN_SHELL);
            return outward * (maxSpeed * (1f + depth));
        }

        private static Vector3 CalculateSide(Vector3 outward, float sideSign) {
            Vector3 side = Vector3.Cross(outward, Vector3.up);
            if (side.sqrMagnitude < MIN_LENGTH)
                side = Vector3.Cross(outward, Vector3.forward);

            return side.normalized * sideSign;
        }
    }
}
