using Godot;

public partial class SceneManager : Node
{
	//parts involved in the scene.
	[Export] private Node track;
	[Export] private Node itemManager;
	[Export] private Node killPlane;
	[Export] private Node finishline;
	[Export] private Node checkpointManager;

	public override void _Ready()
	{
		//get the correct cooresponding child of each node
		//Player playerP = (Player)player;
		Track trackT = (Track)track;
		ItemManager itemManagerIM = (ItemManager)itemManager;
		KillPlane killPlaneKP = (KillPlane)killPlane;
		Finishline finishlineFL = (Finishline)finishline;
		CheckpointManager checkpointManagerCM = (CheckpointManager)checkpointManager;

		//initialize everyone
		killPlaneKP.Init();
		trackT.Init();
		checkpointManagerCM.Init(trackT.Checkpoints);
		//spawn all players
		foreach (Player p in PlayerList.Instance.List)
		{
			AddChild(p);
			p.Init(trackT.StartingPoint, trackT.Path3D, checkpointManagerCM.TotalCheckpoints);
			//subscribe Player and Killplane Events
			//killPlaneKP.IsCollidingKillPlane += playerP.ReturnToTrack;
			killPlaneKP.IsCollidingKillPlane += p.ToPreviousCheckpoint;

			finishlineFL.OnCrossingEvent += p.IncrementLap;
			finishlineFL.OnCrossingEvent += p.ClearCheckpoints;
		}
		finishlineFL.Init(trackT.StartingPoint);
		itemManagerIM.Init(trackT);


	}

}
