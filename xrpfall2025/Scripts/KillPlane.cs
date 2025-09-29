using Godot;
using System;

public delegate bool OnKillPlane();
public partial class KillPlane : Node
{
	private event OnKillPlane isCollidingKillPlane;
	[Export] private Node playerNode = null;
	private Player playerP = null;
	[Export] private CsgBox3D planeBox = null;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		playerP = (Player)playerNode.GetNode<CharacterBody3D>("Node3D/Player");
		isCollidingKillPlane += IsColliding;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (IsColliding())
		{
			//fire event
			isCollidingKillPlane?.Invoke();
		}
	}

	private bool IsColliding()
	{
		return playerP.AABB.Intersects(planeBox.GetAabb());
	}
}
