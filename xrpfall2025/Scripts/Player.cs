using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using Vector3 = Godot.Vector3;

//Enum States
public enum States
{
	Inverted,
	Slow,
	Fast,
	Regular
};

public partial class Player : Node
{
	private States current; //current state

	//if the player gets a speedup or speed debuff, 
	//multiply/divide speed by cooresponding multiplier
	//has to be int because Vectors are made up by ints
	[Export] private double slowMultiplier = 0.5;
	[Export] private double fastMultiplier = 2;

	private const double MAXSPEED = 30;
	private double maxSpeed = 30;

	// How fast the player moves in meters per second.
	private double speed = 0;
	private double acceleration = 10;

	// Temporary values
	private int place = 1;
	private int lap = 1;
	private bool finishedRace = false;

	// Expose properties for HUD
	public double Speed => CurrentSpeed;
	public double Acceleration => acceleration;
	public Vector3 CurrentPosition => charbody3d.Position;
	public int Place => place;
	public int Lap => lap;

	//Rotation speed
	private double rotationIncrement = Mathf.DegToRad(2); //per delta

	// The downward acceleration when in the air, in meters per second squared.
	[Export] private int fallAcceleration = 10;
	private Vector3 prevPosition; //tracked just before an object starts falling
	private List<Vector3> pathOfFalling;

	private Path3D track;

	//Gizmos for debugging
	private Godot.Color color;
	[Export] private CsgBox3D csgBox3D = null;
	[Export] private CharacterBody3D charbody3d = null;
	[Export] private Camera3D camera = null;

	private float radius = 1;
	private Vector3 halflength;

	//Timer for transitioning between states
	private Stopwatch timer;

	//checkpoints passed for the onFinishline Collision check --> lap increment
	private List<Checkpoint> passedCheckpoints = new List<Checkpoint>();

