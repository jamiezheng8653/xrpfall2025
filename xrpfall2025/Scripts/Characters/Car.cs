using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

//Enum States
public enum States
{
	Inverted,
	Slow,
	Fast,
	Regular
};

/// <summary>
/// Parent class that all car types will inherit from.
/// In this case: Player and Enemy AI
/// </summary>
public partial class Car : Node
{
    //Fields
    protected States current; //current state
    //Timer for transitioning between states
    protected Stopwatch timer;

    #region racing related fields
    // Temporary values
    protected int place = 1;
    protected int lap = 1;
    protected bool finishedRace = false;
    protected Path3D track;

    //checkpoints passed for the onFinishline Collision check --> lap increment
    protected List<Checkpoint> passedCheckpoints = new List<Checkpoint>();

    protected int totalCheckpoints = 0;
    #endregion

    #region movement related fields
    protected const double MAXSPEED = 30;
    protected double maxSpeed = 30;

    //if the player gets a speedup or speed debuff, 
    //multiply/divide speed by cooresponding multiplier
    //has to be int because Vectors are made up by ints
    [Export] protected double slowMultiplier = 0.5;
    [Export] protected double fastMultiplier = 2;

    // How fast the player moves in meters per second.
    protected double speed = 0;
    protected double acceleration = 10;
    //Rotation speed
    protected double rotationIncrement = Mathf.DegToRad(2); //per delta
                                                            // The downward acceleration when in the air, in meters per second squared.
    [Export] protected int fallAcceleration = 10;
    protected Vector3 prevPosition; //tracked just before an object starts falling
    protected List<Vector3> pathOfFalling;
    #endregion

    #region car mesh and size related fields
    //Gizmos for debugging
    protected Godot.Color color;
    [Export] protected CsgBox3D csgBox3D = null;
    [Export] protected CharacterBody3D charbody3d = null;
    // Used for customization of car mesh color
    protected MeshInstance3D carMesh;

    protected float radius = 1;
    protected Vector3 halflength;
    #endregion

    #region Properties
    // Expose properties for HUD
    public double Speed => CurrentSpeed;
    public double Acceleration => acceleration;
    public Vector3 CurrentPosition => charbody3d.Position;
    public Vector3 GlobalPosition => charbody3d.GlobalPosition;
    public int Place => place;
    public int Lap => lap;

    /// <summary>
    /// The current state of the player car. Get/Set
    /// Relevant when we eventually add items that modify
    /// the player's state (defined by enum State)
    /// </summary>
    public States Current
    {
        get { return current; }
        set { current = value; }
    }

    /// <summary>
    /// How fast is the car currently going
    /// </summary>
    public double CurrentSpeed
    {
        get
        {
            return current switch
            {
                States.Slow => speed * slowMultiplier,
                States.Fast => speed * fastMultiplier,
                _ => speed,
            };
        }
    }

    /// <summary>
    /// Property to give the player's aabb centered on the player,
    /// used for collision calculations happening in the collided item
    /// </summary>
    public Aabb AABB
    {
        get
        {
            //move the aabb to the actual object's transform
            //otherwise the aabb sits in the origin
            Aabb temp = csgBox3D.GetAabb();
            temp.Position = charbody3d.GlobalPosition - temp.Size / 2;
            return temp;
        }
    }

    /// <summary>
    /// Refers to the radius of the bounding circle 
    /// used in the checkpoint collision checks
    /// </summary>
    public float Radius
    {
        get { return radius; }
    }

    /// <summary>
    /// Refers to the halflength of the bounding box in 
    /// the x, y, and z direction. Used for aabb collision checks
    /// </summary>
    public Vector3 Halflength
    {
        get { return halflength; }
    }

    // making end condition public so it can be reached in HUD
    public bool FinishedRace
    {
        get { return finishedRace; }
    }
    #endregion

    /// <summary>
    /// Initialize the spawning position of the player car 
    /// and set the reference to the stage's track
    /// </summary>
    /// <param name="startingPosition"></param>
    /// <param name="facingDirection"></param>
    public void Init(Vector3 startingPosition, Path3D track, int totalCheckpoints/*, Vector3 facingDirection*/)
    {
        charbody3d.GlobalPosition = startingPosition + new Vector3(0, 5, 0);
        this.track = track;
        this.totalCheckpoints = totalCheckpoints;
    }

    #region methods regarding the car's state
    /// <summary>
    /// Calculates the speed value for the final position change 
    /// without modifying the original speed value. 
    /// </summary>
    /// <param name="state">What state is the player in currently</param>
    /// <param name="speed">How fast is the player supposedly going</param>
    /// <returns>The final speed value to be fed to the player</returns>
    protected double UpdateStateSpeed(States state, double speed)
    {
        switch (state)
        {
            //multiply speed
            //quickly accelerate to twice the current speed (like one to 1.5 seconds, whatever feels good)
            case States.Fast:
                if (timer.ElapsedMilliseconds >= 0)
                {
                    speed *= fastMultiplier;
                }
                break;
            //divide speed
            //cut speed to 50% to 75% of the max speed
            case States.Slow:
                if (timer.ElapsedMilliseconds >= 0)
                {
                    speed *= slowMultiplier;
                }
                break;
            //flip flop controls
            case States.Inverted:
                speed *= -1;
                break;
        }
        //stop moving if you've completed all three laps
        if (finishedRace) speed *= 0;
        //GD.Print("speed: " + speed);
        return speed;
    }

