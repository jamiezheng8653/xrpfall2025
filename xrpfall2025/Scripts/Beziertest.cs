using Godot;
using System;
using System.Collections.Generic;
using Vector3 = Godot.Vector3;

public partial class Beziertest : Node
{
	private float _t = 0f;
	private float t_i = 0.1f;
	private int _i = 0;
	private int numOfPts = 10;
	private float scale = 2f;
	private Vector3 startPoint;
	[Export] private Path3D path3d = null;
	[Export] private Path3D norm = null;
	private List<Vector3> pts = new List<Vector3>();

	public override void _Process(double delta)
	{
		if (pts.Count <= 0) PopulateList();
		if (_i >= pts.Count - 2) return;
		//norm.Curve.Tessellate(2, 5);
		//if (_i + 3 >= pts.Count) return;
		_t += t_i;
		// _t += (float)delta;
		
		//start generating the track loop. 
		//the first one will be inner track, second loop will be the outer track. 
		// Since the mesh generates towards the origin rather than centered on the 
		// bezier curve, we will treat the inner track as the track's midpoint line overall

		//if (path3d.Curve.GetPointPosition(path3d.Curve.PointCount - 1) != pts[_i + 3])
		if (path3d.Curve.GetPointPosition(path3d.Curve.PointCount - 1) != pts[_i + 2])
		{
			// path3d.Curve.AddPoint(CubicBezier(
			// 	pts[_i], pts[_i + 1], pts[_i+2], pts[_i + 3], _t
			// ));

			path3d.Curve.AddPoint(QuadraticBezier(
				pts[_i], pts[_i + 1], pts[_i + 2], _t
			));
		}

		//if (path3d.Curve.GetPointOut(path3d.Curve.PointCount - 1) == pts[_i + 3])
		//if (path3d.Curve.GetPointOut(path3d.Curve.PointCount - 1) == pts[_i + 2])
		if (_t > 1) 
		//else
		{
			GD.Print("_i = ", _i, ", ", pts[_i]);
			_t = 0;
			_i++;
		}

		// float fracI = 0.25f;
		// float fracT = 0;
		// for (int i = 0; i < pts.Count; i++)
		// {
		// 	Path3D temp = new Path3D();
		// 	CsgPolygon3D cptemp = new CsgPolygon3D();
		// 	temp.Curve = new Curve3D();
		// 	//CallDeferred(MethodName.AddChild, temp);
		// 	//CallDeferred(MethodName.AddChild, cptemp);
		// 	AddChild(temp);
		// 	AddChild(cptemp);
		// 	cptemp.Mode = CsgPolygon3D.ModeEnum.Path;
		// 	cptemp.PathNode = temp.GetPath();
		// 	while (fracT < 1)
		// 	{
		// 		if (i == pts.Count - 1)
		// 		{
		// 			temp.Curve.AddPoint(QuadraticBezier(
		// 				pts[_i], pts[0], pts[i], _t
		// 			));
		// 		}
		// 		else
		// 		{
		// 			temp.Curve.AddPoint(QuadraticBezier(
		// 				pts[_i], pts[_i + 1], pts[_i + 2], _t
		// 			));
		// 		}
			
		// 		fracT += fracI;
		// 	}
		// 	fracT = 0;
			
		// }

	}

	private void PopulateList()
	{
		//list of points that will generate track loop
		//checkpoints scattered throughout the track
		Random rng = new Random();
		//Path3D outerPath = new Path3D();

		//change in angle
		double deltaTheta = Mathf.DegToRad(360 / numOfPts);

		//with the origin being the center of generation, we generate numOfPts random lengths. 
		// then calculate the point from there
		for (int i = 0; i < numOfPts; i++)
		{
			double hypotenus = rng.Next(1, 10); //how far from the origin is the point
			double x = hypotenus * Mathf.Cos(deltaTheta * i + 1); //find x coord
			double z = hypotenus * Mathf.Sin(deltaTheta * i + 1); //find z coord
			pts.Add(new Vector3((float)x, 0, (float)z) * scale); //add and scale the point
			norm.Curve.AddPoint(pts[i], null, null, i);
		}
		//get the starting point saved
		startPoint = pts[0];

		//ensure the loop is closed
		path3d.Curve.Closed = true;
		norm.Curve.Closed = true;
	}

	private Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
	{
		Vector3 q0 = p0.Lerp(p1, t);
		Vector3 q1 = p1.Lerp(p2, t);
		Vector3 r = q0.Lerp(q1, t);
		return r;
	}

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
}
