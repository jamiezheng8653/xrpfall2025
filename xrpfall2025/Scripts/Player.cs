//Code from https://docs.godotengine.org/en/4.4/getting_started/first_3d_game/03.player_movement_code.html
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
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

public partial class Player : CharacterBody3D
{
	private States current; //current state

	//if the player gets a speedup or speed debuff, 
	//multiply/divide speed by cooresponding multiplier
	//has to be int because Vectors are made up by ints
	[Export] private int slowMultiplier = 2;
	[Export] private int fastMultiplier = 2;

	private double maxSpeed = 30;

	// How fast the player moves in meters per second.
	private double speed = 0;
	private double acceleration = 10;

	// Temporary values
	private int place = 3;
	private int lap = 2;

	// Expose properties for HUD
	public double Speed => speed;
	public double Acceleration => acceleration;
	public Vector3 CurrentPosition => Position;
	public int Place => place;
	public int Lap => lap;

	//Rotation speed
	[Export] private double rotationSpeed = 1.5;
	private double rotationIncrement = Mathf.DegToRad(2); //per delta

	// The downward acceleration when in the air, in meters per second squared.
	[Export] private int fallAcceleration = 10;
	private Vector3 prevPosition; //tracked just before an object starts falling
	private List<Vector3> pathOfFalling;
	private bool kpImpact = false;

	private Curve3D track;

	//Gizmos for debugging
	private Color color;
	[Export] private CsgBox3D csgBox3D = null;

	//Timer for transitioning between states
	private Stopwatch timer;

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
			temp.Position = this.GlobalPosition - temp.Size / 2;
			return temp;
		}
	}

	/// <summary>
	/// Initialize the spawning position of the player car 
	/// and set the reference to the stage's track
	/// </summary>
	/// <param name="startingPosition"></param>
	/// <param name="facingDirection"></param>
	public void Init(Vector3 startingPosition, Curve3D track/*, Vector3 facingDirection*/)
	{
		GlobalPosition = startingPosition + new Vector3(0, 5, 0);
		this.track = track;
	}

	/// <summary>
	/// Initialize any values upon load. If you're initializing this 
	/// through another script, please utilize the Init() method.
	/// Currently no Init() method is in place, if we choose to 
	/// programmatically load all objects in, be sure to implement Init()
	/// </summary>
	public override void _Ready()
	{
		color = new Color("CYAN");
		current = States.Regular;
		timer = new Stopwatch();
		prevPosition = new Vector3();
		pathOfFalling = new List<Vector3>();
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
		DebugDraw3D.DrawBox(AABB.Position, Godot.Quaternion.Identity, Vector3.One, color);
		//GD.Print("Player State: " + Current);

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
		//Adjust left and right steering
		if ((Input.IsActionPressed("right") && current != States.Inverted)
			|| (Input.IsActionPressed("left") && current == States.Inverted))
		{
			RotateObjectLocal(new Vector3(0, 1, 0), -(float)rotationIncrement);
			//GD.Print("Pressed D right");
		}
		else if ((Input.IsActionPressed("left") && current != States.Inverted)
			|| (Input.IsActionPressed("right") && current == States.Inverted))
		{
			RotateObjectLocal(new Vector3(0, 1, 0), (float)rotationIncrement);
			//GD.Print("Pressed A left");
		}

		//accelerate in forward or backward direction
		if (Input.IsActionPressed("back"))
		{
			if (speed > -maxSpeed) speed -= acceleration * delta;
		}
		else if (Input.IsActionPressed("forward"))
		{
			//direction.Z -= 1.0f;
			if (speed < maxSpeed) speed += acceleration * delta;
			//GD.Print("Pressed W forward");
		}
		else
		{
			//will eventually add friction to come to a gradual stop
			speed *= 0.9 * delta;
		}

		// Vertical velocity
		if (!IsOnFloor()) // If in the air, fall towards the floor. Literally gravity
		{
			//keep adding position before gravity is implemented until impact with kill plane
			pathOfFalling.Add(GlobalPosition);
			Position -= GetTransform().Basis.Y * (float)(delta * fallAcceleration);
			//how to check when player just starts falling and not more 
			//GD.Print("I'm falling!");
			GD.Print(pathOfFalling[^1]);
		}
		//else if (!pathOfFalling.Any() && IsOnFloor())
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
		Position += GetTransform().Basis.Z * (float)(delta * UpdateStateSpeed(current, speed)) * -1;

		MoveAndSlide();
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
		//check states and adjust the targetVelocity accordingly
		switch (state)
		{
			//multiply speed
			//quickly accelerate to twice the current speed (like one to 1.5 seconds, whatever feels good)
			case States.Fast:
				if (timer.ElapsedMilliseconds <= 0)
				{
					speed *= fastMultiplier;
					if (speed > maxSpeed * 1.5) speed = maxSpeed * 1.5;
				}
				break;
			//divide speed
			//cut speed to 50% to 75% of the max speed
			case States.Slow:
				if (timer.ElapsedMilliseconds <= 0)
				{
					speed /= slowMultiplier;
					maxSpeed /= 2;
				}
				break;
			//flip flop controls
			case States.Inverted:
				speed *= -1;
				break;
				//regular has no change
				//case States.Regular:
				//break;
		}

		//start the timer if you have not already
		//if (current != States.Regular && timer.ElapsedMilliseconds <= 0) OnItemCollision?.Invoke();

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
				if (timer.ElapsedMilliseconds <= 5000) speed -= acceleration * delta;
				if (speed < maxSpeed)
				{
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
					maxSpeed *= 2;
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
			GD.Print("Getting back on track!");
			GD.Print("First point: " + pathOfFalling[0] + " Last Point: " + pathOfFalling[^1]);
			//calculate the point which the player is closests to the line. 

			//calculate the projection of the vector between prv and current onto the kill plane
			Vector3 u = pathOfFalling[^1] - pathOfFalling[0];
			//eventually this point will be a point on the bezier curve when track generation gets implemented
			//Vector3 v = pathOfFalling[0] + u; 
			Vector3 v = track.GetClosestPoint(Position);
			//point the player towards the track direction of flow. 

			pathOfFalling.Clear(); //empty the list for the next fall
			//set the player's global position to the new pt
			//if the orientation of the player is acute 
			GlobalPosition = -(Utils.ProjUOntoV(u, v).Normalized() - new Vector3(10, 5, -10)) - v;
			//if the orientation of the player is obtuse
			//GlobalPosition = -(Utils.ProjUOntoV(u, v) + new Vector3(10, 5, -10)) + v;
			//if the orientation of the player is orthogonal
			//GlobalPosition = -(Utils.ProjUOntoV(u, v) - new Vector3(0, 5, 10)) + v;
			GD.Print("Projection vector: " + Utils.ProjUOntoV(u, v));
		}

	}
}
