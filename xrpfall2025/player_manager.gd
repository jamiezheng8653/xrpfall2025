extends Node

#@export
var player_count : int = 0

var player_scene = preload("res://Scenes/Prefabs/test_player.tscn")

func _ready() -> void:
	player_count = len(Input.get_connected_joypads())
	#player_count = 4
	for joy in Input.get_connected_joypads():
		print(Input.get_joy_info(joy))
	add_all_controls()
	create_players()

# bulk creation of all new events for each player
# simply maps for input devices 0-[player_count]
func add_all_controls() -> void:
	var actionList = InputMap.get_actions()
	for p in range(player_count):
		for action in actionList:
			if action.begins_with("ui_"):
				continue
			var newAction = action + "_player%s" % p
			var correspondingEvents = InputMap.action_get_events(action)
			for e in correspondingEvents:
				var newEvent = e.duplicate(true)
				newEvent.device = p
				InputMap.add_action(newAction)
				InputMap.action_add_event(newAction, newEvent)
	#print(Input.action)
				
func create_players() -> void:
	for p in range(player_count):
		var new_player = player_scene.instantiate()
		add_child(new_player)
		new_player.name += "_player%" % p
		new_player.init(p,player_count) # place camera
