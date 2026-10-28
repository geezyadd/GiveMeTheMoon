using System.Collections.Generic;

namespace Features.FieldAgentModule.Scripts.Agents {
    // Orders agent slots by id for the model's binary search.
    public sealed class FieldAgentIdComparer : IComparer<FieldAgentState> {
        public static readonly FieldAgentIdComparer Instance = new();

        public int Compare(FieldAgentState left, FieldAgentState right) =>
            left.Id.CompareTo(right.Id);
    }
}
