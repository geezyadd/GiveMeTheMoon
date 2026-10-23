using Features.CharacterMovableModule.Scripts.PlayerStats;
using UnityEngine;

namespace Features.CharacterMovableModule.Scripts {
    // The jump and air gravity stats of one player, read once per step: the station body and the ship ride share them.
    public readonly struct JumpTuning {
        public readonly float JumpHeight;
        public readonly float JumpCooldown;
        public readonly float CoyoteTime;
        public readonly float JumpBuffer;
        public readonly float RiseGravity;
        public readonly float ShortHopGravity;
        public readonly float FallGravity;
        public readonly int ExtraAirJumps;

        public JumpTuning(
            float jumpHeight,
            float jumpCooldown,
            float coyoteTime,
            float jumpBuffer,
            float riseGravity,
            float shortHopGravity,
            float fallGravity,
            int extraAirJumps) {
            JumpHeight = jumpHeight;
            JumpCooldown = jumpCooldown;
            CoyoteTime = coyoteTime;
            JumpBuffer = jumpBuffer;
            RiseGravity = riseGravity;
            ShortHopGravity = shortHopGravity;
            FallGravity = fallGravity;
            ExtraAirJumps = extraAirJumps;
        }

        public static JumpTuning Read(PlayerStatEntity stats) =>
            new JumpTuning(
                stats.Read(PlayerStatType.JumpHeight),
                stats.Read(PlayerStatType.JumpCooldown),
                stats.Read(PlayerStatType.CoyoteTime),
                stats.Read(PlayerStatType.JumpBuffer),
                stats.Read(PlayerStatType.RiseGravity),
                stats.Read(PlayerStatType.ShortHopGravity),
                stats.Read(PlayerStatType.FallGravity),
                Mathf.RoundToInt(stats.Read(PlayerStatType.ExtraAirJumps)));
    }
}
