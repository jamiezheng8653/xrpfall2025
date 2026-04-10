"""
ar_stream.py - Camera + Tag detection stream to Godot
  TCP 6003: JPEG frames (4-byte length prefix)
  UDP 6002: Tag detection JSON

Detection: SIFT image matching (primary) + ArUco (fallback)
Overlay: Mario Kart AR banner on detected tags

Usage:
  # First time: crop a reference image from a photo
  python3 ar_stream.py --crop path/to/tag_photo.png

  # Run with webcam
  OPENCV_AVFOUNDATION_SKIP_AUTH=1 python3 ar_stream.py --webcam
  python3 ar_stream.py --webcam --headless

  # Run with RTSP
  python3 ar_stream.py --rtsp rtsp://192.168.4.124:8554/cam

  # Force ArUco-only mode (for black/white printed tags)
  python3 ar_stream.py --webcam --aruco-only
"""

import socket, json, time, sys, struct, threading, argparse, os
import cv2
import numpy as np

GODOT_IP = "127.0.0.1"
TAG_PORT = 6002
FRAME_PORT = 6003
JPEG_QUALITY = 50
TAG_SIZE = 0.05
PERSIST_FRAMES = 10
SIFT_MIN_MATCHES = 20
SIFT_RATIO = 0.7
SIFT_MIN_INLIERS = 10
REF_MAX_DIM = 400
SIFT_NFEATURES = 1000
CAM_W, CAM_H = 960, 540  # lower camera res for speed

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REF_IMAGE_PATH = os.path.join(SCRIPT_DIR, "tag_ref_0.png")

tag_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
tcp_client = None
tcp_lock = threading.Lock()

GATE_DEFS = {
    0: {"name": "START/FINISH", "type": "finish",  "event": "checkpoint",  "color": (0, 255, 0)},
    1: {"name": "SPEED BOOST",  "type": "boost",   "event": "speed_boost", "color": (0, 200, 255)},
    2: {"name": "ITEM BOX",     "type": "item",    "event": "mystery_box", "color": (255, 100, 0)},
    3: {"name": "HAZARD",       "type": "hazard",  "event": "banana_trap", "color": (0, 0, 255)},
}

OBJ_POINTS = np.array([
    [-TAG_SIZE / 2,  TAG_SIZE / 2, 0],
    [ TAG_SIZE / 2,  TAG_SIZE / 2, 0],
    [ TAG_SIZE / 2, -TAG_SIZE / 2, 0],
    [-TAG_SIZE / 2, -TAG_SIZE / 2, 0],
], dtype=np.float64)


