import os
import sys

# Set conda paths so RTSP/ffmpeg works when launched from Godot
CONDA_PREFIX = "/Users/rishiyennu/anaconda3/envs/apriltag-laptop-env"
os.environ["DYLD_LIBRARY_PATH"] = f"{CONDA_PREFIX}/lib"
os.environ["PATH"] = f"{CONDA_PREFIX}/bin:" + os.environ.get("PATH", "")

"""
laptop_apriltag_detect.py

Pulls RTSP stream directly from Pi (via MediaMTX),
runs AprilTag detection, draws overlays, and sends:
  - Annotated video to Godot via TCP (port 6001)
  - Pose data to Godot via UDP (port 6000)

Pipeline:
  Pi (MediaMTX) --RTSP--> This script --TCP:6001 (video)--> Godot
                                       --UDP:6000 (pose) --> Godot
"""

import socket
import struct
import numpy as np
import cv2
import math
from contextlib import contextmanager
from pupil_apriltags import Detector


# -------- Suppress noisy C-level stderr from AprilTag lib --------
@contextmanager
def suppress_stderr():
    devnull = os.open(os.devnull, os.O_WRONLY)
    orig = os.dup(2)
    try:
        os.dup2(devnull, 2)
        yield
    finally:
        os.dup2(orig, 2)
        os.close(orig)
        os.close(devnull)


# -------- CONFIG --------

# Pi RTSP stream
PI_IP = "192.168.4.124"
RTSP_URL = f"rtsp://{PI_IP}:8554/cam"

# Send to Godot
GODOT_IP = "127.0.0.1"

# Pose data -- UDP (small, fast)
POSE_PORT = 6000
pose_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# Video frames -- TCP (no size limit)
VIDEO_PORT = 6001
video_conn = None

# Camera intrinsics (for 640x480)
FRAME_W = 640.0
FRAME_H = 480.0
fx = 800.0
fy = 800.0
cx = FRAME_W / 2.0
cy = FRAME_H / 2.0
camera_params = [fx, fy, cx, cy]

# AprilTag config
TAG_SIZE = 0.05  # Meters -- measure your actual tag!

detector = Detector(
    families="tag36h11",
    nthreads=2,
    quad_decimate=2.0,
    refine_edges=True
)


def rotation_euler_from_R(R):
    """Compute roll, pitch, yaw from rotation matrix R."""
    yaw = math.atan2(R[0, 2], R[2, 2])
    pitch = -math.asin(R[1, 2])
    roll = math.atan2(R[1, 0], R[1, 1])
    return roll, pitch, yaw


def send_pose(tag_id, pose_t, R):
    """Send pose to Godot via UDP."""
    tx, ty, tz = pose_t.flatten().tolist()
    roll, pitch, yaw = rotation_euler_from_R(R)
    msg = f"{tag_id},{tx:.3f},{ty:.3f},{tz:.3f},{roll:.3f},{pitch:.3f},{yaw:.3f}\n"
    pose_sock.sendto(msg.encode("utf-8"), (GODOT_IP, POSE_PORT))


def send_video_frame(jpg_bytes):
    """Send a JPEG frame to Godot via TCP with a 4-byte length header."""
    global video_conn
    if video_conn is None:
        return
    try:
        header = struct.pack("!I", len(jpg_bytes))
        video_conn.sendall(header + jpg_bytes)
    except (BrokenPipeError, ConnectionResetError, OSError):
        print("[Video] Godot disconnected.")
        video_conn = None


def wait_for_godot_connection(server_sock):
    """Non-blocking check for Godot TCP connection."""
    global video_conn
    try:
        server_sock.settimeout(0.01)
        conn, addr = server_sock.accept()
        # Close old connection if exists
        if video_conn is not None:
            try:
                video_conn.close()
            except:
                pass
        video_conn = conn
        print(f"[Video] Godot connected from {addr}")
    except socket.timeout:
        pass


def main():
    global video_conn

    # ---- TCP server for video to Godot ----
    video_server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    video_server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    video_server.bind(("0.0.0.0", VIDEO_PORT))
    video_server.listen(5)
    print(f"[Video] TCP server listening on port {VIDEO_PORT}")
    print(f"[Video] Waiting for Godot to connect...")

    # ---- Pull RTSP stream directly ----
    print(f"[Camera] Connecting to RTSP stream: {RTSP_URL}")
    cap = cv2.VideoCapture(RTSP_URL)
    if not cap.isOpened():
        print("[Camera] ERROR: Cannot open RTSP stream. Check Pi IP and MediaMTX.")
        return
    print("[Camera] Connected to Pi RTSP stream!")

    frame_count = 0

    while True:
        # Check if Godot has connected (non-blocking)
        wait_for_godot_connection(video_server)

        # Read frame from RTSP
        ret, img = cap.read()
        if not ret:
            print("[Camera] Lost RTSP connection, reconnecting...")
            cap.release()
            cap = cv2.VideoCapture(RTSP_URL)
            continue

        frame_count += 1

        # -------- APRILTAG DETECTION --------
        gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

        with suppress_stderr():
            detections = detector.detect(
                gray,
                estimate_tag_pose=True,
                camera_params=camera_params,
                tag_size=TAG_SIZE,
            )

        # -------- DRAW OVERLAYS + SEND POSE --------
        for d in detections:
            tag_id = d.tag_id
            cx_p, cy_p = d.center
            corners = d.corners.astype(int)

            # Green border around tag
            for i in range(4):
                p1 = tuple(corners[i])
                p2 = tuple(corners[(i + 1) % 4])
                cv2.line(img, p1, p2, (0, 255, 0), 2)

            # Red center dot
            cv2.circle(img, (int(cx_p), int(cy_p)), 6, (0, 0, 255), -1)

            # Tag label
            cv2.putText(
                img, f"Tag {tag_id}",
                (int(cx_p) - 40, int(cy_p) - 20),
                cv2.FONT_HERSHEY_SIMPLEX, 0.8,
                (0, 255, 0), 2, cv2.LINE_AA
            )

            # Distance label
            if d.pose_t is not None:
                tz = d.pose_t[2][0]
                cv2.putText(
                    img, f"{tz:.2f}m",
                    (int(cx_p) - 30, int(cy_p) + 30),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.6,
                    (255, 255, 0), 2, cv2.LINE_AA
                )
                send_pose(tag_id, d.pose_t, d.pose_R)

        # -------- SEND ANNOTATED FRAME TO GODOT (TCP) --------
        ret, encoded = cv2.imencode(
            ".jpg", img, [int(cv2.IMWRITE_JPEG_QUALITY), 75]
        )
        if ret:
            send_video_frame(encoded.tobytes())

        # Status print every 100 frames
        if frame_count % 100 == 0:
            status = "connected" if video_conn else "waiting"
            print(f"[Status] Frame {frame_count}, Godot: {status}, Tags: {len(detections)}")

    cap.release()
    video_server.close()


if __name__ == "__main__":
    main()