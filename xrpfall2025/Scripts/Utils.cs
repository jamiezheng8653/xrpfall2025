using Godot;
using System;
using Vector3 = Godot.Vector3;
using Plane = Godot.Plane;

/// <summary>
/// Class to store any universal functions that may be used throughout the project
/// </summary>
public static class Utils
{
	/// <summary>
	/// Math which projects vector u onto vector v
	/// v(u*v)/(||v||^2) 
	/// </summary>
	/// <param name="u">What vector are you projecting</param>
	/// <param name="v">What vector are you projecting onto</param>
	/// <returns>Resulting vector after the projection</returns>
	public static Vector3 ProjUOntoV(Vector3 u, Vector3 v)
	{
		//Projection of vector v onto vector u = v(v * u)/||v||^2
		return (v.Dot(u) / Mathf.Pow(v.Length(), 2)) * v;
	}

	/// <summary>
	/// Compares two vector3s and find which one has the larger magnitude
	/// </summary>
	/// <param name="u">Vector3 1</param>
	/// <param name="v">Vector3 2</param>
	/// <returns>Which vector of u and v is larger in magnitude</returns>
	public static Vector3 GreaterMagnitude(Vector3 u, Vector3 v)
	{
		if (u.LengthSquared() < v.LengthSquared()) return v;
		else return u;
	}

	/// <summary>
	/// Finds the greatest Vector3 of a set of Vector3s based on its magnitude
	/// </summary>
	/// <param name="set">Set of Vector3s you are comparing and finding the largest of</param>
	/// <returns>The Vector3 with the greatest magnitude</returns>
	public static Vector3 Max(Vector3[] set)
	{
		Vector3 v = set[0];
		for (int i = 1; i < set.Length; i++)
		{
			v = GreaterMagnitude(v, set[i]);
		}
		return v;
	}

	/// <summary>
	/// Returns the smallest Vector3 of a set of Vector3s based on its magnitude
	/// </summary>
	/// <param name="set">Set of Vector3s you are comparing and finding the smallest of</param>
	/// <returns>The Vector3 with the smallest magnitude in the set</returns>
	public static Vector3 Min(Vector3[] set)
	{
		Vector3 v = set[0];
		for (int i = 1; i < set.Length; i++)
		{
			if (v > GreaterMagnitude(v, set[i])) v = set[i];
		}
		return v;
	}

	/// <summary>
	/// Finds the maximum between three values
	/// </summary>
	/// <param name="a">Value 1</param>
	/// <param name="b">Value 2</param>
	/// <param name="c">Value 3</param>
	/// <returns>Which value is the greatest of a, b, and c</returns>
	public static float Max(float a, float b, float c)
	{
		return Mathf.Max(Mathf.Max(a, b), c);
	}

	/// <summary>
	/// Finds the minimum between three values
	/// </summary>
	/// <param name="a">Value 1</param>
	/// <param name="b">Value 2</param>
	/// <param name="c">Value 3</param>
	/// <returns>Which values is the smallest of a, b, and c</returns>
	public static float Min(float a, float b, float c)
	{
		return Mathf.Min(Mathf.Min(a, b), c);
	}

	/// <summary>
	/// Given a path3d, find the closest point on the path from the given Global Position.
	/// Code from: https://medium.com/@oddlyshapeddog/finding-the-nearest-global-position-on-a-curve-in-godot-4-726d0c23defb
	/// </summary>
	/// <param name="path">The Path3D you want to snap to</param>
	/// <param name="GlobalPosition">Where in world space are you right now</param>
	/// <returns>The point on the path closest to the Global Position in world space</returns>
	public static Vector3 GetClosestAbsolutePosition(Path3D path, Vector3 GlobalPosition)
	{
		Curve3D curve = path.Curve;

		//transform the target position to local space
		Transform3D pathTransform = path.GlobalTransform;
		Vector3 localPoint = GlobalPosition * pathTransform;

		//get the nearest offset on the curve
		float offset = curve.GetClosestOffset(localPoint);

		//get the local position at this offset
		Vector3 curvePoint = curve.SampleBaked(offset, true);

		//transform it back to world space
		curvePoint = pathTransform * curvePoint;

		return curvePoint;
	}

	/// <summary>
	/// Checks for intersection between an AABB and a plane
	/// </summary>
	/// <param name="aabb">And array of vector3s with at least the min and max points of the aabb</param>
	/// <param name="plane">What plane are you testing against the aabb</param>
	/// <returns></returns>
	public static bool AABBPlaneIntersect(Vector3[] aabb, Plane plane)
	{
		//these two lines not necessary with a (center, extents) AABB representation
		Vector3 c = (Max(aabb) + Min(aabb)) * 0.5f;
		Vector3 e = Max(aabb) - c;

		//compute the projection interval radius of b onto L(t) = aabb.c + t * plane.normal
		float r = e[0] * Mathf.Abs(plane.Normal[0]) + e[1] * Mathf.Abs(plane.Normal[1]) + e[2] * Mathf.Abs(plane.Normal[2]);

		//compute distance of box center from plane 
		float s = plane.Normal.Dot(c) - plane.D;

		GD.Print("AABBPlaneIntersect: " + (Mathf.Abs(s) <= r));

		//intersection occurs when distance s falls within [-r, r] interval
		return Mathf.Abs(s) <= r;
	}

