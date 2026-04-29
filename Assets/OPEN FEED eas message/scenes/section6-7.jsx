// Section 6: Correction / Twist (325–370s)
// Section 7: Final Image (370–400s)

function Section6() {
  return (
    <Sprite start={247} end={282}>
      <div style={{ position: 'absolute', inset: 0, background: '#f3e9d0' }} />

      {/* 325-328: Cutoff glitch — signal goes haywire, color bars flash in */}
      <Sprite start={247} end={250}>
        <SignalCutoff />
      </Sprite>

      {/* 328-334: We're sorry — apologetic title */}
      <Sprite start={250} end={255}>
        <CheerCard
          big="WE'RE SORRY"
          small="for any inconvenience"
        />
      </Sprite>

      {/* 334-344: Disregard the previous broadcast */}
      <Sprite start={255} end={263}>
        <CheerNote
          heading="A SHORT NOTE"
          lines={[
            "Please disregard",
            "the previous broadcast.",
            "It was not meant for you.",
          ]}
          footer="— with apologies, your friends at Civil Relay —"
        />
      </Sprite>

      {/* 344-354: Reassurances list */}
      <Sprite start={263} end={270}>
        <CheerList
          heading="EVERYTHING IS FINE"
          lines={[
            "the moon is in its usual place.",
            "no one is calling your name.",
            "the door is just a door.",
            "there is nobody outside your house.",
            "you may open the curtains again.",
            "we hope you have a lovely evening.",
          ]}
        />
      </Sprite>

      {/* 354-363: Postcard — sun + houses */}
      <Sprite start={270} end={276}>
        <CheerPostcard />
      </Sprite>

      {/* 363-370: Thank you */}
      <Sprite start={276} end={282}>
        <CheerCard
          big="THANK YOU"
          small="for your patience"
        />
      </Sprite>

      {/* Chrome only after the glitch resolves */}
      <Sprite start={250} end={282}>
        <BroadcastChrome
          bottomLeft="WITH OUR APOLOGIES"
          bottomRight="THANK YOU"
          color="#3a2f1c"
          blink={false}
        />
      </Sprite>
    </Sprite>
  );
}

// Cut-off glitch: rapid corruption, then color bars take over
function SignalCutoff() {
  const { localTime } = useSprite();
  // Phase 1 (0–0.6s): broadcast goes haywire — heavy interference
  // Phase 2 (0.6–2.6s): color bars (held)
  // Phase 3 (2.6–3.0s): bars warp / fade to cream
  const phase = localTime < 0.6 ? 1 : localTime < 2.6 ? 2 : 3;

  // Phase 1 — strobing glitch
  if (phase === 1) {
    const k = localTime / 0.6; // 0..1
    const slip = Math.sin(localTime * 80) * 30;
    const slip2 = Math.cos(localTime * 110) * 24;
    const tearOn = Math.floor(localTime * 30) % 2 === 0;
    return (
      <div style={{
        position: 'absolute', inset: 0,
        background: '#000', overflow: 'hidden',
      }}>
        {/* Old transmission ghost — chunks of text/static */}
        <div style={{
          position: 'absolute', inset: 0,
          background: '#0a0a0a',
          fontFamily: '"VT323", monospace',
          color: '#f0f2dc',
          fontSize: 70, letterSpacing: '0.18em',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          textAlign: 'center',
          textShadow: CHROMA,
          opacity: 0.85,
          transform: `translate(${slip}px, ${slip2}px)`,
        }}>
          THIS MESSAGE WILL REPEAT
        </div>
        {/* Horizontal slice tears */}
        {[15, 32, 50, 68, 82].map((y, i) => {
          if (!tearOn && i % 2 === 0) return null;
          const off = Math.sin(localTime * 50 + i) * 80;
          return (
            <div key={i} style={{
              position: 'absolute',
              left: 0, right: 0, top: `${y}%`, height: '8%',
              background: i % 2 ? 'rgba(232,90,58,0.4)' : 'rgba(58,180,232,0.4)',
              transform: `translateX(${off}px)`,
              mixBlendMode: 'screen',
            }} />
          );
        })}
        {/* White flash dropouts */}
        {Math.sin(localTime * 30) > 0.6 && (
          <div style={{ position: 'absolute', inset: 0, background: '#fff', opacity: 0.7 }} />
        )}
        {/* Static noise field */}
        <div style={{
          position: 'absolute', inset: 0,
          background: `repeating-linear-gradient(${localTime * 1000}deg, #000 0 2px, #fff 2px 4px, #888 4px 6px)`,
          opacity: 0.3 + 0.4 * k,
          mixBlendMode: 'overlay',
        }} />
      </div>
    );
  }

  // Phase 2 — color bars (slight wobble)
  if (phase === 2) {
    const wobble = Math.sin(localTime * 8) * 1.5;
    return (
      <div style={{
        position: 'absolute', inset: 0,
        transform: `translateY(${wobble}px)`,
      }}>
        <ColorBarsCutoff />
      </div>
    );
  }

  // Phase 3 — bars warp out / fade to cream
  const k = (localTime - 2.6) / 0.4; // 0..1
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#f3e9d0',
    }}>
      <div style={{
        position: 'absolute', inset: 0,
        opacity: 1 - k,
        transform: `scaleY(${1 - k * 0.95})`,
        transformOrigin: 'center',
      }}>
        <ColorBarsCutoff />
      </div>
      {/* Thin bright line as bars collapse */}
      {k > 0.5 && (
        <div style={{
          position: 'absolute',
          left: 0, right: 0,
          top: '50%',
          height: 2,
          background: '#fff',
          opacity: 1 - (k - 0.5) * 2,
          boxShadow: '0 0 12px #fff',
        }} />
      )}
    </div>
  );
}