	private int totalCheckpoints = 0;

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
	/// get/set the position of this player in global space
	/// </summary>
	public Vector3 GlobalPosition
	{
		get { return charbody3d.GlobalPosition; }
		set {charbody3d.GlobalPosition = value;}
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

	public override void _EnterTree()
	{
		SetMultiplayerAuthority(int.Parse(Name));
		camera.Visible = IsMultiplayerAuthority();
	}


	/// <summary>
	/// Initialize any values upon load. If you're initializing this 
	/// through another script, please utilize the Init() method.
	/// Currently no Init() method is in place, if we choose to 
	/// programmatically load all objects in, be sure to implement Init()
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
	/// Any logic to ensure no memory leaks (particularly with any events)
	/// when this Player leaves the scene tree during runtime for whatever reason
	/// </summary>
	public override void _ExitTree()
	{
	}

	/// <summary>
	/// General game logic being run every frame.
	/// If you're modifying anything relating to the Player's physics,
	/// please use _PhysicsProcess()
	/// </summary>
	/// <param name="delta">delta time</param>
	public override void _Process(double delta)
	{
		//draw gizmos
		//DebugDraw3D.DrawBox(AABB.Position, Godot.Quaternion.Identity, Vector3.One, color);
		//GD.Print("Player State: " + Current);

		//closest point
		/*DebugDraw3D.DrawBox(
			GetClosestAbsolutePosition(track, GlobalPosition),
			Godot.Quaternion.Identity,
			Vector3.One,
			color
			);*/

		//check the timer
		//GD.Print("Player Timer: " + timer.ElapsedMilliseconds);

		//when collision event, run rng and start stopwatch. unsubscribe collision event and start stopwatch event
		//after certain time, resubscribe collision event, reset stop watch.
	}

	/// <summary>
	/// Called every frame to process how this object will move by what
	/// Will rotate this object left or right, move forwards or backwards locally
	/// and fall if necessary
	/// </summary>
	/// <param name="delta">delta time</param>
	public override void _PhysicsProcess(double delta)
	{
		if (IsMultiplayerAuthority())
		//Adjust left and right steering
		if (((Input.IsActionPressed("right") && current != States.Inverted)
			|| (Input.IsActionPressed("left") && current == States.Inverted)) && charbody3d.IsOnFloor())
		{
			charbody3d.RotateObjectLocal(new Vector3(0, 1, 0), -(float)rotationIncrement);
		}
		else if (((Input.IsActionPressed("left") && current != States.Inverted)
			|| (Input.IsActionPressed("right") && current == States.Inverted)) && charbody3d.IsOnFloor())
		{
			charbody3d.RotateObjectLocal(new Vector3(0, 1, 0), (float)rotationIncrement);
		}

		//accelerate in forward or backward direction
		if (Input.IsActionPressed("back") && charbody3d.IsOnFloor())
		{
			if (speed > -maxSpeed) speed -= acceleration * delta;
		}
		else if (Input.IsActionPressed("forward") && charbody3d.IsOnFloor())
		{
			if (speed < maxSpeed) speed += acceleration * delta;
		}
		else
		{
			//will eventually add friction to come to a gradual stop
			speed *= 0.9 * delta;
		}

		// Vertical velocity
		if (!charbody3d.IsOnFloor()) // If in the air, fall towards the floor. Literally gravity
		{
			//keep adding position before gravity is implemented until impact with kill plane
			pathOfFalling.Add(GlobalPosition);
			charbody3d.Position -= charbody3d.GetTransform().Basis.Y * (float)(delta * fallAcceleration);
		}
		else
		{
			pathOfFalling.Clear();
			//GD.Print("Clearing list!");
		}

		if (timer.ElapsedMilliseconds > 0)
		{
			RevertState(current, speed, delta);
		}

		// Moving the character
		charbody3d.Position += charbody3d.GetTransform().Basis.Z * (float)(delta * UpdateStateSpeed(current, speed)) * -1;

		charbody3d.MoveAndSlide();
		//GD.Print("Player speed: " + speed);
	}

	/// <summary>
	/// Calculating the Axis Realigned Bounding Box (ARBB) of this object
	/// </summary>
	private void CalculateARBB()
	{
		//find 8 corners of oriented bounding box

		//will need to globalize the vectors vector3(matrix4 * vector4(vector, 1))

		//find min and max of the 8 corners

		//find size of the box

	}

	/// <summary>
	/// Calculates the speed value for the final position change 
	/// without modifying the original speed value. 
	/// </summary>
	/// <param name="state">What state is the player in currently</param>
	/// <param name="speed">How fast is the player supposedly going</param>
	/// <returns>The final speed value to be fed to the player</returns>
	private double UpdateStateSpeed(States state, double speed)
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
	private void RevertState(States prevState, double speed, double delta)
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
	private void ClearTimer()
	{
		timer.Reset();
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
			GlobalPosition = -(Utils.ProjUOntoV(u, v).Normalized() - new Vector3(d.X, 5, d.Z))
				+ Utils.GetClosestAbsolutePosition(track, GlobalPosition);

			//point the player towards the track direction of flow.
			Vector3 direction = (
				Utils.GetClosestAbsolutePosition(track, GlobalPosition) -
				Utils.GetClosestAbsolutePosition(track, GlobalPosition + new Vector3(0.001f, 0, 0.001f))
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
		//different checkpoint check
		/* 		//this method will be called when the player falls off the map
				//the slice will be simplified into a isosceles triangle with 
				// the long sides being as long as the length of the longer two magnitudes.
				//generate a slice of angle 360/numOfPts and check for if the player is in the slice. 
				//If true, teleport the player to the point that is behind the front most one (i - 1)

				for (int i = 0; i < track.Curve.PointCount - 1; i++)
				{
					//Triangle points: p[i], p[i+1], origin
					Vector3[] triangle = [
						Vector3.Zero,
						new Vector3((GlobalTransform*track.Curve.GetPointPosition(i)).X, (GlobalTransform*track.Curve.GetPointPosition(i)).Y, (GlobalTransform*track.Curve.GetPointPosition(i)).Z),
						new Vector3((GlobalTransform*track.Curve.GetPointPosition(i + 1)).X, (GlobalTransform*track.Curve.GetPointPosition(i + 1)).Y, (GlobalTransform*track.Curve.GetPointPosition(i + 1)).Z)
						//GlobalTransform * track.Curve.GetPointPosition(i),
						//GlobalTransform * track.Curve.GetPointPosition(i + 1)
					];

					DebugDraw3D.DrawPoints(triangle);

					//GD.Print("TPC Triangle: " + triangle[0] + triangle[1] + triangle[2]);

					//getting the global points that make up the aabb
					//aabb.position is the lower bottom left point (min). 
					// Position/globalPosition is in the center of the object tho
					Vector3[] boxPoints = [
						new Vector3((GlobalPosition - (AABB.Size * 0.5f)).X, (GlobalPosition - (AABB.Size * 0.5f)).Y, (GlobalPosition - (AABB.Size * 0.5f)).Z),
						new Vector3((GlobalPosition + (AABB.Size * 0.5f)).X, (GlobalPosition + (AABB.Size * 0.5f)).Y, (GlobalPosition + (AABB.Size * 0.5f)).Z)
						//GlobalPosition - (AABB.Size * 0.5f), //min
						//GlobalPosition + (AABB.Size * 0.5f)	//max
					];
					//if SAT returns true, move the player to p[i] checkpoint
					if (Utils.TriangleAABBSAT(triangle, boxPoints))
					{
						GlobalPosition = track.Curve.GetPointPosition(i) + (Vector3.Up * 5);
						//kill the loop
						return;
					}
				} */

		charbody3d.GlobalPosition = passedCheckpoints[^1].Position + new Vector3(0, 1, 0);

	}

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
}
