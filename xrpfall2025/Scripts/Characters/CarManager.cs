using Godot;
using System;
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
}
