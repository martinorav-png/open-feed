// Section 2: Escalation (90–160s)
// Section 3: Final Privacy Protocol (160–220s)

function Section2() {
  return (
    <Sprite start={75} end={128}>
      <div style={{ position: 'absolute', inset: 0, background: '#040505' }} />

      {/* 90-100: Title shift */}
      <Sprite start={75} end={82}>
        <CenterCard
          line1="ADVISORY"
          line2="UPDATED"
          subtitle="— REVISION 03 —"
        />
      </Sprite>

      {/* 100-110 */}
      <Sprite start={82} end={90}>
        <FullScreenText
          lines={[
            "IF YOU ARE NOT",
            "AT YOUR HOME,",
            "IT IS ALREADY TOO LATE.",
          ]}
        />
      </Sprite>

      {/* 110-118 */}
      <Sprite start={90} end={96}>
        <FullScreenText
          lines={[
            "DO NOT ATTEMPT",
            "TO REACH",
            "THOSE WHO ARE NOT.",
          ]}
        />
      </Sprite>

      {/* 118-126 */}
      <Sprite start={96} end={102}>
        <SceneFrame label="DIAGRAM 2 — INTERIOR">
          <HouseDiagramFading phase={0} />
        </SceneFrame>
        <BottomCaption text="DO NOT LOOK OUTSIDE" delay={1} />
      </Sprite>

      {/* 126-134 */}
      <Sprite start={102} end={108}>
        <SceneFrame label="DIAGRAM 2 — INTERIOR">
          <HouseDiagramFading phase={1} />
        </SceneFrame>
        <BottomCaption text="IF YOU HEAR YOUR NAME, DO NOT ANSWER" delay={1} />
      </Sprite>

      {/* 134-142: Driveway cam — UNCONFIRMED */}
      <Sprite start={108} end={114}>
        <SceneFrame label="CAM 04 — N. DRIVEWAY">
          <PlaceholderImage kind="driveway" />
        </SceneFrame>
        <StatusOverlay
          rows={[
            ['HOUSEHOLD STATUS', 'UNCONFIRMED'],
            ['INTERIOR CAMS', '0/3 ONLINE'],
            ['LAST CHECK-IN', '— — :— —'],
          ]}
        />
      </Sprite>

      {/* 142-150 */}
      <Sprite start={114} end={120}>
        <FullScreenText
          lines={[
            "DO NOT LOOK",
            "AT THE MOON.",
          ]}
        />
      </Sprite>

      {/* 150-156 */}
      <Sprite start={120} end={124}>
        <FullScreenText
          lines={[
            "DO NOT LET",
            "ANYTHING SEE YOU",
            "SEEING IT.",
          ]}
          subtle
        />
      </Sprite>

      {/* 156-160: Authority verified flash */}
      <Sprite start={124} end={128}>
        <SceneFrame label="RELAY AUTHORITY">
          <AuthorityVerifiedScreen />
        </SceneFrame>
      </Sprite>

      <BroadcastChrome
        bottomLeft="ADVISORY · CH 06"
        bottomRight="DO NOT VERIFY"
      />
    </Sprite>
  );
}

function FullScreenText({ lines, subtle = false }) {
  const { localTime, duration } = useSprite();
  const exitStart = Math.max(0, duration - 0.5);
  const fadeOut = 1;
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#040505',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      flexDirection: 'column',
      fontFamily: '"VT323", monospace',
      color: subtle ? '#b8bca4' : '#e6e8d4',
      letterSpacing: '0.16em',
      opacity: fadeOut,
    }}>
      {lines.map((line, i) => {
        const start = i * 0.7;
        const fade = (localTime >= start) ? 1 : 0;
        return (
          <div key={i} style={{
            fontSize: 56, lineHeight: 1.15,
            opacity: fade,
            textShadow: subtle ? CHROMA_LIGHT : CHROMA,
          }}>
            {line}
          </div>
        );
      })}
    </div>
  );
}

