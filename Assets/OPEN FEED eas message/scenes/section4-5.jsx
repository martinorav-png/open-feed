// Section 4: Reference / unease (220–275s)
// Section 5: Broadcast corruption (275–325s)

function Section4() {
  return (
    <Sprite start={167} end={209}>
      <div style={{ position: 'absolute', inset: 0, background: '#040504' }} />

      <Sprite start={167} end={173}>
        <CenterCard
          line1="THE FOLLOWING PLACES"
          line2="ARE NO LONGER SAFE"
          subtitle="— CONFIRMED SIGHTINGS —"
        />
        />
      </Sprite>

      <Sprite start={173} end={180}>
        <SceneFrame label="REF/01 — GROCERY 24H">
          <PlaceholderImage kind="grocery" />
        </SceneFrame>
        <BottomCaption text="DO NOT ENTER ANY SHOP THAT IS STILL OPEN" delay={1.4} />
      </Sprite>

      <Sprite start={180} end={186}>
        <SceneFrame label="REF/02 — LOT 4-B">
          <PlaceholderImage kind="lot" />
        </SceneFrame>
        <BottomCaption text="DO NOT ENTER ANY ROOM THAT IS WAITING FOR YOU" delay={1.4} />
      </Sprite>

      <Sprite start={186} end={192}>
        <SceneFrame label="REF/03 — RT. 11">
          <PlaceholderImage kind="forest" />
        </SceneFrame>
        <BottomCaption text="DO NOT STOP THE CAR" delay={1.4} />
      </Sprite>

      <Sprite start={192} end={198}>
        <SceneFrame label="REF/04 — TERMINAL">
          <PlaceholderImage kind="desk" />
        </SceneFrame>
        <BottomCaption text="DO NOT TURN AROUND IN YOUR CHAIR" delay={1.4} />
      </Sprite>

      <Sprite start={198} end={204}>
        <SceneFrame label="REF/05 — RECEIVER">
          <PlaceholderImage kind="radio" />
        </SceneFrame>
        <BottomCaption text="DO NOT CHANGE THE FREQUENCY" delay={1.4} />
      </Sprite>

      <Sprite start={204} end={209}>
        <SceneFrame label="REF/06 — REG. 03">
          <PlaceholderImage kind="cashier" />
        </SceneFrame>
        <BottomCaption text="THERE IS NO ONE LEFT TO COME TO WORK" delay={1.4} />
      </Sprite>

      {/* Hidden visual clues - flicker for less than a second */}
      <HiddenClue start={232} dur={0.35} text="QTH 48.097N 014.322E" pos={{ top: '20%', right: '8%' }} />
      <HiddenClue start={246} dur={0.30} text="GRID: JO22JL" pos={{ top: '78%', left: '8%' }} />
      <HiddenClue start={258} dur={0.25} text="civil-relay.local/notice" pos={{ top: '14%', left: '10%' }} />
      <HiddenClue start={266} dur={0.30} text="CAM ID: 03-RR" pos={{ top: '24%', left: '60%' }} />

      <BroadcastChrome
        bottomLeft="REFERENCE FEED"
        bottomRight="REBROADCAST SOURCE UNKNOWN"
      />

      {/* Crawl */}
      <CrawlBar
        text="REBROADCAST SOURCE UNKNOWN ·  ·  CALLSIGN K6CR ·  ·  RELAY TRACE :: 61.214 / -149.886 ·  ·  CIVIL RELAY ARCHIVE 03 ·  ·  IF YOU ARE WATCHING THIS YOU ARE NOT ALONE — WE THINK ·  ·  "
      />
    </Sprite>
  );
}

function HiddenClue({ start, dur, text, pos }) {
  const t = useTime();
  const visible = t >= start && t < start + dur;
  if (!visible) return null;
  return (
    <div style={{
      position: 'absolute',
      ...pos,
      fontFamily: 'VT323, monospace', fontSize: 16,
      color: '#e85a3a',
      letterSpacing: '0.12em',
      textShadow: '0 0 4px rgba(232,90,58,0.5)',
      background: 'rgba(0,0,0,0.4)',
      padding: '2px 6px',
      pointerEvents: 'none',
    }}>
      {text}
    </div>
  );
}

