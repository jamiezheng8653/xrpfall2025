using Godot;
using System;

public delegate void OnKillPlaneDelegate(Player p);
public partial class KillPlane : Node
{
	public event OnKillPlaneDelegate IsCollidingKillPlane;
	//private Player playerP = null;
	[Export] private CsgBox3D planeBox = null;
	private Color color;

	public Aabb AABB
	{
		get
		{
			//move the aabb to the actual object's transform
			//otherwise the aabb sits in the origin
			Aabb temp = planeBox.GlobalTransform * planeBox.GetAabb();
			temp.Size = planeBox.GlobalTransform.Basis.Scale;
			return temp;
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		color = new Color("RED");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		foreach (Player p in PlayerList.Instance.List)
        {
			if (IsColliding(p))
			{
				//GD.Print("I'm colliding!");
				//fire event
				IsCollidingKillPlane?.Invoke(p);
			}
        }

		//draw aabb 
		DebugDraw3D.DrawAabb(AABB, color);

	}

	/// <summary>
	/// Initializes the object. Associates any external references that this object needs
	/// </summary>
	/// <param name="player"></param>
	public void Init()
	{
		
	}

	/// <summary>
	/// Checks if the player is colliding with the kill plane
	/// </summary>
	/// <returns>True if the player is colliding with the kill plane</returns>
	private bool IsColliding(Player p)
	{
		// we don't need to adjust the position of the kill plane's aabb 
		// because the kill plane is never going to move while the game is running
		return p.AABB.Intersects(AABB);
	}
}
