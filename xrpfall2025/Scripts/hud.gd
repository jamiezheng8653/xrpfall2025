extends Control

# Connecting player node with player script
# There are possibly better solutions for this connection but it does work for now
@onready var player_node = get_node("../../Player/Node3D/Player")

var thresholds = [25.0, 20.0, 15.0, 10.0, 5.0, 0.1]

@onready var bars = [
	$SpeedBar/SpeedOne,
	$SpeedBar/SpeedTwo,
	$SpeedBar/SpeedThree,
	$SpeedBar/SpeedFour,
	$SpeedBar/SpeedFive,
	$SpeedBar/SpeedSix,
]

# Displaying live data taken from Player
func _process(_delta: float) -> void:
	if player_node:
		# Set speed to absolute value
		var current_speed = abs(player_node.Speed)
		
		# Updating text
		$TelemetryWindow/Acceleration.text = " Acceleration: " + str(player_node.Acceleration)
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
		
		
			
		
# FSunction to add the right suffix to the place number
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
