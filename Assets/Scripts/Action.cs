using System.Collections.Generic;
using UnityEngine;

public class AgentAction
{
    public string Name { get; }
    public float Cost { get; private set; }

    public HashSet<AgentBelief> Preconditions { get; } = new();
    public HashSet<AgentBelief> Effects { get; } = new();

    IActionStrategy strategy;

    public bool Complete => strategy.Complete;

    AgentAction(string name)
    {
        Name = name;
    }

    public void Start() => strategy.Start();

    public void Update(float deltaTime)
    {
        if (strategy.CanPerform)
        {
            strategy.Update(deltaTime);
        }

        if (!strategy.Complete) return;

        foreach (var effect in Effects)
        {
            effect.Evaluate();
        }
    }

    public void Stop() => strategy.Stop();

    public class ActionBuilder
    {
        readonly AgentAction action;
        public ActionBuilder(string name)
        {
            action = new AgentAction(name)
            {
                Cost = 1
            };
        }
        public ActionBuilder WithCost(float cost)
        {
            action.Cost = cost;
            return this;
        }
        public ActionBuilder WithStrategy(IActionStrategy strategy)
        {
            action.strategy = strategy;
            return this;
        }
        public ActionBuilder AddPrecondition(AgentBelief precondition)
        {
            action.Preconditions.Add(precondition);
            return this;
        }
        public ActionBuilder AddEffect(AgentBelief effect)
        {
            action.Effects.Add(effect);
            return this;
        }
        public AgentAction Build()
        {
            return action;
        }
    }
}
