//Code from https://docs.godotengine.org/en/4.4/getting_started/first_3d_game/03.player_movement_code.html
using Godot;

//Enum States
public enum States
{
	Regular,
	Inverted,
	Slow,
	Fast
};

public partial class Player : CharacterBody3D
{
	//current state
	private States current = States.Regular;
	//if the player gets a speedup or speed debuff, 
	//multiply/divide _targetVelocity by cooresponding multiplier
	//has to be int because Vectors are made up by ints
	[Export]
	private int slowMultiplier = 2;

	[Export]
	private int fastMultiplier = 2;

	private Vector3 _targetVelocity = Vector3.Zero;

	// How fast the player moves in meters per second.
	[Export]
	public int Speed { get; set; } = 14;

	// The downward acceleration when in the air, in meters per second squared.
	[Export]
	public int FallAcceleration { get; set; } = 75;

	[Export]
	//change of state. may update this when we hash out 
	// how we want to track player state upon item collision
	public States Current
	{
		get { return current; }
		set { current = value; }
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 direction = Vector3.Zero;

		if (Input.IsActionPressed("right"))
		{
			direction.X += 1.0f;
			//GD.Print("Pressed D right");
		}
		if (Input.IsActionPressed("left"))
		{
			direction.X -= 1.0f;
			//GD.Print("Pressed A left");
		}
		if (Input.IsActionPressed("back"))
		{
			direction.Z += 1.0f;
			//GD.Print("Pressed S back");
		}
		if (Input.IsActionPressed("forward"))
		{
			direction.Z -= 1.0f;
			//GD.Print("Pressed W forward");
		}

		if (direction != Vector3.Zero)
		{
			direction = direction.Normalized();
			// Setting the basis property will affect the rotation of the node.
			GetNode<Node3D>("Pivot").Basis = Basis.LookingAt(direction);
		}

		// Ground velocity
		_targetVelocity.X = direction.X * Speed;
		_targetVelocity.Z = direction.Z * Speed;

		// Vertical velocity
		if (!IsOnFloor()) // If in the air, fall towards the floor. Literally gravity
		{
			_targetVelocity.Y -= FallAcceleration * (float)delta;
			//GD.Print("I'm falling!");
		}

		//check states
		switch (current)
		{
			//multiply speed
			case States.Fast:
				_targetVelocity.X *= fastMultiplier;
				_targetVelocity.Z *= fastMultiplier;
				break;
			//divide speed
			case States.Slow:
				_targetVelocity.X /= slowMultiplier;
				_targetVelocity.Z /= slowMultiplier;
				break;
			//flip flop controls
			case States.Inverted:
				_targetVelocity.X *= -1;
				_targetVelocity.Z *= -1;
				break;

			//regular has no change
		}

		// Moving the character
		Velocity = _targetVelocity;
		MoveAndSlide();
	}
}
