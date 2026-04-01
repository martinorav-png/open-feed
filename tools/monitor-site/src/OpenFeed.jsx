import { useEffect, useRef, useState } from "react";

const CAMERAS = [
  { id: "001", label: "Gas Station - Route 14", country: "US", tz: "CST", status: "live", quality: "240p", viewers: 23 },
  { id: "002", label: "Shop Interior - District 4", country: "TW", tz: "CST", status: "live", quality: "144p", viewers: 41 },
  { id: "003", label: "Hallway - Block 9, Floor 3", country: "UA", tz: "EET", status: "live", quality: "240p", viewers: 112, isNew: true },
  { id: "004", label: "Back Alley - Unknown", country: "??", tz: "---", status: "offline", quality: "---", viewers: 7 },
  { id: "005", label: "Parking Garage B2", country: "DE", tz: "CET", status: "live", quality: "320p", viewers: 18 },
  { id: "006", label: "Laundromat", country: "US", tz: "EST", status: "live", quality: "240p", viewers: 9 },
  { id: "007", label: "Warehouse Loading Dock", country: "PL", tz: "CET", status: "live", quality: "144p", viewers: 34 },
  { id: "008", label: "Hotel Lobby", country: "TH", tz: "ICT", status: "live", quality: "480p", viewers: 56 },
  { id: "009", label: "Underpass Cam", country: "BR", tz: "BRT", status: "offline", quality: "---", viewers: 3 },
  { id: "010", label: "Rooftop - Building 14", country: "JP", tz: "JST", status: "live", quality: "240p", viewers: 71 },
  { id: "011", label: "Stairwell C", country: "RO", tz: "EET", status: "live", quality: "144p", viewers: 15 },
  { id: "012", label: "Bus Depot - Cam 3", country: "MX", tz: "CST", status: "live", quality: "320p", viewers: 28 },
  { id: "013", label: "Courtyard", country: "EG", tz: "EET", status: "live", quality: "240p", viewers: 6, isNew: true },
  { id: "014", label: "Convenience Store", country: "KR", tz: "KST", status: "live", quality: "480p", viewers: 44 },
  { id: "015", label: "Corridor - Floor 7", country: "RU", tz: "MSK", status: "offline", quality: "---", viewers: 0 }
];

const NOISE_COLORS = [
  ["#2a3a20", "#3a4a30", "#1a2a18"],
  ["#3a3420", "#4a4430", "#2a2418"],
  ["#20243a", "#303448", "#181c2a"],
  ["#1a1a1a", "#2a2a2a", "#0e0e0e"],
  ["#2a2a20", "#3a3a30", "#1a1a18"],
  ["#1e2e1e", "#2e3e2e", "#121e12"],
  ["#2a2024", "#3a3034", "#1a1018"],
  ["#24201e", "#34302e", "#181410"]
];

