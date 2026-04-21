"""
ar_stream.py - Camera + Tag detection stream to Godot
  TCP 6003: JPEG frames (4-byte length prefix, 960x540)
  UDP 6002: Tag detection JSON

Detection: SIFT image matching (primary) + ArUco (fallback)

Usage:
  python3 ar_stream.py --crop path/to/tag_photo.png
  OPENCV_AVFOUNDATION_SKIP_AUTH=1 python3 ar_stream.py --webcam
  python3 ar_stream.py --webcam --headless
  python3 ar_stream.py --webcam --aruco-only
"""

import socket, json, time, sys, struct, threading, argparse, os
import cv2
import numpy as np

GODOT_IP = "127.0.0.1"
TAG_PORT = 6002
FRAME_PORT = 6003
JPEG_QUALITY = 80
TAG_SIZE = 0.05
SIFT_MIN_MATCHES = 20
SIFT_RATIO = 0.7
SIFT_MIN_INLIERS = 10
REF_MAX_DIM = 400
SIFT_NFEATURES = 1000
CAM_W, CAM_H = 960, 540
SEND_W, SEND_H = 960, 540

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REF_IMAGE_PATH = os.path.join(SCRIPT_DIR, "tag_ref_0.png")

tag_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
tcp_client = None
tcp_lock = threading.Lock()

GATE_DEFS = {
    0: {"name": "START/FINISH", "type": "finish",  "event": "checkpoint"},
    1: {"name": "SPEED BOOST",  "type": "boost",   "event": "speed_boost"},
    2: {"name": "ITEM BOX",     "type": "item",    "event": "mystery_box"},
    3: {"name": "HAZARD",       "type": "hazard",  "event": "banana_trap"},
}

OBJ_POINTS = np.array([
    [-TAG_SIZE / 2,  TAG_SIZE / 2, 0],
    [ TAG_SIZE / 2,  TAG_SIZE / 2, 0],
    [ TAG_SIZE / 2, -TAG_SIZE / 2, 0],
    [-TAG_SIZE / 2, -TAG_SIZE / 2, 0],
], dtype=np.float64)


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


def send_frame(frame):
    """Always resize to SEND_W x SEND_H before sending."""
    global tcp_client
    with tcp_lock:
        if tcp_client is None:
            return
    small = cv2.resize(frame, (SEND_W, SEND_H))
    _, buf = cv2.imencode('.jpg', small, [cv2.IMWRITE_JPEG_QUALITY, JPEG_QUALITY])
    data = buf.tobytes()
    with tcp_lock:
        try:
            tcp_client.sendall(struct.pack(">I", len(data)) + data)
        except:
            print("[TCP] Disconnected")
            tcp_client = None


def send_tags(tags):
    tag_list = []
    for t in tags:
        if t["id"] not in GATE_DEFS:
            continue
        g = GATE_DEFS[t["id"]]
        # Scale corners from camera res to send res
        corners = np.array(t["corners"])
        corners[:, 0] *= SEND_W / float(t.get("frame_w", CAM_W))
        corners[:, 1] *= SEND_H / float(t.get("frame_h", CAM_H))
        center = corners.mean(axis=0)
        tag_list.append({
            "id": t["id"], "name": g["name"], "type": g["type"], "event": g["event"],
            "corners": corners.tolist(), "center": center.tolist(),
            "pose_R": t.get("pose_R", []), "pose_t": t.get("pose_t", []),
        })
    if not tag_list:
        return
    try:
        data = json.dumps({"type": "ar_tags", "tags": tag_list, "timestamp": time.time()})
        tag_sock.sendto(data.encode(), (GODOT_IP, TAG_PORT))
    except:
        pass


def estimate_pose(corners_2d, cam_matrix):
    success, rvec, tvec = cv2.solvePnP(OBJ_POINTS, corners_2d.astype(np.float64), cam_matrix, np.zeros(5))
    if success:
        R, _ = cv2.Rodrigues(rvec)
        return R.tolist(), tvec.flatten().tolist()
    return np.eye(3).tolist(), [0.0, 0.0, 0.0]


def is_valid_quad(pts):
    pts = pts.reshape(4, 2)
    area = cv2.contourArea(pts)
    if area < 500 or area > 500000:
        return False
    if not cv2.isContourConvex(pts):
        return False
    w = np.linalg.norm(pts[1] - pts[0])
    h = np.linalg.norm(pts[3] - pts[0])
    if w < 1 or h < 1:
        return False
    if max(w, h) / min(w, h) > 4:
        return False
    return True


