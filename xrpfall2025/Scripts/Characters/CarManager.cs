using Godot;
using System;
using System.Collections.Generic;

public partial class CarManager : Node
{
	private PackedScene playerPrefab = ResourceLoader.Load<PackedScene>("res://Scenes/Prefabs/player.tscn");
	private PackedScene enemyPrefab = ResourceLoader.Load<PackedScene>("res://Scenes/Prefabs/enemy_ai.tscn");

	private List<Car> cars;
	private int numberOfEnemies = 0;
	private const int TOTALCARS = 4;
	private int numberOfPlayers = 1;

	public void Init(Vector3 startingPosition, Path3D track, int totalCheckpoints)
	{
		cars = new List<Car>();
		numberOfEnemies = TOTALCARS - numberOfPlayers;
		//spawn all enemies
		for(int i = 0; i < numberOfEnemies; i++)
		{
			cars.Add((EnemyAi)enemyPrefab.Instantiate());
			AddChild(cars[^1]);
			cars[^1].Init(startingPosition, track, totalCheckpoints);
		}

		//spawn all players
		for (int i = 0; i < numberOfPlayers; i++)
		{
			cars.Add((Player)playerPrefab.Instantiate());
			AddChild(cars[^1]);
			cars[^1].Init(startingPosition, track, totalCheckpoints);
		}

	}
}