function CameraThumb({ cam, index, onClick }) {
  const canvasRef = useRef(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) {
      return undefined;
    }

    const ctx = canvas.getContext("2d");
    const w = 160;
    const h = 120;
    canvas.width = w;
    canvas.height = h;

    const colors = NOISE_COLORS[index % NOISE_COLORS.length];
    const isOff = cam.status === "offline";

    function draw() {
      const imgData = ctx.createImageData(w, h);
      const d = imgData.data;

      const baseR = parseInt(colors[0].slice(1, 3), 16);
      const baseG = parseInt(colors[0].slice(3, 5), 16);
      const baseB = parseInt(colors[0].slice(5, 7), 16);

      for (let i = 0; i < d.length; i += 4) {
        const noise = (Math.random() - 0.5) * (isOff ? 50 : 24);
        d[i] = Math.max(0, Math.min(255, baseR + noise));
        d[i + 1] = Math.max(0, Math.min(255, baseG + noise));
        d[i + 2] = Math.max(0, Math.min(255, baseB + noise));
        d[i + 3] = 255;
      }

      if (!isOff) {
        const lightX = (index % 3) * 50 + 30;
        const lightY = 30 + (index % 4) * 15;
        const lr = parseInt(colors[1].slice(1, 3), 16);
        const lg = parseInt(colors[1].slice(3, 5), 16);
        const lb = parseInt(colors[1].slice(5, 7), 16);

        for (let y = 0; y < h; y++) {
          for (let x = 0; x < w; x++) {
            const dist = Math.sqrt((x - lightX) ** 2 + (y - lightY) ** 2);
            if (dist < 50) {
              const pixelIndex = (y * w + x) * 4;
              const factor = (1 - dist / 50) * 0.3;
              d[pixelIndex] = Math.min(255, d[pixelIndex] + lr * factor);
              d[pixelIndex + 1] = Math.min(255, d[pixelIndex + 1] + lg * factor);
              d[pixelIndex + 2] = Math.min(255, d[pixelIndex + 2] + lb * factor);
            }
          }
        }

        const scanY = Math.floor(Math.random() * h);
        for (let x = 0; x < w; x++) {
          const pixelIndex = (scanY * w + x) * 4;
          d[pixelIndex] = Math.min(255, d[pixelIndex] + 20);
          d[pixelIndex + 1] = Math.min(255, d[pixelIndex + 1] + 20);
          d[pixelIndex + 2] = Math.min(255, d[pixelIndex + 2] + 20);
        }
      }

      ctx.putImageData(imgData, 0, 0);

      if (isOff) {
        ctx.fillStyle = "rgba(180,180,180,0.3)";
        ctx.font = "bold 12px monospace";
        ctx.textAlign = "center";
        ctx.fillText("NO SIGNAL", w / 2, h / 2 + 4);
      }
    }

    draw();
    const interval = setInterval(draw, isOff ? 400 : 200);
    return () => clearInterval(interval);
  }, [cam.status, index]);

  return (
    <td
      onClick={() => onClick(cam)}
      style={{
        padding: "4px",
        verticalAlign: "top",
        cursor: "pointer"
      }}
    >
      <table cellPadding="0" cellSpacing="0" style={{ border: "1px solid #999", background: "#000" }}>
        <tbody>
          <tr>
            <td style={{ position: "relative" }}>
              <canvas
                ref={canvasRef}
                style={{ display: "block", width: 160, height: 120, imageRendering: "pixelated" }}
              />
              <span
                style={{
                  position: "absolute",
                  top: 2,
                  left: 4,
                  fontFamily: "monospace",
                  fontSize: 9,
                  color: "#8a8",
                  textShadow: "0 0 2px #000"
                }}
              >
                {`CAM_${cam.id}`}
              </span>
              {cam.status === "live" && (
                <span
                  style={{
                    position: "absolute",
                    top: 2,
                    right: 4,
                    fontFamily: "monospace",
                    fontSize: 9,
                    color: "#c44",
                    textShadow: "0 0 2px #000"
                  }}
                >
                  ● LIVE
                </span>
              )}
              {cam.isNew && (
                <span
                  style={{
                    position: "absolute",
                    bottom: 2,
                    right: 4,
                    fontFamily: "monospace",
                    fontSize: 8,
                    color: "#f00",
                    background: "#ff0",
                    padding: "0 3px",
                    fontWeight: "bold"
                  }}
                >
                  NEW!
                </span>
              )}
            </td>
          </tr>
          <tr>
            <td
              style={{
                fontFamily: "Verdana, sans-serif",
                fontSize: 10,
                padding: "3px 4px",
                background: "#eee",
                color: "#333",
                borderTop: "1px solid #999"
              }}
            >
              <b>{`[${cam.country}]`}</b> {cam.label}
              <br />
              <span style={{ fontSize: 9, color: "#777" }}>
                {cam.quality} - {cam.viewers} watching
              </span>
            </td>
          </tr>
        </tbody>
      </table>
    </td>
  );
}

