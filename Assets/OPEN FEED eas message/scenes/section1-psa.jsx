// Reusable chromatic-aberration text shadow for CRT-style elements
const CHROMA = '0 0 8px rgba(240,242,220,0.4), 3px 0 0 rgba(232,90,58,0.55), -3px 0 0 rgba(58,180,232,0.55), 1px 0 0 rgba(232,90,58,0.35), -1px 0 0 rgba(58,180,232,0.35)';
const CHROMA_LIGHT = '0 0 6px rgba(240,242,220,0.3), 2px 0 0 rgba(232,90,58,0.4), -2px 0 0 rgba(58,180,232,0.4)';
window.CHROMA = CHROMA;
window.CHROMA_LIGHT = CHROMA_LIGHT;

// Section 0: Station Ident (0–25s)
// Section 1: Normal PSA (25–90s)

function StationIdent() {
  // 0 - 25s
  return (
    <Sprite start={0} end={18}>
      <div style={{ position: 'absolute', inset: 0, background: '#0a0a0a' }} />

      {/* Color bars test pattern (0-6s) */}
      <Sprite start={0} end={5}>
        <ColorBars />
      </Sprite>

      {/* Station ident card (6-25s) */}
      <Sprite start={5} end={18}>
        <IdentCard />
      </Sprite>

      <BroadcastChrome
        bottomLeft="EMERGENCY ALERT"
        bottomRight="CH 06"
      />
    </Sprite>
  );
}

function ColorBars() {
  const bars = [
    '#bdbdbd', '#bdbd2a', '#2abdbd', '#2abd2a',
    '#bd2abd', '#bd2a2a', '#2a2abd', '#1a1a1a',
  ];
  return (
    <div style={{ position: 'absolute', inset: 0, display: 'flex', flexDirection: 'column' }}>
      <div style={{ flex: 1, display: 'flex' }}>
        {bars.map((c, i) => (
          <div key={i} style={{ flex: 1, background: c }} />
        ))}
      </div>
      <div style={{ height: 80, display: 'flex' }}>
        <div style={{ flex: 1, background: '#1a1a8a' }} />
        <div style={{ flex: 1, background: '#0a0a0a' }} />
        <div style={{ flex: 1, background: '#8a1a8a' }} />
        <div style={{ flex: 1, background: '#0a0a0a' }} />
        <div style={{ flex: 1, background: '#bdbdbd' }} />
      </div>
      <div style={{
        position: 'absolute', bottom: 90, left: 30,
        fontFamily: 'VT323, monospace', fontSize: 22,
        color: '#0a0a0a', background: '#bdbdbd',
        padding: '2px 8px',
      }}>
        SMPTE BARS — 1KHZ TONE
      </div>
    </div>
  );
}

function IdentCard() {
  const { localTime } = useSprite();
  const flicker = 0.85 + 0.15 * Math.sin(localTime * 7.3);
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: 'radial-gradient(ellipse at center, #102018 0%, #050805 80%)',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      flexDirection: 'column',
      color: '#d4d8c8',
      fontFamily: 'VT323, "Courier New", monospace',
      opacity: flicker,
      padding: '0 80px',
    }}>
      <div style={{
        fontSize: 60, letterSpacing: '0.08em',
        fontFamily: '"VT323", monospace',
        textShadow: CHROMA,
        textAlign: 'center', lineHeight: 1.15,
        whiteSpace: 'nowrap',
      }}>
        EMERGENCY<br/>ALERT SYSTEM
      </div>
      <div style={{
        fontSize: 22, letterSpacing: '0.24em',
        marginTop: 50, opacity: 0.85, textAlign: 'center',
        textShadow: CHROMA_LIGHT,
        whiteSpace: 'nowrap',
      }}>
        PUBLIC INFORMATION SERVICE
      </div>
      <div style={{
        fontSize: 16, letterSpacing: '0.28em',
        marginTop: 28, opacity: 0.55,
        whiteSpace: 'nowrap',
      }}>
        CIVIL RELAY · CHANNEL 6
      </div>
      <div style={{
        fontSize: 14, letterSpacing: '0.22em',
        marginTop: 36, opacity: 0.45,
        maxWidth: '70%', textAlign: 'center', lineHeight: 1.5,
      }}>
        THIS MESSAGE WILL REPEAT
        <br />
        UNTIL THERE ARE NONE TO SEE IT
      </div>

      <div style={{
        position: 'absolute', bottom: 40,
        fontSize: 12, letterSpacing: '0.32em', opacity: 0.35,
      }}>
        — STAND BY —
      </div>
    </div>
  );
}

