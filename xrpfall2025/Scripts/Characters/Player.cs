using Godot;
using Vector3 = Godot.Vector3;
using System.IO;
using System.Text.Json;

/// <summary>
/// Blueprint for a player controlled car. Handles inputs
/// </summary>
public partial class Player : Car
{
	/// <summary>
	/// Any additional set up logic here outside of Init and parent field set up
	/// </summary>
	private Vector3 xrpOffset = Vector3.Zero;

	public override void Init(Vector3 startingPosition, Path3D track, int totalCheckpoints)
	{
		base.Init(startingPosition, track, totalCheckpoints);
		// Save where the car spawns — XRP (0,0) maps to this position
		xrpOffset = startingPosition + new Vector3(0, 5, 0);
	}
	
	public override void _Ready()
	{
		base._Ready();
		
		// For customization, not yet applied
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
		// ---- Try XRP control ----
		var receiver = GetNode("/root/NetworkReceiver");
		bool xrpActive = receiver != null && (bool)receiver.Get("xrp_connected");

		if (xrpActive)
		{
			float xrpX = (float)receiver.Get("xrp_x");
			float xrpY = (float)receiver.Get("xrp_y");
			float xrpAngle = (float)receiver.Get("xrp_angle");

			// Map XRP cm to Godot world units — adjust this to match your track
			float scale = 0.5f;

			// XRP x,y (2D floor) → Godot x,z (3D floor)
			float currentY = charbody3d.GlobalPosition.Y;
			Vector3 targetPos = new Vector3(
				xrpOffset.X + xrpX * scale,
				charbody3d.GlobalPosition.Y,
				xrpOffset.Z + (-xrpY * scale)
			);

			// Smooth position
			charbody3d.GlobalPosition = charbody3d.GlobalPosition.Lerp(targetPos, 0.3f);

			// Apply angle: XRP 0° = forward, Godot Y-rotation 0° = -Z
			float targetRotY = Mathf.DegToRad(-xrpAngle);
			charbody3d.Rotation = new Vector3(
				charbody3d.Rotation.X,
				Mathf.LerpAngle(charbody3d.Rotation.Y, targetRotY, 0.3f),
				charbody3d.Rotation.Z
			);

			// Set speed for HUD display
			speed = 10;
			base._PhysicsProcess(delta);
			return;
		}
		else
		{
			// ---- Fallback: keyboard ----
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
				speed *= 0.9 * delta;
			}
			// Process movement (gravity, state speed, move-and-slide)
			base._PhysicsProcess(delta);
		}

		
	}
	
	//Customization file reading, saves the color to be applied to the car
	protected override void ApplySavedCarColor()
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
	protected override void SetCarColor(Color color)
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
	 
}