function CameraExpanded({ cam, index, onClose }) {
  const canvasRef = useRef(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) {
      return undefined;
    }

    const ctx = canvas.getContext("2d");
    const w = 640;
    const h = 480;
    canvas.width = w;
    canvas.height = h;

    const colors = NOISE_COLORS[index % NOISE_COLORS.length];
    const isOff = cam.status === "offline";

    function draw() {
      const imgData = ctx.createImageData(w, h);
      const d = imgData.data;
      const baseR = parseInt(colors[0].slice(1, 3), 16);
      const baseG = parseInt(colors[0].slice(3, 5), 16);
      const baseB = parseInt(colors[0].slice(5, 7), 16);

      for (let i = 0; i < d.length; i += 4) {
        const px = (i / 4) % w;
        const py = Math.floor((i / 4) / w);
        const blockX = Math.floor(px / 2);
        const blockY = Math.floor(py / 2);
        const seed = (blockX * 7 + blockY * 13 + Date.now() * 0.001) % 1;
        const noise = Math.sin(seed * 6283) * 0.5 * (isOff ? 50 : 20);

        d[i] = Math.max(0, Math.min(255, baseR + noise));
        d[i + 1] = Math.max(0, Math.min(255, baseG + noise));
        d[i + 2] = Math.max(0, Math.min(255, baseB + noise));
        d[i + 3] = 255;
      }

      if (!isOff) {
        for (let lIdx = 0; lIdx < 2; lIdx++) {
          const lx = 100 + lIdx * 300 + Math.sin(Date.now() * 0.0003 + lIdx) * 20;
          const ly = 100 + lIdx * 80;
          const lr = parseInt(colors[1].slice(1, 3), 16);
          const lg = parseInt(colors[1].slice(3, 5), 16);
          const lb = parseInt(colors[1].slice(5, 7), 16);
          const radius = 140;

          const startY = Math.max(0, Math.floor(ly - radius));
          const endY = Math.min(h, Math.ceil(ly + radius));
          const startX = Math.max(0, Math.floor(lx - radius));
          const endX = Math.min(w, Math.ceil(lx + radius));

          for (let y = startY; y < endY; y++) {
            for (let x = startX; x < endX; x++) {
              const dist = Math.sqrt((x - lx) ** 2 + (y - ly) ** 2);
              if (dist < radius) {
                const pixelIndex = (y * w + x) * 4;
                const factor = (1 - dist / radius) * 0.35;
                d[pixelIndex] = Math.min(255, d[pixelIndex] + lr * factor);
                d[pixelIndex + 1] = Math.min(255, d[pixelIndex + 1] + lg * factor);
                d[pixelIndex + 2] = Math.min(255, d[pixelIndex + 2] + lb * factor);
              }
            }
          }
        }

        for (let y = 0; y < h; y += 3) {
          for (let x = 0; x < w; x++) {
            const pixelIndex = (y * w + x) * 4;
            d[pixelIndex] = Math.max(0, d[pixelIndex] - 6);
            d[pixelIndex + 1] = Math.max(0, d[pixelIndex + 1] - 6);
            d[pixelIndex + 2] = Math.max(0, d[pixelIndex + 2] - 6);
          }
        }
      }

      ctx.putImageData(imgData, 0, 0);
      ctx.shadowColor = "#000";
      ctx.shadowBlur = 2;
      ctx.font = "12px monospace";
      ctx.fillStyle = "rgba(140,170,130,0.8)";
      ctx.fillText(`CAM_${cam.id}`, 10, 18);

      const now = new Date();
      const ts = now.toLocaleTimeString("en-GB", { hour12: false });
      ctx.fillText(`${ts} ${cam.tz}`, 10, h - 10);
      ctx.fillText(cam.quality, w - 40, h - 10);

      if (cam.status === "live") {
        ctx.fillStyle = "rgba(200,70,70,0.9)";
        ctx.fillText("● REC", w - 55, 18);
      }

      if (isOff) {
        ctx.fillStyle = "rgba(200,200,200,0.4)";
        ctx.font = "bold 24px monospace";
        ctx.textAlign = "center";
        ctx.fillText("NO SIGNAL", w / 2, h / 2);
        ctx.textAlign = "start";
      }

      ctx.shadowBlur = 0;
    }

    draw();
    const interval = setInterval(draw, isOff ? 300 : 120);
    return () => clearInterval(interval);
  }, [cam, index]);

  return (
    <div
      style={{
        position: "fixed",
        inset: 0,
        background: "rgba(0,0,0,0.85)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        zIndex: 1000,
        cursor: "default"
      }}
      onClick={onClose}
    >
      <div onClick={(e) => e.stopPropagation()} style={{ background: "#ddd", border: "2px solid #666", padding: 2 }}>
        <table cellPadding="0" cellSpacing="0" width="644" style={{ background: "#c0c0c0", borderBottom: "1px solid #999" }}>
          <tbody>
            <tr>
              <td
                style={{
                  fontFamily: "Verdana, sans-serif",
                  fontSize: 11,
                  padding: "3px 6px",
                  fontWeight: "bold",
                  color: "#000"
                }}
              >
                {`CAM_${cam.id} - [${cam.country}] ${cam.label}`}
              </td>
              <td align="right" style={{ padding: "2px 4px" }}>
                <button
                  onClick={onClose}
                  style={{
                    fontFamily: "monospace",
                    fontSize: 12,
                    padding: "0 6px",
                    background: "#c0c0c0",
                    border: "2px outset #ddd",
                    cursor: "pointer",
                    fontWeight: "bold",
                    lineHeight: "18px"
                  }}
                >
                  X
                </button>
              </td>
            </tr>
          </tbody>
        </table>
        <canvas ref={canvasRef} style={{ display: "block", width: 640, height: 480, imageRendering: "auto" }} />
        <table cellPadding="0" cellSpacing="0" width="644" style={{ background: "#eee", borderTop: "1px solid #999" }}>
          <tbody>
            <tr>
              <td style={{ fontFamily: "Verdana, sans-serif", fontSize: 10, padding: "4px 6px", color: "#555" }}>
                Status: <b style={{ color: cam.status === "live" ? "#060" : "#900" }}>{cam.status.toUpperCase()}</b>
                {" | "}Quality: {cam.quality}
                {" | "}Viewers: {cam.viewers}
                {" | "}Timezone: {cam.tz}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  );
}

