namespace Lifecycle.Core;

/// <summary>Thread-safe lifecycle notifications. Invalid inputs throw; ordinary sink failures do not.</summary>
public interface ILifecycle
{
    void Phase(LifecyclePhase phase);
    void Milestone(string name);
    void Milestone(string name, IReadOnlyDictionary<string, object?> attributes);
    void Condition(string name, ConditionState state);
    void Condition(string name, ConditionState state, IReadOnlyDictionary<string, object?> attributes);
}