function BroadcastChrome({ topLeft, topRight, bottomLeft, bottomRight, blink = true, color = '#d4d8c8' }) {
  const t = useTime();
  const blinkOn = !blink || Math.floor(t * 1.5) % 2 === 0;
  const mono = 'VT323, "Courier New", monospace';
  return (
    <>
      {topLeft && (
        <div style={{
          position: 'absolute', top: 18, left: 22,
          fontFamily: mono, fontSize: 22, letterSpacing: '0.12em',
          color: '#e85a3a', textShadow: '0 0 4px rgba(232,90,58,0.6)',
          display: 'flex', alignItems: 'center', gap: 8,
          opacity: blinkOn ? 1 : 0.25,
        }}>
          <span style={{ width: 10, height: 10, borderRadius: '50%', background: '#e85a3a' }} />
          {topLeft}
        </div>
      )}
      {topRight && (
        <div style={{
          position: 'absolute', top: 18, right: 22,
          fontFamily: mono, fontSize: 20, letterSpacing: '0.1em',
          color, opacity: 0.85,
        }}>
          {topRight}
        </div>
      )}
      {bottomLeft && (
        <div style={{
          position: 'absolute', bottom: 22, left: 22,
          fontFamily: mono, fontSize: 18, letterSpacing: '0.18em',
          color, opacity: 0.7,
          whiteSpace: 'nowrap',
        }}>
          {bottomLeft}
        </div>
      )}
      {bottomRight && (
        <div style={{
          position: 'absolute', bottom: 22, right: 22,
          fontFamily: mono, fontSize: 18, letterSpacing: '0.18em',
          color, opacity: 0.7,
          whiteSpace: 'nowrap',
        }}>
          {bottomRight}
        </div>
      )}
    </>
  );
}

// ──────────────────────────────────────────────────────────────────────────
// SECTION 1 — NORMAL PSA (25–90s)

function Section1() {
  return (
    <Sprite start={18} end={75}>
      <div style={{ position: 'absolute', inset: 0, background: '#080a08' }} />

      {/* 25-32: Title card */}
      <Sprite start={18} end={24}>
        <CenterCard
          line1="DO NOT MOVE"
          line2="DO NOT MAKE A SOUND"
          subtitle="— LISTEN —"
        />
      </Sprite>

      {/* 32-43 */}
      <Sprite start={24} end={33}>
        <BigTextCard text="DO NOT APPROACH YOUR WINDOWS" />
      </Sprite>
      {/* gap 43-44 */}

      {/* 44-53 */}
      <Sprite start={34} end={42}>
        <BigTextCard text="DO NOT LOOK OUTSIDE" />
      </Sprite>
      {/* gap 53-54 */}

      {/* 54-63 */}
      <Sprite start={43} end={51}>
        <BigTextCard text="DO NOT LOOK AT THE MOON" />
      </Sprite>
      {/* gap 63-64 */}

      {/* 64-73 */}
      <Sprite start={52} end={60}>
        <BigTextCard text="IF YOU ARE NOT AT YOUR HOME&#10;IT IS ALREADY TOO LATE" />
      </Sprite>
      {/* gap 73-74 */}

      {/* 74-81 */}
      <Sprite start={61} end={67}>
        <BigTextCard text="DO NOT ANSWER IF YOU&#10;HEAR YOUR NAME" />
      </Sprite>
      {/* gap 81-82 */}

      {/* 82-90 */}
      <Sprite start={68} end={75}>
        <BigTextCard text="STAY TUNED TO THIS BROADCAST&#10;DO NOT CLICK AWAY&#10;OR IT WILL COME" />
      </Sprite>

      <BroadcastChrome
        bottomRight="REBROADCAST"
      />
    </Sprite>
  );
}

