using System.Collections.Generic;
using UnityEngine;

public class AgentGoal
{
    public string Name {  get; }
    public float Priority { get; private set; }
    public HashSet<AgentBelief> DesiredEffects { get; } = new();

    AgentGoal(string name)
    { 
        Name = name;
    }

    public class GoalBuilder
    {
        readonly AgentGoal goal;

        public GoalBuilder(string name)
        {
            goal = new AgentGoal(name);
        }
        public GoalBuilder WithPriority(float priority)
        {
            goal.Priority = priority;
            return this;
        }
        public GoalBuilder WithDesiredEffect(AgentBelief effect)
        {
            goal.DesiredEffects.Add(effect);
            return this;
        }
        public AgentGoal Build()
        {
            return goal;
        }
    }
    }
