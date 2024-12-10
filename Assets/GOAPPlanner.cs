using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public interface IGOAPPlanner
{
    ActionPlan Plan(GOAPAgent agent, HashSet<AgentGoal> goals, AgentGoal mostRecentGoal = null);
}

public class GOAPPlanner : IGOAPPlanner
{
    public ActionPlan Plan(GOAPAgent agent, HashSet<AgentGoal> goals, AgentGoal mostRecentGoal = null)
    {
        List<AgentGoal> orderedGoals = goals.Where(g => g.DesiredEffects.Any(b => !b.Evaluate())).OrderByDescending(g => g == mostRecentGoal ? g.Priority - 0.01 : g.Priority).ToList();
        foreach (var goal in orderedGoals)
        {
            Node goalNode = new Node(null, null, goal.DesiredEffects, 0);

            if (FindPath(goalNode, agent.actions))
            {
                if (goalNode.isLeafDead) continue;
                
                Stack<AgentAction> actionStack = new Stack<AgentAction>();
                while(goalNode.Leaves.Count > 0)
                {
                    var cheapestLeaf = goalNode.Leaves.OrderBy(leaf => leaf.Cost).First();
                    goalNode = cheapestLeaf;
                    actionStack.Push(cheapestLeaf.Action);
                }
                return new ActionPlan(goal, actionStack, goalNode.Cost);
            }
        }

        Debug.Log("No plan found");
        return null;
    }
    bool FindPath(Node parent, HashSet<AgentAction> actions)
    {
        foreach (var action in actions)
        {
            var requiredEffects = parent.RequiredEffects;

            requiredEffects.RemoveWhere(b => b.Evaluate());

            if (requiredEffects.Count == 0)
            {
                return true;
            }
            if (action.Effects.Any(requiredEffects.Contains))
            {
                var newRequiredEffects = new HashSet<AgentBelief>(requiredEffects);
                newRequiredEffects.ExceptWith(action.Effects);
                newRequiredEffects.UnionWith(action.Preconditions);

                var newAvailableActions = new HashSet<AgentAction>(actions);
                newAvailableActions.Remove(action);

                var newNode = new Node(parent, action, newRequiredEffects, parent.Cost + action.Cost);

                if (FindPath(newNode, newAvailableActions))
                {
                    parent.Leaves.Add(newNode);
                    newRequiredEffects.ExceptWith(newNode.Action.Preconditions);
                }

                if (newRequiredEffects.Count == 0)
                {
                    return true;
                }
            }
        }
        return false;
    }
}

public class Node
{
    public Node Parent { get; }
    public AgentAction Action { get; }
    public HashSet<AgentBelief> RequiredEffects { get; }
    public List<Node> Leaves { get; }
    public float Cost { get; }

    public bool isLeafDead => Leaves.Count == 0 && Action == null;

    public Node(Node parent, AgentAction actio, HashSet<AgentBelief> effects, float cost)
    {
        Parent = parent;
        Action = Action;
        RequiredEffects = new HashSet<AgentBelief>(effects);
        Leaves = new List<Node>();
        Cost = cost;
    }
}



public class ActionPlan : MonoBehaviour
{

    public AgentGoal AgentGoal { get; }
    public Stack<AgentAction> Actions { get; }
    public float TotalCost { get; }

    public ActionPlan(AgentGoal goal, Stack<AgentAction> actions, float totalCost)
    {
        AgentGoal = goal;
        Actions = actions;
        TotalCost = totalCost;
    }
}