function CenterCard({ line1, line2, subtitle }) {
  const { localTime, duration } = useSprite();
  const exitStart = Math.max(0, duration - 0.6);
  const fadeIn = 1;
  const fadeOut = 1;
  const op = fadeIn * fadeOut;
  return (
    <div style={{
      position: 'absolute', inset: 0,
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      flexDirection: 'column',
      background: '#050605',
      color: '#e6e8d8',
      fontFamily: '"VT323", monospace',
      letterSpacing: '0.1em',
      opacity: op,
    }}>
      <div style={{ fontSize: 64, lineHeight: 1.15, textAlign: 'center', textShadow: CHROMA }}>
        {line1}<br />{line2}
      </div>
      {subtitle && (
        <div style={{ fontSize: 18, marginTop: 56, opacity: 0.6, letterSpacing: '0.3em', textAlign: 'center', textShadow: CHROMA_LIGHT }}>
          {subtitle}
        </div>
      )}
    </div>
  );
}

function SceneFrame({ label, children }) {
  const { localTime, duration } = useSprite();
  const fadeIn = 1;
  const exitStart = Math.max(0, duration - 0.5);
  const fadeOut = 1;
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#020403',
      opacity: fadeIn * fadeOut,
    }}>
      <div style={{
        position: 'absolute',
        top: '7%', left: '6%', right: '6%', bottom: '14%',
      }}>
        {children}
      </div>
      {label && (
        <div style={{
          position: 'absolute',
          top: 60, left: 50,
          fontFamily: 'VT323, monospace', fontSize: 18,
          color: '#d4d8c8', letterSpacing: '0.15em',
          background: 'rgba(0,0,0,0.5)', padding: '2px 8px',
          opacity: 0.85,
        }}>
          {label}
        </div>
      )}
    </div>
  );
}

function BottomCaption({ text, delay = 0 }) {
  const { localTime, duration } = useSprite();
  if (localTime < delay) return null;
  const flicker = 0.92 + 0.08 * Math.sin(localTime * 11);
  const exitStart = Math.max(0, duration - 0.5);
  const fadeOut = 1;
  return (
    <div style={{
      position: 'absolute',
      bottom: 80, left: 0, right: 0,
      textAlign: 'center',
      fontFamily: '"VT323", monospace',
      fontSize: 38, letterSpacing: '0.16em',
      color: '#f0f2dc',
      textShadow: CHROMA,
      opacity: flicker * fadeOut,
    }}>
      {text}
    </div>
  );
}

// PSX-style placeholders — these are intentionally crude, low-poly looking
function PlaceholderImage({ kind }) {
  const styles = {
    width: '100%', height: '100%',
    position: 'relative', overflow: 'hidden',
  };
  // Real photo scenes
  if (kind === 'street') return <PhotoScene style={styles} src="assets/liminalhouse.jpg" />;
  if (kind === 'lot') return <PhotoScene style={styles} src="assets/carpark.jpg" />;
  if (kind === 'grocery') return <PhotoScene style={styles} src="assets/stor1.jpg" />;
  if (kind === 'forest') return <PhotoScene style={styles} src="assets/darkness.jpg" />;
  if (kind === 'driveway') return <PhotoScene style={styles} src="assets/liminalhouse.jpg" />;
  // Text-only black cards
  if (kind === 'phone') return <TextCard style={styles} text="TELEPHONE — OFF HOOK" />;
  if (kind === 'radio') return <TextCard style={styles} text="STAY TUNED" />;
  if (kind === 'desk') return <TextCard style={styles} text="TERMINAL — IDLE" />;
  if (kind === 'cashier') return <TextCard style={styles} text="REGISTER 03 — UNATTENDED" />;
  if (kind === 'house') return <HouseDiagram style={styles} />;
  return <div style={{ ...styles, background: '#222' }} />;
}

function PhotoScene({ style, src }) {
  return (
    <div style={{ ...style, background: '#000' }}>
      <img src={src} alt="" style={{
        width: '100%', height: '100%',
        objectFit: 'cover',
        display: 'block',
        filter: 'contrast(0.92) saturate(0.7) brightness(0.85)',
      }} />
      {/* Subtle washed-out overlay */}
      <div style={{
        position: 'absolute', inset: 0,
        background: 'linear-gradient(180deg, rgba(20,20,30,0.18) 0%, transparent 30%, transparent 70%, rgba(0,0,0,0.35) 100%)',
        pointerEvents: 'none',
      }} />
    </div>
  );
}

function TextCard({ style, text }) {
  return (
    <div style={{
      ...style,
      background: '#000',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      fontFamily: '"VT323", monospace',
      color: '#f0f2dc',
      fontSize: 44, letterSpacing: '0.18em',
      textAlign: 'center',
      textShadow: '0 0 6px rgba(240,242,220,0.25)',
    }}>
      {text}
    </div>
  );
}