function ColorBarsCutoff() {
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
    </div>
  );
}

function CheerCard({ big, small }) {
  const { localTime, duration } = useSprite();
  const fadeIn = 1;
  const exitStart = Math.max(0, duration - 0.6);
  const fadeOut = 1;
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#f3e9d0',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      flexDirection: 'column',
      color: '#3a2f1c',
      fontFamily: 'Georgia, "Times New Roman", serif',
      opacity: fadeIn * fadeOut,
      padding: '0 80px',
    }}>
      <div style={{
        fontSize: 140, letterSpacing: '0.04em',
        fontStyle: 'italic',
        textAlign: 'center', lineHeight: 1.05,
      }}>
        {big}
      </div>
      <div style={{
        fontSize: 36, marginTop: 30,
        opacity: 0.75, letterSpacing: '0.06em',
        fontStyle: 'italic',
      }}>
        — {small} —
      </div>
    </div>
  );
}

function CheerNote({ heading, lines, footer }) {
  const { localTime, duration } = useSprite();
  const fadeIn = 1;
  const exitStart = Math.max(0, duration - 0.5);
  const fadeOut = 1;
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#f3e9d0',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      flexDirection: 'column',
      color: '#3a2f1c',
      fontFamily: 'Georgia, "Times New Roman", serif',
      padding: '0 120px',
      opacity: fadeIn * fadeOut,
    }}>
      <div style={{
        fontSize: 22, letterSpacing: '0.4em', opacity: 0.55,
        marginBottom: 50, textTransform: 'uppercase',
      }}>
        {heading}
      </div>
      {lines.map((l, i) => (
        <div key={i} style={{
          fontSize: 56, lineHeight: 1.35,
          textAlign: 'center', fontStyle: 'italic',
          opacity: 0.92,
        }}>
          {l}
        </div>
      ))}
      <div style={{
        fontSize: 20, marginTop: 64, opacity: 0.55,
        letterSpacing: '0.05em', fontStyle: 'italic',
      }}>
        {footer}
      </div>
    </div>
  );
}

function CheerList({ heading, lines }) {
  const { localTime } = useSprite();
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#f3e9d0',
      padding: '90px 140px',
      fontFamily: 'Georgia, "Times New Roman", serif',
      color: '#3a2f1c',
    }}>
      <div style={{
        fontSize: 26, letterSpacing: '0.4em', opacity: 0.55,
        marginBottom: 56, textTransform: 'uppercase',
      }}>
        {heading}
      </div>
      {lines.map((l, i) => {
        const start = i * 1.3;
        if (localTime < start) return null;
        const fade = (localTime >= start) ? 1 : 0;
        return (
          <div key={i} style={{
            fontSize: 38, lineHeight: 1.55,
            opacity: fade * 0.9,
            fontStyle: 'italic',
            marginBottom: 8,
          }}>
            · {l}
          </div>
        );
      })}
    </div>
  );
}

