using Godot;
using System;

public partial class SceneManager : Node
{
	//parts involved in the scene.
	[Export] private Node player;
	[Export] private Node track;
	[Export] private Node itemManager;
	[Export] private Node killPlane;
	[Export] private Node finishline;
	[Export] private Node checkpointManager;

	public override void _Ready()
	{
		//get the correct cooresponding child of each node
		Player playerP = (Player)player;
		Track trackT = (Track)track;
		ItemManager itemManagerIM = (ItemManager)itemManager;
		KillPlane killPlaneKP = (KillPlane)killPlane;
		Finishline finishlineFL = (Finishline)finishline;
		CheckpointManager checkpointManagerCM = (CheckpointManager)checkpointManager;

		//initialize everyone
		killPlaneKP.Init(playerP);
		trackT.Init();
		checkpointManagerCM.Init(playerP, trackT.Checkpoints);
		playerP.Init(trackT.StartingPoint, trackT.Path3D, checkpointManagerCM.TotalCheckpoints);
		finishlineFL.Init(playerP, trackT.StartingPoint); 
		itemManagerIM.Init(playerP, trackT);

		//subscribe Player and Killplane Events
		//killPlaneKP.IsCollidingKillPlane += playerP.ReturnToTrack;
		killPlaneKP.IsCollidingKillPlane += playerP.ToPreviousCheckpoint;

		finishlineFL.OnCrossingEvent += playerP.IncrementLap;
		finishlineFL.OnCrossingEvent += playerP.ClearCheckpoints;
	}

}