function StreetScene({ style }) {
  // Snowy suburban street, low poly aesthetic
  return (
    <div style={{ ...style, background: 'linear-gradient(#0a0e1a 0%, #1a1e2a 60%, #2a2e36 100%)' }}>
      {/* Snow particles */}
      <SnowField count={40} />
      {/* Distant houses */}
      <svg width="100%" height="100%" viewBox="0 0 800 540" preserveAspectRatio="none" style={{ position: 'absolute', inset: 0 }}>
        {/* Ground */}
        <polygon points="0,400 800,400 800,540 0,540" fill="#1f2530" />
        {/* Road */}
        <polygon points="320,400 480,400 700,540 100,540" fill="#0a0d12" />
        <line x1="400" y1="400" x2="400" y2="540" stroke="#3a3a2a" strokeWidth="2" strokeDasharray="8 12" opacity="0.5" />
        {/* House silhouettes */}
        <polygon points="60,360 60,400 180,400 180,360 120,310" fill="#0a0f18" />
        <rect x="100" y="350" width="14" height="18" fill="#cfa850" opacity="0.85" />
        <polygon points="240,370 240,400 320,400 320,370 280,330" fill="#0a0f18" />
        <polygon points="490,370 490,400 580,400 580,370 535,330" fill="#0a0f18" />
        <rect x="510" y="378" width="10" height="12" fill="#cfa850" opacity="0.7" />
        <polygon points="640,360 640,400 760,400 760,360 700,315" fill="#0a0f18" />
        {/* Streetlight */}
        <rect x="395" y="220" width="3" height="180" fill="#3a3a3a" />
        <circle cx="396" cy="220" r="14" fill="#f5d680" opacity="0.7" />
        <circle cx="396" cy="220" r="40" fill="#f5d680" opacity="0.12" />
      </svg>
    </div>
  );
}

function ParkingLotScene({ style }) {
  return (
    <div style={{ ...style, background: '#0a0805' }}>
      <SnowField count={25} />
      <svg width="100%" height="100%" viewBox="0 0 800 540" preserveAspectRatio="none" style={{ position: 'absolute', inset: 0 }}>
        <polygon points="0,360 800,360 800,540 0,540" fill="#15110a" />
        {/* Parking lines */}
        {[...Array(7)].map((_, i) => {
          const x1 = 100 + i * 90;
          return <line key={i} x1={x1} y1="380" x2={x1 - 30 + i * -10} y2="540" stroke="#5a4a20" strokeWidth="2" opacity="0.5" />;
        })}
        {/* Sodium lamp poles */}
        {[150, 400, 650].map((x, i) => (
          <g key={i}>
            <rect x={x-2} y="80" width="4" height="280" fill="#1a1a1a" />
            <ellipse cx={x} cy="80" rx="22" ry="10" fill="#ffb648" opacity="0.85" />
            <ellipse cx={x} cy="200" rx="120" ry="180" fill="#ffb648" opacity="0.07" />
          </g>
        ))}
        {/* Empty far building */}
        <rect x="0" y="240" width="800" height="120" fill="#0a0805" />
        <rect x="50" y="270" width="700" height="6" fill="#1a1408" opacity="0.6" />
      </svg>
    </div>
  );
}

