(function () {
  "use strict";

  var UI = {
    brand: "OPEN FEED - Esitlus",
    prev: "Eelmine",
    next: "J\u00e4rgmine",
    exit: "V\u00e4lju",
    footer: "M\u00e4ngudisaini dokumentatsioon, mai 2026",
    slideLabel: "Slaid",
    dialogLabel: "OPEN FEED esitlus",
    enterLabel: "Ava esitlusre\u017eiim",
  };

  /** 15 slides (14 topics + thank you), Estonian presentation copy */
  var SLIDES = [
    {
      title: "Concept ja moodboard",
      deck: "Hiliste veebilehtede ja tühjade kohtade tunne, enne kui ühtki stseeni polnud.",
      bullets: [
        "First idea oli the old internet ja unsecured kaamerad. Oluline oli see, et atmosfääriliselt on mäng kõhedusttekitav, mitte otseselt jumpscarede peale üles ehitatud.",
        "Moodboard pani paika PS1-vibe'i, VHS-i, 90ndate veebi ja jälgituse tunnet.",
        "Flowchart aitas näha, kuidas menüü, pood, sõit, maja ja laud üheks ööks kokku saavad.",
      ],
      image: "progress%20screens/open%20feed%20moodboard_page-0001.jpg",
    },
    {
      title: "First Concept",
      bullets: [
        "Mängija liigub poest sõiduni, majja ja monitori juurde. OpenFeed veeb on seal päriselt osa loost.",
        "Hirm tuleb tihti vaikusest, keskkonnast, teadmatuset, ambient muusikast.",
        "Ma ei tahtnud mängu, mis iga poole minut karjub \"õudus!\".",
      ],
    },
    {
      title: "Disainipõhimõtted",
      deck: "Reeglid, mille juurde ma ikka ja jälle tagasi jõudsin.",
      bullets: [
        "Fookus on tavalistel kohtadel, argipäevasus mis muutub imelikuks.",
        "Flow ja UI peaksid tunduma nagu rohkem web style, grounded.",
        "Stylized PSX graafika, meelega natuke kehv, natuke keeruliselt loetav.",
        "Autos või monitori ees võib vaikus olla sama tähtis kui iga heli.",
        "Fade'id ja subtiitrid hoiavad Unity stseenid ühe öö sees.",
      ],
    },
    {
      title: "Žanr ja atmosfäär",
      deck: "Kitsas, lineaarne õudus. Rohkem rütm ja heli kui avatud maailm.",
      bullets: [
        "Uurid, kuulad, klõpsad. Veeb monitoril on päris HTML, mitte pilt peal (Unity WebView).",
        "Jälgitud olemise tunne, üksindus, see, et sa vaatad midagi, mis on tehniliselt avalik, aga ikkagi vale.",
        "Visuaalselt PS1 ja found footage poole. Mängija peab ise otsustama, kas jätkata vaatamist.",
      ],
    },
    {
      title: "Mängijakogemus",
      deck: "Erinevad keskkonnad, 3D ja 2D.",
      bullets: [
        "Poes kolm asja ja kassa. Autos raadio. Kirjuta <strong>home</strong>, kui tahad koju liikuda.",
        "Hiding sequence loob tensionit ja konkreetsemat õudust. Kui lähed katki, tuleb <strong>YOU WERE SEEN</strong> ja saad TV juurest uuesti proovida.",
      ],
    },
    {
      title: "Stseenivoog ja build",
      deck: "Kogu mäng toimub ühe öö vältel.",
      bullets: [
        "Flow chart näitab täielikku lugu, sh laud. Valmis mäng muutus sellest märgatavalt.",
        "Poe järel tuli must ekraan ja lühike tekst, siis alles auto. Sujuvad fade to black üleminekud stseenide vahel.",
      ],
      callout: "MainMenu → supermarket2 → ForestDrive → house",
      image: "assets/flowchart_image.jpg",
    },
    {
      title: "Scene overview",
      deck: "Five scenes.",
      bullets: [
        "Menüü: toon ja typing intro, siis pood.",
        "Pood (<code>supermarket2</code>): korv, kassa, <code>SupermarketTaskController</code>.",
        "Sõit (<code>ForestDrive</code>): auto ja raadio; <code>home</code> viib majja.",
        "Maja (<code>house</code>): veeb, TV/EAS, peitmine, põgenemine, tagasi menüüsse.",
        "Laua/monitori kiht: OpenFeed, glitch, Westfield Herald, siis offline lukk.",
      ],
      image: "progress%20screens/mainscreen1.png",
    },
    {
      title: "The Enemy",
      deck: "Photoshopis kokku pandud erinevatest elementidest: amalgamation of flesh and bone.",
      bullets: [
        "Käin Play Mode'is tee läbi ja salvestan. Ta kordab seda (<code>window1</code>, <code>housepath</code>, <code>window2</code>).",
        "Pärast TV hoiatust: <strong>FIND A PLACE TO HIDE. NOW.</strong> 3 spots, mille vahel valida.",
        "Kui ei peida, enemy läheb hunt mode'i ja liigub kiiresti sinna, kus sa oled. Game over screen, try again.",
      ],
      image: "assets/creature.png",
    },
    {
      title: "Mehaanika ja reeglid",
      deck: "Short interactive horror experience.",
      bullets: [
        "Poes: valida 3 toodet, dialoog kassapidajaga, siis level exit.",
        "Autos: saab interactida raadioga, mis mängib väikese raadiosaate clipi. Mõne aja pärast ilmub <strong>home</strong> levelisse liikumise võimalus typinguga.",
        "Majas: TV ütleb liigu; näed enemyt, peitu. Selle sektsiooni ajal front door avamine = instant game over, sama mis hiljaks jäämine.",
      ],
    },
    {
      title: "Diegeetiline veebikogemus: OpenFeed",
      deck: "Veeb on loo üks põhitegelasi.",
      bullets: [
        "Ma tahtsin, et see oleks veidi kole ja usutav: tabelid, live/offline, fake viewer count, kohati lohakas FAQ.",
        "Alguses tegin UI Unity canvasena. HTML webview (<code>tools/monitor-site</code> → <code>StreamingAssets</code>) oli teine iteratsioon, kuid performance kannatas. Lõpuks trigger external browserisse oli lahendus, mis töötas kõige paremini.",
        "Lõpus tuleb messenger, news artikkel, glitch ja brauser lukustub, kui site läheb offline. Browser shutoff callib mängu ennast, et flowga edasi minna.",
      ],
      image: "progress%20screens/image%20(1).png",
    },
    {
      title: "Heli",
      deck: "Vaikus ja helid on sama olulised.",
      bullets: [
        "Poes ja autos on ruumiheli. Majas ka sammud, sosinad, ukse krigisemine.",
        "Menu typewriter, EAS sounds, VHS sounds: kõik peaks andma atmosfäärile juurde.",
        "<code>boneaudio.mp3</code> joondus TV <em>LEAVE NOW</em> hetkel. Kui MonitorSite sureb, helid kaovad ära. Soundtrackid proovisin alignida kindlate juhtumishetkedega, et impacti rohkem anda.",
      ],
    },
    {
      title: "Tehnoloogia",
      deck: "Unity 6000.4, URP, veeb ja stseenid samas repos.",
      bullets: [
        "Input System, Timeline, WebView, React/Vite monitor-site.",
        "Tegin ise ka <code>Assets/Editor/OpenFeed</code> tööriistu, et stseene ja tekstuure kiiremini kokku saada.",
        "Mängu loogika elab <code>Assets/Scripts/</code> all.",
      ],
    },
    {
      title: "Mis valmis (19. mai)",
      bullets: [
        "MonitorSite kukub kokku, tuleb EAS, peitmine või fail, siis auto ja closing credits.",
        "Viimane päev oli troubleshooting ja polish: palju buge, mis tekkisid, ja fixid, mis tekitasid omajagu probleeme.",
      ],
    },
    {
      title: "Õppetunnid ja lahtised otsad",
      deck: "Mida see projekt mulle tegelikult tegi.",
      bullets: [
        "Scope kasvab kiiremini, kui alguses arvata võib. Üks hea idee tõmbab viis süsteemi kaasa.",
        "Typewriter intro ja <code>home</code> vihje võiksid olla veel selgemad first time mängijale.",
      ],
    },
    {
      title: "Ait\u00e4h!",
      thankYou: true,
      deck: "OPEN FEED, Martin Orav, mai 2026",
      body: "K\u00fcsimused?",
    },
  ];

  var overlay;
  var stage;
  var counterEl;
  var btnEnter;
  var btnPrev;
  var btnNext;
  var btnExit;

  var slides = [];
  var index = 0;
  var built = false;

  function bindDom() {
    overlay = document.getElementById("presentation-overlay");
    stage = document.getElementById("presentation-stage");
    counterEl = document.getElementById("pres-counter");
    btnEnter = document.getElementById("btn-enter-presentation");
    btnPrev = document.getElementById("pres-prev");
    btnNext = document.getElementById("pres-next");
    btnExit = document.getElementById("pres-exit");
  }

  function applyUiStrings() {
    var brand = document.querySelector(".presentation-brand");
    var footerMeta = document.getElementById("pres-footer-meta");
    if (brand) brand.innerHTML = "OPEN<span>FEED</span> - Esitlus";
    if (btnPrev) {
      btnPrev.textContent = UI.prev;
      btnPrev.setAttribute("aria-label", UI.prev + " slaid");
    }
    if (btnNext) {
      btnNext.textContent = UI.next;
      btnNext.setAttribute("aria-label", UI.next + " slaid");
    }
    if (btnExit) {
      btnExit.textContent = UI.exit;
      btnExit.setAttribute("aria-label", UI.exit + " esitlusest");
    }
    if (footerMeta) footerMeta.textContent = UI.footer;
    if (overlay) overlay.setAttribute("aria-label", UI.dialogLabel);
    if (btnEnter) btnEnter.setAttribute("aria-label", UI.enterLabel);
  }

  function makeSlide(num, data) {
    var slide = document.createElement("article");
    slide.className = "presentation-slide" + (data.thankYou ? " presentation-slide--thanks" : "");
    slide.setAttribute("role", "group");
    slide.setAttribute("aria-label", data.title);

    var html = "";
    if (!data.thankYou) {
      html += '<p class="slide-kicker">' + UI.slideLabel + " " + num + " / " + SLIDES.length + "</p>";
    }
    html += '<h2 class="slide-title">' + data.title + "</h2>";

    if (data.thankYou) {
      html += '<div class="slide-content slide-content--thanks">';
      if (data.body) html += '<p class="slide-thanks-body">' + data.body + "</p>";
      if (data.deck) html += '<p class="slide-thanks-meta">' + data.deck + "</p>";
      html += "</div>";
    } else {
      if (data.deck) html += '<p class="slide-deck">' + data.deck + "</p>";
      html += '<div class="slide-content">';
      if (data.callout) html += '<p class="slide-callout">' + data.callout + "</p>";
      if (data.bullets && data.bullets.length) {
        html += "<ul>";
        data.bullets.forEach(function (b) {
          html += "<li>" + b + "</li>";
        });
        html += "</ul>";
      }
      html += "</div>";
    }

    if (data.image) {
      html += (
        '<div class="slide-media"><img src="' +
        data.image +
        '" alt="" loading="lazy" /></div>'
      );
    }

    slide.innerHTML = html;

    return slide;
  }

  function buildSlides() {
    if (!stage) return;
    slides = [];
    stage.innerHTML = "";
    SLIDES.forEach(function (data, i) {
      var slide = makeSlide(i + 1, data);
      slides.push(slide);
      stage.appendChild(slide);
    });
    built = true;
  }

  function showSlide(i) {
    if (!slides.length) return;
    index = Math.max(0, Math.min(i, slides.length - 1));
    slides.forEach(function (s, j) {
      s.classList.toggle("is-active", j === index);
    });
    if (counterEl) counterEl.textContent = index + 1 + " / " + slides.length;
    if (btnPrev) btnPrev.disabled = index === 0;
    if (btnNext) btnNext.disabled = index === slides.length - 1;
    var active = slides[index];
    if (active) active.scrollTop = 0;
  }

  function enterPresentation() {
    if (!overlay || !stage) return;
    if (!built) buildSlides();
    if (!slides.length) return;
    applyUiStrings();
    document.body.classList.add("presentation-active");
    overlay.removeAttribute("hidden");
    showSlide(0);
    if (btnExit) btnExit.focus();
  }

  function exitPresentation() {
    document.body.classList.remove("presentation-active");
    if (overlay) overlay.setAttribute("hidden", "");
    if (btnEnter) btnEnter.focus();
  }

  function onKeydown(e) {
    if (!document.body.classList.contains("presentation-active")) return;
    if (e.key === "Escape") {
      e.preventDefault();
      exitPresentation();
    } else if (e.key === "ArrowRight" || e.key === "PageDown") {
      e.preventDefault();
      showSlide(index + 1);
    } else if (e.key === "ArrowLeft" || e.key === "PageUp") {
      e.preventDefault();
      showSlide(index - 1);
    } else if (e.key === "Home") {
      e.preventDefault();
      showSlide(0);
    } else if (e.key === "End") {
      e.preventDefault();
      showSlide(slides.length - 1);
    }
  }

  function init() {
    bindDom();
    applyUiStrings();

    if (btnEnter) btnEnter.addEventListener("click", enterPresentation);
    if (btnExit) btnExit.addEventListener("click", exitPresentation);
    if (btnPrev) btnPrev.addEventListener("click", function () { showSlide(index - 1); });
    if (btnNext) btnNext.addEventListener("click", function () { showSlide(index + 1); });
    document.addEventListener("keydown", onKeydown);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
