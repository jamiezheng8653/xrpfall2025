using Godot;
using System;
using System.Numerics;

public partial class MarkovStateMachine : Node
{
    //state vector
    private const int N = 5; //n states
    private float[] state; //size N

    //frames to wait before using the default transition
    private const int RESETTIME = 240;

    //default transition matrix (N x N)
    private float[,] defaultTransitionMatrix;

    //the current countdown
    private int currentTime = RESETTIME;

    //a list of transitions
    private MarkovTransition[] transitions;

    public override void _Ready()
    {
        state = new float[N];
        defaultTransitionMatrix = new float[N, N];
    }
    
    //should return a set of method that returns void
    private Action[] Update() 
    {
        //check each transition for a trigger
        MarkovTransition triggeredTransition = null;

        foreach (MarkovTransition transition in transitions)
        {
            if (transition.IsTriggered())
            {
                triggeredTransition = transition;
                break;
            }
        }

        //check if we have a transition to fire
        if (triggeredTransition != null)
        {
            //reset timer
            currentTime = RESETTIME;

            //multiply the matrix and the state vector
            float[,] matrix = triggeredTransition.GetMatrix();
            state = Utils.MatrixMultiplication(matrix, state);

            //return the triggered transition's action list
            return triggeredTransition.GetActions();
        }
        else
        {
            //otherwise check the timer
            currentTime -= 1;

            if (currentTime <= 0)
            {
                //do the default transition
                state = Utils.MatrixMultiplication(defaultTransitionMatrix, state);
                currentTime = RESETTIME;
            }

            //return no actions since no transition triggered;
            return [];
        }
    }
}

public class MarkovTransition
{
    /// <summary>
    /// Determines if this transition is to be triggered
    /// </summary>
    /// <returns>If conditions are met to transition, return true. otherwise else</returns>
    public bool IsTriggered()
    {
        bool result = false;

        return result;
    }

    /// <summary>
    /// Grab the transition matrix to be multiplied with a state vector
    /// </summary>
    /// <returns>The transition matrix</returns>
    public float[,] GetMatrix()
    {
        float[,] result = new float[5, 5];

        return result;
    }

    /// <summary>
    /// Get cooresponding methods that return void and take a double
    /// i.e., private void FuncName(double delta)
    /// </summary>
    /// <returns>Set of methods for this transition</returns>
    public Action[] GetActions()
    {
        Action[] result = new Action[5];

        return result;
    }
}
