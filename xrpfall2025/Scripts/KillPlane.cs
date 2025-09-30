using Godot;
using System;

public delegate bool OnKillPlaneDelegate();
public partial class KillPlane : Node
{
	private event OnKillPlaneDelegate IsCollidingKillPlane;
	[Export] private Node playerNode = null;
	private Player playerP = null;
	[Export] private CsgBox3D planeBox = null;
	private Color color;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		color = new Color("RED");
		playerP = (Player)playerNode.GetNode<CharacterBody3D>("Node3D/Player");
		IsCollidingKillPlane += IsColliding;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (IsColliding())
		{
			//fire event
			IsCollidingKillPlane?.Invoke();
		}

		//draw aabb 
		DebugDraw3D.DrawAabb(planeBox.GetAabb(), color);

	}

	private bool IsColliding()
	{
		// we don't need to adjust the position of the kill plane's aabb 
		// because the kill plane is never going to move while the game is running
		return playerP.AABB.Intersects(planeBox.GetAabb());
	}
}
