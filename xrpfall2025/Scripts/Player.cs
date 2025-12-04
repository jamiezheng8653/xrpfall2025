using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Godot;
using Vector3 = Godot.Vector3;
using System.IO;
using System.Text.Json;

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
	private States storedItem = States.Regular;

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
	public Vector3 CurrentPosition => Position;
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

	private float radius = 1;
	private Vector3 halflength;

	//Timer for transitioning between states
	private Stopwatch timer;

	//checkpoints passed for the onFinishline Collision check --> lap increment
	private List<Checkpoint> passedCheckpoints = new List<Checkpoint>();

	private int totalCheckpoints = 0;
	
	// Used for customization of car mesh color
	private MeshInstance3D carMesh;

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
	// Makes the current stored item publc to be able to access in hud.gd
	public States StoredItem
	{
		get { return storedItem; }
		set { storedItem = value; }
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
			temp.Position = this.GlobalPosition - temp.Size / 2;
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

	/// <summary>
	/// Initialize the spawning position of the player car 
	/// and set the reference to the stage's track
	/// </summary>
	/// <param name="startingPosition"></param>
	/// <param name="facingDirection"></param>
	public void Init(Vector3 startingPosition, Path3D track, int totalCheckpoints/*, Vector3 facingDirection*/)
	{
		GlobalPosition = startingPosition + new Vector3(0, 5, 0);
		this.track = track;
		this.totalCheckpoints = totalCheckpoints;
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
		
		// For customization, not yet applied
		carMesh = GetNode<MeshInstance3D>("CollisionShape3D/XRP_Car/SM_Car");
		ApplySavedCarColor();
		
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
		// Activate stored item when E is pressed
		if (Input.IsActionJustPressed("use_item"))
		{
			if (storedItem != States.Regular)
			{
				ApplyItemEffect(storedItem); // Apply the stored effect
				storedItem = States.Regular; // Clear stored item after use
			}
			else
			{
				GD.Print("No stored item to use.");
			}
		}
		//Adjust left and right steering
		if (((Input.IsActionPressed("right") && current != States.Inverted)
			|| (Input.IsActionPressed("left") && current == States.Inverted)) && IsOnFloor())
		{
			RotateObjectLocal(new Vector3(0, 1, 0), -(float)rotationIncrement);
		}
		else if (((Input.IsActionPressed("left") && current != States.Inverted)
			|| (Input.IsActionPressed("right") && current == States.Inverted)) && IsOnFloor())
		{
			RotateObjectLocal(new Vector3(0, 1, 0), (float)rotationIncrement);
		}

		//accelerate in forward or backward direction
		if (Input.IsActionPressed("back") && IsOnFloor())
		{
			if (speed > -maxSpeed) speed -= acceleration * delta;
		}
		else if (Input.IsActionPressed("forward") && IsOnFloor())
		{
			if (speed < maxSpeed) speed += acceleration * delta;
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
		switch (state)
{
	case States.Inverted:
		speed *= -1; // keep inverted controls
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
					//ClearTimer();
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
					//ClearTimer();
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
					//ClearTimer();
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
			LookAt(GlobalTransform.Origin + direction, Vector3.Up);

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

		GlobalPosition = passedCheckpoints[^1].Position + new Vector3(0, 1, 0);

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
	
	//Customization file reading, saves the color to be applied to the car
	private void ApplySavedCarColor()
	{
		string path = ProjectSettings.GlobalizePath("user://customization.json");
		if (!File.Exists(path))
		{
			GD.Print("No customization file found.");
			return;
		}
		else 
		{
			GD.Print("File Found.");
		}

		string json = File.ReadAllText(path);
		var data = JsonSerializer.Deserialize<CustomizationData>(json);

		if (data != null && !string.IsNullOrEmpty(data.CarColor))
		{
			Color color = new Color(data.CarColor); // Godot parses "#RRGGBB"
			
			if (color != null){
				GD.Print(color);
			}
			
			SetCarColor(color);
		}
	}
	
	// Sets the material of the car to the proper color
	private void SetCarColor(Color color)
	{
		if (carMesh == null)
		{
			GD.PrintErr("Car mesh not found!");
			return;
		}

		var mat = carMesh.GetActiveMaterial(0) as StandardMaterial3D;
		if (mat != null)
		{
			var newMat = (StandardMaterial3D)mat.Duplicate();
			newMat.AlbedoColor = color;
			carMesh.SetSurfaceOverrideMaterial(0, newMat);
		}
		else
		{
			var newMat = new StandardMaterial3D();
			newMat.AlbedoColor = color;
			carMesh.MaterialOverride = newMat;
		}
	}

	private class CustomizationData
	{
		[System.Text.Json.Serialization.JsonPropertyName("car_color")]
		public string CarColor { get; set; }
	}
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
	 
}
