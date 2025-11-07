using Godot;

//upon collision from a direction call this event
public delegate void OnCrossing();

public partial class Finishline : Node
{
	public event OnCrossing OnCrossingEvent;
	[Export] private Area3D area3D;
	private bool prevCollisionState = false;
	[Export] private CollisionShape3D collisionShape3D;
	private Vector3 halflength;
	private float radius = 5;
	private Color color;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//this is temporary
		color = new Color("BLUE");
		halflength = new Vector3(radius * 2, 1, radius);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//check for when overlap and the direction of the velocity vector of the player points correctly
		//this will have to be revamped when enemy ai cars get implemented
		foreach (Player p in PlayerList.Instance.List)
        {
            if (IsOverlapping(p) && HitAllCheckpoints(p)) OnCrossingEvent?.Invoke(); //call event
        }
		DebugDraw3D.DrawBox(area3D.GlobalPosition, Quaternion.Identity, 2 * halflength, color, true);
	}

	/// <summary>
	/// Initialize the external fields of the finshline.
	/// </summary>
	/// <param name="p">Reference the player in scene</param>
	/// <param name="startPt">Where the track first begins generating, where the finish line will be placed</param>
	/// <param name="scale">How big is the track, and therefore how wide do we need the track</param>
	public void Init(Vector3 startPt, double scale = 1)
	{
		area3D.Position = startPt + new Vector3(0, 1.5f, 0);

		//ensure the finishline takes up the width of the road
		// set the "forward" direction vector to match the flow of the track

	}

	/// <summary>
	/// Checks if the player is crossing the finish line with an aabb check
	/// </summary>
	/// <returns>If the player is going over the finishline, return true</returns>
	private bool IsOverlapping(Player p)
	{
		if (Utils.AABBCollision(p.GlobalPosition, p.Halflength, area3D.GlobalPosition, halflength))
		{
			GD.Print("is overlapping finishline true");
			return true;
		}
		else return false;
	}
	
	/// <summary>
	/// Checks if the inputted player has passed all the checkpoints 
	/// on the track to count as a lap upon passing the finish line
	/// </summary>
	/// <param name="p">The player we want to check for checkpoints passed</param>
	/// <returns>True if the player has all checkpoint references in their passed list</returns>
	private bool HitAllCheckpoints(Player p)
	{
		//check if the player has crossed all checkpoints
		if (p.CheckCheckpoints())
		{
			GD.Print("Checking checkpoints true");
			return true;
		}
		else
		{
			GD.Print("Checking checkpoints false");
			return false;
		}
	}
}
