extends Node

@export var checkpoints : Array[Vector3]
@export var innerPath : Path3D
@export var startingPoint : Vector3

@export var filePath : String

@export var scale : float

func Init():
	var scene = load(filePath).instantiate()
	add_child(scene)
	print(get_children())
	$Track.scale *= scale
	innerPath = $"Track/Final Path"
	startingPoint = innerPath.curve.get_point_position(0)
	generate_checkpoints()

func generate_checkpoints():
	var c = innerPath.curve
	for i in c.point_count:
		if i % 2 == 0:
			checkpoints.append(c.get_point_position(i) * scale)
	print(checkpoints)
