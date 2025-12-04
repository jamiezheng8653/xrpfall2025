using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#region Enums
/// <summary>
/// Enum for defining the car's current 
/// state given an item effect from impact/use
/// </summary>
public enum States
{
	Inverted,
	Slow,
	Fast,
	Regular
};
#endregion

/// <summary>
/// Parent class that all car types will inherit from.
/// In this case: Player and Enemy AI
/// </summary>
public partial class Car : Node
{
	#region Fields
	protected States current; //current state
	protected States storedItem = States.Regular;

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

	//if the car gets a speedup or speed debuff, 
	//multiply/divide speed by cooresponding multiplier
	//has to be int because Vectors are made up by ints
	[Export] protected double slowMultiplier = 0.5;
	[Export] protected double fastMultiplier = 2;

	// How fast the car moves in meters per second.
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
	[Export] protected MeshInstance3D carMesh = null;

	protected float radius = 1;
	protected Vector3 halflength;
	#endregion
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
	/// The current state of the car. Get/Set
	/// Relevant when we eventually add items that modify
	/// the car's state (defined by enum State)
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

	// Makes the current stored item publc to be able to access in hud.gd
	public States StoredItem
	{
		get { return storedItem; }
		set { storedItem = value; }
	}

	/// <summary>
	/// How many checkpoints has this car passed this lap?
	/// </summary>
	public int NumPassedCheckpoints
	{
		get { return passedCheckpoints.Count; }
	}

	/// <summary>
	/// Property to give the car's aabb centered on the car,
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

	#region Methods

	/// <summary>
	/// Initialize any values upon load. If you're initializing this 
	/// through another script, please utilize the Init() method.
	/// </summary>
	public override void _Ready()
	{
		color = new Godot.Color("CYAN");
		current = States.Regular;
		timer = new Stopwatch();
		prevPosition = new Vector3();
		pathOfFalling = new List<Vector3>();
		halflength = new Vector3(radius, radius, radius); //TODO
	}

	/// <summary>
	/// Handle any forces or causes of speed change. 
	/// Call this method base._PhysicsProcess(delta) 
	/// in children AFTER handling input
	/// </summary>
	/// <param name="delta">Delta time</param>
	public override void _PhysicsProcess(double delta)
	{
		// Vertical velocity
		if (!charbody3d.IsOnFloor()) // If in the air, fall towards the floor. Literally gravity
		{
			//keep adding position before gravity is implemented until impact with kill plane
			pathOfFalling.Add(charbody3d.GlobalPosition);
			charbody3d.Position -= charbody3d.GetTransform().Basis.Y * (float)(delta * fallAcceleration);
			speed = 0;
		}
		else
		{
			pathOfFalling?.Clear();
			//GD.Print("Clearing list!");
		}

		if (timer.ElapsedMilliseconds > 0)
		{
			RevertState(current, speed, delta);
		}

		// Moving the character
		charbody3d.Position += charbody3d.GetTransform().Basis.Z * (float)(delta * UpdateStateSpeed(current, speed)) * -1;

		charbody3d.MoveAndSlide();
	}