function VisitorCounter() {
  const [count, setCount] = useState(847);

  useEffect(() => {
    const interval = setInterval(() => {
      setCount((current) => {
        const next = current + (Math.random() > 0.5 ? 1 : -1);
        return Math.max(780, Math.min(920, next));
      });
    }, 7000);

    return () => clearInterval(interval);
  }, []);

  return <span>{String(count).padStart(5, "0")}</span>;
}

export default function OpenFeed() {
  const [selectedCam, setSelectedCam] = useState(null);
  const [selectedIndex, setSelectedIndex] = useState(0);

  const handleOpen = (cam) => {
    const index = CAMERAS.findIndex((entry) => entry.id === cam.id);
    setSelectedIndex(index);
    setSelectedCam(cam);
  };

  const rows = [];
  for (let i = 0; i < CAMERAS.length; i += 5) {
    rows.push(CAMERAS.slice(i, i + 5));
  }

  const faqs = [
    {
      q: "What is openfeed?",
      a: "openfeed indexes publicly accessible surveillance feeds from around the world. we do not host cameras. we aggregate what is already visible."
    },
    {
      q: "Is this legal?",
      a: "these feeds are accessible without authentication. no systems have been compromised. you are watching what anyone can watch. whether you should is a different question."
    },
    {
      q: "How do I submit a feed?",
      a: "use the submit page. provide the direct stream URL. feeds are verified manually before listing. we do not accept feeds from private residences."
    },
    {
      q: "A feed shows something wrong. What do I do?",
      a: "you can report it. or you can close the tab. both are choices."
    },
    {
      q: "Who runs this?",
      a: "openfeed has been indexing open surveillance streams since 2019. the project operates on the belief that if a camera is watching, someone should know it is being watched."
    },
    {
      q: "Do you accept donations?",
      a: "no. we accept no donations and we run no ads."
    }
  ];

  return (
    <div
      style={{
        background: "#e8e8e0",
        minHeight: "100vh",
        fontFamily: "Verdana, Geneva, sans-serif",
        fontSize: 12,
        color: "#222"
      }}
    >
      <table width="100%" cellPadding="0" cellSpacing="0" style={{ background: "#fff", borderBottom: "2px solid #888" }}>
        <tbody>
          <tr>
            <td style={{ padding: "8px 16px" }}>
              <span style={{ fontFamily: "monospace", fontSize: 22, fontWeight: "bold", letterSpacing: 1 }}>
                OPEN<span style={{ color: "#c00" }}>:</span>FEED
              </span>
              <span style={{ fontFamily: "Verdana, sans-serif", fontSize: 10, color: "#777", marginLeft: 10 }}>
                public surveillance camera index
              </span>
            </td>
            <td align="right" style={{ padding: "8px 16px", fontFamily: "monospace", fontSize: 11, color: "#555" }}>
              <VisitorCounter /> visitors online
            </td>
          </tr>
        </tbody>
      </table>

      <table width="100%" cellPadding="0" cellSpacing="0" style={{ background: "#d4d4cc", borderBottom: "1px solid #aaa" }}>
        <tbody>
          <tr>
            <td style={{ padding: "4px 16px" }}>
              {["[feeds]", "[map]", "[submit]", "[faq]", "[about]"].map((item, i) => (
                <a
                  key={item}
                  href="#"
                  onClick={(e) => e.preventDefault()}
                  style={{
                    fontFamily: "monospace",
                    fontSize: 11,
                    color: i === 0 ? "#000" : "#0000cc",
                    textDecoration: i === 0 ? "none" : "underline",
                    marginRight: 14,
                    fontWeight: i === 0 ? "bold" : "normal"
                  }}
                >
                  {item}
                </a>
              ))}
            </td>
            <td align="right" style={{ padding: "4px 16px", fontFamily: "monospace", fontSize: 10, color: "#888" }}>
              {CAMERAS.filter((cam) => cam.status === "live").length} feeds active /{" "}
              {CAMERAS.filter((cam) => cam.status === "offline").length} offline
            </td>
          </tr>
        </tbody>
      </table>

      <div style={{ padding: "12px 16px" }}>
        <table
          width="100%"
          cellPadding="6"
          cellSpacing="0"
          style={{
            border: "1px solid #bbb",
            background: "#fffff0",
            marginBottom: 12
          }}
        >
          <tbody>
            <tr>
              <td style={{ fontFamily: "Verdana, sans-serif", fontSize: 10, color: "#555" }}>
                <b>Notice:</b> openfeed.icu indexes publicly accessible camera feeds. We do not host or operate any cameras.
                All streams listed here are accessible without authentication. Click any feed to view in full size.{" "}
                <a
                  href="#"
                  onClick={(e) => e.preventDefault()}
                  style={{ color: "#0000cc", fontSize: 10 }}
                >
                  read more
                </a>
              </td>
            </tr>
          </tbody>
        </table>

        <table cellPadding="0" cellSpacing="0" style={{ marginBottom: 6 }}>
          <tbody>
            <tr>
              <td>
                <img
                  src="data:image/gif;base64,R0lGODlhBQAFAIAAAP8AAP///yH5BAEAAAEALAAAAAAFAAUAAAIHjI+py+0PADs="
                  width="5"
                  height="5"
                  alt=""
                  style={{ marginRight: 6 }}
                />
              </td>
              <td style={{ fontFamily: "Verdana, sans-serif", fontSize: 12, fontWeight: "bold", color: "#333" }}>
                Live Camera Feeds
              </td>
              <td style={{ fontFamily: "Verdana, sans-serif", fontSize: 10, color: "#999", paddingLeft: 8 }}>
                ({CAMERAS.length} indexed)
              </td>
            </tr>
          </tbody>
        </table>

        <hr style={{ border: "none", borderTop: "1px solid #bbb", marginBottom: 10 }} />

        <table cellPadding="0" cellSpacing="0">
          <tbody>
            {rows.map((row, rowIndex) => (
              <tr key={`row-${rowIndex}`}>
                {row.map((cam, cellIndex) => (
                  <CameraThumb
                    key={cam.id}
                    cam={cam}
                    index={rowIndex * 5 + cellIndex}
                    onClick={handleOpen}
                  />
                ))}
                {row.length < 5 &&
                  Array.from({ length: 5 - row.length }).map((_, emptyIndex) => (
                    <td key={`empty-${rowIndex}-${emptyIndex}`} style={{ padding: 4 }} />
                  ))}
              </tr>
            ))}
          </tbody>
        </table>

        <hr style={{ border: "none", borderTop: "1px solid #bbb", margin: "14px 0" }} />

        <table cellPadding="0" cellSpacing="0" style={{ marginBottom: 6 }}>
          <tbody>
            <tr>
              <td>
                <img
                  src="data:image/gif;base64,R0lGODlhBQAFAIAAAP8AAP///yH5BAEAAAEALAAAAAAFAAUAAAIHjI+py+0PADs="
                  width="5"
                  height="5"
                  alt=""
                  style={{ marginRight: 6 }}
                />
              </td>
              <td style={{ fontFamily: "Verdana, sans-serif", fontSize: 12, fontWeight: "bold", color: "#333" }}>
                Frequently Asked Questions
              </td>
            </tr>
          </tbody>
        </table>

        <hr style={{ border: "none", borderTop: "1px solid #bbb", marginBottom: 10 }} />

        <table width="700" cellPadding="8" cellSpacing="0" style={{ border: "1px solid #ccc", background: "#fff" }}>
          <tbody>
            {faqs.map((faq, index) => (
              <tr key={faq.q}>
                <td style={{ verticalAlign: "top", fontFamily: "Verdana, sans-serif", fontSize: 11 }}>
                  <b style={{ color: "#333" }}>Q: {faq.q}</b>
                  <br />
                  <span style={{ color: "#666", fontSize: 11 }}>A: {faq.a}</span>
                  {index < faqs.length - 1 && (
                    <hr style={{ border: "none", borderTop: "1px solid #eee", margin: "6px 0 0 0" }} />
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        <hr style={{ border: "none", borderTop: "1px solid #bbb", margin: "14px 0" }} />

        <div style={{ fontFamily: "Verdana, sans-serif", fontSize: 9, color: "#999", textAlign: "center", padding: "8px 0 16px" }}>
          openfeed.icu does not operate, own, or maintain any listed camera feeds.
          all streams are publicly accessible. viewer discretion advised.
          <br />
          contact: openfeed@onionmail.org
          <br />
          <br />
          <span style={{ fontFamily: "monospace", fontSize: 10, color: "#aaa" }}>
            last indexed: 2 hrs ago | running since 2019
          </span>
        </div>
      </div>

      {selectedCam && (
        <CameraExpanded
          cam={selectedCam}
          index={selectedIndex}
          onClose={() => setSelectedCam(null)}
        />
      )}
    </div>
  );
}
