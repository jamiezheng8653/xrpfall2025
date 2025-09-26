//Code from https://docs.godotengine.org/en/4.4/getting_started/first_3d_game/03.player_movement_code.html
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
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
	public event ItemCollisionDelegate OnItemCollision;
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

	//Rotation speed
	//will be replaced upon adding angular velocity
	[Export] private double rotationSpeed = 1.5f;
	private double rotationIncrement = Mathf.DegToRad(2); //per delta

	// The downward acceleration when in the air, in meters per second squared.
	[Export] private int fallAcceleration = 10;

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
	/// 
	/// </summary>
	public override void _Ready()
	{
		color = new Color("CYAN");
		current = States.Regular;
		timer = new Stopwatch();
		OnItemCollision += StartTimer;
	}

	/// <summary>
	/// 
	/// </summary>
	public override void _ExitTree()
	{
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="delta"></param>
	public override void _Process(double delta)
	{
		//draw gizmos
		DebugDraw3D.DrawBox(AABB.Position, Godot.Quaternion.Identity, Vector3.One, color);
		GD.Print("Player State: " + Current);

		//check the timer
		GD.Print("Player Timer: " + timer.ElapsedMilliseconds);

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
		//TODO: update inverted state checking logic to be more elegant
		if (Input.IsActionPressed("right"))
		{
			if (current == States.Inverted)
			{
				RotateObjectLocal(new Vector3(0, 1, 0), (float)rotationIncrement);
			}
			else RotateObjectLocal(new Vector3(0, 1, 0), -(float)rotationIncrement);
			//GD.Print("Pressed D right");
		}
		else if (Input.IsActionPressed("left"))
		{
			if (current == States.Inverted)
			{
				RotateObjectLocal(new Vector3(0, 1, 0), -(float)rotationIncrement);
			}
			else RotateObjectLocal(new Vector3(0, 1, 0), (float)rotationIncrement);
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
		//TODO: currently doesn't quite work
		if (!IsOnFloor()) // If in the air, fall towards the floor. Literally gravity
		{
			Position -= GetTransform().Basis.Y * (float)(delta * fallAcceleration);
			//GD.Print("I'm falling!");
		}

		if (timer.ElapsedMilliseconds > 0)
		{
			RevertState(current, speed, delta);
		}

		// Moving the character
		Position += GetTransform().Basis.Z * (float)(delta * UpdateStateSpeed(current, speed)) * -1;

		MoveAndSlide();
		GD.Print("Player speed: " + speed);
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
	/// <returns></returns>
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
		if (current != States.Regular && timer.ElapsedMilliseconds <= 0) OnItemCollision?.Invoke();

		return speed;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="prevState"></param>
	private void RevertState(States prevState, double speed, double delta)
	{
		//according to what state was previously, the process to revert back to States.Regular will differ slightly
		switch (prevState)
		{
			//Fast -> Regular
			//slowly decelerate to the normal max speed (over the course of 5 seconds, whatever feels good)
			//change state to Regular
			case States.Fast:
				if(timer.ElapsedMilliseconds <= 5000) speed -= acceleration * delta;
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
	/// 
	/// </summary>
	private void StartTimer()
	{
		timer.Start();
		OnItemCollision -= StartTimer;
	}

	/// <summary>
	/// 
	/// </summary>
	private void ClearTimer()
	{
		timer.Reset();
		OnItemCollision += StartTimer;
	}
}
