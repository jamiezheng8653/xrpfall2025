using Godot;

public partial class SceneManager : Node
{
	//parts involved in the scene.
	//[Export] private Node player;
	[Export] private Node carManager;
	[Export] private Node track;
	[Export] private Node itemManager;
	[Export] private Node killPlane;
	[Export] private Node finishline;
	[Export] private Node checkpointManager;

	public override void _Ready()
	{
		//get the correct cooresponding child of each node
		CarManager carManagerCM = (CarManager)carManager;
		Track trackT = (Track)track;
		ItemManager itemManagerIM = (ItemManager)itemManager;
		KillPlane killPlaneKP = (KillPlane)killPlane;
		Finishline finishlineFL = (Finishline)finishline;
		CheckpointManager checkpointManagerCM = (CheckpointManager)checkpointManager;

		//initialize everyone
		killPlaneKP.Init(carManagerCM.Cars);
		trackT.Init();
		checkpointManagerCM.SpawnCheckpoints(trackT.Checkpoints);
		carManagerCM.Init(trackT.StartingPoint, trackT.Path3D, checkpointManagerCM.TotalCheckpoints);
		checkpointManagerCM.Init(carManagerCM.Cars, trackT.Checkpoints);
		finishlineFL.Init(carManagerCM.Cars, trackT.StartingPoint); 
		//itemManagerIM.Init(carManagerCM.Cars, trackT);

		//subscribe Player and Killplane Events
		foreach (Car c in carManagerCM.Cars)
		{
			GD.Print("Car: ", c, ", Type of c: ", c.GetType());
			//killPlaneKP.IsCollidingKillPlane += c.ReturnToTrack;
			killPlaneKP.IsCollidingKillPlane += c.ToPreviousCheckpoint;

			finishlineFL.OnCrossingEvent += c.IncrementLap;
			finishlineFL.OnCrossingEvent += c.ClearCheckpoints;
		}
	}

}
