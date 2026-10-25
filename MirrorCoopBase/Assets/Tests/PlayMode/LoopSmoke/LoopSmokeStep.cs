#if UNITY_INCLUDE_TESTS
using System.Reflection;

namespace Tests.PlayMode.LoopSmoke {
    // One registered step: a method of a check class and the slot and order it runs at.
    public sealed class LoopSmokeStep {
        public LoopSmokeSlot Slot { get; }
        public int Order { get; }
        public MethodInfo Method { get; }

        public string Name =>
            $"{Method.DeclaringType.Name}.{Method.Name}";

        public LoopSmokeStep(LoopSmokeSlot slot, int order, MethodInfo method) {
            Slot = slot;
            Order = order;
            Method = method;
        }
    }
}
#endif
