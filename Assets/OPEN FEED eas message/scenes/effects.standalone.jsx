// CRT/VHS overlay shader and audio engine

function CRTOverlay() {
  const t = useTime();
  // Drifting tracking band
  const bandY = ((t * 18) % 130) - 15;
  // Occasional tear
  const tearActive = Math.sin(t * 0.7) > 0.94 && t < 325;
  // Cheery section dampens the whole overlay
  const cheery = t >= 325;
  const k = cheery ? 0.35 : 1;
  return (
    <>
      {/* Scanlines */}
      <div style={{
        position: 'absolute', inset: 0,
        pointerEvents: 'none',
        background: `repeating-linear-gradient(0deg, rgba(0,0,0,${0.30*k}) 0 1px, transparent 1px 3px)`,
        mixBlendMode: 'multiply',
        zIndex: 60,
      }} />
      {/* Aperture grille */}
      <div style={{
        position: 'absolute', inset: 0,
        pointerEvents: 'none',
        background: `repeating-linear-gradient(90deg, rgba(255,80,80,${0.04*k}) 0 1px, rgba(80,255,80,${0.04*k}) 1px 2px, rgba(80,80,255,${0.04*k}) 2px 3px)`,
        mixBlendMode: 'screen',
        zIndex: 60,
      }} />
      {/* Vignette */}
      <div style={{
        position: 'absolute', inset: 0,
        pointerEvents: 'none',
        background: `radial-gradient(ellipse at 50% 50%, transparent 50%, rgba(0,0,0,${0.55*k}) 100%)`,
        zIndex: 61,
      }} />
      {/* Curvature lensing — extra soft edges */}
      <div style={{
        position: 'absolute', inset: 0,
        pointerEvents: 'none',
        boxShadow: `inset 0 0 100px rgba(0,0,0,${0.5*k}), inset 0 0 200px rgba(0,0,0,${0.4*k})`,
        zIndex: 62,
      }} />
      {/* Tracking band */}
      <div style={{
        position: 'absolute', left: 0, right: 0,
        top: `${bandY}%`,
        height: 24,
        background: `linear-gradient(180deg, transparent 0%, rgba(255,255,255,${0.10*k}) 40%, rgba(255,255,255,${0.18*k}) 50%, rgba(255,255,255,${0.10*k}) 60%, transparent 100%)`,
        mixBlendMode: 'screen',
        pointerEvents: 'none',
        zIndex: 63,
      }} />
      {/* Static noise */}
      {!cheery && <NoiseLayer t={t} />}
      {/* Tear */}
      {tearActive && (
        <div style={{
          position: 'absolute',
          left: 0, right: 0,
          top: `${30 + Math.sin(t * 31) * 30}%`,
          height: 4,
          background: '#fff',
          mixBlendMode: 'difference',
          opacity: 0.5,
          zIndex: 64,
        }} />
      )}
      {/* Color fringe / chromatic */}
      <div style={{
        position: 'absolute', inset: 0,
        pointerEvents: 'none',
        background: `linear-gradient(90deg, rgba(232,90,58,${0.04*k}), transparent 20%, transparent 80%, rgba(58,180,232,${0.04*k}))`,
        mixBlendMode: 'screen',
        zIndex: 60,
      }} />
    </>
  );
}

function NoiseLayer({ t }) {
  // CSS noise via radial gradients animated by time
  const seed = Math.floor(t * 30);
  const dots = React.useMemo(() => {
    const out = [];
    for (let i = 0; i < 80; i++) {
      const x = ((seed * 13 + i * 47) % 100);
      const y = ((seed * 31 + i * 71) % 100);
      const a = ((seed * 7 + i * 17) % 10) / 10;
      out.push({ x, y, a });
    }
    return out;
  }, [seed]);
  return (
    <div style={{ position: 'absolute', inset: 0, pointerEvents: 'none', zIndex: 63, opacity: 0.18 }}>
      {dots.map((d, i) => (
        <div key={i} style={{
          position: 'absolute',
          left: `${d.x}%`, top: `${d.y}%`,
          width: 2, height: 2,
          background: '#fff', opacity: d.a,
        }} />
      ))}
    </div>
  );
}

