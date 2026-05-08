
    // ── GATE ──────────────────────────────────────────────────────────────────
    var GAME_KEY = 'openfeed_game';
    var GAME = { chapter:1, extraFeeds:[], choices:{}, puzzlesSolved:[], path:null, elevatorClue:false };

    (function() {
      try {
        var raw = localStorage.getItem(GAME_KEY);
        if (raw) { var s = JSON.parse(raw); for (var k in s) GAME[k] = s[k]; }
      } catch(e) {}
      if (!GAME.elevatorClue || GAME.puzzlesSolved.indexOf('puzzle_01') < 0) {
        window.location.replace('index.html');
      }
      if (GAME.forumVisited) {
        var fl = document.getElementById('navForumLink');
        if (fl) fl.style.display = '';
      }
    })();

    function gameSave() {
      try { localStorage.setItem(GAME_KEY, JSON.stringify(GAME)); } catch(e) {}
    }
    function gameSolvePuzzle(id) {
      if (GAME.puzzlesSolved.indexOf(id) < 0) GAME.puzzlesSolved.push(id);
      gameSave();
    }
    function unlockExtraFeed(id) {
      if (GAME.extraFeeds.indexOf(id) < 0) GAME.extraFeeds.push(id);
      gameSave();
    }

    // ── AUDIO: STATIC ─────────────────────────────────────────────────────────
    var _ac = null;
    function getAC() {
      if (!_ac) try { _ac = new (window.AudioContext || window.webkitAudioContext)(); } catch(e) {}
      if (_ac && _ac.state === 'suspended') try { _ac.resume(); } catch(e) {}
      return _ac;
    }

    var _staticNode = null, _staticGain = null, _staticRunning = false;

    function startStatic() {
      if (_staticRunning) return;
      var ctx = getAC(); if (!ctx) return;
      _staticRunning = true;
      var bufLen = ctx.sampleRate * 2;
      var buf = ctx.createBuffer(1, bufLen, ctx.sampleRate);
      var d = buf.getChannelData(0);
      for (var i = 0; i < bufLen; i++) d[i] = Math.random() * 2 - 1;
      _staticNode = ctx.createBufferSource();
      _staticNode.buffer = buf;
      _staticNode.loop = true;
      var bpf = ctx.createBiquadFilter();
      bpf.type = 'bandpass';
      bpf.frequency.value = 1200;
      bpf.Q.value = 0.6;
      _staticGain = ctx.createGain();
      _staticGain.gain.value = 0;
      _staticNode.connect(bpf);
      bpf.connect(_staticGain);
      _staticGain.connect(ctx.destination);
      _staticNode.start();
    }

    function setStaticVolume(vol) {
      if (!_staticGain) return;
      _staticGain.gain.setTargetAtTime(vol, getAC().currentTime, 0.08);
    }

    function stopStatic() {
      _staticRunning = false;
      if (_staticNode) { try { _staticNode.stop(); } catch(e) {} _staticNode = null; }
      _staticGain = null;
    }

    function playSfxLock() {
      var ctx = getAC(); if (!ctx) return;
      [523, 659, 784].forEach(function(f, i) {
        var o = ctx.createOscillator(), g = ctx.createGain();
        o.connect(g); g.connect(ctx.destination);
        o.frequency.value = f; o.type = 'sine';
        var t = ctx.currentTime + i * 0.13;
        g.gain.setValueAtTime(0, t);
        g.gain.linearRampToValueAtTime(0.18, t + 0.04);
        g.gain.exponentialRampToValueAtTime(0.001, t + 0.45);
        o.start(t); o.stop(t + 0.46);
      });
    }

    function playClueAudio() {
      var a = new Audio('message2.mp3');
      a.play().catch(function() {});
    }

    // ── AUDIO: MORSE ──────────────────────────────────────────────────────────
    var MORSE_UNIT = 120; // ms per dot-length

    function buildMorseSeq(text) {
      var CODE = {
        A:'.-',  B:'-...', C:'-.-.', D:'-..', E:'.', F:'..-.',
        G:'--.', H:'....', I:'..', J:'.---', K:'-.-', L:'.-..',
        M:'--',  N:'-.',   O:'---', P:'.--.', Q:'--.-', R:'.-.',
        S:'...', T:'-',    U:'..-', V:'...-', W:'.--', X:'-..-',
        Y:'-.--',Z:'--..'
      };
      var seq = [];
      for (var i = 0; i < text.length; i++) {
        if (i > 0) seq.push([MORSE_UNIT * 3, false]); // letter gap
        var code = CODE[text[i].toUpperCase()];
        if (!code) continue;
        for (var j = 0; j < code.length; j++) {
          if (j > 0) seq.push([MORSE_UNIT, false]); // element gap
          seq.push([code[j] === '-' ? MORSE_UNIT * 3 : MORSE_UNIT, true]);
        }
      }
      seq.push([MORSE_UNIT * 7, false]); // word gap at end
      return seq;
    }

    var morseSeq = buildMorseSeq('OPEN');
    var _morseOsc = null, _morseGain = null, _morseActive = false, _morseLoopTimeout = null;

    function ensureMorseNodes() {
      var ctx = getAC(); if (!ctx) return false;
      if (!_morseOsc) {
        _morseGain = ctx.createGain();
        _morseGain.gain.value = 0;
        _morseGain.connect(ctx.destination);
        _morseOsc = ctx.createOscillator();
        _morseOsc.frequency.value = 750;
        _morseOsc.type = 'sine';
        _morseOsc.connect(_morseGain);
        _morseOsc.start();
      }
      return true;
    }

    function scheduleMorseLoop() {
      if (!_morseActive) return;
      var ctx = getAC(); if (!ctx) return;
      if (!ensureMorseNodes()) return;
      var t = ctx.currentTime + 0.02;
      var totalMs = 0;
      for (var mi = 0; mi < morseSeq.length; mi++) {
        var durS = morseSeq[mi][0] / 1000;
        var isOn = morseSeq[mi][1];
        if (isOn) {
          _morseGain.gain.setValueAtTime(0.13, t);
          t += durS;
          _morseGain.gain.setValueAtTime(0, t);
        } else {
          t += durS;
        }
        totalMs += morseSeq[mi][0];
      }
      _morseLoopTimeout = setTimeout(scheduleMorseLoop, totalMs + 300);
    }

    function startMorse() {
      if (_morseActive) return;
      _morseActive = true;
      if (!ensureMorseNodes()) { _morseActive = false; return; }
      scheduleMorseLoop();
    }

    function stopMorse() {
      if (!_morseActive) return;
      _morseActive = false;
      clearTimeout(_morseLoopTimeout);
      _morseLoopTimeout = null;
      if (_morseGain && _ac) {
        try {
          _morseGain.gain.cancelScheduledValues(_ac.currentTime);
          _morseGain.gain.setValueAtTime(0, _ac.currentTime);
        } catch(e) {}
      }
    }

    // ── DIAL STATE ────────────────────────────────────────────────────────────
    var TARGET_DEG = 317;
    var LOCK_RANGE = 5;   // degrees — must be within this to start hold timer
    var MORSE_ZONE = 10;  // degrees — morse starts within this range
    var DRIFT_ZONE = 20;  // degrees — dial fights back within this range
    var DRIFT_MAX  = 0.11; // max degrees of nudge per tick (50ms)
    var HOLD_MS    = 7000; // ms the player must hold to trigger lock

    var currentDeg = 0;
    var isLocked   = false;
    var _holdStart = null;

    var canvas = document.getElementById('dialCanvas');
    var ctx2d  = canvas.getContext('2d');
    var CX = 100, CY = 100, RADIUS = 88;

    // Build signal bars
    var BAR_COUNT = 8;
    var barsEl = document.getElementById('signalBars');
    for (var bi = 0; bi < BAR_COUNT; bi++) {
      var bar = document.createElement('div');
      bar.className = 'signal-bar';
      bar.style.height = (30 + (bi / (BAR_COUNT - 1)) * 70) + '%';
      barsEl.appendChild(bar);
    }

    function repeatStr(s, n) {
      var r = '';
      for (var ri = 0; ri < n; ri++) r += s;
      return r;
    }

    function updateSignalBars(dist) {
      var strength = isLocked ? 1 : Math.max(0, 1 - Math.pow(dist / 40, 0.7));
      var active = Math.round(strength * BAR_COUNT);
      var bars = barsEl.children;
      for (var i = 0; i < BAR_COUNT; i++) {
        bars[i].classList.remove('active', 'peak');
        if (i < active) bars[i].classList.add(isLocked ? 'peak' : 'active');
      }
      var lbl = document.getElementById('signalLabel');
      lbl.classList.toggle('locked', isLocked);
      lbl.textContent = isLocked ? 'LOCKED' : 'no lock';
    }

    function drawDial(deg) {
      var c = ctx2d;
      c.clearRect(0, 0, 200, 200);

      c.beginPath();
      c.arc(CX, CY, RADIUS + 8, 0, Math.PI * 2);
      c.strokeStyle = '#333';
      c.lineWidth = 2;
      c.stroke();

      for (var tk = 0; tk < 360; tk += 5) {
        var isMajor = tk % 45 === 0;
        var isMed   = tk % 15 === 0;
        var len     = isMajor ? 12 : isMed ? 7 : 4;
        var rad     = (tk - 90) * Math.PI / 180;
        var r0 = RADIUS + 7;
        c.beginPath();
        c.moveTo(CX + r0 * Math.cos(rad), CY + r0 * Math.sin(rad));
        c.lineTo(CX + (r0 - len) * Math.cos(rad), CY + (r0 - len) * Math.sin(rad));
        c.strokeStyle = isMajor ? '#555' : isMed ? '#333' : '#222';
        c.lineWidth = isMajor ? 1.5 : 1;
        c.stroke();
      }

      [0, 45, 90, 135, 180, 225, 270, 315].forEach(function(lv) {
        var rad = (lv - 90) * Math.PI / 180;
        var lr  = RADIUS - 18;
        c.fillStyle = '#3a3a3a';
        c.font = '8px monospace';
        c.textAlign = 'center';
        c.textBaseline = 'middle';
        c.fillText(lv, CX + lr * Math.cos(rad), CY + lr * Math.sin(rad));
      });

      c.beginPath();
      c.arc(CX, CY, RADIUS - 4, 0, Math.PI * 2);
      c.fillStyle = '#181818';
      c.fill();
      c.strokeStyle = '#2a2a2a';
      c.lineWidth = 1;
      c.stroke();

      for (var gv = 0; gv < 16; gv++) {
        var ga = ((deg + gv * 22.5) - 90) * Math.PI / 180;
        c.beginPath();
        c.moveTo(CX + 10 * Math.cos(ga), CY + 10 * Math.sin(ga));
        c.lineTo(CX + (RADIUS - 6) * Math.cos(ga), CY + (RADIUS - 6) * Math.sin(ga));
        c.strokeStyle = '#1e1e1e';
        c.lineWidth = 1;
        c.stroke();
      }

      var pdist = Math.abs(angleDist(deg, TARGET_DEG));
      var pColor = isLocked ? '#6aaa4a' : pdist <= LOCK_RANGE ? '#4a7a3a' : pdist < 15 ? '#3a6a2a' : '#3a5a2a';
      var pRad = (deg - 90) * Math.PI / 180;
      c.beginPath();
      c.moveTo(CX, CY);
      c.lineTo(CX + (RADIUS - 6) * Math.cos(pRad), CY + (RADIUS - 6) * Math.sin(pRad));
      c.strokeStyle = pColor;
      c.lineWidth = 2.5;
      c.stroke();

      c.beginPath();
      c.arc(CX + (RADIUS - 6) * Math.cos(pRad), CY + (RADIUS - 6) * Math.sin(pRad), 3.5, 0, Math.PI * 2);
      c.fillStyle = pColor;
      c.fill();

      c.beginPath();
      c.arc(CX, CY, 10, 0, Math.PI * 2);
      c.fillStyle = '#111';
      c.fill();
      c.strokeStyle = '#2a2a2a';
      c.lineWidth = 1;
      c.stroke();
    }

    function angleDist(a, b) {
      var d = ((b - a) % 360 + 360) % 360;
      return d > 180 ? d - 360 : d;
    }

    function applyDeg(deg) {
      currentDeg = ((deg % 360) + 360) % 360;
      drawDial(currentDeg);
      document.getElementById('degDisplay').textContent = String(Math.round(currentDeg)).padStart(3, '0');
      var dist = Math.abs(angleDist(currentDeg, TARGET_DEG));
      updateSignalBars(dist);
      setStaticVolume(dist <= LOCK_RANGE ? 0.02 : Math.min(0.22, 0.04 + (dist / 60) * 0.18));
      if (!isLocked && _holdStart === null) updateHint(dist);
      // Lock is triggered only by hold timer in driftAndHoldTick, not here
    }

    function updateHint(dist) {
      var box = document.getElementById('hintBox');
      if (dist <= LOCK_RANGE) {
        box.innerHTML =
          '<span class="h-dim">&gt; signal acquired. <span class="h-key">maintain position.</span></span><br>' +
          '<span class="h-dim">&gt; hold steady &mdash; do not release.</span>';
      } else if (dist <= MORSE_ZONE) {
        box.innerHTML =
          '<span class="h-dim">&gt; strong signal on this band. something is transmitting.</span><br>' +
          '<span style="color:#2a5a2a">&gt; listen carefully &mdash; can you read what it is sending?</span><br>' +
          '<span class="h-dim">&gt; need a reference? check the <a href="forum.html" style="color:#3a7a3a;text-decoration:underline">[board]</a>.</span>';
      } else if (dist <= DRIFT_ZONE) {
        box.innerHTML =
          '<span class="h-dim">&gt; signal detected. getting closer.</span><br>' +
          '<span style="color:#2a5a2a">&gt; fine-tune the dial &mdash; almost there.</span><br>' +
          '<span class="h-dim">&gt; target: <span class="h-key">close</span></span>';
      } else if (dist <= 60) {
        box.innerHTML =
          '<span class="h-dim">&gt; weak signal trace on this band.</span><br>' +
          '<span class="h-dim">&gt; continue rotating.</span><br>' +
          '<span class="h-dim">&gt; target: <span class="h-key">unknown</span></span>';
      } else {
        box.innerHTML =
          '<span class="h-dim">&gt; receiver ready. awaiting input.</span><br>' +
          '<span class="h-dim">&gt; rotate dial to find target frequency.</span><br>' +
          '<span class="h-dim">&gt; target: <span class="h-key">unknown</span></span>';
      }
    }


    // ── DRIFT + HOLD TICK (runs every 50ms) ───────────────────────────────────
    var dragging = false, dragStartAngle = 0, dragStartDeg = 0;

    function driftAndHoldTick() {
      if (isLocked) return;

      var dist = Math.abs(angleDist(currentDeg, TARGET_DEG));
      var moved = false;

      // ── Drift: dial fights back within DRIFT_ZONE ──
      if (dist < DRIFT_ZONE) {
        var forceMag = DRIFT_MAX * Math.pow(1 - dist / DRIFT_ZONE, 1.2);
        if (Math.random() < 0.08) forceMag *= (1.2 + Math.random() * 0.9); // occasional jerk
        var signedDist = angleDist(currentDeg, TARGET_DEG); // positive = target is clockwise-ahead
        var direction  = signedDist > 0 ? -1 : 1; // push opposite direction (away from target)
        var delta = direction * forceMag;

        currentDeg = ((currentDeg + delta) % 360 + 360) % 360;
        // Also shift drag baseline so drift fights through active dragging
        if (dragging) dragStartDeg = ((dragStartDeg + delta) % 360 + 360) % 360;

        dist = Math.abs(angleDist(currentDeg, TARGET_DEG));
        moved = true;
      }

      if (moved) {
        drawDial(currentDeg);
        document.getElementById('degDisplay').textContent = String(Math.round(currentDeg)).padStart(3, '0');
        updateSignalBars(dist);
        setStaticVolume(dist <= LOCK_RANGE ? 0.02 : Math.min(0.22, 0.04 + (dist / 60) * 0.18));
        if (_holdStart === null) updateHint(dist);
      }

      // ── Morse audio: on within MORSE_ZONE ──
      if (dist <= MORSE_ZONE) { startMorse(); } else { stopMorse(); }

      // ── Hold timer: must stay within LOCK_RANGE for HOLD_MS to trigger lock ──
      if (dist <= LOCK_RANGE) {
        var now = Date.now();
        if (_holdStart === null) _holdStart = now;
        var elapsed = now - _holdStart;
        var progress = Math.min(1, elapsed / HOLD_MS);
        var filled = Math.round(progress * 18);
        document.getElementById('hintBox').innerHTML =
          '&gt; signal acquired. <span class="h-key">hold position.</span><br>' +
          '&gt; [' + repeatStr('=', filled) + repeatStr('&nbsp;', 18 - filled) + '] ' +
          Math.round(progress * 100) + '%<br>' +
          '&gt; <span style="color:#4a7a4a">hold steady &mdash; do not release</span>';
        if (elapsed >= HOLD_MS) triggerLock();
      } else {
        if (_holdStart !== null) { _holdStart = null; updateHint(dist); }
      }
    }

    setInterval(driftAndHoldTick, 50);

    function triggerLock() {
      isLocked = true;
      stopStatic();
      stopMorse();
      playSfxLock();
      canvas.classList.add('locked-state');
      document.getElementById('degDisplay').classList.add('locked');
      document.getElementById('navStatus').textContent = 'LOCKED :: 317°';

      updateSignalBars(0);

      gameSolvePuzzle('puzzle_02');
      unlockExtraFeed('E02');

      var panel = document.getElementById('successPanel');
      panel.style.display = 'block';
      document.getElementById('successBody').innerHTML =
        'frequency <b>317&deg;</b> confirmed. relay signal acquired. audio transmission decoding&hellip; ' +
        '<span style="color:#555;font-size:9px">feed E02 has been added to your index.</span>';

      document.getElementById('hintBox').innerHTML =
        '<span style="color:#3a6a3a">&gt; <span class="h-key">SIGNAL LOCKED</span> at 317&#176;</span><br>' +
        '<span class="h-dim">&gt; audio feed active. listen carefully.</span>';

      setTimeout(playClueAudio, 600);
    }

    // ── INPUT ─────────────────────────────────────────────────────────────────
    function pointerAngle(e) {
      var rect = canvas.getBoundingClientRect();
      var src  = e.touches ? e.touches[0] : e;
      return Math.atan2(src.clientY - rect.top - CY, src.clientX - rect.left - CX) * 180 / Math.PI + 90;
    }

    canvas.addEventListener('mousedown', function(e) {
      if (isLocked) return;
      startStatic(); dragging = true;
      dragStartAngle = pointerAngle(e); dragStartDeg = currentDeg;
      e.preventDefault();
    });
    window.addEventListener('mousemove', function(e) {
      if (!dragging || isLocked) return;
      applyDeg(dragStartDeg + pointerAngle(e) - dragStartAngle);
    });
    window.addEventListener('mouseup', function() { dragging = false; });

    canvas.addEventListener('wheel', function(e) {
      if (isLocked) return;
      startStatic(); applyDeg(currentDeg + (e.deltaY > 0 ? 1 : -1));
      e.preventDefault();
    }, { passive: false });

    document.addEventListener('keydown', function(e) {
      if (isLocked) return;
      if (e.key === 'ArrowRight' || e.key === 'ArrowUp')   { startStatic(); applyDeg(currentDeg + 1); e.preventDefault(); }
      if (e.key === 'ArrowLeft'  || e.key === 'ArrowDown') { startStatic(); applyDeg(currentDeg - 1); e.preventDefault(); }
    });

    canvas.addEventListener('touchstart', function(e) {
      if (isLocked) return;
      startStatic(); dragging = true;
      dragStartAngle = pointerAngle(e); dragStartDeg = currentDeg;
      e.preventDefault();
    }, { passive: false });
    canvas.addEventListener('touchmove', function(e) {
      if (!dragging || isLocked) return;
      applyDeg(dragStartDeg + pointerAngle(e) - dragStartAngle);
      e.preventDefault();
    }, { passive: false });
    canvas.addEventListener('touchend', function() { dragging = false; });

    function nudgeDeg(d) { if (isLocked) return; startStatic(); applyDeg(currentDeg + d); }
    function setDeg(d)   { if (isLocked) return; startStatic(); applyDeg(d); }

    // ── INIT ──────────────────────────────────────────────────────────────────
    if (GAME.puzzlesSolved.indexOf('puzzle_02') >= 0) {
      currentDeg = TARGET_DEG;
      isLocked = true;
      drawDial(currentDeg);
      document.getElementById('degDisplay').textContent = '317';
      document.getElementById('degDisplay').classList.add('locked');
      canvas.classList.add('locked-state');
      updateSignalBars(0);
      document.getElementById('navStatus').textContent = 'LOCKED :: 317°';
      document.getElementById('successPanel').style.display = 'block';
      document.getElementById('successBody').innerHTML =
        'frequency <b>317&deg;</b> confirmed. feed E02 already unlocked.';
      document.getElementById('hintBox').innerHTML =
        '<span style="color:#3a6a3a">&gt; <span class="h-key">SIGNAL LOCKED</span> at 317&#176;</span><br>' +
        '<span class="h-dim">&gt; puzzle already solved.</span>';
    } else {
      applyDeg(0);
    }
  