function CrawlBar({ text }) {
  const t = useTime();
  const offset = (t * 80) % 2000;
  return (
    <div style={{
      position: 'absolute', bottom: 50, left: 0, right: 0,
      height: 28, overflow: 'hidden',
      background: 'rgba(0,0,0,0.7)',
      borderTop: '1px solid #5a6028',
      borderBottom: '1px solid #5a6028',
    }}>
      <div style={{
        whiteSpace: 'nowrap',
        fontFamily: 'VT323, monospace',
        fontSize: 22, color: '#e6e8c8',
        letterSpacing: '0.15em',
        position: 'absolute',
        left: `-${offset}px`, top: 2,
      }}>
        {text}{text}{text}
      </div>
    </div>
  );
}

// ──────────────────────────────────────────────────────────────────────────
// SECTION 5 — BROADCAST CORRUPTION (275–325s)

function Section5() {
  return (
    <Sprite start={209} end={247}>
      <div style={{ position: 'absolute', inset: 0, background: '#040405' }} />

      {/* 275-285: Repeating "do not verify" with corruption */}
      <Sprite start={209} end={217}>
        <CorruptedRepeat />
      </Sprite>

      {/* 285-292: Seal glitches */}
      <Sprite start={217} end={222}>
        <SealGlitch />
      </Sprite>

      {/* 292-300: PUBLIC vs PRIVATE flicker */}
      <Sprite start={222} end={228}>
        <WordFlicker pairs={[
          ['PUBLIC SAFETY', 'PRIVATE SAFETY'],
          ['AUTHORIZED MESSAGE', 'UNAUTHORIZED MESSAGE'],
        ]} />
      </Sprite>

      {/* 300-308: Black flashes + 2nd voice indication */}
      <Sprite start={228} end={234}>
        <SecondVoiceScreen />
      </Sprite>

      {/* 308-316: Error stack */}
      <Sprite start={234} end={240}>
        <ErrorStack />
      </Sprite>

      {/* 316-325: Final corruption — "THIS IS NOT A CIVIL MESSAGE" */}
      <Sprite start={240} end={247}>
        <NotCivilMessage />
      </Sprite>

      <BroadcastChrome
        bottomLeft="SIGNAL DEGRADED"
        bottomRight="??"
      />

      {/* Sporadic full-frame black blink */}
      <BlackBlink times={[280.4, 287.2, 293.7, 301.5, 308.8, 314.1, 318.9, 322.5]} />
    </Sprite>
  );
}

function CorruptedRepeat() {
  const { localTime } = useSprite();
  // Repeat "Do not verify" stutters
  const lines = [
    { t: 0,   text: "DO NOT VERIFY." },
    { t: 1.6, text: "DO NOT VERIFY." },
    { t: 3.0, text: "DO NOT VERIFY THE ORIGIN OF THIS MESSAGE." },
    { t: 5.5, text: "DO NOT ASK YOUR LOCAL STATION." },
    { t: 7.5, text: "DO NOT CONTACT CIVIL DEFENSE." },
  ];
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#040505',
      padding: '120px 80px',
      fontFamily: 'VT323, monospace',
      color: '#e6e8d4',
      fontSize: 36, letterSpacing: '0.14em',
      lineHeight: 1.6,
    }}>
      {lines.map((l, i) => {
        if (localTime < l.t) return null;
        const age = localTime - l.t;
        const opacity = Math.min(1, age * 2);
        const shifted = i % 2 === 0;
        return (
          <div key={i} style={{
            opacity,
            transform: shifted ? `translateX(${Math.sin(localTime * 6 + i) * 4}px)` : 'none',
            textShadow: '2px 0 0 rgba(232,90,58,0.5), -2px 0 0 rgba(58,180,232,0.5)',
            marginBottom: 4,
          }}>
            {l.text}
          </div>
        );
      })}
    </div>
  );
}

function SealGlitch() {
  const { localTime } = useSprite();
  const glitch = Math.floor(localTime * 5) % 2 === 0;
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#040505',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      flexDirection: 'column',
      fontFamily: 'VT323, monospace', color: '#d4d8c8',
    }}>
      <div style={{
        width: 200, height: 200, borderRadius: '50%',
        border: '3px double #d4d8c8',
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        position: 'relative',
        filter: glitch ? 'invert(1) hue-rotate(60deg)' : 'none',
        transform: glitch ? `translateX(${Math.sin(localTime * 30) * 4}px)` : 'none',
      }}>
        <div style={{ fontSize: 38, letterSpacing: '0.15em', textAlign: 'center' }}>
          06<br/><span style={{ fontSize: 11 }}>{glitch ? 'CIVIL ████' : 'CIVIL RELAY'}</span>
        </div>
      </div>
      <div style={{ marginTop: 28, fontSize: 22, letterSpacing: '0.2em', opacity: 0.7 }}>
        {glitch ? 'SEAL :: MISMATCH' : 'SEAL :: VERIFIED'}
      </div>
    </div>
  );
}