function GroceryScene({ style }) {
  return (
    <div style={{ ...style, background: '#1a1d10' }}>
      <svg width="100%" height="100%" viewBox="0 0 800 540" preserveAspectRatio="none" style={{ position: 'absolute', inset: 0 }}>
        {/* Floor — vanishing perspective */}
        <polygon points="0,540 800,540 540,300 260,300" fill="#2a2e1a" />
        <polygon points="260,300 540,300 480,260 320,260" fill="#1a1d10" />
        {/* Floor tile lines */}
        {[...Array(8)].map((_, i) => {
          const t = (i + 1) / 9;
          const y = 300 + t * 240;
          const xL = 260 - t * 260;
          const xR = 540 + t * 260;
          return <line key={i} x1={xL} y1={y} x2={xR} y2={y} stroke="#3a3e22" strokeWidth="1" opacity="0.4" />;
        })}
        {/* Shelves */}
        <rect x="20" y="180" width="140" height="180" fill="#0f1208" />
        <rect x="20" y="200" width="140" height="6" fill="#3a4020" />
        <rect x="20" y="240" width="140" height="6" fill="#3a4020" />
        <rect x="20" y="280" width="140" height="6" fill="#3a4020" />
        <rect x="20" y="320" width="140" height="6" fill="#3a4020" />
        <rect x="640" y="180" width="140" height="180" fill="#0f1208" />
        <rect x="640" y="200" width="140" height="6" fill="#3a4020" />
        <rect x="640" y="240" width="140" height="6" fill="#3a4020" />
        <rect x="640" y="280" width="140" height="6" fill="#3a4020" />
        {/* Fluorescent ceiling lights */}
        <rect x="0" y="0" width="800" height="120" fill="#1a1d10" />
        {[...Array(4)].map((_, i) => (
          <rect key={i} x={120 + i * 160} y="40" width="80" height="14" fill="#f0f8d8" opacity="0.92" />
        ))}
        {/* Far wall + register sign */}
        <rect x="320" y="220" width="160" height="80" fill="#0a0c05" />
        <text x="400" y="265" textAnchor="middle" fill="#5a6028" fontSize="16" fontFamily="VT323, monospace" letterSpacing="0.2em">10 ITEMS OR LESS</text>
      </svg>
      {/* Buzzy glow over fluorescents */}
      <div style={{
        position: 'absolute', inset: 0,
        background: 'radial-gradient(ellipse at 50% 8%, rgba(240,248,216,0.18) 0%, transparent 50%)',
        pointerEvents: 'none',
      }} />
    </div>
  );
}

function PhoneScene({ style }) {
  return (
    <div style={{ ...style, background: '#0a0805' }}>
      <svg width="100%" height="100%" viewBox="0 0 800 540" preserveAspectRatio="none" style={{ position: 'absolute', inset: 0 }}>
        {/* Table surface */}
        <polygon points="0,300 800,300 800,540 0,540" fill="#3a2a18" />
        <polygon points="0,300 800,300 800,330 0,330" fill="#1a1208" opacity="0.6" />
        {/* Phone — flip phone, off the cradle */}
        <g transform="translate(280, 320) rotate(-12)">
          <rect x="0" y="0" width="240" height="80" rx="12" fill="#1a1a1a" stroke="#3a3a3a" strokeWidth="2" />
          <rect x="20" y="14" width="200" height="40" rx="4" fill="#1a3a4a" />
          {/* dial pad dots */}
          {[...Array(3)].map((_, r) => [...Array(4)].map((_, c) => (
            <circle key={`${r}-${c}`} cx={42 + c * 50} cy={62 + r * 10} r="2" fill="#5a5a5a" />
          )))}
        </g>
        {/* Receiver, separate */}
        <g transform="translate(140, 360) rotate(8)">
          <rect x="0" y="0" width="120" height="36" rx="18" fill="#0a0a0a" stroke="#3a3a3a" strokeWidth="2" />
        </g>
        {/* Faint cable */}
        <path d="M 260 380 Q 320 420 380 396 Q 440 372 500 380" stroke="#1a1a1a" strokeWidth="3" fill="none" />
      </svg>
      <div style={{
        position: 'absolute', inset: 0,
        background: 'radial-gradient(ellipse at 30% 30%, rgba(180,200,160,0.05) 0%, transparent 50%)',
      }} />
    </div>
  );
}

