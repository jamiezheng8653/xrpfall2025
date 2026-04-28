extends Node

# ============================================================
# Architecture (Python fusion node refactor):
#
#   Godot launches the bundled laptop_fusion executable on startup.
#   Python (laptop_fusion) sends to us:
#     - UDP 6000: fused robot pose JSON {x, y, angle}
#     - TCP 6001: raw RGB video frames
#
#   We send to Python:
#     - UDP 4003 (localhost): gamepad packets, Python forwards to XRP
#
#   GAMEPAD PROTOCOL: trimmed to just the two axes the XRP uses.
#   Reducing packet size + rate dramatically eases WiFi congestion
#   on the Pi hotspot, which previously caused both video FPS and
#   XRP sensor rate to collapse when gamepad traffic was active.
#     Old: 38 bytes @ 50Hz  = ~1.9 KB/s
#     New:  6 bytes @ 20Hz  = ~0.12 KB/s  (~16x less traffic)
#
#   Variable names like `xrp_x`, `xrp_connected`, `xrp_trail` are kept
#   for backwards-compat with existing game scripts (Player.cs).
# ============================================================

# Pi RTSP IP — passed as argv to the fusion binary
const PI_IP: String = "10.42.0.1"

# Localhost ports — Python is the broker for everything off-laptop
const POSE_PORT: int = 6000
const VIDEO_PORT: int = 6001
const GAMEPAD_RELAY_PORT: int = 4003
const GAMEPAD_RELAY_HOST: String = "127.0.0.1"

# UDP for fused robot pose (from Python)
var udp_pose_in: PacketPeerUDP = PacketPeerUDP.new()
var pose_last_received: float = 0.0
const POSE_TIMEOUT: float = 8.0

# UDP for gamepad commands (to Python relay)
var udp_gamepad: PacketPeerUDP = PacketPeerUDP.new()
var gamepad_connected: bool = false
const GAMEPAD_SEND_HZ: float = 20.0     # was 50; lower to ease WiFi congestion
var gamepad_send_timer: float = 0.0
const GAMEPAD_HEADER: int = 0x55
const DEADZONE: float = 0.08

# TCP for video — raw RGB protocol: [u32 width][u32 height][u32 length][rgb bytes]
var tcp_client: StreamPeerTCP = StreamPeerTCP.new()
var tcp_connected: bool = false
var tcp_buffer: PackedByteArray = PackedByteArray()
var tcp_retry_timer: float = 0.0

# Fusion process
var fusion_pid: int = -1

# Latest video frame
var video_texture: ImageTexture = null
var _video_confirmed: bool = false

# Robot state — fused EKF output from Python (encoder + IMU + camera).
var xrp_x: float = 0.0
var xrp_y: float = 0.0
var xrp_angle: float = 0.0
var xrp_connected: bool = false
var xrp_trail: Array[Vector2] = []
const TRAIL_MAX: int = 300

# Debug
var debug_timer: float = 0.0
var _frames_dropped_this_period: int = 0
var _frames_shown_this_period: int = 0


func _get_base_dir() -> String:
	if OS.has_feature("editor"):
		return ProjectSettings.globalize_path("res://")
	else:
		return OS.get_executable_path().get_base_dir()


func _get_fusion_path() -> String:
	var base = _get_base_dir()
	if OS.get_name() == "Windows":
		return base.path_join("laptop_fusion.exe")
	else:
		return base.path_join("laptop_fusion")


func _ready() -> void:
	var err_pose = udp_pose_in.bind(POSE_PORT, "0.0.0.0")
	if err_pose != OK:
		push_error("[NetworkReceiver] Failed to bind pose port %d (error %d)" % [POSE_PORT, err_pose])
	else:
		print("[NetworkReceiver] Listening for fused pose on UDP %d" % POSE_PORT)

	udp_gamepad.set_dest_address(GAMEPAD_RELAY_HOST, GAMEPAD_RELAY_PORT)
	print("[NetworkReceiver] Gamepad relayed via %s:%d at %d Hz" % [
		GAMEPAD_RELAY_HOST, GAMEPAD_RELAY_PORT, int(GAMEPAD_SEND_HZ)
	])

	Input.joy_connection_changed.connect(_on_joy_connection_changed)
	var pads = Input.get_connected_joypads()
	if not pads.is_empty():
		print("[NetworkReceiver] Controller already connected: %s" % Input.get_joy_name(0))

	_start_fusion()
	await get_tree().create_timer(4.0).timeout
	_connect_video_tcp()


