using Godot;
using System.Collections.Generic;

public partial class CarManager : Node
{
	private PackedScene playerPrefab = ResourceLoader.Load<PackedScene>("res://Scenes/Prefabs/player.tscn");
	private PackedScene enemyPrefab = ResourceLoader.Load<PackedScene>("res://Scenes/Prefabs/enemy_ai.tscn");

	private List<Car> cars = new List<Car>();
	private int numberOfEnemies = 0;
	private const int TOTALCARS = 2;
	private int numberOfPlayers = 1;

	/// <summary>
	/// Get a reference to the list of cars in the game
	/// </summary>
	public List<Car> Cars
	{
		get {return cars;}
	}

	/// <summary>
	/// How many players are in the race
	/// </summary>
	public int TotalPlayers
	{
		get {return numberOfPlayers;}
	}

    public override void _Process(double delta)
    {
        PlacementTracker();
    }

	/// <summary>
	/// Initializes all cars for the new track
	/// </summary>
	/// <param name="startingPosition">Where will the cars be spawning</param>
	/// <param name="track">Reference to the path underlying the current track</param>
	/// <param name="totalCheckpoints">How many checkpoints exist on the current track</param>
	public void Init(Vector3 startingPosition, Path3D track, int totalCheckpoints)
	{
		numberOfEnemies = TOTALCARS - numberOfPlayers;
		Vector3 spawnDisplacement = new Vector3(4, 2, -4);
		//spawn all enemies
		for(int i = 0; i < numberOfEnemies; i++)
		{
			cars.Add((Car)enemyPrefab.Instantiate());
			AddChild(cars[^1]);
			((EnemyAi)cars[^1]).Init(startingPosition += spawnDisplacement, track, totalCheckpoints);
		}

		//spawn all players
		for (int i = 0; i < numberOfPlayers; i++)
		{
			cars.Add((Car)playerPrefab.Instantiate());
			AddChild(cars[^1]);
			cars[^1].Init(startingPosition += spawnDisplacement, track, totalCheckpoints);
		}

	}

	/// <summary>
	/// Calculates and sorts the placement of all cars
	/// Admitted the current implementation can be optimized significantly.
	/// But for now its performance is good enough for our intents and purposes.
	/// 
	/// </summary>
	private void PlacementTracker()
	{
		// LinkedList<Car> ordered = new LinkedList<Car>();
		// ordered.AddFirst(cars[0]);
		// LinkedListNode<Car> current = ordered.First;
		// //order the cars in terms of placement
		// for(int i = 0; i < TOTALCARS; i++)
		// {
		// 	for (int j = i + 1; j < TOTALCARS; j++)
		// 	{
		// 		//first check if two cars are on different laps
		// 		if (current.Value.Lap < cars[j].Lap)
		// 		{
		// 			current.
		// 			ordered.AddFirst(cars[j]);
		// 			current = cars[j];
		// 			continue;
		// 		}
		// 		//if two cars are on the same lap, check who has more checkpoints
		// 		else if (current.NumPassedCheckpoints < cars[i].Lap)
		// 		{
		// 			ordered.AddFirst(cars[j]);
		// 			current = cars[j];
		// 			continue;
		// 		}
		// 		//if two cars have the same amount of checkpoints, calculate who is farther from the last checkpoint passed
		// 		else if(current.DistanceFromLastCheckpoint() < cars[i].DistanceFromLastCheckpoint())
		// 		{
		// 			ordered.AddFirst(cars[j]);
		// 			current = cars[j];
		// 			continue;
		// 		}
		// 		else
		// 		{
		// 			ordered.
		// 		}
		// 	}


		// }


		// //modify placement values for each car
		// for(int i = 1; i <= TOTALCARS; i++)
		// {
		// 	now.Value.PlacementChanged(i);
		// 	if(i + 1 > TOTALCARS) return;
		// 	now = now.Next;
		// }

	}
}