function CheerPostcard() {
  const { localTime, duration } = useSprite();
  const fadeIn = 1;
  const exitStart = Math.max(0, duration - 0.6);
  const fadeOut = 1;
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#f3e9d0',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      opacity: fadeIn * fadeOut,
    }}>
      <svg viewBox="0 0 1280 720" width="92%" height="92%" style={{ overflow: 'visible' }}>
        {/* sky */}
        <rect x="120" y="120" width="1040" height="480" fill="#fff7e3" stroke="#3a2f1c" strokeWidth="3" />
        {/* sun */}
        <circle cx="980" cy="240" r="60" fill="none" stroke="#c25e2a" strokeWidth="3" />
        {[0,1,2,3,4,5,6,7].map(i => {
          const a = (i / 8) * Math.PI * 2;
          const x1 = 980 + Math.cos(a) * 78;
          const y1 = 240 + Math.sin(a) * 78;
          const x2 = 980 + Math.cos(a) * 110;
          const y2 = 240 + Math.sin(a) * 110;
          return <line key={i} x1={x1} y1={y1} x2={x2} y2={y2} stroke="#c25e2a" strokeWidth="3" />;
        })}
        {/* horizon */}
        <line x1="120" y1="460" x2="1160" y2="460" stroke="#3a2f1c" strokeWidth="2" />
        {/* houses */}
        <g stroke="#3a2f1c" strokeWidth="2.5" fill="none">
          <polygon points="260,460 260,360 330,300 400,360 400,460" />
          <rect x="295" y="400" width="30" height="60" />
          <rect x="345" y="380" width="35" height="35" />
          <polygon points="500,460 500,330 580,260 660,330 660,460" />
          <rect x="540" y="395" width="35" height="65" />
          <rect x="600" y="370" width="40" height="40" />
          <polygon points="760,460 760,370 820,310 880,370 880,460" />
          <rect x="795" y="410" width="28" height="50" />
        </g>
        {/* tiny figures */}
        <g stroke="#3a2f1c" strokeWidth="2" fill="none">
          <circle cx="450" cy="500" r="6" />
          <line x1="450" y1="506" x2="450" y2="528" />
          <line x1="450" y1="514" x2="442" y2="522" />
          <line x1="450" y1="514" x2="458" y2="522" />
          <line x1="450" y1="528" x2="445" y2="540" />
          <line x1="450" y1="528" x2="455" y2="540" />
          <circle cx="700" cy="510" r="6" />
          <line x1="700" y1="516" x2="700" y2="538" />
          <line x1="700" y1="524" x2="692" y2="532" />
          <line x1="700" y1="524" x2="708" y2="532" />
          <line x1="700" y1="538" x2="695" y2="550" />
          <line x1="700" y1="538" x2="705" y2="550" />
        </g>
        {/* caption */}
        <text x="640" y="640" textAnchor="middle" fontFamily="Georgia, serif" fontStyle="italic" fontSize="32" fill="#3a2f1c">
          a lovely evening, after all.
        </text>
      </svg>
    </div>
  );
}

function CorrectionList() { return null; }
function ImpossibleDate() { return null; }
function ContradictoryCrawl() { return null; }

// ──────────────────────────────────────────────────────────────────────────
// SECTION 7 — FINAL IMAGE (370–400s)

function Section7() {
  return (
    <Sprite start={282} end={300}>
      <div style={{ position: 'absolute', inset: 0, background: '#f3e9d0' }} />

      {/* 370-385: A "goodnight" card — fades to a starry night */}
      <Sprite start={282} end={293}>
        <Goodnight />
      </Sprite>

      {/* 385-393: Sign-off card */}
      <Sprite start={293} end={297}>
        <SignOff />
      </Sprite>

      {/* 393-400: End */}
      <Sprite start={297} end={300}>
        <EndOfBroadcast />
      </Sprite>
    </Sprite>
  );
}