function HouseDiagramFading({ phase = 0 }) {
  // phase 0: 4 figures, phase 1: 2 figures, phase 2: 0 figures
  const visible = [4, 2, 0][phase];
  const t = useTime();
  return (
    <div style={{ width: '100%', height: '100%', background: '#050805', position: 'relative' }}>
      <svg width="100%" height="100%" viewBox="0 0 800 540" preserveAspectRatio="none">
        <polygon points="200,160 400,80 600,160 600,440 200,440" fill="none" stroke="#d4d8c8" strokeWidth="2" />
        <line x1="400" y1="80" x2="400" y2="440" stroke="#d4d8c8" strokeWidth="1" opacity="0.5" />
        <line x1="200" y1="280" x2="600" y2="280" stroke="#d4d8c8" strokeWidth="1" opacity="0.5" />
        <rect x="380" y="400" width="40" height="40" fill="#050805" stroke="#d4d8c8" strokeWidth="2" />
        <rect x="240" y="320" width="40" height="40" fill="none" stroke="#d4d8c8" strokeWidth="1" />
        <rect x="520" y="320" width="40" height="40" fill="none" stroke="#d4d8c8" strokeWidth="1" />
        {[260, 340, 460, 540].slice(0, visible).map((x, i) => (
          <StickFigure key={i} x={x} y={200} fade={t} idx={i} />
        ))}
        <text x="400" y="490" textAnchor="middle" fill="#d4d8c8" fontSize="14" fontFamily="VT323, monospace" letterSpacing="0.2em">
          OCCUPANTS: {visible}/4
        </text>
      </svg>
    </div>
  );
}

function StatusOverlay({ rows }) {
  const t = useTime();
  const blink = Math.floor(t * 1.3) % 2 === 0;
  return (
    <div style={{
      position: 'absolute',
      bottom: 90, left: 60, right: 60,
      background: 'rgba(0,0,0,0.75)',
      border: '1px solid #5a6028',
      padding: '14px 22px',
      fontFamily: 'VT323, monospace',
      color: '#d8e0a8',
      fontSize: 22, letterSpacing: '0.1em',
    }}>
      {rows.map(([k, v], i) => (
        <div key={i} style={{ display: 'flex', justifyContent: 'space-between', padding: '3px 0' }}>
          <span style={{ opacity: 0.7 }}>{k}</span>
          <span style={{
            color: v.includes('UNCONFIRMED') ? '#e85a3a' : '#d8e0a8',
            opacity: v.includes('UNCONFIRMED') ? (blink ? 1 : 0.4) : 1,
          }}>
            {v}
          </span>
        </div>
      ))}
    </div>
  );
}

function AuthorityVerifiedScreen() {
  const { localTime } = useSprite();
  const flash = localTime < 0.4 || (localTime > 1.6 && localTime < 2.0);
  return (
    <div style={{
      width: '100%', height: '100%',
      background: '#0a1810',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      flexDirection: 'column',
      fontFamily: 'VT323, monospace',
      color: '#90ffb0',
    }}>
      <div style={{ fontSize: 14, letterSpacing: '0.4em', opacity: 0.6 }}>
        SYS:: relay/auth
      </div>
      <div style={{ fontSize: 36, letterSpacing: '0.18em', marginTop: 16, opacity: flash ? 1 : 0.85 }}>
        RELAY AUTHORITY VERIFIED
      </div>
      <div style={{ fontSize: 16, marginTop: 22, opacity: 0.5, letterSpacing: '0.2em' }}>
        SOURCE :: ████████ ████
      </div>
      <div style={{ fontSize: 14, marginTop: 60, opacity: 0.4, letterSpacing: '0.2em' }}>
        SIG: 0xA4-3F-EE-██
      </div>
    </div>
  );
}

// ──────────────────────────────────────────────────────────────────────────
// SECTION 3 — FINAL PRIVACY PROTOCOL (160–220s)

