using Godot;
using System;
using System.Collections.Generic;
using Vector3 = Godot.Vector3;

/// <summary>
/// Custom track generation. Feed a set of vector3 points and a scale to size the track
/// The first node should ALWAYS be the origin, this will not be checked
/// </summary>
public partial class Track : Node
{
	#region Fields
	[Export] private Path3D innerPath;
	[Export] private PathFollow3D innerFollow;
	[Export] private Path3D outerPath;
	[Export] private PathFollow3D outerFollow;
	[Export] private int scale = 1;
	[Export] private int numOfPts = 0;
	private Vector3 startPoint;
	private List<Vector3> checkPoints = new List<Vector3>();
	private List<Vector3> points = new List<Vector3>();
	#endregion

	#region Properties
	/// <summary>
	/// The Main Path3D underlying the track mesh
	/// </summary>
	public Path3D Path3D
	{
		get { return innerPath; }
	}

	/// <summary>
	/// Reference to a follow path for enemy ai to path along
	/// </summary>
	public PathFollow3D PathFollow3D
	{
		get { return innerFollow; }
	}

	/// <summary>
	/// First point where the track generates. The finish line will spawn here
	/// </summary>
	public Vector3 StartingPoint
	{
		get { return startPoint; }
	}
	
	/// <summary>
	/// List of all checkpoint locations scattered through out the track
	/// </summary>
	public List<Vector3> Checkpoints
	{
		get { return checkPoints; }
	}
    #endregion

    public override void _Process(double delta)
    {
        //innerFollow.Progress += 10 * (float)delta;
    }


	/// <summary>
	/// Initializes the track curve and generates the trash mesh along it
	/// </summary>
	public void Init()
	{
		if (points.Count <= 0)
		{
			
			GeneratePoints(points, numOfPts);
			//get the starting point saved
			startPoint = points[0];
			//ensure the loop is closed
			innerPath.Curve.Closed = true;
			outerPath.Curve.Closed = true;
		} 
		
		//GenerateTrackMeshStraight(Checkpoints, points, outerPath);
		GenerateTrackMeshBezier(checkPoints, points, innerPath);
		GD.Print("Track point count: ", innerPath.Curve.PointCount);
	}

	#region Math + Bezier curve calculations
	/// <summary>
	/// Interpolate point of a quadratic bezier 
	/// </summary>
	/// <param name="p0"></param>
	/// <param name="p1"></param>
	/// <param name="p2"></param>
	/// <param name="t">Weight (must be less than one)</param>
	/// <returns></returns>
	private Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
	{
		Vector3 q0 = p0.Lerp(p1, t);
		Vector3 q1 = p1.Lerp(p2, t);
		Vector3 r = q0.Lerp(q1, t);
		return r;
	}

