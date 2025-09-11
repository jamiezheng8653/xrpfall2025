using Godot;
using System;

public partial class MinimapSystem : Node
{
	//top down camera for minimap
	[Export] Camera3D minimapCamera;
	//player instance to make the camera follow
	[Export] Node3D player
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SubViewport subViewport = $SubViewportContainer/SubViewport;
		Camera3D normalCamera = GetViewport().GetCamera3D();
		
		var minimapCameraRid = minimapCamera.GetCameraRid();
		var normalCameraRid = normalCamera.GetCameraRid();
		
		var subViewportRid = subViewport.GetViewportRid();
		var viewportRid = GetViewport().GetViewportRid();
		
		RenderingServer.ViewportAttachCamera(subViewportRid, minimapCameraRid);
		RenderingServer.ViewportAttachCamera(viewportRid, normalCameraRid);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		minimapCamera.GlobalPosition.X = player.GlobalPosition.X;
		minimapCamera.GlobalPosition.Z = player.GlobalPosition.Z;
	}
}
