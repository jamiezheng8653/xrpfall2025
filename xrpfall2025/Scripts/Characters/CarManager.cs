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
		//sort the cars into their proper placements and save this new list
		cars = MergeSort(Cars);

		//adjust the placements of each car
		for (int i = 0; i < cars.Count; i++)
		{
			cars[i].PlacementChanged(i + 1);
		}
		//PlacementTracker();
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
	/// Returns whether car a is infront of car b
	/// </summary>
	private bool CompareCars(Car a, Car b)
	{
		//return the car that is infront of the other car
		//if car a has passed less laps than car b, return b
		if(a.Lap < b.Lap) return false;
		//if car a and b are on the same lap, 
		// then if car a has passed less checkpoints than car b, return b
		else if (a.NumPassedCheckpoints < b.NumPassedCheckpoints) return false;
		//if car a and b are on the same lap and have passed the 
		// same number of checkpoints, then if car a is closer 
		// to the last checkpoint than car b, return b
		else if (a.DistanceFromLastCheckpoint() < b.DistanceFromLastCheckpoint()) return false;
		//otherwise car a is infront of car b
		else return true;
	}

	/// <summary>
	/// Given two sorted list, sort the elements 
	/// sequencially and return a single sorted list
	/// </summary>
	private List<Car> Merge(List<Car> leftList, List<Car> rightList)
	{
		List<Car> sorted = new List<Car>();
		int lI = 0; //left list index
		int rI = 0;	//right list index 

		//index through both lists and add them to each list in sequencial order
		while (lI < leftList.Count && rI < rightList.Count)
		{
			// if the current leftList car is infront of the current 
			// rightList car, then add it to the sorted list and increment lI
			if(CompareCars(leftList[lI], rightList[rI]))
			{
				sorted.Add(leftList[lI]);
				lI++;
			}
			//otherwise add the rightList car to the list and increment rI
			else
			{
				sorted.Add(rightList[rI]);
				rI++;
			}
		}

		//add any remaining cars. Assumes at least either leftList or 
		// rightList cars have been completely added to the sorted list
		while (lI < leftList.Count)
		{
			sorted.Add(leftList[lI]);
			lI++;
		}
		while(rI < rightList.Count)
		{
			sorted.Add(rightList[rI]);
			rI++;
		}

		return sorted;
	}

	/// <summary>
	/// Recursively sorts a given list of 
	/// cars in the order of their placement
	/// </summary>
	/// <param name="carList"></param>
	private List<Car> MergeSort(List<Car> carList)
	{
		//as long as the size of the list is greater than one, then we can keep splitting
		if (1 < carList.Count)
		{
			List<Car> leftList = new List<Car>();
			List<Car> rightList = new List<Car>();
			int lSize, rSize;

			//divide the list recursively into two halves until it can no longer be
			if (carList.Count % 2 == 0) 
			{ 
				lSize = carList.Count/2;
				rSize = carList.Count/2;
			}
			else
			{
				lSize = carList.Count/2 - 1;
				rSize = carList.Count/2;
			}

			//create the left list
			for(int i = 0; i < lSize; i++)
			{
				leftList.Add(carList[i]);
			}
			//create the right list
			for(int i = rSize; i < carList.Count; i++)
			{
				rightList.Add(carList[i]);
			}

			//sort the first and second halves
			MergeSort(leftList);
			MergeSort(rightList);

			//merge the two lists and return its sorted version
			return Merge(leftList, rightList);
		}
		else return null;
		
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