function Section3() {
  return (
    <Sprite start={128} end={167}>
      <div style={{ position: 'absolute', inset: 0, background: '#000' }} />

      {/* 160-168: Black title */}
      <Sprite start={128} end={134}>
        <BlackTitle title="FINAL PRIVACY PROTOCOL" subtitle="WAIT FOR CONFIRMATION TONE" />
      </Sprite>

      {/* 168-178 */}
      <Sprite start={134} end={141}>
        <FullScreenText
          lines={[
            "REMAIN WITH",
            "YOUR HOUSEHOLD.",
            "LOWER YOUR EYES.",
          ]}
        />
      </Sprite>

      {/* 178-186 */}
      <Sprite start={141} end={148}>
        <FullScreenText
          lines={[
            "DO NOT LOOK AT",
            "THE FACES OF",
            "THOSE BESIDE YOU.",
          ]}
        />
      </Sprite>

      {/* 186-194 */}
      <Sprite start={148} end={154}>
        <FullScreenText
          lines={[
            "DO NOT OPEN THE DOOR.",
            "DO NOT OPEN THE DOOR",
            "FOR ANY VOICE YOU KNOW.",
          ]}
        />
      </Sprite>

      {/* 194-202 */}
      <Sprite start={154} end={160}>
        <FullScreenText
          lines={[
            "DO NOT ALLOW YOURSELF",
            "TO BE SEEN.",
          ]}
          subtle
        />
      </Sprite>

      {/* 202-210 */}
      <Sprite start={160} end={164}>
        <ReassuranceText
          lines={[
            "you do not have to be brave.",
            "you do not have to understand what is happening.",
            "you only have to remain very still, and very quiet.",
          ]}
        />
      </Sprite>

      {/* 210-220: Loading bar stuck at 63% */}
      <Sprite start={164} end={167}>
        <LoadingScreen />
      </Sprite>

      <BroadcastChrome
        bottomLeft="PROTOCOL ACTIVE"
        bottomRight="STAND BY"
      />
    </Sprite>
  );
}

function BlackTitle({ title, subtitle }) {
  const { localTime, duration } = useSprite();
  const fade = 1;
  const subFade = (localTime >= 2.5) ? 1 : 0;
  const exitStart = Math.max(0, duration - 0.5);
  const fadeOut = 1;
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#000',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      flexDirection: 'column',
      fontFamily: '"VT323", monospace',
      color: '#e6e8d4',
      opacity: fadeOut,
    }}>
      <div style={{ fontSize: 60, letterSpacing: '0.18em', opacity: fade, textShadow: CHROMA }}>
        {title}
      </div>
      <div style={{ fontSize: 22, letterSpacing: '0.32em', marginTop: 36, opacity: subFade * 0.6 }}>
        — {subtitle} —
      </div>
    </div>
  );
}

function ReassuranceText({ lines }) {
  const { localTime, duration } = useSprite();
  const exitStart = Math.max(0, duration - 0.5);
  const fadeOut = 1;
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#020303',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      flexDirection: 'column',
      fontFamily: '"VT323", monospace',
      color: '#b0b4a0',
      padding: '0 100px',
      opacity: fadeOut,
    }}>
      {lines.map((line, i) => {
        const start = i * 1.6;
        const fade = (localTime >= start) ? 1 : 0;
        return (
          <div key={i} style={{
            fontSize: 32, lineHeight: 1.6,
            opacity: fade * 0.85,
            letterSpacing: '0.04em',
            textAlign: 'center', marginBottom: 18,
          }}>
            {line}
          </div>
        );
      })}
    </div>
  );
}

function LoadingScreen() {
  const { localTime } = useSprite();
  // Bar fills to 63% over 3s, then stalls
  const pct = Math.min(63, localTime * 25);
  const cursor = Math.floor(localTime * 1.6) % 2 === 0;
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#020303',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      flexDirection: 'column',
      fontFamily: '"VT323", monospace',
      color: '#d4d8c8',
    }}>
      <div style={{ fontSize: 22, letterSpacing: '0.2em', marginBottom: 32, opacity: 0.85 }}>
        REMAIN STILL UNTIL THE TONE
      </div>
      <div style={{
        width: 600, height: 26,
        border: '2px solid #d4d8c8',
        position: 'relative',
        background: '#020303',
      }}>
        <div style={{
          position: 'absolute', left: 0, top: 0, bottom: 0,
          width: `${pct}%`,
          background: 'repeating-linear-gradient(90deg, #d4d8c8 0 8px, #5a6028 8px 14px)',
        }} />
      </div>
      <div style={{ marginTop: 20, fontSize: 18, opacity: 0.7, letterSpacing: '0.18em' }}>
        {Math.floor(pct)}%  ·  THE TONE HAS NOT YET ARRIVED
        {cursor && <span style={{ marginLeft: 8 }}>█</span>}
      </div>
      <div style={{ marginTop: 80, fontSize: 14, opacity: 0.4, letterSpacing: '0.25em' }}>
        do not move
      </div>
    </div>
  );
}

window.Section2 = Section2;
window.Section3 = Section3;
window.FullScreenText = FullScreenText;