func _process(_delta: float) -> void:
	_process_tcp_status()
	_process_video()
	_process_pose()
	_process_gamepad_send(_delta)

	if not tcp_connected:
		tcp_retry_timer += _delta
		if tcp_retry_timer > 3.0:
			tcp_retry_timer = 0.0
			tcp_client.disconnect_from_host()
			tcp_client = StreamPeerTCP.new()
			var err = tcp_client.connect_to_host("127.0.0.1", VIDEO_PORT)
			if err != OK:
				print("[NetworkReceiver] TCP connect attempt failed (error %d)" % err)
			else:
				print("[NetworkReceiver] Retrying TCP connection to %d..." % VIDEO_PORT)

	debug_timer += _delta
	if debug_timer > 5.0:
		debug_timer = 0.0
		var tcp_status = tcp_client.get_status()
		var tcp_status_name = "NONE"
		match tcp_status:
			StreamPeerTCP.STATUS_NONE: tcp_status_name = "NONE"
			StreamPeerTCP.STATUS_CONNECTING: tcp_status_name = "CONNECTING"
			StreamPeerTCP.STATUS_CONNECTED: tcp_status_name = "CONNECTED"
			StreamPeerTCP.STATUS_ERROR: tcp_status_name = "ERROR"
		print("[NetworkReceiver] Status - TCP: %s | Pose: %s | Gamepad: %s | Video: %s | Frames shown/dropped: %d/%d" % [
			tcp_status_name,
			"active" if xrp_connected else "waiting",
			"connected" if gamepad_connected else "none",
			"yes" if _video_confirmed else "no",
			_frames_shown_this_period,
			_frames_dropped_this_period,
		])
		_frames_shown_this_period = 0
		_frames_dropped_this_period = 0


# -------- Fusion process launcher --------

func _start_fusion() -> void:
	_kill_old_fusion()
	await get_tree().create_timer(1.0).timeout
	var fusion_path = _get_fusion_path()
	if not FileAccess.file_exists(fusion_path):
		push_error("[NetworkReceiver] Fusion binary not found: %s" % fusion_path)
		return
	if OS.get_name() == "macOS":
		OS.execute("xattr", ["-rd", "com.apple.quarantine", fusion_path])
		OS.execute("chmod", ["+x", fusion_path])
	fusion_pid = OS.create_process(fusion_path, [PI_IP])
	if fusion_pid > 0:
		print("[NetworkReceiver] Fusion started (PID: %d)" % fusion_pid)
	else:
		push_error("[NetworkReceiver] Failed to start fusion binary")


func _kill_old_fusion() -> void:
	if OS.get_name() == "Windows":
		OS.execute("taskkill", ["/F", "/IM", "laptop_fusion.exe"])
	else:
		OS.execute("pkill", ["-f", "laptop_fusion"])


func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_CLOSE_REQUEST:
		_cleanup()
		get_tree().quit()


func _cleanup() -> void:
	if fusion_pid > 0:
		OS.kill(fusion_pid)
		print("[NetworkReceiver] Fusion stopped (PID: %d)" % fusion_pid)
		fusion_pid = -1
	_kill_old_fusion()
	tcp_client.disconnect_from_host()
	tcp_connected = false
	udp_pose_in.close()
	udp_gamepad.close()


# -------- TCP video --------

func _connect_video_tcp() -> void:
	tcp_client.disconnect_from_host()
	tcp_client = StreamPeerTCP.new()
	var err = tcp_client.connect_to_host("127.0.0.1", VIDEO_PORT)
	if err != OK:
		push_error("[NetworkReceiver] Failed to initiate TCP connection (error %d)" % err)
	else:
		print("[NetworkReceiver] Connecting to video TCP %d..." % VIDEO_PORT)


func _process_tcp_status() -> void:
	tcp_client.poll()
	var status = tcp_client.get_status()
	if status == StreamPeerTCP.STATUS_CONNECTED and not tcp_connected:
		tcp_connected = true
		tcp_buffer.clear()
		tcp_client.set_no_delay(true)
	elif (status == StreamPeerTCP.STATUS_ERROR or status == StreamPeerTCP.STATUS_NONE) and tcp_connected:
		tcp_connected = false
		tcp_buffer.clear()
		_video_confirmed = false
		video_texture = null
		print("[NetworkReceiver] Video TCP disconnected")


func _read_uint32_be(data: PackedByteArray, offset: int) -> int:
	return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]


