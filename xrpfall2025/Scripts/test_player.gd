extends Node3D

@export
var speed : int = 1

var player_num : int = -1
@onready
var player_mesh : MeshInstance3D = $SubViewportContainer/SubViewport/MeshInstance3D
@onready
var svc = $SubViewportContainer
@onready
var vp = $SubViewportContainer/SubViewport

var WIDTH = 1920
var HEIGHT = 1080

func _ready() -> void:
	var res = get_tree().root.size
	WIDTH = res.x
	HEIGHT = res.y

func init(p_num,total_player) -> void:
	player_num = p_num
	match total_player:
		1:
			svc.size = Vector2(WIDTH,HEIGHT)
			vp.size = Vector2(WIDTH,HEIGHT)
		2:
			svc.size = Vector2(WIDTH/2,HEIGHT)
			vp.size = Vector2(WIDTH/2,HEIGHT)
		3,4: # 3 and 4 players are both 2x2 grids
			svc.size = Vector2(WIDTH/2,HEIGHT/2)
			vp.size = Vector2(WIDTH/2,HEIGHT/2)
		_:
			print("invalid player count")
	# for going left to right, the top left corners will always be the same
	# only size will change
	match player_num:
		0:
			svc.position = Vector2(0,0)
		1:
			svc.position = Vector2(WIDTH/2,0)
		2:
			svc.position = Vector2(0,HEIGHT/2)
		3:
			svc.position = Vector2(WIDTH/2,HEIGHT/2)
		_:
			print("invalid player num")

	# randomize color for testing
	var mat : StandardMaterial3D = $SubViewportContainer/SubViewport/MeshInstance3D.get_active_material(0).duplicate()
	mat.albedo_color = Color(randf(),randf(),randf())
	$SubViewportContainer/SubViewport/MeshInstance3D.set_surface_override_material(0,mat)
	

func _process(delta) -> void:
	if player_num != -1:
		var vel = Vector3.RIGHT * Input.get_axis("left_player%s" % player_num, "right_player%s" % player_num) * speed * delta
		player_mesh.position += vel