function RadioScene({ style }) {
  const t = useTime();
  // Radio dial drifts
  const drift = Math.sin(t * 0.7) * 14;
  return (
    <div style={{ ...style, background: '#080604' }}>
      <svg width="100%" height="100%" viewBox="0 0 800 540" preserveAspectRatio="none" style={{ position: 'absolute', inset: 0 }}>
        {/* Radio body */}
        <rect x="120" y="160" width="560" height="240" rx="12" fill="#2a1f10" stroke="#5a4628" strokeWidth="2" />
        <rect x="120" y="160" width="560" height="40" fill="#1a1408" />
        {/* Speaker grille */}
        <rect x="160" y="220" width="200" height="160" fill="#0a0805" />
        {[...Array(20)].map((_, i) => (
          <line key={i} x1={170} y1={232 + i * 8} x2={350} y2={232 + i * 8} stroke="#3a2e18" strokeWidth="2" />
        ))}
        {/* Tuning dial face */}
        <rect x="400" y="220" width="240" height="60" fill="#15201a" stroke="#3a4630" strokeWidth="2" />
        {[...Array(20)].map((_, i) => (
          <line key={i} x1={410 + i * 12} y1={222} x2={410 + i * 12} y2={i % 5 === 0 ? 250 : 242} stroke="#5a8060" strokeWidth="1" />
        ))}
        {/* Dial pointer */}
        <line x1={520 + drift} y1="218" x2={520 + drift} y2="282" stroke="#e85a3a" strokeWidth="2" />
        {/* Knobs */}
        <circle cx="440" cy="340" r="22" fill="#1a1408" stroke="#5a4628" strokeWidth="2" />
        <circle cx="600" cy="340" r="22" fill="#1a1408" stroke="#5a4628" strokeWidth="2" />
        <line x1="440" y1="318" x2="440" y2="328" stroke="#e6d4a0" strokeWidth="2" />
        <line x1="600" y1="328" x2="608" y2="320" stroke="#e6d4a0" strokeWidth="2" />
        {/* Frequency label */}
        <text x="520" y="280" textAnchor="middle" fill="#5a8060" fontSize="13" fontFamily="VT323, monospace">1.4 MHz</text>
      </svg>
    </div>
  );
}

function DeskScene({ style }) {
  const t = useTime();
  const cursor = Math.floor(t * 1.5) % 2 === 0;
  return (
    <div style={{ ...style, background: '#040302' }}>
      <svg width="100%" height="100%" viewBox="0 0 800 540" preserveAspectRatio="none" style={{ position: 'absolute', inset: 0 }}>
        {/* Desk */}
        <polygon points="0,400 800,400 800,540 0,540" fill="#2a1f10" />
        {/* CRT monitor */}
        <rect x="220" y="100" width="360" height="280" rx="20" fill="#1f1a14" stroke="#3a3228" strokeWidth="3" />
        <rect x="250" y="130" width="300" height="220" rx="6" fill="#0a1810" />
        {/* Monitor glow */}
        <rect x="252" y="132" width="296" height="216" rx="4" fill="url(#crtGlow)" opacity="0.5" />
        <defs>
          <radialGradient id="crtGlow" cx="50%" cy="50%">
            <stop offset="0%" stopColor="#3aff80" stopOpacity="0.4" />
            <stop offset="100%" stopColor="#0a1810" stopOpacity="0" />
          </radialGradient>
        </defs>
        {/* Monitor scanline pattern */}
        {[...Array(40)].map((_, i) => (
          <line key={i} x1={252} y1={134 + i * 5.4} x2={548} y2={134 + i * 5.4} stroke="#3aff80" strokeWidth="0.4" opacity="0.18" />
        ))}
        {/* Prompt text */}
        <text x="270" y="170" fill="#90ffb0" fontSize="14" fontFamily="VT323, monospace" letterSpacing="0.05em">{">"} READY.</text>
        <text x="270" y="195" fill="#90ffb0" fontSize="14" fontFamily="VT323, monospace" letterSpacing="0.05em">{">"} AWAITING INPUT</text>
        {cursor && <rect x="270" y="208" width="10" height="14" fill="#90ffb0" />}
        {/* Stand */}
        <rect x="380" y="380" width="40" height="20" fill="#1a1408" />
        <rect x="340" y="395" width="120" height="8" fill="#1a1408" />
      </svg>
    </div>
  );
}

function DrivewayScene({ style }) {
  return (
    <div style={{ ...style, background: '#080a0c' }}>
      <SnowField count={20} />
      <svg width="100%" height="100%" viewBox="0 0 800 540" preserveAspectRatio="none" style={{ position: 'absolute', inset: 0 }}>
        <polygon points="0,360 800,360 800,540 0,540" fill="#1a1f1a" />
        <polygon points="280,360 520,360 700,540 100,540" fill="#2a2e2a" />
        {/* Garage */}
        <rect x="280" y="260" width="240" height="100" fill="#0f1414" />
        <rect x="300" y="280" width="200" height="70" fill="#1a201a" stroke="#3a423a" strokeWidth="1" />
        {[...Array(5)].map((_, i) => (
          <line key={i} x1={300} y1={280 + i * 14} x2={500} y2={280 + i * 14} stroke="#3a423a" strokeWidth="1" />
        ))}
        {/* Camera HUD */}
      </svg>
    </div>
  );
}