function Goodnight() {
  const { localTime, duration } = useSprite();
  // background slowly cools from cream to deep blue
  const k = Math.min(1, localTime / (duration - 1));
  const bg = `rgb(${Math.round(243 - 219*k)}, ${Math.round(233 - 211*k)}, ${Math.round(208 - 168*k)})`;
  const txt = k < 0.5 ? '#3a2f1c' : `rgb(${Math.round(58 + 200*((k-0.5)*2))}, ${Math.round(47 + 195*((k-0.5)*2))}, ${Math.round(28 + 180*((k-0.5)*2))})`;
  const fadeIn = 1;
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: bg,
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      flexDirection: 'column',
      fontFamily: 'Georgia, "Times New Roman", serif',
      color: txt,
      transition: 'background 0.4s linear, color 0.4s linear',
      opacity: fadeIn,
    }}>
      {/* stars appear in second half */}
      {k > 0.45 && (
        <svg width="100%" height="100%" viewBox="0 0 1280 720" style={{ position: 'absolute', inset: 0, pointerEvents: 'none' }}>
          {Array.from({ length: 60 }).map((_, i) => {
            const x = (i * 137.508) % 1280;
            const y = ((i * 263.31) % 620);
            const r = 0.6 + (i % 4) * 0.5;
            const op = ((k - 0.45) / 0.55) * (0.4 + 0.6 * Math.sin(localTime * 1.7 + i));
            return <circle key={i} cx={x} cy={y} r={r} fill="#fff7e3" opacity={Math.max(0, op)} />;
          })}
        </svg>
      )}
      <div style={{
        fontSize: 130, letterSpacing: '0.04em',
        fontStyle: 'italic',
        textAlign: 'center', lineHeight: 1.05,
        zIndex: 2,
      }}>
        good night
      </div>
      <div style={{
        fontSize: 28, marginTop: 40,
        opacity: 0.75, letterSpacing: '0.18em',
        zIndex: 2,
      }}>
        — sleep well —
      </div>
    </div>
  );
}

function SignOff() {
  const { localTime, duration } = useSprite();
  const fadeIn = 1;
  const exitStart = Math.max(0, duration - 0.5);
  const fadeOut = 1;
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#0a1428',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      flexDirection: 'column',
      fontFamily: 'Georgia, "Times New Roman", serif',
      color: '#f3e9d0',
      opacity: fadeIn * fadeOut,
    }}>
      <svg width="100%" height="100%" viewBox="0 0 1280 720" style={{ position: 'absolute', inset: 0, pointerEvents: 'none' }}>
        {Array.from({ length: 90 }).map((_, i) => {
          const x = (i * 137.508) % 1280;
          const y = ((i * 263.31) % 720);
          const r = 0.5 + (i % 4) * 0.6;
          const op = 0.3 + 0.7 * Math.sin(localTime * 1.7 + i);
          return <circle key={i} cx={x} cy={y} r={r} fill="#f3e9d0" opacity={Math.max(0.15, op * 0.5)} />;
        })}
      </svg>
      <div style={{
        fontSize: 56, letterSpacing: '0.06em',
        fontStyle: 'italic',
        textAlign: 'center', lineHeight: 1.2,
        zIndex: 2, maxWidth: '80%',
      }}>
        we now return you<br/>to regular programming
      </div>
      <div style={{
        fontSize: 22, marginTop: 50,
        opacity: 0.7, letterSpacing: '0.22em',
        zIndex: 2, fontStyle: 'italic',
      }}>
        thank you for listening.
      </div>
    </div>
  );
}

function EndOfBroadcast() {
  const { localTime } = useSprite();
  const fadeOut = Math.max(0, Math.min(1, 1 - (localTime - 5.5) / 1.5));
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#000',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      fontFamily: 'Georgia, "Times New Roman", serif',
      color: '#f3e9d0',
      opacity: fadeOut,
    }}>
      <div style={{
        fontSize: 18, letterSpacing: '0.4em',
        opacity: 0.55,
        fontStyle: 'italic',
      }}>
        — end of broadcast —
      </div>
    </div>
  );
}

function DarkRoomCRT() { return null; }
function RolledFinalLine() { return null; }

