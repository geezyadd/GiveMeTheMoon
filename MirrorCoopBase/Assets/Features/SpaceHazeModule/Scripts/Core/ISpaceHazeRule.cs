using Features.SpaceHazeModule.Data;

namespace Features.SpaceHazeModule.Scripts.Core {
    public interface ISpaceHazeRule {
        public SpaceHazeShape CreateShape(SpaceHazeModel spaceHazeModel);
        public float GetHazeShare(SpaceHazeShape shape, float distanceMeters);
    }
}