	/// <summary>
	/// Implementation of the Seperating Axis Theorem (SAT) against a triangle and aabb
	/// Checks 8 planes
	/// </summary>
	/// <returns>If the two passed in shapes overlap in any way</returns>
	public static bool TriangleAABBSAT(Vector3[] triangle, Vector3[] box)
	{
		GD.Print("TRIAABBSAT Triangle: " + triangle[0] + triangle[1] + triangle[2] + " box: " + box[0] + box[1]);
		if (triangle.Length <= 0 || box.Length <= 0) return false; //no triangle nor aabb exists
		float p0, p1, p2, r;

		//compute the box center and extends (if not already given in that format)
		Vector3 c = (Min(box) + Max(box)) * 0.5f;
		float e0 = (Max(box).X - Min(box).X) * 0.5f;
		float e1 = (Max(box).Y - Min(box).Y) * 0.5f;
		float e2 = (Max(box).Z - Min(box).Z) * 0.5f;

		//translate triangle as conceptually moving AABB to origin
		triangle[0] = triangle[0] - c;
		triangle[1] = triangle[1] - c;
		triangle[2] = triangle[2] - c;

		//compute edge vectors for triangle
		Vector3[] edges = [
			triangle[1] - triangle[0],
			triangle[2] - triangle[1],
			triangle[0] - triangle[2]
			];

		Vector3 aij;

		//TODO: Make these three loops DRY
		//test axes a00...a22 (category 3)
		for (int i = 0; i < 3; i++)
		{
			aij = new Vector3(0, -edges[i].Z, edges[i].Y);
			p0 = triangle[0].Dot(aij);
			p1 = triangle[1].Dot(aij);
			p2 = triangle[2].Dot(aij);
			r = e1 * Mathf.Abs(edges[i].Z) + e2 * Mathf.Abs(edges[i].Y);
			if (Mathf.Max(-Max(p0, p1, p2), Min(p0, p1, p2)) > r) return false; //axis is a seperating axis
		}

		for (int i = 0; i < 3; i++){
			aij = new Vector3 (edges[i].Z, 0, -edges[i].X);
			p0 = triangle[0].Dot(aij);
			p1 = triangle[1].Dot(aij);
			p2 = triangle[2].Dot(aij);
			r = e0 + Mathf.Abs(edges[i].Z) + e2 * Mathf.Abs(edges[i].X);
			if (Mathf.Max(-Max(p0, p1,p2), Min(p0, p1,p2)) > r) return false; //axis is a seperating axis
		}

		for (int i = 0; i < 3; i++){
			aij = new Vector3(-edges[i].Y, edges[i].Z, 0);
			p0 = triangle[0].Dot(aij);
			p1 = triangle[1].Dot(aij);
			p2 = triangle[2].Dot(aij);
			r = e0 * Mathf.Abs(edges[i].Y) + e1 * Mathf.Abs(edges[i].X);
			if (Mathf.Max(-Max(p0, p1,p2), Min(p0, p1,p2)) > r) return false; //axis is a seperating axis
		}
		
		//test the three axes corresponding to the face normals of AABB b (category 1)
		//exit if...
		//... [-e0, e0] and [min(triangle[0].X, t1.X, t2.X), max(t0.X, t2.X, t2.X)] do not overlap
		if (Max(triangle[0].X, triangle[1].X, triangle[2].X) < -e0
			|| Min(triangle[0].X, triangle[1].X, triangle[2].X) > e0) return false;
		//... [-e1, e1] and [min(t0.Y, t1.Y, t2.Y), max(t0.Y, t1.Y, t2.Y)] do not overlap
		if (Max(triangle[0].Y, triangle[1].Y, triangle[2].Y) < -e1
			|| Min(triangle[0].Y, triangle[1].Y, triangle[2].Y) > e1) return false;
		//... [-e2, e2] and [min(t0.Z, t1.Z, t2.Z), max(t0.Z, t1.Z, t2.Z)] do not overlap
		if (Max(triangle[0].Z, triangle[1].Z, triangle[2].Z) < -e2
			|| Min(triangle[0].Z, triangle[1].Z, triangle[2].Z) > e2) return false;


		//test seperating axis corresponding to triangle face normal (category 2)
		//param 1: normal
		Plane p = new Plane(edges[0].Cross(edges[1]));
		//distance from orgin
		p.D = p.Normal.Dot(triangle[0]);
		return AABBPlaneIntersect(box, p);

	}
}