function ForestRoadScene({ style }) {
  const t = useTime();
  const dashOffset = (t * 80) % 40;
  return (
    <div style={{ ...style, background: '#0a0c0a' }}>
      <svg width="100%" height="100%" viewBox="0 0 800 540" preserveAspectRatio="none" style={{ position: 'absolute', inset: 0 }}>
        {/* Road vanishing */}
        <polygon points="0,540 800,540 460,260 340,260" fill="#0f0f0a" />
        <polygon points="340,260 460,260 420,200 380,200" fill="#080805" />
        {/* Center dashes */}
        {[...Array(8)].map((_, i) => {
          const t = (i + dashOffset/40) / 8;
          const y = 260 + t * 280;
          const w = 2 + t * 30;
          return <rect key={i} x={400 - w/2} y={y} width={w} height={t * 14 + 2} fill="#d4d8b0" opacity={0.6 - t * 0.3} />;
        })}
        {/* Trees on sides */}
        {[
          { x: 60, h: 240, w: 80 },
          { x: 180, h: 200, w: 70 },
          { x: 270, h: 170, w: 50 },
          { x: 320, h: 140, w: 35 },
          { x: 740, h: 240, w: 80 },
          { x: 620, h: 200, w: 70 },
          { x: 530, h: 170, w: 50 },
          { x: 480, h: 140, w: 35 },
        ].map((tr, i) => (
          <polygon key={i}
            points={`${tr.x},540 ${tr.x - tr.w/2},${540 - tr.h * 0.4} ${tr.x},${540 - tr.h} ${tr.x + tr.w/2},${540 - tr.h * 0.4}`}
            fill="#0a1208" stroke="#1a2218" strokeWidth="1" />
        ))}
        {/* Headlight glow */}
        <ellipse cx="400" cy="540" rx="220" ry="80" fill="#f0f0c0" opacity="0.06" />
      </svg>
    </div>
  );
}

function CashierScene({ style }) {
  return (
    <div style={{ ...style, background: '#15180a' }}>
      <svg width="100%" height="100%" viewBox="0 0 800 540" preserveAspectRatio="none" style={{ position: 'absolute', inset: 0 }}>
        <polygon points="0,540 800,540 600,320 200,320" fill="#2a2d18" />
        {/* Counter */}
        <rect x="120" y="280" width="560" height="80" fill="#1a1d10" />
        <rect x="120" y="280" width="560" height="6" fill="#3a4020" />
        {/* Register */}
        <rect x="320" y="220" width="160" height="70" fill="#0a0c05" stroke="#3a4020" strokeWidth="1" />
        <rect x="340" y="234" width="120" height="36" fill="#1a3a1a" />
        <text x="400" y="258" textAnchor="middle" fill="#90ff90" fontSize="14" fontFamily="VT323, monospace">REG. 03</text>
        {/* Empty stool */}
        <rect x="395" y="320" width="10" height="60" fill="#1a1408" />
        <ellipse cx="400" cy="320" rx="22" ry="6" fill="#2a1f10" />
        {/* Ceiling lights */}
        <rect x="0" y="0" width="800" height="120" fill="#15180a" />
        {[...Array(3)].map((_, i) => (
          <rect key={i} x={140 + i * 200} y="30" width="120" height="12" fill="#f0f8d8" opacity="0.85" />
        ))}
      </svg>
    </div>
  );
}

function HouseDiagram({ style }) {
  const t = useTime();
  return (
    <div style={{ ...style, background: '#050805' }}>
      <svg width="100%" height="100%" viewBox="0 0 800 540" preserveAspectRatio="none" style={{ position: 'absolute', inset: 0 }}>
        {/* House outline */}
        <polygon points="200,160 400,80 600,160 600,440 200,440" fill="none" stroke="#d4d8c8" strokeWidth="2" />
        <line x1="400" y1="80" x2="400" y2="440" stroke="#d4d8c8" strokeWidth="1" opacity="0.5" />
        {/* Rooms */}
        <line x1="200" y1="280" x2="400" y2="280" stroke="#d4d8c8" strokeWidth="1" opacity="0.5" />
        <line x1="400" y1="280" x2="600" y2="280" stroke="#d4d8c8" strokeWidth="1" opacity="0.5" />
        {/* Door */}
        <rect x="380" y="400" width="40" height="40" fill="#050805" stroke="#d4d8c8" strokeWidth="2" />
        {/* Windows */}
        <rect x="240" y="320" width="40" height="40" fill="none" stroke="#d4d8c8" strokeWidth="1" />
        <rect x="520" y="320" width="40" height="40" fill="none" stroke="#d4d8c8" strokeWidth="1" />
        {/* Stick figures — 4 of them */}
        {[260, 340, 460, 540].map((x, i) => (
          <StickFigure key={i} x={x} y={200} fade={t} idx={i} />
        ))}
        <text x="400" y="490" textAnchor="middle" fill="#d4d8c8" fontSize="14" fontFamily="VT323, monospace" letterSpacing="0.2em">FIG. 1 — DWELLING</text>
      </svg>
    </div>
  );
}

