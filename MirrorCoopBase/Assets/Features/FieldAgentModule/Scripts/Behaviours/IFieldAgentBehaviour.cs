namespace Features.FieldAgentModule.Scripts.Behaviours {
    // What an agent wants now: a small swappable class (seek, orbit, attach, carry, flee, idle) that turns the agent's
    // situation into a goal; the movement modes decide how to reach it. Whoever owns the agent swaps it from outside
    // (IFieldAgentControl.SetBehaviour), for example once IsFinished is true.
    public interface IFieldAgentBehaviour {
        public FieldAgentGoal Evaluate(in FieldAgentBehaviourInput input);
        public bool IsFinished(in FieldAgentBehaviourInput input);
    }
}
