extends Control

# Connecting player node with player script
# There are possibly better solutions for this connection but it does work for now
@onready var player_node = get_node("../../Player/Node3D/Player")

# Speed bar variables
var thresholds = [25.0, 20.0, 15.0, 10.0, 5.0, 0.1]

@onready var bars = [
	$SpeedBar/SpeedOne,
	$SpeedBar/SpeedTwo,
	$SpeedBar/SpeedThree,
	$SpeedBar/SpeedFour,
	$SpeedBar/SpeedFive,
	$SpeedBar/SpeedSix,
]

# Countdown variables
@onready var countdown_label: Label = $CountdownLabel  
var counter: int = 3
var countdown_active: bool = false
signal countdown_finished

var use_tween := false # Toggle tween if wanted

# Item Bar variables
@onready var item_icon: TextureRect = $ItemWindow/Item1

var item_textures = {
	"Fast": preload("res://Assets/Images/FastPlaceholder.png"),
	"Slow": preload("res://Assets/Images/SlowPlaceholder.png"),
	"Inverted": preload("res://Assets/Images/InvertedPlaceholder.png")
	# I need to add images for the other states
}

var last_item_type: String = ""

# Pause menu
@onready var pause_menu: Control = $PauseMenu

# On game start
func _ready():
	# When I want it to start at the beginning of the race instead I would add
	# $HUD.start_countdown() to scene manager script instead
	start_countdown() 
	
	#create a default texture for item
	if not item_icon.texture:
		item_icon.texture = ImageTexture.new()  # empty texture
	item_icon.visible = false
	
	# Make sure Pause Menu isn't visible
	pause_menu.visible = false

# Constantly updating
func _process(_delta: float) -> void:
	if player_node:
		# Set speed to absolute value
		var current_speed = abs(player_node.Speed)
		
		# Displaying live data taken from Player and updating text
		$TelemetryWindow/State.text = " State: " + str(state_name(player_node.Current))
		$TelemetryWindow/Position.text = " Position: (%.2f, %.2f)" % [player_node.CurrentPosition.x, player_node.CurrentPosition.z]
		$TelemetryWindow/Speed.text = " Speed: %.2f" % current_speed
		$PlaceText.text = get_place_suffix(player_node.Place) + " Place"
		$LapText.text = "Lap \n       " + str(player_node.Lap) + "/3"
		
		# Speed bar. Toggles transparency of each bar
		for i in bars.size():
			if current_speed > thresholds[i]:
				bars[i].modulate.a = 1.0
			else:
				bars[i].modulate.a = 0.0
				
		# Item Bar. Constantly checks if the state has changed
		var current_state_name = state_name(player_node.Current) 
		if current_state_name != last_item_type:
			update_item_icon(current_state_name)
			last_item_type = current_state_name
		
# Helper functions:

#return the cooresponding name of the State enum because to_string() is being silly
func state_name(current: int) -> String:
	match current:
		0:
			return "Inverted"
		1:
			return "Slow"
		2:
			return "Fast"
		3:
			return "Regular"	
		_:
			return "Other"
			
		
# Function to add the right suffix to the place number
func get_place_suffix(place_number: int) -> String:
	match place_number:
		1:
			return str(place_number) + "st"
		2:
			return str(place_number) + "nd"
		3:
			return str(place_number) + "rd"
		_:
			return str(place_number) + "th"
			
# Function to set up/start countdown
func start_countdown():
	counter = 3
	countdown_label.visible = true
	countdown_active = true
	
	# I need to condense this better if I like it like that
	$LapText.visible = false
	$PlaceText.visible = false
	$SpeedBar.visible = false
	$TelemetryWindow.visible = false
	
	_show_number()
	
# Function that displays the number for countdow until it reaches 0, then go
func _show_number():
	if counter > 0:
		countdown_label.text = str(counter)
		countdown_label.scale = Vector2(0.5, 0.5)
		_animate_label()
	else:
		_show_go()

# Use if/else to control tween because I don't know if I like it.
# Toggleable T/F in variables
func _animate_label():
	if use_tween:	
		var tween := create_tween()
		countdown_label.modulate = Color(1, 1, 1, 0)
		tween.parallel().tween_property(countdown_label, "modulate:a", 1.0, 0.3)
		tween.tween_property(countdown_label, "scale", Vector2(1.5, 1.5), 0.8).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		tween.tween_callback(Callable(self, "_next_step"))
	else:
		countdown_label.modulate = Color(1, 1, 1, 1)
		countdown_label.scale = Vector2.ONE
		await get_tree().create_timer(1.0).timeout
		_next_step()
		
# Decrements counter 
func _next_step(): 
	counter -= 1
	if counter > 0:
		_show_number()
	else:
		_show_go()

# Actually displays "Go" and calls to end the countdown
func _show_go():
	countdown_label.text = "Go!"
	if use_tween:
		countdown_label.scale = Vector2(0.5, 0.5)
		var tween := create_tween()
		tween.tween_property(countdown_label, "scale", Vector2(1.5, 1.5), 0.8).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		tween.tween_callback(Callable(self, "_end_countdown"))
	else:
		await get_tree().create_timer(1.0).timeout
		_end_countdown()

# Ends the countdown and returns everything to its normal states.
# Sends out signal that can be used for Player class to indicate car can move again.
func _end_countdown():
	await get_tree().create_timer(0.5).timeout
	countdown_label.visible = false
	countdown_active = false
	
	# Again could be done better if I want to keep this
	$LapText.visible = true
	$PlaceText.visible = true
	$SpeedBar.visible = true
	
	
	emit_signal("countdown_finished")

# Item Bar function

func update_item_icon(item_type: String) -> void:
	if item_type in item_textures:
		item_icon.texture = item_textures[item_type]
		item_icon.visible = true
	else:
		item_icon.visible = false  # Hide if no item
		
# Pause Menu

func _input(event):
	if event.is_action_pressed("ui_cancel"):  # Default Escape key
		if get_tree().paused:
			_resume_game()
		else:
			_pause_game()

func _pause_game():
	get_tree().paused = true
	pause_menu.visible = true

# Resume the game
func _resume_game():
	get_tree().paused = false
	pause_menu.visible = false
	

func _on_resume_button_pressed() -> void:
	_resume_game()
	pass # Replace with function body.


func _on_home_pressed() -> void:
	get_tree().paused = false
	get_tree().change_scene_to_file("res://Scenes/GameplayScenes/title_screen.tscn")
	pass # Replace with function body.
