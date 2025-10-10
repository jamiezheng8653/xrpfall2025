using Godot;
using System;

public partial class SceneManager : Node
{
	//parts involved in the scene.
	[Export] private Node player;
	[Export] private Node track;
	[Export] private Node itemManager;
	[Export] private Node killPlane;

	public override void _Ready()
	{
		//get the correct cooresponding child of each node
		Player playerP = (Player)player.GetNode<CharacterBody3D>("Node3D/Player");
		Track trackT = (Track)track;
		ItemManager itemManagerIM = (ItemManager)itemManager;
		KillPlane killPlaneKP = (KillPlane)killPlane;

		//initialize everyone
		killPlaneKP.Init(playerP);
		trackT.Init();
		itemManagerIM.Init(playerP, trackT);
		playerP.Init(trackT.StartingPoint, trackT.Path3D);

		//subscribe Player and Killplane Events
		//killPlaneKP.IsCollidingKillPlane += playerP.ReturnToTrack;
		killPlaneKP.IsCollidingKillPlane += playerP.ToPreviousCheckpoint;
	}

}