class SIFTDetector:
    def __init__(self, ref_path):
        ref = cv2.imread(ref_path)
        if ref is None:
            raise FileNotFoundError(f"Reference image not found: {ref_path}")
        s = REF_MAX_DIM / max(ref.shape[:2])
        ref = cv2.resize(ref, (0, 0), fx=s, fy=s)
        self.ref_gray = cv2.cvtColor(ref, cv2.COLOR_BGR2GRAY)
        self.ref_h, self.ref_w = self.ref_gray.shape
        self.sift = cv2.SIFT_create(nfeatures=SIFT_NFEATURES)
        self.kp1, self.des1 = self.sift.detectAndCompute(self.ref_gray, None)
        self.bf = cv2.BFMatcher()
        self.lost_count = 0
        self.last_dst = None
        print(f"[SIFT] Ref: {self.ref_w}x{self.ref_h}, {len(self.kp1)} features")

    def detect(self, frame):
        gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        kp2, des2 = self.sift.detectAndCompute(gray, None)
        if des2 is None or self.des1 is None:
            self.lost_count += 1
            if self.lost_count > 3: self.last_dst = None
            return self._persisted()
        matches = self.bf.knnMatch(self.des1, des2, k=2)
        good = [m for m_n in matches if len(m_n) == 2
                for m, nn in [m_n] if m.distance < SIFT_RATIO * nn.distance]
        if len(good) < SIFT_MIN_MATCHES:
            self.lost_count += 1
            if self.lost_count > 3: self.last_dst = None
            return self._persisted()
        src_pts = np.float32([self.kp1[m.queryIdx].pt for m in good]).reshape(-1, 1, 2)
        dst_pts = np.float32([kp2[m.trainIdx].pt for m in good]).reshape(-1, 1, 2)
        M, mask = cv2.findHomography(src_pts, dst_pts, cv2.RANSAC, 5.0)
        inliers = mask.sum() if mask is not None else 0
        if M is None or inliers < SIFT_MIN_INLIERS:
            self.lost_count += 1
            if self.lost_count > 3: self.last_dst = None
            return self._persisted()
        pts = np.float32([[0, 0], [self.ref_w, 0],
                          [self.ref_w, self.ref_h], [0, self.ref_h]]).reshape(-1, 1, 2)
        dst = cv2.perspectiveTransform(pts, M)
        if not is_valid_quad(np.int32(dst)):
            self.lost_count += 1
            if self.lost_count > 3: self.last_dst = None
            return self._persisted()
        self.last_dst = dst
        self.lost_count = 0
        corners = dst.reshape(4, 2)
        return [{"id": 0, "corners": corners.tolist(), "center": corners.mean(axis=0).tolist(),
                 "matches": len(good), "method": "sift"}]

    def _persisted(self):
        if self.last_dst is not None:
            corners = self.last_dst.reshape(4, 2)
            return [{"id": 0, "corners": corners.tolist(), "center": corners.mean(axis=0).tolist(),
                     "matches": 0, "method": "sift_persist"}]
        return []


class ArUcoDetector:
    def __init__(self):
        self.detector = cv2.aruco.ArucoDetector(
            cv2.aruco.getPredefinedDictionary(cv2.aruco.DICT_ARUCO_ORIGINAL),
            cv2.aruco.DetectorParameters())
        print("[ArUco] DICT_ARUCO_ORIGINAL ready (fallback)")

    def detect(self, frame):
        corners, ids, _ = self.detector.detectMarkers(frame)
        if ids is None: return []
        tags = []
        for i, tid in enumerate(ids.flatten()):
            if int(tid) not in GATE_DEFS: continue
            c = corners[i][0]
            tags.append({"id": int(tid), "corners": c.tolist(),
                         "center": c.mean(axis=0).tolist(), "method": "aruco"})
        return tags


