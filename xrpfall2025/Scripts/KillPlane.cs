using Godot;
using System;

public delegate void OnKillPlaneDelegate();
public partial class KillPlane : Node
{
	public event OnKillPlaneDelegate IsCollidingKillPlane;
	private Player playerP = null;
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
		if (IsColliding())
		{
			//GD.Print("I'm colliding!");
			//fire event
			IsCollidingKillPlane?.Invoke();
		}

		//draw aabb 
		DebugDraw3D.DrawAabb(AABB, color);

	}

	public void Init(Player player)
	{
		playerP = player;
	}

	private bool IsColliding()
	{
		// we don't need to adjust the position of the kill plane's aabb 
		// because the kill plane is never going to move while the game is running
		return playerP.AABB.Intersects(AABB);
	}
}