function StickFigure({ x, y, fade, idx }) {
  // Used in section 2 — figures slowly disappear
  // fade is global time; in S2 we'll wrap with a Sprite that controls visibility.
  return (
    <g>
      <circle cx={x} cy={y} r="8" fill="none" stroke="#d4d8c8" strokeWidth="1.5" />
      <line x1={x} y1={y + 8} x2={x} y2={y + 32} stroke="#d4d8c8" strokeWidth="1.5" />
      <line x1={x} y1={y + 16} x2={x - 10} y2={y + 26} stroke="#d4d8c8" strokeWidth="1.5" />
      <line x1={x} y1={y + 16} x2={x + 10} y2={y + 26} stroke="#d4d8c8" strokeWidth="1.5" />
      <line x1={x} y1={y + 32} x2={x - 8} y2={y + 46} stroke="#d4d8c8" strokeWidth="1.5" />
      <line x1={x} y1={y + 32} x2={x + 8} y2={y + 46} stroke="#d4d8c8" strokeWidth="1.5" />
    </g>
  );
}

function SnowField({ count = 30 }) {
  const t = useTime();
  // Deterministic pseudo-random snow
  const flakes = React.useMemo(() => {
    const out = [];
    for (let i = 0; i < count; i++) {
      out.push({
        x: ((i * 73.13) % 100),
        baseY: ((i * 37.7) % 100),
        speed: 8 + (i * 1.7) % 14,
        size: 1 + (i % 3),
      });
    }
    return out;
  }, [count]);
  return (
    <div style={{ position: 'absolute', inset: 0, pointerEvents: 'none' }}>
      {flakes.map((f, i) => {
        const y = (f.baseY + t * f.speed) % 100;
        return (
          <div key={i} style={{
            position: 'absolute',
            left: `${f.x}%`, top: `${y}%`,
            width: f.size, height: f.size,
            background: '#d8dcd0', opacity: 0.5,
            borderRadius: '50%',
          }} />
        );
      })}
    </div>
  );
}

window.StationIdent = StationIdent;
window.Section1 = Section1;
window.BroadcastChrome = BroadcastChrome;
window.PlaceholderImage = PlaceholderImage;
window.SceneFrame = SceneFrame;
window.BottomCaption = BottomCaption;
window.CenterCard = CenterCard;
window.SnowField = SnowField;
window.HouseDiagram = HouseDiagram;
window.StickFigure = StickFigure;

function BigTextCard({ text }) {
  const { localTime, duration } = useSprite();
  const fadeIn = 1;
  const exitStart = Math.max(0, duration - 0.8);
  const fadeOut = 1;
  // slow uneven flicker
  const flicker = 0.92 + 0.08 * (0.5 + 0.5 * Math.sin(localTime * 7.3) * Math.cos(localTime * 1.7));
  // occasional dropout
  const dropout = (Math.sin(localTime * 0.9) > 0.985) ? 0.3 : 1;
  const lines = text.split(/&#10;|\n/);
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#000',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      padding: '0 80px',
      fontFamily: '"VT323", monospace',
      color: '#f0f2dc',
      fontSize: lines.length > 2 ? 60 : 76,
      letterSpacing: '0.18em',
      lineHeight: 1.2,
      textAlign: 'center',
      textShadow: 'none',
      opacity: fadeIn * fadeOut * flicker * dropout,
    }}>
      <div style={{ textShadow: CHROMA }}>
        {lines.map((line, i) => (
          <div key={i}>{line}</div>
        ))}
      </div>
    </div>
  );
}
window.BigTextCard = BigTextCard;