func _process_video() -> void:
	if not tcp_connected:
		return

	var available = tcp_client.get_available_bytes()
	if available > 0:
		var result = tcp_client.get_data(available)
		if result[0] == OK:
			tcp_buffer.append_array(result[1])

	if tcp_buffer.size() > 32 * 1024 * 1024:
		print("[NetworkReceiver] TCP buffer overflow, clearing")
		tcp_buffer.clear()
		return

	var latest_w: int = 0
	var latest_h: int = 0
	var latest_data: PackedByteArray = PackedByteArray()
	var frames_in_this_batch: int = 0

	const HEADER_SIZE = 12

	while tcp_buffer.size() >= HEADER_SIZE:
		var w = _read_uint32_be(tcp_buffer, 0)
		var h = _read_uint32_be(tcp_buffer, 4)
		var frame_len = _read_uint32_be(tcp_buffer, 8)

		if w <= 0 or h <= 0 or w > 7680 or h > 4320:
			print("[NetworkReceiver] Bad frame dimensions: %dx%d, clearing buffer" % [w, h])
			tcp_buffer.clear()
			break

		var expected_len = w * h * 3
		if frame_len != expected_len:
			print("[NetworkReceiver] Frame length mismatch: got %d, expected %d, clearing" % [frame_len, expected_len])
			tcp_buffer.clear()
			break

		if tcp_buffer.size() < HEADER_SIZE + frame_len:
			break

		latest_w = w
		latest_h = h
		latest_data = tcp_buffer.slice(HEADER_SIZE, HEADER_SIZE + frame_len)
		tcp_buffer = tcp_buffer.slice(HEADER_SIZE + frame_len)
		frames_in_this_batch += 1

	if frames_in_this_batch == 0:
		return

	if frames_in_this_batch > 1:
		_frames_dropped_this_period += frames_in_this_batch - 1

	var img = Image.create_from_data(latest_w, latest_h, false, Image.FORMAT_RGB8, latest_data)
	if img == null:
		return

	if video_texture == null:
		video_texture = ImageTexture.create_from_image(img)
	else:
		video_texture.update(img)

	_frames_shown_this_period += 1

	if not _video_confirmed:
		_video_confirmed = true
		print("[NetworkReceiver] Video stream active! (%dx%d RGB)" % [latest_w, latest_h])


# -------- Gamepad sending --------
# Trimmed to just throttle (idx 1) and steer (idx 2). The XRP only acts
# on those two indices anyway, and slimming the packet drastically
# reduces WiFi airtime on the Pi hotspot.

func _encode_axis(value: float) -> int:
	return clampi(int((value + 1.0) * 127.5), 0, 255)

func _apply_deadzone(value: float) -> float:
	if absf(value) < DEADZONE:
		return 0.0
	return value

func _process_gamepad_send(delta: float) -> void:
	if Input.get_connected_joypads().is_empty():
		if gamepad_connected:
			gamepad_connected = false
			print("[NetworkReceiver] Gamepad disconnected")
		return

	if not gamepad_connected:
		gamepad_connected = true
		var pad_name = Input.get_joy_name(0)
		print("[NetworkReceiver] Gamepad connected: %s" % pad_name)

	gamepad_send_timer += delta
	if gamepad_send_timer < 1.0 / GAMEPAD_SEND_HZ:
		return
	gamepad_send_timer = 0.0

	# Only the two axes the XRP uses
	var ly = _apply_deadzone(Input.get_joy_axis(0, JOY_AXIS_LEFT_Y))   # throttle
	var rx = _apply_deadzone(Input.get_joy_axis(0, JOY_AXIS_RIGHT_X))  # steer

	# Packet: [0x55][0x04][1][throttle_byte][2][steer_byte] = 6 bytes
	var packet := PackedByteArray()
	packet.append(GAMEPAD_HEADER)
	packet.append(4)                        # n_pairs * 2 = 4 bytes of payload
	packet.append(1); packet.append(_encode_axis(ly))
	packet.append(2); packet.append(_encode_axis(rx))

	udp_gamepad.put_packet(packet)


# -------- UDP fused pose from Python --------

func _process_pose() -> void:
	while udp_pose_in.get_available_packet_count() > 0:
		var pkt = udp_pose_in.get_packet()
		var msg = pkt.get_string_from_utf8().strip_edges()
		var json = JSON.new()
		var err = json.parse(msg)
		if err != OK:
			print("[NetworkReceiver] Pose parse error: ", msg)
			continue
		var data = json.data
		if data is Dictionary:
			if not xrp_connected:
				xrp_connected = true
				print("[NetworkReceiver] Robot pose active!")
			pose_last_received = Time.get_ticks_msec() / 1000.0
			xrp_x = float(data.get("x", 0.0))
			xrp_y = float(data.get("y", 0.0))
			xrp_angle = float(data.get("angle", 0.0))
			xrp_trail.append(Vector2(xrp_x, xrp_y))
			if xrp_trail.size() > TRAIL_MAX:
				xrp_trail.remove_at(0)

	if xrp_connected:
		var now = Time.get_ticks_msec() / 1000.0
		if now - pose_last_received > POSE_TIMEOUT:
			xrp_connected = false
			print("[NetworkReceiver] Pose stream timed out")


func _on_joy_connection_changed(device_id: int, connected: bool) -> void:
	if connected:
		print("[NetworkReceiver] Controller %d connected: %s" % [device_id, Input.get_joy_name(device_id)])
	else:
		print("[NetworkReceiver] Controller %d disconnected" % device_id)
		if device_id == 0:
			gamepad_connected = false
