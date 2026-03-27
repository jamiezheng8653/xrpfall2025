extends Node3D

@export var camera : Camera3D

var pan = false
var pan_start : Vector2

var dragging = false
var drag_object : Node3D = null

var currently_snapped = false

func _input(event: InputEvent) -> void:
	if event is InputEventKey:
		if event.keycode == KEY_F and event.pressed and dragging:
			drag_object.rotation.z += PI # flip over
			# did try and use scale * -1 but it interacted weirdly with rotations
		elif event.keycode == KEY_D and event.pressed and dragging:
			drag_object.queue_free()
			drag_object = null
			dragging = false
		elif event.keycode == KEY_S and event.pressed:
			save_track()
	elif event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_RIGHT:
			#print(event)
			# this might have an issue if right click is held while initializing
			if event.pressed == true and pan == false: # just pressed
				pan = true
				var pos =  mouse_raycast_on_layer(2).get("position", Vector3.ZERO)
				pan_start = Vector2(pos.x, pos.z) 
			elif event.pressed == true and pan == true:
				pass
			else:
				pan = false
		if event.button_index == MOUSE_BUTTON_LEFT:
			if event.pressed == true and dragging == false:
				var result = mouse_raycast_on_layer(1)
				if result == null:
					pass
				var collider = result.get("collider",null)
				if collider == null:
					return
				drag_object = collider.get_parent()
				print(drag_object)
				dragging = true
				drag_object.disable_points()
			elif event.pressed == true and dragging == true:
				pass
			elif event.pressed == false and dragging == true:
				print("just released button")
				dragging = false
				drag_object.enable_points()
			else:
				pass
				#dragging = false
				# eventually should have some kind of snap to gridmap
				# use GridMap.local_to_map()
	elif event is InputEventMouseMotion:
		if pan:
			var target = mouse_raycast_on_layer(2).get("position", Vector3.ZERO)
			var diff = Vector2(target.x, target.z) - pan_start
			camera.position.x -= diff.x
			camera.position.z -= diff.y
		elif dragging:
			var result = mouse_raycast_on_layer(4) # bitmasks baybee
			if result != {} and !currently_snapped: # hovering over a snap point
				#print(result["collider"])
				drag_object.global_position = result["collider"].global_position
				drag_object.global_rotation.y = result["collider"].global_rotation.y# + drag_object.rotation
				currently_snapped = true
			elif result == {} and currently_snapped: # just dragged off a point
				currently_snapped = false
			elif result == {}:
				#currently_snapped = false
				drag_object.position = mouse_raycast_on_layer(2).get("position", Vector3.ZERO)
				#drag_object.rotation = Vector3.ZERO
			else:
				pass

func mouse_raycast_on_layer(layer_mask : int) -> Dictionary:
	var mouse_position = get_viewport().get_mouse_position()
	var origin = camera.project_ray_origin(mouse_position)
	var direction = camera.project_ray_normal(mouse_position)
	var end = origin + direction * 10
	var space_state = get_world_3d().direct_space_state
	var query = PhysicsRayQueryParameters3D.create(origin,end, layer_mask)
	return space_state.intersect_ray(query)

# instantiate given scene, and manually start drag
func spawn_part(part : PackedScene) -> void:
	var spawned = part.instantiate()
	$Track.add_child(spawned)
	var pos = mouse_raycast_on_layer(2).get("position", Vector3.ZERO)
	spawned.position = pos
	drag_object = spawned
	dragging = true
	spawned.disable_points()

func save_track():
	var saveScene = PackedScene.new()
	var track = $Track
	track.owner = track
	for child in track.get_children():
		child.owner = track
	saveScene.pack(track)
	
	var fileDialog = $FileDialog
	fileDialog.popup_centered_clamped()
	
	var path = await fileDialog.file_selected
	
	if !path.ends_with(".tscn"):
		path += ".tscn"
	
	var err = ResourceSaver.save(saveScene, path)