// ──────────────────────────────────────────────────────────────────────────
// SECTION 8 — POST-BROADCAST (300–330s)
// Viewer counter, house diagram, openfeed reference. Does not auto-close.

function Section8() {
  return (
    <Sprite start={300} end={363}>
      <div style={{ position: 'absolute', inset: 0, background: '#000' }} />

      {/* Viewer counter — appears at 301, increments at 304. Persists. */}
      <ViewerCounter />

      {/* House diagram — fades in at 305, holds, then fades out before final card */}
      <Sprite start={305} end={332}>
        <YourDwellingDiagram />
      </Sprite>

      {/* OpenFeed reference lines — fade in 308, 308.8, 309.6, hold, then fade out */}
      <Sprite start={308} end={332}>
        <OpenFeedReference />
      </Sprite>

      {/* Final card — fades in at 333, holds */}
      <Sprite start={333} end={348}>
        <FinalCard />
      </Sprite>

      {/* Credits */}
      <Sprite start={349} end={363}>
        <CreditsCard />
      </Sprite>
    </Sprite>
  );
}

function ViewerCounter() {
  const { localTime } = useSprite();
  // Section8 starts at 300. localTime 0 = 300s.
  // Counter visible from localTime 1 (301s). Increments at localTime 4 (304s).
  if (localTime < 1) return null;
  const count = localTime >= 4 ? 2 : 1;
  const visible = localTime >= 1;
  if (!visible) return null;
  return <ViewerCounterInner count={count} />;
}

function ViewerCounterInner({ count }) {
  const prevRef = React.useRef(count);
  React.useEffect(() => {
    if (prevRef.current !== count) {
      prevRef.current = count;
      try {
        const a = new Audio('assets/click.wav');
        a.volume = 0.6;
        a.play().catch(() => {});
      } catch {}
    }
  }, [count]);
  return (
    <div style={{
      position: 'absolute',
      bottom: 22, right: 22,
      fontFamily: 'VT323, monospace',
      fontSize: 18,
      letterSpacing: '0.18em',
      color: '#d4d8c8',
      opacity: 0.7,
      whiteSpace: 'nowrap',
      textShadow: window.CHROMA_LIGHT || '0 0 1px rgba(232,90,58,0.6), 0 0 1px rgba(58,160,232,0.6)',
      zIndex: 5,
    }}>
      VIEWERS: {count}
    </div>
  );
}

