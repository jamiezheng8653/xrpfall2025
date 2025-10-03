using Godot;
using System;

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

	public static Vector3 SortByMagnitude(Vector3 u, Vector3 v)
	{
		if (u.LengthSquared() < v.LengthSquared()) return v;
		else return u;
	}
}