    /// <summary>
    /// Transition logic that should happen after the player 
    /// transitions from States.Regular to any other state.
    /// After a clear condition is met, the player's current 
    /// state will revert back to States.Regular.
    /// </summary>
    /// <param name="prevState">The player's current state that is not States.Regular</param>
    /// <param name="speed">How fast is the player moving right now</param>
    /// <param name="delta">deltatime</param>
    protected void RevertState(States prevState, double speed, double delta)
    {
        //according to what state was previously, the process to revert back to States.Regular will differ slightly
        switch (prevState)
        {
            //Fast -> Regular
            //slowly decelerate to the normal max speed (over the course of 5 seconds, whatever feels good)
            //change state to Regular
            case States.Fast:
                //if (timer.ElapsedMilliseconds >= 5000) speed -= acceleration * delta;
                if (timer.ElapsedMilliseconds >= 5000) //if (speed < maxSpeed)
                {
                    maxSpeed = MAXSPEED;
                    current = States.Regular;
                    ClearTimer();
                }
                break;

            //Slow -> Regular
            //be slow for about 10s
            //change state to Regular, acceleration should be as normal
            case States.Slow:
                //after 10s, revert the maxSpeed
                if (timer.ElapsedMilliseconds >= 10000)
                {
                    maxSpeed = MAXSPEED;
                    current = States.Regular;
                    ClearTimer();
                }

                break;

            //Inverted to Regular
            //set timer of 15s, controls are inverted during this time
            //after 15s, change state to Regular. 
            case States.Inverted:
                //after 10s, revert speed's sign to what it was before
                //change state to Regular
                if (timer.ElapsedMilliseconds >= 10000)
                {
                    current = States.Regular;
                    ClearTimer();
                }
                break;
        }
    }

    /// <summary>
    /// Starts the timer associated with the state transition logic
    /// Unsubscribes this method from the OnItemCollision event once called
    /// </summary>
    public void StartTimer()
    {
        timer.Restart();
    }

    /// <summary>
    /// Resets the timer associated with the state transition logic
    /// Resubscribes StartTimer method to OnItemCollision event
    /// </summary>
    public void ClearTimer()
    {
        timer.Reset();
    }
    #endregion

    #region methods regarding the car's checkpoint tracking
    /// <summary>
    /// If the player has crossed one of the checkpoints placed 
    /// around the track, add that specific checkpoint to the 
    /// player's list of passed checkpoints this lap
    /// </summary>
    /// <param name="chpt">The current checkpoint passed</param>
    public void AddCheckpoint(Checkpoint chpt)
    {
        //ensure the checkpoint does not exist in the list before adding 
        if (!passedCheckpoints.Contains(chpt)) passedCheckpoints.Add(chpt);
        GD.Print("Adding Checkpoint: " + chpt);
    }

    /// <summary>
    /// Checks if this player has gone through all checkpoints on the track
    /// </summary>
    /// <returns>True if all checkpoints exist in this "passedCheckpoints" list</returns>
    public bool CheckCheckpoints()
    {
        if (passedCheckpoints.Count >= totalCheckpoints) return true;
        else return false;
    }

    /// <summary>
    /// Clears the list of checkpoints passed. Called after CheckCheckpoints() returns true
    /// </summary>
    public void ClearCheckpoints()
    {
        passedCheckpoints.Clear();
        GD.Print("Clearing Checkpoints");
    }
    #endregion

    /// <summary>
    /// Increment the lap counter. This is called upon
    /// passing the finishline collision check
    /// </summary>
    public void IncrementLap()
    {
        if (lap + 1 > 3) finishedRace = true;
        else lap++;

    }

    /// <summary>
    /// Logic that happens whenever the player falls off the track and hits the kill plane
    /// </summary>
    /// <param name="prevPosition">the position of the player just before falling</param>
    /// <param name="currentPosition">the position of the player upon impact with the kill plane</param>
    public void ReturnToTrack()
    {
        if (pathOfFalling?.Any() == true)
        {
            //GD.Print("Getting back on track!");
            //GD.Print("First point: " + pathOfFalling[0] + " Last Point: " + pathOfFalling[^1]);
            //calculate the point which the player is closests to the line. 

            //calculate the projection of the vector between prv and current onto the kill plane
            Vector3 u = pathOfFalling[^1] - pathOfFalling[0];
            Vector3 v = new Vector3(pathOfFalling[0].X, pathOfFalling[^1].Y, pathOfFalling[0].Z);

            //get the direction that points towards the origin
            Vector3 d = (Vector3.Zero - pathOfFalling[0]).Normalized();

            pathOfFalling.Clear(); //empty the list for the next fall
                                   //set the player's global position to the new pt
            charbody3d.GlobalPosition = -(Utils.ProjUOntoV(u, v).Normalized() - new Vector3(d.X, 5, d.Z))
                + Utils.GetClosestAbsolutePosition(track, charbody3d.GlobalPosition);

            //point the player towards the track direction of flow.
            Vector3 direction = (
                Utils.GetClosestAbsolutePosition(track, charbody3d.GlobalPosition) -
                Utils.GetClosestAbsolutePosition(track, charbody3d.GlobalPosition + new Vector3(0.001f, 0, 0.001f))
                ).Normalized();
            //only consistent if we are driving in the general south direction. Otherwise the direction given is backwards. 
            charbody3d.LookAt(charbody3d.GlobalTransform.Origin + direction, Vector3.Up);

            //GD.Print("Projection vector: " + Utils.ProjUOntoV(u, v));
        }

    }

    /// <summary>
    /// Returns the player to the last point in the bezier curve 
    /// they passed in the event they fall off the track and hit the kill plane
    /// </summary>
    public void ToPreviousCheckpoint()
    {
        charbody3d.GlobalPosition = passedCheckpoints[^1].Position + new Vector3(0, 1, 0);
    }

    virtual protected void ApplySavedCarColor() { }
    virtual protected void SetCarColor(Color color) { }
}