function YourDwellingDiagram() {
  const { localTime } = useSprite();
  // Sprite starts at 305s; this localTime is relative to 305.
  // Fade in over 1.5s.
  const fade = Math.min(1, localTime / 1.5);
  return (
    <div style={{
      position: 'absolute',
      inset: 0,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      flexDirection: 'column',
      opacity: fade,
      fontFamily: 'VT323, monospace',
      color: '#d4d8c8',
    }}>
      <svg viewBox="0 0 800 480" width="640" height="384" style={{ display: 'block' }}>
        {/* Outer house outline */}
        <rect x="120" y="80" width="560" height="320" fill="none" stroke="#d4d8c8" strokeWidth="2" />
        {/* Interior walls — back room with desk on right */}
        <line x1="460" y1="80" x2="460" y2="280" stroke="#d4d8c8" strokeWidth="2" />
        <line x1="460" y1="280" x2="680" y2="280" stroke="#d4d8c8" strokeWidth="2" />
        {/* Hallway wall */}
        <line x1="120" y1="240" x2="380" y2="240" stroke="#d4d8c8" strokeWidth="2" />
        {/* Doorway gaps (simulated by small white rects) */}
        <rect x="380" y="238" width="40" height="4" fill="#000" />
        <rect x="458" y="160" width="4" height="40" fill="#000" />

        {/* Window on right wall (back room) — between desk figure and exterior */}
        <line x1="678" y1="180" x2="682" y2="180" stroke="#000" strokeWidth="6" />
        <line x1="676" y1="160" x2="676" y2="200" stroke="#d4d8c8" strokeWidth="2" />
        <line x1="684" y1="160" x2="684" y2="200" stroke="#d4d8c8" strokeWidth="2" />
        <line x1="676" y1="180" x2="684" y2="180" stroke="#d4d8c8" strokeWidth="1" />

        {/* Other windows — front + side, to make it feel like a real dwelling */}
        <line x1="200" y1="78" x2="240" y2="78" stroke="#000" strokeWidth="4" />
        <line x1="200" y1="76" x2="200" y2="84" stroke="#d4d8c8" strokeWidth="1" />
        <line x1="240" y1="76" x2="240" y2="84" stroke="#d4d8c8" strokeWidth="1" />
        <line x1="220" y1="76" x2="220" y2="84" stroke="#d4d8c8" strokeWidth="1" />

        <line x1="320" y1="78" x2="360" y2="78" stroke="#000" strokeWidth="4" />
        <line x1="320" y1="76" x2="320" y2="84" stroke="#d4d8c8" strokeWidth="1" />
        <line x1="360" y1="76" x2="360" y2="84" stroke="#d4d8c8" strokeWidth="1" />

        {/* Front door */}
        <line x1="118" y1="340" x2="122" y2="340" stroke="#000" strokeWidth="6" />
        <rect x="116" y="320" width="6" height="40" fill="#000" />
        <rect x="116" y="320" width="6" height="40" fill="none" stroke="#d4d8c8" strokeWidth="1" />

        {/* Desk in the back room */}
        <rect x="540" y="150" width="120" height="14" fill="none" stroke="#d4d8c8" strokeWidth="1.5" />
        {/* Monitor on the desk */}
        <rect x="585" y="128" width="30" height="22" fill="none" stroke="#d4d8c8" strokeWidth="1.5" />
        <line x1="600" y1="150" x2="600" y2="155" stroke="#d4d8c8" strokeWidth="1" />

        {/* Stick figure at desk (interior) */}
        {/* head */}
        <circle cx="600" cy="190" r="9" fill="none" stroke="#d4d8c8" strokeWidth="1.5" />
        {/* body */}
        <line x1="600" y1="199" x2="600" y2="225" stroke="#d4d8c8" strokeWidth="1.5" />
        {/* arms reaching toward desk */}
        <line x1="600" y1="208" x2="585" y2="195" stroke="#d4d8c8" strokeWidth="1.5" />
        <line x1="600" y1="208" x2="615" y2="195" stroke="#d4d8c8" strokeWidth="1.5" />
        {/* legs */}
        <line x1="600" y1="225" x2="592" y2="245" stroke="#d4d8c8" strokeWidth="1.5" />
        <line x1="600" y1="225" x2="608" y2="245" stroke="#d4d8c8" strokeWidth="1.5" />
        {/* "YOU" tag */}
        <text x="600" y="270" fontFamily="VT323, monospace" fontSize="14" letterSpacing="2"
              textAnchor="middle" fill="#d4d8c8">YOU</text>

        {/* Stick figure outside, just past the window */}
        {/* head */}
        <circle cx="730" cy="170" r="9" fill="none" stroke="#d4d8c8" strokeWidth="1.5" />
        {/* body */}
        <line x1="730" y1="179" x2="730" y2="210" stroke="#d4d8c8" strokeWidth="1.5" />
        {/* arms — straight down at sides, not moving */}
        <line x1="730" y1="186" x2="722" y2="208" stroke="#d4d8c8" strokeWidth="1.5" />
        <line x1="730" y1="186" x2="738" y2="208" stroke="#d4d8c8" strokeWidth="1.5" />
        {/* legs */}
        <line x1="730" y1="210" x2="723" y2="232" stroke="#d4d8c8" strokeWidth="1.5" />
        <line x1="730" y1="210" x2="737" y2="232" stroke="#d4d8c8" strokeWidth="1.5" />

        {/* Direction-of-gaze: small dashed line from exterior figure toward window/desk */}
        <line x1="722" y1="175" x2="690" y2="175" stroke="#d4d8c8" strokeWidth="1" strokeDasharray="3 3" opacity="0.5" />

        {/* Compass */}
        <g opacity="0.6">
          <line x1="160" y1="120" x2="160" y2="105" stroke="#d4d8c8" strokeWidth="1" />
          <text x="160" y="100" fontFamily="VT323, monospace" fontSize="12" textAnchor="middle" fill="#d4d8c8">N</text>
        </g>
      </svg>

      <div style={{
        marginTop: 28,
        fontSize: 18,
        letterSpacing: '0.32em',
        opacity: 0.75,
        textShadow: window.CHROMA_LIGHT,
      }}>
        FIG. 1 — YOUR DWELLING
      </div>
    </div>
  );
}