def crop_reference(image_path):
    ref = cv2.imread(image_path)
    if ref is None:
        import subprocess
        png_path = image_path.rsplit('.', 1)[0] + '_converted.png'
        subprocess.run(['sips', '-s', 'format', 'png', image_path, '--out', png_path])
        ref = cv2.imread(png_path)
    if ref is None:
        print(f"ERROR: Cannot read {image_path}"); sys.exit(1)
    h, w = ref.shape[:2]
    scale = min(800 / w, 600 / h)
    small = cv2.resize(ref, (0, 0), fx=scale, fy=scale)
    print("Drag box around tag, press ENTER")
    roi = cv2.selectROI("Crop reference tag", small)
    cv2.destroyAllWindows()
    x, y, rw, rh = [int(v / scale) for v in roi]
    x, y = max(0, x), max(0, y)
    rw, rh = min(rw, w - x), min(rh, h - y)
    if rw < 10 or rh < 10:
        print("ERROR: Too small"); sys.exit(1)
    cv2.imwrite(REF_IMAGE_PATH, ref[y:y+rh, x:x+rw])
    print(f"Saved: {REF_IMAGE_PATH} ({rw}x{rh})")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--rtsp", type=str)
    parser.add_argument("--webcam", action="store_true")
    parser.add_argument("--headless", action="store_true")
    parser.add_argument("--crop", type=str)
    parser.add_argument("--aruco-only", action="store_true")
    args = parser.parse_args()

    if args.crop:
        crop_reference(args.crop)
        return

    sift_det = None
    aruco_det = ArUcoDetector()
    if not args.aruco_only:
        if os.path.exists(REF_IMAGE_PATH):
            try: sift_det = SIFTDetector(REF_IMAGE_PATH)
            except Exception as e: print(f"[SIFT] Failed: {e}")
        else:
            print(f"[SIFT] No ref image. Run: python3 ar_stream.py --crop path/to/photo.png")

    source = args.rtsp if args.rtsp else 0
    if args.webcam: source = 0

    threading.Thread(target=tcp_server_loop, daemon=True).start()

    cap = cv2.VideoCapture(source)
    if not cap.isOpened():
        print("[AR] ERROR: Cannot open camera!"); sys.exit(1)
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, CAM_W)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, CAM_H)
    frame_w = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
    frame_h = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))

    cam_matrix = np.array([
        [800.0 * frame_w / 640.0, 0, frame_w / 2.0],
        [0, 800.0 * frame_h / 480.0, frame_h / 2.0],
        [0, 0, 1.0]], dtype=np.float64)

    mode = "SIFT + ArUco" if sift_det else "ArUco only"
    print(f"[AR] Capture: {frame_w}x{frame_h} | Send: {SEND_W}x{SEND_H} q{JPEG_QUALITY} | {mode}")
    print(f"[AR] Tags->UDP:{TAG_PORT} Frames->TCP:{FRAME_PORT}")
    if not args.headless: print("[AR] Press 'q' to quit")

    n, fps, fps_n, fps_t = 0, 0, 0, time.time()

    while True:
        ret, frame = cap.read()
        if not ret: continue

        n += 1; fps_n += 1
        now = time.time()
        if now - fps_t >= 1.0:
            fps = fps_n; fps_n = 0; fps_t = now

        tags = []
        if n % 2 == 0:
            if sift_det: tags = sift_det.detect(frame)
            if not tags: tags = aruco_det.detect(frame)
        else:
            if sift_det: tags = sift_det._persisted()

        for tag in tags:
            c = np.array(tag["corners"], dtype=np.float64)
            tag["pose_R"], tag["pose_t"] = estimate_pose(c, cam_matrix)
            tag["frame_w"] = frame_w
            tag["frame_h"] = frame_h

        if tags:
            send_tags(tags)
            if n % 30 == 0:
                names = [GATE_DEFS[t["id"]]["name"] for t in tags if t["id"] in GATE_DEFS]
                print(f"[AR] {', '.join(names)} FPS:{fps}")

        if n % 3 == 0:
            send_frame(frame)

        if not args.headless:
            for tag in tags:
                pts = np.array(tag["corners"], dtype=np.int32)
                cv2.polylines(frame, [pts], True, (0, 0, 255), 5)
                cv2.polylines(frame, [pts], True, (0, 215, 255), 3)
                cx = int(pts[:, 0].mean())
                cy = int(pts[:, 1].min()) - 20
                cv2.putText(frame, 'MARIO KART AR', (cx - 100, cy),
                           cv2.FONT_HERSHEY_DUPLEX, 0.8, (0, 0, 0), 3)
                cv2.putText(frame, 'MARIO KART AR', (cx - 100, cy),
                           cv2.FONT_HERSHEY_DUPLEX, 0.8, (255, 255, 255), 2)
            status = "CONNECTED" if tcp_client else "waiting..."
            cv2.rectangle(frame, (0, 0), (frame_w, 30), (0, 0, 0), -1)
            cv2.putText(frame, f"FPS:{fps} | Tags:{len(tags)} | {mode} | TCP:{status}",
                        (10, 22), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 1)
            cv2.imshow("AR Kart Stream (Q to quit)", frame)
            if cv2.waitKey(1) & 0xFF == ord('q'): break
        else:
            time.sleep(0.001)

    cap.release()
    if not args.headless: cv2.destroyAllWindows()
    print("[AR] Done.")


if __name__ == "__main__":
    main()