# ── TCP frame server ──────────────────────────────────────────────

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
    global tcp_client
    with tcp_lock:
        if tcp_client is None:
            return
    small = cv2.resize(frame, (640, 480))
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
        tag_list.append({
            "id": t["id"], "name": g["name"], "type": g["type"], "event": g["event"],
            "corners": t["corners"], "center": t["center"],
            "pose_R": t["pose_R"], "pose_t": t["pose_t"],
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


# ── Geometry validation ───────────────────────────────────────────

def is_valid_quad(pts):
    """Check if detected quad is reasonable (convex, right size, not too stretched)."""
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


# ── Mario Kart AR Overlay ─────────────────────────────────────────

def draw_mario_overlay(frame, corners_2d, tag_id, gate_name):
    """Draw Mario Kart style AR overlay on detected tag."""
    pts = np.array(corners_2d, dtype=np.int32)

    # Red + gold border
    cv2.polylines(frame, [pts], True, (0, 0, 255), 5)
    cv2.polylines(frame, [pts], True, (0, 215, 255), 3)

    # Gold corner dots
    for pt in pts:
        x, y = int(pt[0]), int(pt[1])
        cv2.circle(frame, (x, y), 8, (0, 215, 255), -1)
        cv2.circle(frame, (x, y), 8, (0, 0, 255), 2)

    cx = int(pts[:, 0].mean())
    top_y = int(pts[:, 1].min())
    bot_y = int(pts[:, 1].max())
    tag_h = bot_y - top_y

    # ── Main banner: "MARIO KART AR" ──
    font = cv2.FONT_HERSHEY_DUPLEX
    font_scale = max(0.5, tag_h / 250.0)
    thickness = max(2, int(font_scale * 2))

    banner_text = "MARIO KART AR"
    (tw, th), _ = cv2.getTextSize(banner_text, font, font_scale, thickness)
    bx1 = cx - tw // 2 - 12
    by1 = top_y - th - 25
    bx2 = cx + tw // 2 + 12
    by2 = top_y - 5

    # Red banner with transparency
    overlay = frame.copy()
    cv2.rectangle(overlay, (bx1, by1), (bx2, by2), (0, 0, 200), -1)
    cv2.addWeighted(overlay, 0.85, frame, 0.15, 0, frame)
    cv2.rectangle(frame, (bx1, by1), (bx2, by2), (0, 215, 255), 3)

    # White text with shadow
    tx = cx - tw // 2
    ty = by2 - 10
    cv2.putText(frame, banner_text, (tx + 2, ty + 2), font, font_scale, (0, 0, 0), thickness + 1)
    cv2.putText(frame, banner_text, (tx, ty), font, font_scale, (255, 255, 255), thickness)

    # ── Sub banner: gate name ──
    sub_scale = font_scale * 0.6
    sub_thick = max(1, int(sub_scale * 2))
    (stw, sth), _ = cv2.getTextSize(gate_name, font, sub_scale, sub_thick)
    sx1 = cx - stw // 2 - 8
    sy1 = bot_y + 8
    sx2 = cx + stw // 2 + 8
    sy2 = bot_y + sth + 18

    overlay2 = frame.copy()
    cv2.rectangle(overlay2, (sx1, sy1), (sx2, sy2), (0, 180, 255), -1)
    cv2.addWeighted(overlay2, 0.85, frame, 0.15, 0, frame)
    cv2.rectangle(frame, (sx1, sy1), (sx2, sy2), (0, 0, 200), 2)

    stx = cx - stw // 2
    sty = sy2 - 6
    cv2.putText(frame, gate_name, (stx + 1, sty + 1), font, sub_scale, (0, 0, 0), sub_thick + 1)
    cv2.putText(frame, gate_name, (stx, sty), font, sub_scale, (255, 255, 255), sub_thick)

    # Corner decoration blocks
    block_size = max(6, int(tag_h / 18))
    colors = [(0, 0, 255), (0, 215, 255), (0, 200, 0), (255, 100, 0)]
    for i, pt in enumerate(pts):
        x, y = int(pt[0]), int(pt[1])
        cv2.rectangle(frame, (x - block_size, y - block_size),
                      (x + block_size, y + block_size), colors[i % 4], -1)
        cv2.rectangle(frame, (x - block_size, y - block_size),
                      (x + block_size, y + block_size), (0, 0, 0), 2)


# ── SIFT detector ─────────────────────────────────────────────────

class SIFTDetector:
    def __init__(self, ref_path):
        ref = cv2.imread(ref_path)
        if ref is None:
            raise FileNotFoundError(f"Reference image not found: {ref_path}")

        # Resize reference to REF_MAX_DIM
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
        """Detect tag in frame. Returns list of tag dicts or empty list."""
        gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        kp2, des2 = self.sift.detectAndCompute(gray, None)

        if des2 is None or self.des1 is None:
            self.lost_count += 1
            if self.lost_count > 5:
                self.last_dst = None
            return self._persisted()

        matches = self.bf.knnMatch(self.des1, des2, k=2)
        good = [m for m_n in matches if len(m_n) == 2
                for m, nn in [m_n] if m.distance < SIFT_RATIO * nn.distance]

        if len(good) < SIFT_MIN_MATCHES:
            self.lost_count += 1
            if self.lost_count > 5:
                self.last_dst = None
            return self._persisted()

        src_pts = np.float32([self.kp1[m.queryIdx].pt for m in good]).reshape(-1, 1, 2)
        dst_pts = np.float32([kp2[m.trainIdx].pt for m in good]).reshape(-1, 1, 2)
        M, mask = cv2.findHomography(src_pts, dst_pts, cv2.RANSAC, 5.0)
        inliers = mask.sum() if mask is not None else 0

        if M is None or inliers < SIFT_MIN_INLIERS:
            self.lost_count += 1
            if self.lost_count > 5:
                self.last_dst = None
            return self._persisted()

        pts = np.float32([
            [0, 0], [self.ref_w, 0],
            [self.ref_w, self.ref_h], [0, self.ref_h]
        ]).reshape(-1, 1, 2)
        dst = cv2.perspectiveTransform(pts, M)

        if not is_valid_quad(np.int32(dst)):
            self.lost_count += 1
            if self.lost_count > 5:
                self.last_dst = None
            return self._persisted()

        # Valid detection
        self.last_dst = dst
        self.lost_count = 0
        corners = dst.reshape(4, 2)
        return [{
            "id": 0,
            "corners": corners.tolist(),
            "center": corners.mean(axis=0).tolist(),
            "matches": len(good),
            "inliers": int(inliers),
            "method": "sift",
        }]

    def _persisted(self):
        """Return last known position if still within persistence window."""
        if self.last_dst is not None:
            corners = self.last_dst.reshape(4, 2)
            return [{
                "id": 0,
                "corners": corners.tolist(),
                "center": corners.mean(axis=0).tolist(),
                "matches": 0,
                "inliers": 0,
                "method": "sift_persist",
            }]
        return []


# ── ArUco detector ────────────────────────────────────────────────

class ArUcoDetector:
    def __init__(self):
        self.aruco_dict = cv2.aruco.getPredefinedDictionary(cv2.aruco.DICT_ARUCO_ORIGINAL)
        self.params = cv2.aruco.DetectorParameters()
        self.detector = cv2.aruco.ArucoDetector(self.aruco_dict, self.params)
        print("[ArUco] DICT_ARUCO_ORIGINAL ready (fallback)")

    def detect(self, frame):
        corners, ids, _ = self.detector.detectMarkers(frame)
        if ids is None:
            return []
        tags = []
        for i, tag_id in enumerate(ids.flatten()):
            if int(tag_id) not in GATE_DEFS:
                continue
            c = corners[i][0]
            tags.append({
                "id": int(tag_id),
                "corners": c.tolist(),
                "center": c.mean(axis=0).tolist(),
                "method": "aruco",
            })
        return tags


# ── Crop tool ─────────────────────────────────────────────────────

def crop_reference(image_path):
    ref = cv2.imread(image_path)
    if ref is None:
        import subprocess
        png_path = image_path.rsplit('.', 1)[0] + '_converted.png'
        subprocess.run(['sips', '-s', 'format', 'png', image_path, '--out', png_path])
        ref = cv2.imread(png_path)
    if ref is None:
        print(f"ERROR: Cannot read {image_path}")
        sys.exit(1)

    h, w = ref.shape[:2]
    scale = min(800 / w, 600 / h)
    small = cv2.resize(ref, (0, 0), fx=scale, fy=scale)

    print("Drag a box around JUST the tag pattern (no fingers/background)")
    print("Press ENTER or SPACE to confirm, C to cancel")
    roi = cv2.selectROI("Crop reference tag", small)
    cv2.destroyAllWindows()

    x, y, rw, rh = [int(v / scale) for v in roi]
    x, y = max(0, x), max(0, y)
    rw, rh = min(rw, w - x), min(rh, h - y)

    if rw < 10 or rh < 10:
        print("ERROR: Selection too small")
        sys.exit(1)

    tag_img = ref[y:y+rh, x:x+rw]
    cv2.imwrite(REF_IMAGE_PATH, tag_img)
    print(f"Saved: {REF_IMAGE_PATH} ({rw}x{rh})")
    print("Tip: For best results, use a flat, well-lit, straight-on photo")


# ── Main ──────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--rtsp", type=str)
    parser.add_argument("--webcam", action="store_true")
    parser.add_argument("--headless", action="store_true")
    parser.add_argument("--crop", type=str, help="Crop reference image")
    parser.add_argument("--aruco-only", action="store_true")
    args = parser.parse_args()

    if args.crop:
        crop_reference(args.crop)
        return

    sift_det = None
    aruco_det = ArUcoDetector()

    if not args.aruco_only:
        if os.path.exists(REF_IMAGE_PATH):
            try:
                sift_det = SIFTDetector(REF_IMAGE_PATH)
            except Exception as e:
                print(f"[SIFT] Failed: {e}, using ArUco only")
        else:
            print(f"[SIFT] No ref image. Run: python3 ar_stream.py --crop path/to/photo.png")

    source = args.rtsp if args.rtsp else 0
    if args.webcam:
        source = 0

    threading.Thread(target=tcp_server_loop, daemon=True).start()

    cap = cv2.VideoCapture(source)
    if not cap.isOpened():
        print("[AR] ERROR: Cannot open camera!")
        sys.exit(1)

    # Set camera to 960x540 for speed
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, CAM_W)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, CAM_H)
    frame_w = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
    frame_h = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))

    cam_matrix = np.array([
        [800.0 * frame_w / 640.0, 0, frame_w / 2.0],
        [0, 800.0 * frame_h / 480.0, frame_h / 2.0],
        [0, 0, 1.0]
    ], dtype=np.float64)

    mode = "SIFT + ArUco" if sift_det else "ArUco only"
    print(f"[AR] {frame_w}x{frame_h} | {mode}")
    print(f"[AR] Tags->UDP:{TAG_PORT} Frames->TCP:{FRAME_PORT}")
    if not args.headless:
        print("[AR] Press 'q' to quit")

    n, fps, fps_n, fps_t = 0, 0, 0, time.time()

    while True:
        ret, frame = cap.read()
        if not ret:
            continue

        n += 1
        fps_n += 1
        now = time.time()
        if now - fps_t >= 1.0:
            fps = fps_n
            fps_n = 0
            fps_t = now

        # Detect every 2nd frame for speed
        tags = []
        if n % 2 == 0:
            if sift_det:
                tags = sift_det.detect(frame)
            if not tags:
                tags = aruco_det.detect(frame)
        else:
            # On skip frames, use SIFT persistence
            if sift_det:
                tags = sift_det._persisted()

        # Add pose estimation
        for tag in tags:
            c = np.array(tag["corners"], dtype=np.float64)
            tag["pose_R"], tag["pose_t"] = estimate_pose(c, cam_matrix)

        if tags:
            send_tags(tags)
            if n % 30 == 0:
                names = [GATE_DEFS[t["id"]]["name"] for t in tags if t["id"] in GATE_DEFS]
                methods = set(t.get("method", "?") for t in tags)
                print(f"[AR] {', '.join(names)} via {methods} FPS:{fps}")

        # Send every 3rd frame to Godot
        if n % 3 == 0:
            send_frame(frame)

        # Preview
        if not args.headless:
            for tag in tags:
                tid = tag["id"]
                g = GATE_DEFS.get(tid, {})
                gate_name = g.get("name", f"TAG {tid}")
                draw_mario_overlay(frame, tag["corners"], tid, gate_name)

            status = "CONNECTED" if tcp_client else "waiting..."
            cv2.rectangle(frame, (0, 0), (frame_w, 35), (0, 0, 0), -1)
            cv2.putText(frame, f"FPS:{fps} | Tags:{len(tags)} | {mode} | TCP:{status}",
                        (10, 25), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 0), 2)
            cv2.imshow("AR Kart Stream (Q to quit)", frame)
            if cv2.waitKey(1) & 0xFF == ord('q'):
                break
        else:
            time.sleep(0.001)

    cap.release()
    if not args.headless:
        cv2.destroyAllWindows()
    print("[AR] Done.")


if __name__ == "__main__":
    main()