	/// <summary>
	/// Interpolate point of a cubic bezier
	/// </summary>
	/// <param name="p0"></param>
	/// <param name="p1"></param>
	/// <param name="p2"></param>
	/// <param name="p3"></param>
	/// <param name="t">Weight (must be less than one)</param>
	/// <returns></returns>
	private Vector3 CubicBezier (Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
	{
		Vector3 q0 = p0.Lerp(p1, t);
		Vector3 q1 = p1.Lerp(p2, t);
		Vector3 q2 = p2.Lerp(p3, t);
		Vector3 r0 = q0.Lerp(q1, t);
		Vector3 r1 = q1.Lerp(q1, t);
		Vector3 s = r0.Lerp(r1, t);
		return s;
	}

	/// <summary>
	/// Get the direction of flow at a specific point on the track curve
	/// </summary>
	/// <returns></returns>
	private Transform3D GetTangentOfPoint()
	{
		Transform3D result  = new Transform3D();

		return result;
	}
	#endregion

	#region Related track generation functions
	/// <summary>
	/// Generates a list of points 
	/// </summary>
	/// <param name="pts">List to upload points to</param>
	/// <param name="n">Number of points to be generated</param>
	private void GeneratePoints(List<Vector3> pts, int n)
	{
		//list of points that will generate track loop
		//checkpoints scattered throughout the track
		Random rng = new Random();
		//Path3D outerPath = new Path3D();

		//change in angle
		double deltaTheta = Mathf.DegToRad(360 / n);

		//with the origin being the center of generation, we generate numOfPts random lengths. 
		// then calculate the point from there
		for (int i = 0; i < numOfPts; i++)
		{
			double hypotenus = rng.Next(1, 12); //how far from the origin is the point
			double x = hypotenus * Mathf.Cos(deltaTheta * i + 1); //find x coord
			double z = hypotenus * Mathf.Sin(deltaTheta * i + 1); //find z coord
			pts.Add(new Vector3((float)x, 0, (float)z) * scale); //add and scale the point
		}
	}

	/// <summary>
	/// Generates a track by drawing a straight 
	/// line between each consecutive point
	/// </summary>
	/// <param name="chckpts">A list to record checkpoints to</param>
	/// <param name="pts">A list of pregenerated points</param>
	/// <param name="pathA">Main path</param>
	/// <param name="pathB">Optional second path for a wider track while centering checkpoints</param>
	private void GenerateTrackMeshStraight(List<Vector3> chckpts, List<Vector3> pts, Path3D pathA, Path3D pathB = null)
	{
		//start generating the track loop. 
		//the first one will be inner track, second loop will be the outer track. 
		// Since the mesh generates towards the origin rather than centered on the 
		// bezier curve, we will treat the inner track as the track's midpoint line overall
		for (int i = 0; i < pts.Count; i++) //first; inner
		{
			//GD.Print(pts[i]);
			pathA.Curve.AddPoint(pts[i], null, null, i);
			if(pathB != null) pathB.Curve.AddPoint(pts[i] * (float)1.1, null, null, i);
			if (i % 2 == 0) chckpts.Add(pts[i]);
		}
	}

	/// <summary>
	/// Takes a set of points arranged in a sequencial loop, 
	/// and creates a set of compound bezier curves to form 
	/// a new loop that will be the shape of the track
	/// </summary>
	/// <param name="chckpts">A list to record where checkpoints will be on the track</param>
	/// <param name="pts">Pre generated points</param>
	/// <param name="path">Where the path will be generated on</param>
	public void GenerateTrackMeshBezier(List<Vector3> chckpts, List<Vector3> pts, Path3D pathA)
	{
		//bezier curve calculation weight, sum will have to remain under zero
		float t = 0; 
		float tI = 0.2f;	//increment for t
		//increment counting of which point we are starting at
		int i = 0;
		//track the last point before t resets and i increments that ensures mesh continuity
		Vector3 lastPoint = pts[0];

		//
		while(i < pts.Count - 2)
		{
			//special case for the last point to ensure no index errors
			if(i == pts.Count - 2)
			{
				pathA.Curve.AddPoint(QuadraticBezier(
					lastPoint,
					//pts[i], 
					pts[0], 
					pts[1], 
					t
				));

			}
			else
			{
				pathA.Curve.AddPoint(QuadraticBezier(
					lastPoint,
					//pts[i], 
					pts[i + 1], 
					pts[i + 2], 
					t
				));
			}			

			//increment t
			t += tI;

			//t has to remain under one for the bezier calculations work
			if (t >= 1) 
			{
				//add every other last point to be a checkpoint
				if(i % 2 == 0) chckpts.Add(lastPoint); 
				t = 0;
				i += 2;
				lastPoint = pathA.Curve.GetPointPosition(pathA.Curve.PointCount - 1);
			}
		}
		
	}

	/// <summary>
	/// Public facing method to clear the track upon all cars finishing the race.
	/// </summary>
	public void ResetTrack()
	{
		ResetTrack(points, checkPoints, innerPath);
	}

	/// <summary>
	/// Clears a list storing points, checkpoints, and all track mesh paths
	/// </summary>
	/// <param name="pts">List storing points</param>
	/// <param name="chckpts">List storing checkpoints</param>
	/// <param name="pathA">The main track mesh</param>
	/// <param name="pathB">Optional track mesh generated for a wider track and centered checkpoints</param>
	private void ResetTrack(List<Vector3> pts, List<Vector3> chckpts, Path3D pathA, Path3D pathB = null)
	{
		pts.Clear();
		chckpts.Clear();
		pathA.Curve.ClearPoints();
		if(pathB != null) pathB.Curve.ClearPoints();
	}
	#endregion
	
}