function WordFlicker({ pairs }) {
  const { localTime } = useSprite();
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#040505',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      flexDirection: 'column', gap: 50,
      fontFamily: 'VT323, monospace', color: '#e6e8d4',
    }}>
      {pairs.map(([a, b], i) => {
        const flick = Math.floor((localTime + i * 0.5) * 2.5) % 5 === 0;
        return (
          <div key={i} style={{
            fontSize: 50, letterSpacing: '0.18em',
            color: flick ? '#e85a3a' : '#e6e8d4',
            textShadow: '0 0 8px currentColor',
          }}>
            {flick ? b : a}
          </div>
        );
      })}
    </div>
  );
}

function SecondVoiceScreen() {
  const { localTime } = useSprite();
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#020203',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      flexDirection: 'column',
      fontFamily: 'VT323, monospace', color: '#d4d8c8',
    }}>
      <div style={{ fontSize: 18, letterSpacing: '0.32em', opacity: 0.6, marginBottom: 28 }}>
        — AUDIO :: TWO SOURCES DETECTED —
      </div>
      {/* Two waveforms */}
      <Waveform y={0} amp={1} hue="#d4d8c8" t={localTime} freq={3} />
      <Waveform y={50} amp={0.6} hue="#e85a3a" t={localTime} freq={5.7} offset={1.3} />
      <div style={{ marginTop: 60, fontSize: 14, letterSpacing: '0.24em', opacity: 0.45, lineHeight: 1.7, textAlign: 'center' }}>
        SRC A: NARRATOR · CONFIDENCE 41%<br/>
        SRC B: <span style={{ color: '#e85a3a' }}>UNIDENTIFIED · CONFIDENCE — — %</span>
      </div>
    </div>
  );
}

function Waveform({ amp, hue, t, freq, offset = 0, y = 0 }) {
  const points = [];
  for (let i = 0; i <= 80; i++) {
    const x = i / 80;
    const phase = i * freq * 0.18 + t * 4 + offset;
    const yv = Math.sin(phase) * amp + Math.sin(phase * 2.1) * 0.3 * amp;
    points.push(`${x * 600},${30 + yv * 22}`);
  }
  return (
    <svg width="600" height="60" style={{ marginTop: y }}>
      <polyline points={points.join(' ')} fill="none" stroke={hue} strokeWidth="1.5" />
    </svg>
  );
}

function ErrorStack() {
  const { localTime } = useSprite();
  const lines = [
    'SOURCE MISMATCH',
    'SIGNATURE INVALID',
    'RELAY DOES NOT MATCH COUNTY AUTHORITY',
    'CHECKSUM :: ████████',
    'PATH :: ████ → ████ → ████',
  ];
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#040505',
      padding: '100px 80px',
      fontFamily: 'VT323, monospace',
      color: '#e85a3a',
      fontSize: 30, letterSpacing: '0.12em',
      lineHeight: 1.7,
    }}>
      {lines.map((line, i) => {
        if (localTime < i * 1.2) return null;
        return (
          <div key={i} style={{
            textShadow: '0 0 6px rgba(232,90,58,0.4)',
          }}>
            ! {line}
          </div>
        );
      })}
    </div>
  );
}

function NotCivilMessage() {
  const { localTime } = useSprite();
  const flick = Math.floor(localTime * 3) % 2 === 0;
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: flick ? '#1a0505' : '#000',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      flexDirection: 'column',
      fontFamily: 'VT323, monospace', color: '#e85a3a',
    }}>
      <div style={{ fontSize: 56, letterSpacing: '0.18em', textAlign: 'center', textShadow: '0 0 14px rgba(232,90,58,0.6)' }}>
        THIS IS NOT<br/>A CIVIL MESSAGE
      </div>
    </div>
  );
}

function BlackBlink({ times }) {
  const t = useTime();
  const dur = 0.14;
  const active = times.some(tm => t >= tm && t < tm + dur);
  if (!active) return null;
  return (
    <div style={{
      position: 'absolute', inset: 0,
      background: '#000',
      pointerEvents: 'none',
      zIndex: 50,
    }} />
  );
}

window.Section4 = Section4;
window.Section5 = Section5;
