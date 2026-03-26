"""
ar_stream.py - Camera + AprilTag stream to Godot
  TCP 6003: JPEG frames (4-byte length prefix)
  UDP 6002: AprilTag JSON

Usage:
  OPENCV_AVFOUNDATION_SKIP_AUTH=1 python3 ar_stream.py --webcam
  python3 ar_stream.py --webcam --headless
  python3 ar_stream.py --rtsp rtsp://192.168.4.124:8554/cam
"""

import socket, json, time, sys, struct, threading, argparse
import cv2
import numpy as np
from pupil_apriltags import Detector

GODOT_IP = "127.0.0.1"
TAG_PORT = 6002
FRAME_PORT = 6003
FRAME_W, FRAME_H = 640, 480
JPEG_QUALITY = 50
CAMERA_PARAMS = [800.0, 800.0, FRAME_W / 2.0, FRAME_H / 2.0]
TAG_SIZE = 0.05

tag_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
tcp_client = None
tcp_lock = threading.Lock()

GATE_DEFS = {
    0: {"name": "START/FINISH", "type": "finish",  "event": "checkpoint"},
    1: {"name": "SPEED BOOST",  "type": "boost",   "event": "speed_boost"},
    2: {"name": "ITEM BOX",     "type": "item",    "event": "mystery_box"},
    3: {"name": "HAZARD",       "type": "hazard",  "event": "banana_trap"},
}


def tcp_server_loop():
    global tcp_client
    srv = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    srv.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    srv.bind(("0.0.0.0", FRAME_PORT))
    srv.listen(1)
    print(f"[TCP] Waiting on port {FRAME_PORT}...")
    while True:
        try:
            client, addr = srv.accept()
            with tcp_lock:
                tcp_client = client
            print(f"[TCP] Connected: {addr}")
        except:
            break


def send_tags(tags):
    tag_list = []
    for t in tags:
        if t.tag_id not in GATE_DEFS: continue
        g = GATE_DEFS[t.tag_id]
        tag_list.append({
            "id": t.tag_id, "name": g["name"], "type": g["type"], "event": g["event"],
            "corners": t.corners.tolist(), "center": t.center.tolist(),
            "pose_R": t.pose_R.tolist(), "pose_t": t.pose_t.flatten().tolist(),
        })
    try:
        data = json.dumps({"type": "ar_tags", "tags": tag_list, "timestamp": time.time()})
        tag_sock.sendto(data.encode(), (GODOT_IP, TAG_PORT))
    except: pass


def send_frame(frame):
    global tcp_client
    with tcp_lock:
        if tcp_client is None: return
    _, buf = cv2.imencode('.jpg', frame, [cv2.IMWRITE_JPEG_QUALITY, JPEG_QUALITY])
    data = buf.tobytes()
    with tcp_lock:
        try:
            tcp_client.sendall(struct.pack(">I", len(data)) + data)
        except:
            print("[TCP] Disconnected")
            tcp_client = None


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--rtsp", type=str)
    parser.add_argument("--webcam", action="store_true")
    parser.add_argument("--headless", action="store_true", help="No preview window")
    args = parser.parse_args()

    source = args.rtsp if args.rtsp else 0
    if args.webcam: source = 0

    detector = Detector(families="tag36h11", nthreads=2, quad_decimate=2.0, refine_edges=True)
    threading.Thread(target=tcp_server_loop, daemon=True).start()

    cap = cv2.VideoCapture(source)
    if not cap.isOpened():
        print("[AR] ERROR: Cannot open camera!")
        sys.exit(1)
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, FRAME_W)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, FRAME_H)

    print(f"[AR] {FRAME_W}x{FRAME_H} q{JPEG_QUALITY} | Tags->UDP:{TAG_PORT} Frames->TCP:{FRAME_PORT}")
    if not args.headless: print("[AR] Press 'q' to quit")

    n, fps, fps_n, fps_t = 0, 0, 0, time.time()

    while True:
        ret, frame = cap.read()
        if not ret: continue

        n += 1; fps_n += 1
        now = time.time()
        if now - fps_t >= 1.0:
            fps = fps_n; fps_n = 0; fps_t = now

        # AprilTag detection
        gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        tags = detector.detect(gray, estimate_tag_pose=True, camera_params=CAMERA_PARAMS, tag_size=TAG_SIZE)
        if tags:
            send_tags(tags)
            if n % 30 == 0:
                names = [GATE_DEFS[t.tag_id]["name"] for t in tags if t.tag_id in GATE_DEFS]
                print(f"[AR] Tags: {', '.join(names)} FPS:{fps}")

        # Send every 3rd frame (~10 FPS to Godot)
        if n % 3 == 0: send_frame(frame)

        # Preview window (skip in headless mode)
        if not args.headless:
            for t in tags:
                cv2.polylines(frame, [np.array(t.corners, dtype=np.int32)], True, (0, 255, 0), 2)
            status = "CONNECTED" if tcp_client else "waiting..."
            cv2.rectangle(frame, (0, 0), (FRAME_W, 30), (0, 0, 0), -1)
            cv2.putText(frame, f"AR | FPS:{fps} | Tags:{len(tags)} | TCP:{status}",
                       (10, 20), cv2.FONT_HERSHEY_SIMPLEX, 0.4, (0, 255, 0), 1)
            cv2.imshow("AR Kart Stream (Q to quit)", frame)
            if cv2.waitKey(1) & 0xFF == ord('q'): break
        else:
            time.sleep(0.001)

    cap.release()
    if not args.headless: cv2.destroyAllWindows()
    print("[AR] Done.")


if __name__ == "__main__":
    main()
