extends Node3D

@export var placed = false
#@export var snap_point : Node3D
@export var base_rotation : Vector3

func _ready() -> void:
	if !placed:
		pass
	#	disable_points()
	base_rotation = rotation

func _process(delta: float)  -> void:
	pass
	# figure out a rotation calculation

# probably not necessary (makes no checks on placed) but convenient for now
func place():
	placed = true
	enable_points()

func _unplace() -> void:
	placed = false
	disable_points()

func disable_points():
	$"Snap Point 1".process_mode = Node.PROCESS_MODE_DISABLED
	$"Snap Point 2".process_mode = Node.PROCESS_MODE_DISABLED

func enable_points():
	$"Snap Point 1".process_mode = Node.PROCESS_MODE_INHERIT
	$"Snap Point 2".process_mode = Node.PROCESS_MODE_INHERIT
