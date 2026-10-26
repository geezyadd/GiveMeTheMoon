using UnityEngine;

namespace Features.SpaceHazeModule.Scripts.Rendering {
    // The sky behind a hazed thing is the scene's six-sided skybox (RenderSettings.skybox): its faces, tint, exposure
    // and turn go into the haze material, so a thing in the haze fades into the very sky around it. A skybox without
    // the six faces leaves the haze its plain tint. Read again only when the scene's skybox changes.
    public sealed class SpaceHazeSkySource : ISpaceHazeSkySource {
        // The skybox shader doubles its tint: 2 in gamma space is this in linear space (unity_ColorSpaceDouble).
        private const float LINEAR_COLOR_SPACE_DOUBLE = 4.59479380f;

        private static readonly int _frontId = Shader.PropertyToID("_FrontTex");
        private static readonly int _backId = Shader.PropertyToID("_BackTex");
        private static readonly int _leftId = Shader.PropertyToID("_LeftTex");
        private static readonly int _rightId = Shader.PropertyToID("_RightTex");
        private static readonly int _upId = Shader.PropertyToID("_UpTex");
        private static readonly int _downId = Shader.PropertyToID("_DownTex");
        private static readonly int _tintId = Shader.PropertyToID("_Tint");
        private static readonly int _exposureId = Shader.PropertyToID("_Exposure");
        private static readonly int _rotationId = Shader.PropertyToID("_Rotation");
        private static readonly int _hazeSkyColorId = Shader.PropertyToID("_SpaceHazeSkyColor");
        private static readonly int _hazeSkyRotationId = Shader.PropertyToID("_SpaceHazeSkyRotation");
        private static readonly int _hazeHasSkyId = Shader.PropertyToID("_SpaceHazeHasSky");
        private static readonly int[] _faceIds = { _frontId, _backId, _leftId, _rightId, _upId, _downId };

        private Material _appliedSkybox;
        private Material _appliedHazeMaterial;

        public void Apply(Material hazeMaterial) {
            Material skybox = RenderSettings.skybox;
            if (skybox == _appliedSkybox && hazeMaterial == _appliedHazeMaterial)
                return;

            _appliedSkybox = skybox;
            _appliedHazeMaterial = hazeMaterial;
            bool hasSky = HasSixFaces(skybox);
            hazeMaterial.SetFloat(_hazeHasSkyId, hasSky ? 1f : 0f);
            if (hasSky)
                CopySky(skybox, hazeMaterial);
        }

        private static bool HasSixFaces(Material skybox) {
            if (skybox == null)
                return false;

            foreach (int faceId in _faceIds) {
                if (skybox.HasTexture(faceId) == false || skybox.GetTexture(faceId) == null)
                    return false;
            }

            return true;
        }

        private static void CopySky(Material skybox, Material hazeMaterial) {
            foreach (int faceId in _faceIds)
                hazeMaterial.SetTexture(faceId, skybox.GetTexture(faceId));

            Color tint = skybox.HasColor(_tintId) ? skybox.GetColor(_tintId).linear * LINEAR_COLOR_SPACE_DOUBLE : Color.white;
            float exposure = skybox.HasFloat(_exposureId) ? skybox.GetFloat(_exposureId) : 1f;
            hazeMaterial.SetColor(_hazeSkyColorId, tint * exposure);
            hazeMaterial.SetFloat(_hazeSkyRotationId, skybox.HasFloat(_rotationId) ? skybox.GetFloat(_rotationId) : 0f);
        }
    }
}