// ──────────────────────────────────────────────────────────────────────────
// AUDIO — generative low broadcast hum, fluorescent buzz, tone, dropouts
// All synthesized with WebAudio. No external assets.

function useBroadcastAudio() {
  const ctxRef = React.useRef(null);
  const nodesRef = React.useRef({});
  const startedRef = React.useRef(false);
  const easBufRef = React.useRef(null);
  const easPlayingRef = React.useRef(false);
  const easSrcRef = React.useRef(null);
  const inkAudioRef = React.useRef(null);
  const inkGainRef = React.useRef(null);
  const inkStartedRef = React.useRef(false);
  const clickBufRef = React.useRef(null);
  const lastClickKeyRef = React.useRef(null);

  const ensureCtx = React.useCallback(() => {
    if (ctxRef.current) return ctxRef.current;
    const Ctx = window.AudioContext || window.webkitAudioContext;
    if (!Ctx) return null;
    const ctx = new Ctx();
    ctxRef.current = ctx;
    return ctx;
  }, []);

  const start = React.useCallback(async () => {
    const ctx = ensureCtx();
    if (!ctx) return;
    if (ctx.state === 'suspended') ctx.resume();
    if (startedRef.current) return;
    startedRef.current = true;

    // Master
    const master = ctx.createGain();
    master.gain.value = 0.55;
    master.connect(ctx.destination);

    // 60Hz broadcast hum
    const humOsc = ctx.createOscillator();
    humOsc.type = 'sine';
    humOsc.frequency.value = 60;
    const humGain = ctx.createGain();
    humGain.gain.value = 0.06;
    humOsc.connect(humGain).connect(master);
    humOsc.start();

    // 120Hz harmonic
    const humOsc2 = ctx.createOscillator();
    humOsc2.type = 'sine';
    humOsc2.frequency.value = 120;
    const humGain2 = ctx.createGain();
    humGain2.gain.value = 0.025;
    humOsc2.connect(humGain2).connect(master);
    humOsc2.start();

    // Fluorescent buzz — slightly noisy 100Hz
    const buzzBuf = ctx.createBuffer(1, ctx.sampleRate * 2, ctx.sampleRate);
    const buzzData = buzzBuf.getChannelData(0);
    for (let i = 0; i < buzzData.length; i++) {
      buzzData[i] = (Math.random() * 2 - 1) * 0.3;
    }
    const buzzSrc = ctx.createBufferSource();
    buzzSrc.buffer = buzzBuf;
    buzzSrc.loop = true;
    const buzzFilter = ctx.createBiquadFilter();
    buzzFilter.type = 'bandpass';
    buzzFilter.frequency.value = 2400;
    buzzFilter.Q.value = 8;
    const buzzGain = ctx.createGain();
    buzzGain.gain.value = 0.03;
    buzzSrc.connect(buzzFilter).connect(buzzGain).connect(master);
    buzzSrc.start();

    // Tape hiss — pink-ish noise
    const hissBuf = ctx.createBuffer(1, ctx.sampleRate * 3, ctx.sampleRate);
    const hissData = hissBuf.getChannelData(0);
    let last = 0;
    for (let i = 0; i < hissData.length; i++) {
      const white = Math.random() * 2 - 1;
      last = (last + 0.02 * white) / 1.02;
      hissData[i] = last * 3;
    }
    const hissSrc = ctx.createBufferSource();
    hissSrc.buffer = hissBuf;
    hissSrc.loop = true;
    const hissFilter = ctx.createBiquadFilter();
    hissFilter.type = 'highpass';
    hissFilter.frequency.value = 600;
    const hissGain = ctx.createGain();
    hissGain.gain.value = 0.08;
    hissSrc.connect(hissFilter).connect(hissGain).connect(master);
    hissSrc.start();

    // Emergency tone — 853Hz / 960Hz alternating (used briefly in S0/S5)
    const toneOsc = ctx.createOscillator();
    toneOsc.type = 'sine';
    toneOsc.frequency.value = 853;
    const toneGain = ctx.createGain();
    toneGain.gain.value = 0;
    toneOsc.connect(toneGain).connect(master);
    toneOsc.start();

    // Narration "carrier" — low square chord (synthetic narration impression)
    const narrOsc = ctx.createOscillator();
    narrOsc.type = 'sawtooth';
    narrOsc.frequency.value = 110;
    const narrFilter = ctx.createBiquadFilter();
    narrFilter.type = 'lowpass';
    narrFilter.frequency.value = 600;
    narrFilter.Q.value = 4;
    const narrGain = ctx.createGain();
    narrGain.gain.value = 0;
    narrOsc.connect(narrFilter).connect(narrGain).connect(master);
    narrOsc.start();

    // Whisper / second voice — separate filtered noise we can mix in S5
    const whispBuf = ctx.createBuffer(1, ctx.sampleRate * 2, ctx.sampleRate);
    const wd = whispBuf.getChannelData(0);
    for (let i = 0; i < wd.length; i++) wd[i] = (Math.random() * 2 - 1);
    const whispSrc = ctx.createBufferSource();
    whispSrc.buffer = whispBuf; whispSrc.loop = true;
    const whispFilt = ctx.createBiquadFilter();
    whispFilt.type = 'bandpass';
    whispFilt.frequency.value = 1100; whispFilt.Q.value = 6;
    const whispGain = ctx.createGain();
    whispGain.gain.value = 0;
    whispSrc.connect(whispFilt).connect(whispGain).connect(master);
    whispSrc.start();

    nodesRef.current = {
      ctx, master, humGain, humGain2, buzzGain, hissGain,
      toneOsc, toneGain,
      narrOsc, narrGain, narrFilter,
      whispGain,
    };

    // Pre-load EAS tone sample
    try {
      const resp = await fetch(window.__resources.easTone);
      const arr = await resp.arrayBuffer();
      const buf = await ctx.decodeAudioData(arr);
      easBufRef.current = buf;
    } catch (e) {
      console.warn('EAS tone failed to load', e);
    }

    // Pre-load click sound
    try {
      const resp = await fetch(window.__resources.click);
      const arr = await resp.arrayBuffer();
      const buf = await ctx.decodeAudioData(arr);
      clickBufRef.current = buf;
    } catch (e) {
      console.warn('click failed to load', e);
    }

    // Pre-load Ink Spots track via HTMLAudioElement (long file, easier to seek)
    try {
      const audioEl = new Audio(window.__resources.inkspots);
      audioEl.preload = 'auto';
      audioEl.crossOrigin = 'anonymous';
      audioEl.loop = false;
      const src = ctx.createMediaElementSource(audioEl);
      // Vintage 78rpm vibe: lowpass + slight bandpass narrowing + low gain
      const lp = ctx.createBiquadFilter();
      lp.type = 'lowpass'; lp.frequency.value = 3400; lp.Q.value = 0.7;
      const hp = ctx.createBiquadFilter();
      hp.type = 'highpass'; hp.frequency.value = 220;
      const inkGain = ctx.createGain();
      inkGain.gain.value = 0;
      src.connect(hp).connect(lp).connect(inkGain).connect(master);
      inkAudioRef.current = audioEl;
      inkGainRef.current = inkGain;
    } catch (e) {
      console.warn('Ink Spots track failed to load', e);
    }
  }, [ensureCtx]);

  const playEAS = React.useCallback(() => {
    const ctx = ctxRef.current;
    const buf = easBufRef.current;
    const n = nodesRef.current;
    if (!ctx || !buf || !n.master || easPlayingRef.current) return;
    easPlayingRef.current = true;
    const src = ctx.createBufferSource();
    src.buffer = buf;
    const gain = ctx.createGain();
    gain.gain.value = 0.6;
    src.connect(gain).connect(n.master);
    src.start();
    src.onended = () => { easPlayingRef.current = false; };
    easSrcRef.current = { src, gain };
  }, []);

  const stopEAS = React.useCallback(() => {
    const node = easSrcRef.current;
    const ctx = ctxRef.current;
    if (!node || !ctx) return;
    try {
      node.gain.gain.setTargetAtTime(0, ctx.currentTime, 0.15);
      setTimeout(() => { try { node.src.stop(); } catch {} }, 400);
    } catch {}
    easSrcRef.current = null;
    easPlayingRef.current = false;
  }, []);

  const playClick = React.useCallback(() => {
    const ctx = ctxRef.current;
    const buf = clickBufRef.current;
    const n = nodesRef.current;
    if (!ctx || !buf || !n.master) return;
    const src = ctx.createBufferSource();
    src.buffer = buf;
    const gain = ctx.createGain();
    gain.gain.value = 0.55;
    src.connect(gain).connect(n.master);
    try { src.start(); } catch {}
  }, []);

  // Per-frame mix update keyed to playhead
  const update = React.useCallback((time, playing) => {
    const n = nodesRef.current;
    if (!n.ctx) return;
    if (!playing) {
      // ramp down everything
      const now = n.ctx.currentTime;
      [n.humGain, n.humGain2, n.buzzGain, n.hissGain, n.toneGain, n.narrGain, n.whispGain].forEach(g => {
        if (g) g.gain.setTargetAtTime(0, now, 0.1);
      });
      stopEAS();
      // pause ink spots
      const ink = inkAudioRef.current;
      const inkG = inkGainRef.current;
      if (ink && inkG) {
        inkG.gain.setTargetAtTime(0, now, 0.1);
        try { ink.pause(); } catch {}
        inkStartedRef.current = false;
      }
      return;
    }
    const now = n.ctx.currentTime;

    // Click sound at each PSA card appearance (rescaled timeline)
    const clickTimes = [18, 24, 34, 43, 52, 61, 68];
    for (const ct of clickTimes) {
      if (time >= ct && time < ct + 0.15 && lastClickKeyRef.current !== ct) {
        lastClickKeyRef.current = ct;
        playClick();
        break;
      }
    }
    if (lastClickKeyRef.current !== null) {
      const k = lastClickKeyRef.current;
      if (time < k - 0.5 || time > k + 8) lastClickKeyRef.current = null;
    }

    // EAS tone window: 5-18s (station ident) AND 247-249.5s (cutoff color bars)
    const inEASWindow = (time >= 5 && time < 17.5) || (time >= 247 && time < 249.5);
    const exitedEAS = (time < 4.5) || (time > 18 && time < 247) || (time >= 250);
    if (inEASWindow && !easPlayingRef.current && easBufRef.current) {
      playEAS();
    } else if (exitedEAS && easPlayingRef.current) {
      stopEAS();
    }

    // Section-based mix
    let humL = 0.05, hissL = 0.07, buzzL = 0.025, toneL = 0, narrL = 0, whispL = 0;

    if (time < 5) { // SMPTE bars
      n.toneOsc.frequency.setTargetAtTime(1000, now, 0.05);
      toneL = 0.08; humL = 0.02; hissL = 0.04;
    } else if (time < 18) { // Station ident
      humL = 0.02; hissL = 0.03; toneL = 0;
    } else if (time < 75) { // Section 1
      narrL = 0.03; humL = 0.04; hissL = 0.07; buzzL = 0.04;
      const breath = 0.5 + 0.5 * Math.sin(time * 0.7);
      narrL = 0.025 + 0.02 * breath;
      n.narrFilter.frequency.setTargetAtTime(500 + 200 * breath, now, 0.1);
    } else if (time < 128) { // Section 2
      narrL = 0.022; humL = 0.06; hissL = 0.06;
      n.narrFilter.frequency.setTargetAtTime(420, now, 0.1);
    } else if (time < 164) { // Section 3 (pre-loading)
      narrL = 0.016; humL = 0.07; hissL = 0.05;
    } else if (time < 167) { // loading screen
      toneL = 0.02; humL = 0.05; hissL = 0.05;
      n.toneOsc.frequency.setTargetAtTime(380, now, 0.4);
    } else if (time < 209) { // Section 4
      humL = 0.05; hissL = 0.07; narrL = 0.02;
      buzzL = 0.05;
      n.narrFilter.frequency.setTargetAtTime(540, now, 0.1);
    } else if (time < 247) { // Section 5
      humL = 0.07; hissL = 0.10;
      whispL = 0.05;
      const pitch = 853 - (time - 209) * 13;
      n.toneOsc.frequency.setTargetAtTime(Math.max(120, pitch), now, 0.2);
      toneL = 0.015 + 0.01 * Math.sin(time * 3);
      narrL = 0.018 * (Math.sin(time * 5) > 0 ? 1 : 0);
    } else if (time < 282) { // Section 6
      if (time < 250) {
        humL = 0.02; hissL = 0.04; toneL = 0;
      } else {
        humL = 0.005; hissL = 0.012; narrL = 0;
        whispL = 0;
      }
    } else { // Section 7
      humL = 0.02; hissL = 0.03;
      if (time > 295) { humL = 0.005; hissL = 0.01; }
      if (time > 299) { humL = 0; hissL = 0; }
    }

    // Ink Spots playback: 250s onward
    const ink = inkAudioRef.current;
    const inkG = inkGainRef.current;
    if (ink && inkG) {
      if (time >= 250 && time < 299) {
        if (!inkStartedRef.current) {
          inkStartedRef.current = true;
          try { ink.currentTime = Math.max(0, time - 250); } catch {}
          ink.play().catch(() => {});
        }
        let vol = 0.7;
        if (time < 251) vol = 0.7 * (time - 250);
        if (time > 295) vol = 0.7 * Math.max(0, 1 - (time - 295) / 4);
        inkG.gain.setTargetAtTime(vol, now, 0.2);
      } else if (inkStartedRef.current && (time < 249.5 || time >= 299)) {
        inkG.gain.setTargetAtTime(0, now, 0.3);
        try { ink.pause(); } catch {}
        inkStartedRef.current = false;
      }
    }

    // Apply
    n.humGain.gain.setTargetAtTime(humL, now, 0.15);
    n.humGain2.gain.setTargetAtTime(humL * 0.4, now, 0.15);
    n.buzzGain.gain.setTargetAtTime(buzzL, now, 0.15);
    n.hissGain.gain.setTargetAtTime(hissL, now, 0.15);
    n.toneGain.gain.setTargetAtTime(toneL, now, 0.15);
    n.narrGain.gain.setTargetAtTime(narrL, now, 0.1);
    n.whispGain.gain.setTargetAtTime(whispL, now, 0.2);
  }, []);

  return { start, update };
}

function AudioEngine() {
  const { time, playing } = useTimeline();
  const { start, update } = useBroadcastAudio();

  React.useEffect(() => {
    update(time, playing);
  }, [time, playing, update]);

  return (
    <button
      onClick={start}
      style={{
        position: 'absolute',
        bottom: 16, right: 16,
        zIndex: 100,
        background: 'rgba(20,20,20,0.85)',
        color: '#d4d8c8',
        border: '1px solid #3a3a3a',
        padding: '6px 12px',
        fontFamily: 'VT323, monospace',
        fontSize: 14, letterSpacing: '0.18em',
        cursor: 'pointer',
        borderRadius: 4,
      }}
    >
      ▶ ENABLE AUDIO
    </button>
  );
}

window.CRTOverlay = CRTOverlay;
window.AudioEngine = AudioEngine;
