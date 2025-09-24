//Code from https://docs.godotengine.org/en/4.4/getting_started/first_3d_game/03.player_movement_code.html
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design.Serialization;
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
	//current state
	private States current = States.Regular;
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
	/// Property to give the player's aabb, used for 
	/// collision calculations happening in the collided item
	/// </summary>
	public Aabb AABB
	{
		get { return GetNode<CsgBox3D>("Node3D/Player/Pivot/CSGBox3D").GetAabb(); }
	}

	public override void _Ready()
	{
		color = new Color("CYAN");
	}

	public override void _ExitTree()
	{
	}

	public override void _Process(double delta)
	{
		//draw gizmos
		DebugDraw3D.DrawBox(AABB.Position, Godot.Quaternion.Identity, Vector3.One, color);
	}

	/// <summary>
	/// Called every frame to process how this object will move by what
	/// Will rotate this object left or right, move forwards or backwards locally
	/// and fall if necessary
	/// </summary>
	/// <param name="delta">delta time</param>
	public override void _PhysicsProcess(double delta)
	{
		//GetInput(delta);
		//Adjust left and right steering 
		if (Input.IsActionPressed("right"))
		{
			RotateObjectLocal(new Vector3(0, 1, 0), -(float)rotationIncrement);
			//GD.Print("Pressed D right");
		}
		else if (Input.IsActionPressed("left"))
		{
			RotateObjectLocal(new Vector3(0, 1, 0), (float)rotationIncrement);
			//GD.Print("Pressed A left");
		}

		//TODO: Properly accelerate
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

		//check states and adjust the targetVelocity accordingly
		switch (current)
		{
			//multiply speed
			case States.Fast:
				speed *= fastMultiplier;
				break;
			//divide speed
			case States.Slow:
				speed /= slowMultiplier;
				break;
			//flip flop controls
			case States.Inverted:
				speed *= -1;
				break;
				//regular has no change
				//case States.Regular:
				//break;
		}

		// Moving the character
		Position += GetTransform().Basis.Z * (float)(delta * speed) * -1;
		MoveAndSlide();
		//GD.Print("speed: " + speed);
	}

}