	/// <summary>
	/// Initialize the spawning position of the car 
	/// and set the reference to the stage's track
	/// </summary>
	/// <param name="startingPosition">Where is the car going to spawn on the track</param>
	/// <param name="facingDirection">which way should the car be facing</param>
	public virtual void Init(Vector3 startingPosition, Path3D track, int totalCheckpoints/*, Vector3 facingDirection*/)
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
	/// <param name="state">What state is the car in currently</param>
	/// <param name="speed">How fast is the car supposedly going</param>
	/// <returns>The final speed value to be fed to the car</returns>
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
	/// Transition logic that should happen after the car 
	/// transitions from States.Regular to any other state.
	/// After a clear condition is met, the car's current 
	/// state will revert back to States.Regular.
	/// </summary>
	/// <param name="prevState">The car's current state that is not States.Regular</param>
	/// <param name="speed">How fast is the car moving right now</param>
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
	public void StartTimer(Car c)
	{
		if (c != this) return;
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
	/// If the car has crossed one of the checkpoints placed 
	/// around the track, add that specific checkpoint to the 
	/// car's list of passed checkpoints this lap
	/// </summary>
	/// <param name="chpt">The current checkpoint passed</param>
	public void AddCheckpoint(Checkpoint chpt, Car c)
	{
		if (c != this) return;
		//ensure the checkpoint does not exist in the list before adding 
		if (!passedCheckpoints.Contains(chpt)) 
		{
			passedCheckpoints.Add(chpt);
			GD.Print("Adding Checkpoint: " + chpt);
		}
	}

	/// <summary>
	/// Checks if this car has gone through all checkpoints on the track
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
	public void ClearCheckpoints(Car c)
	{
		if(c != this) return;
		passedCheckpoints.Clear();
		GD.Print("Clearing Checkpoints");
	}

	/// <summary>
	/// Calculate how far this car is from the last checkpoint they pressed
	/// Use in placement tracking logic
	/// </summary>
	/// <returns>The distance squared from the last checkpoint</returns>
	public float DistanceFromLastCheckpoint()
	{
		return passedCheckpoints[^1].GlobalPosition.DistanceSquaredTo(GlobalPosition);
	}
	#endregion

	/// <summary>
	/// Increment the lap counter. This is called upon
	/// passing the finishline collision check
	/// </summary>
	public void IncrementLap(Car c)
	{
		if (c != this) return;
		if (lap + 1 > 3) finishedRace = true;
		else lap++;

	}

	/// <summary>
	/// Changes the placement of the car given a int
	/// </summary>
	/// <param name="i">What place is the car in overall</param>
	public void PlacementChanged(int i)
	{
		place = i;
	}

	/// <summary>
	/// Logic that happens whenever the car falls off the track and hits the kill plane
	/// </summary>
	/// <param name="prevPosition">the position of the car just before falling</param>
	/// <param name="currentPosition">the position of the car upon impact with the kill plane</param>
	public void ReturnToTrack(Car c)
	{
		if (c != this) return;
		if (pathOfFalling?.Any() == true)
		{
			//GD.Print("Getting back on track!");
			//GD.Print("First point: " + pathOfFalling[0] + " Last Point: " + pathOfFalling[^1]);
			//calculate the point which the car is closests to the line. 

			//calculate the projection of the vector between prv and current onto the kill plane
			Vector3 u = pathOfFalling[^1] - pathOfFalling[0];
			Vector3 v = new Vector3(pathOfFalling[0].X, pathOfFalling[^1].Y, pathOfFalling[0].Z);

			//get the direction that points towards the origin
			Vector3 d = (Vector3.Zero - pathOfFalling[0]).Normalized();

			pathOfFalling.Clear(); //empty the list for the next fall
								   //set the car's global position to the new pt
			charbody3d.GlobalPosition = -(Utils.ProjUOntoV(u, v).Normalized() - new Vector3(d.X, 5, d.Z))
				+ Utils.GetClosestAbsolutePosition(track, charbody3d.GlobalPosition);

			//point the car towards the track direction of flow.
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
	/// Returns the car to the last point in the bezier curve 
	/// they passed in the event they fall off the track and hit the kill plane
	/// </summary>
	public void ToPreviousCheckpoint(Car c)
	{
		if (c != this) return;
		charbody3d.GlobalPosition = passedCheckpoints[^1].Position + new Vector3(0, 1, 0);
	}

	virtual protected void ApplySavedCarColor() { }
	virtual protected void SetCarColor(Color color) { }

	/// <summary>
	/// Applies the chosen item effect to the player and starts the timer
	/// </summary>
	private void ApplyItemEffect(States item)
	{
		switch (item)
		{
			case States.Fast:
				current = States.Fast;
				maxSpeed = MAXSPEED * fastMultiplier;
				timer.Restart();
				GD.Print("Activated Fast item");
				break;

			case States.Slow:
				current = States.Slow;
				maxSpeed = MAXSPEED * slowMultiplier;
				timer.Restart();
				GD.Print("Activated Slow item");
				break;

			case States.Inverted:
				current = States.Inverted;
				timer.Restart();
				GD.Print("Activated Inverted item");
				break;

			default:
				GD.Print("Invalid item state.");
				break;
		}
	}

	/// <summary>
	/// Stores an item. Overrides any existing stored item.
	/// </summary>
	public void StoreItem(States newItem)
	{
		storedItem = newItem;
		GD.Print($"Stored new item: {storedItem}");
	}

	#endregion

}