function OpenFeedReference() {
  const { localTime } = useSprite();
  // Sprite starts at 308s. Lines fade in at +0, +0.8, +1.6.
  const lines = [
    'SIGNAL DELIVERED VIA: OPENFEED.ICU',
    'SESSION STATUS: ACTIVE',
    'FEEDS ACCESSED THIS SESSION: 5',
  ];
  return (
    <div style={{
      position: 'absolute',
      left: 0, right: 0,
      bottom: 60,
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      gap: 6,
      fontFamily: 'VT323, monospace',
      color: '#d4d8c8',
    }}>
      {lines.map((text, i) => {
        const start = i * 0.8;
        if (localTime < start) return null;
        const fade = Math.min(1, (localTime - start) / 0.6);
        return (
          <div key={i} style={{
            fontSize: 16,
            letterSpacing: '0.32em',
            opacity: 0.6 * fade,
            textShadow: window.CHROMA_LIGHT,
          }}>
            {text}
          </div>
        );
      })}
    </div>
  );
}

window.Section6 = Section6;
window.Section7 = Section7;
window.Section8 = Section8;

// ──────────────────────────────────────────────────────────────────────────
// FINAL CARD — appears after Section8 reveal (333s+)

function FinalCard() {
  const { localTime } = useSprite();
  // Slow burn-in: fade in over 2.5s, then a tiny tracking glitch every few seconds
  const fadeIn = Math.min(1, localTime / 2.5);

  // Subtle horizontal drift / tracking jitter on each line
  const jitterY = Math.sin(localTime * 1.3) * 0.6;

  // Occasional flicker
  const flicker = (Math.sin(localTime * 11) > 0.985) ? 0.55 : 1;

  // Three lines reveal in sequence
  const linesData = [
    { text: 'THE FEED RUNS BOTH WAYS.', delay: 0.4 },
    { text: 'IT HAS BEEN WATCHING', delay: 1.7 },
    { text: 'FOR AS LONG AS YOU HAVE.', delay: 3.0 },
  ];

  return (
    <div style={{
      position: 'absolute',
      inset: 0,
      background: '#000',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      flexDirection: 'column',
      gap: 18,
      fontFamily: 'VT323, monospace',
      color: '#d4d8c8',
      opacity: fadeIn * flicker,
      transform: `translateY(${jitterY}px)`,
    }}>
      {linesData.map((l, i) => {
        if (localTime < l.delay) return null;
        const lineFade = Math.min(1, (localTime - l.delay) / 0.9);
        const isLast = i === linesData.length - 1;
        return (
          <div key={i} style={{
            fontSize: isLast ? 38 : 34,
            letterSpacing: '0.34em',
            textAlign: 'center',
            lineHeight: 1.2,
            opacity: lineFade,
            textShadow: window.CHROMA,
            color: isLast ? '#e85a3a' : '#d4d8c8',
            paddingLeft: '0.34em',
          }}>
            {l.text}
          </div>
        );
      })}

      {/* Faint cursor-like blink after final line lands */}
      {localTime > 4.5 && (
        <div style={{
          marginTop: 24,
          width: 14, height: 22,
          background: '#d4d8c8',
          opacity: Math.floor(localTime * 2) % 2 === 0 ? 0.85 : 0,
          boxShadow: '0 0 12px rgba(212,216,200,0.5)',
        }} />
      )}
    </div>
  );
}

window.FinalCard = FinalCard;

function CreditsCard() {
  const { localTime } = useSprite();
  const fadeIn = Math.min(1, localTime / 2.5);
  const flicker = (Math.sin(localTime * 9.3) > 0.988) ? 0.5 : 1;
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#000',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      fontFamily: 'VT323, monospace',
      color: '#d4d8c8',
      fontSize: 32,
      letterSpacing: '0.28em',
      textAlign: 'center',
      opacity: fadeIn * flicker,
      textShadow: window.CHROMA_LIGHT,
    }}>
      made with love by martin
    </div>
  );
}

window.CreditsCard = CreditsCard;
