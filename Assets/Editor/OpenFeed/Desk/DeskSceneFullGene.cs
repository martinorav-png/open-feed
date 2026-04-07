using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class DeskSceneGenerator : Editor
{
    [MenuItem("OPEN FEED/Desk/Clear", false, 200)]
    static void ClearScene()
    {
        string[] roots = { "DesktopScene", "DeskObjects", "MonitorCanvas", "PhoneCanvas", "CrosshairCanvas", "InspectOverlay", "MonitorOverlay", "GameFlowCanvas" };
        foreach (string n in roots)
        {
            GameObject o = GameObject.Find(n);
            if (o != null) DestroyImmediate(o);
        }
        Debug.Log("OPENFEED Desk Scene fully cleared.");
    }

    [MenuItem("OPEN FEED/Desk/Generate Full Scene", false, 0)]
    static void GenerateFullScene()
    {
        ClearScene();
        Light[] allLights = FindObjectsByType<Light>();
        foreach (Light l in allLights) { if (l.type == LightType.Directional) DestroyImmediate(l.gameObject); }

        GameObject root = new GameObject("DesktopScene");

        // ============================================================
        // MATERIALS
        // ============================================================
        // Load project textures
        Material matPlastic = LoadMatTex("MonitorPlastic", new Color(0.14f, 0.13f, 0.12f), "Assets/Plastic013A_1K-PNG/Plastic013A_1K-PNG_Color.png");
        Material matWoodTex = LoadMatTex("DeskWoodTex", new Color(0.18f, 0.12f, 0.08f), "Assets/Wood051_1K-PNG/Wood051_1K-PNG_Color.png");

        Material matDarkWood = Matte("DeskWood", new Color(0.18f, 0.12f, 0.08f));
        Material matWall = Matte("Wall", new Color(0.06f, 0.06f, 0.07f));
        Material matFloor = Matte("Floor", new Color(0.08f, 0.07f, 0.06f));
        Material matCeiling = Matte("Ceiling", new Color(0.05f, 0.05f, 0.05f));
        Material matBaseboard = Matte("Baseboard", new Color(0.04f, 0.04f, 0.045f));
        Material matScreen = Matte("Screen", new Color(0.02f, 0.04f, 0.03f));
        Material matScreenEmit = Emit("ScreenEmissive", new Color(0.2f, 0.3f, 0.22f), new Color(0.2f, 0.3f, 0.22f), 0.3f);
        Material matKeycap = Matte("Keycap", new Color(0.12f, 0.12f, 0.13f));
        Material matMousePad = Matte("MousePad", new Color(0.06f, 0.06f, 0.08f));
        Material matLampMetal = Matte("LampMetal", new Color(0.15f, 0.15f, 0.14f));
        Material matLampShade = Matte("LampShade", new Color(0.22f, 0.2f, 0.15f));
        Material matLampBulb = Emit("LampBulb", new Color(0.4f, 0.3f, 0.15f), new Color(0.4f, 0.3f, 0.15f), 0.15f);
        Material matCan = Matte("CanMetal", new Color(0.08f, 0.35f, 0.12f));
        Material matCanTop = Matte("CanTop", new Color(0.5f, 0.5f, 0.5f));
        Material matPhone = Matte("Phone", new Color(0.1f, 0.1f, 0.12f));
        Material matPhoneScreen = Emit("PhoneScreen", new Color(0.08f, 0.1f, 0.18f), new Color(0.08f, 0.1f, 0.18f), 0.08f);
        Material matChairFabric = Matte("ChairFabric", new Color(0.1f, 0.1f, 0.1f));
        Material matChairFrame = Matte("ChairFrame", new Color(0.07f, 0.07f, 0.07f));
        Material matCable = Matte("Cable", new Color(0.05f, 0.05f, 0.05f));
        Material matLED = Emit("PowerLED", new Color(0.05f, 0.3f, 0.05f), new Color(0.05f, 0.3f, 0.05f), 0.1f);
        Material matStressBall = Matte("StressBall", new Color(0.15f, 0.35f, 0.15f));
        Material matPaperCrumpled = Matte("CrumpledPaper", new Color(0.5f, 0.48f, 0.42f));
        Material matPen = Matte("Pen", new Color(0.02f, 0.02f, 0.02f));
        Material matPenClip = Matte("PenClip", new Color(0.2f, 0.2f, 0.22f));
        Material matStickyYellow = Matte("StickyYellow", new Color(0.65f, 0.6f, 0.2f));
        Material matStickyPink = Matte("StickyPink", new Color(0.55f, 0.25f, 0.3f));
        Material matCoinStack = Matte("Coins", new Color(0.18f, 0.15f, 0.08f));
        Material matAshtray = Matte("Ashtray", new Color(0.06f, 0.06f, 0.06f));
        Material matAsh = Matte("Ash", new Color(0.12f, 0.11f, 0.1f));
        Material matCigFilter = Matte("CigFilter", new Color(0.55f, 0.45f, 0.3f));
        Material matCigPaper = Matte("CigPaper", new Color(0.5f, 0.48f, 0.45f));
        Material matCigEmber = Emit("CigEmber", new Color(0.3f, 0.1f, 0.02f), new Color(0.4f, 0.12f, 0.02f), 0.15f);
        Material matLighter = Matte("Lighter", new Color(0.5f, 0.08f, 0.08f));
        Material matLighterMetal = Matte("LighterMetal", new Color(0.15f, 0.15f, 0.16f));
        Material matWindowFrame = Matte("WindowFrame", new Color(0.07f, 0.07f, 0.08f));
        Material matWindowGlass = Emit("WindowGlass", new Color(0.02f, 0.02f, 0.04f), new Color(0.06f, 0.05f, 0.1f), 0.15f);
        Material matBlinds = Matte("Blinds", new Color(0.09f, 0.09f, 0.1f));
        Material matShelf = Matte("Shelf", new Color(0.1f, 0.07f, 0.05f));
        Material matBook = Matte("Book", new Color(0.12f, 0.05f, 0.05f));
        Material matBookBlue = Matte("BookBlue", new Color(0.05f, 0.06f, 0.12f));
        Material matBookGreen = Matte("BookGreen", new Color(0.04f, 0.08f, 0.05f));
        Material matPowerStrip = Matte("PowerStrip", new Color(0.08f, 0.08f, 0.08f));
        Material matRug = Matte("Rug", new Color(0.06f, 0.04f, 0.04f));
        Material matRadiator = Matte("Radiator", new Color(0.1f, 0.1f, 0.1f));
        Material matPosterArt = Emit("PosterArt", new Color(0.1f, 0.08f, 0.06f), new Color(0.12f, 0.08f, 0.06f), 0.05f);
        Material matMonitorStand = Matte("MonitorPlastic", new Color(0.14f, 0.13f, 0.12f));
        Material matMonitorBezel = Matte("MonitorBezel", new Color(0.08f, 0.08f, 0.08f));
        Material matBedFrame = Matte("BedFrame", new Color(0.1f, 0.07f, 0.05f));
        Material matMattress = Matte("Mattress", new Color(0.08f, 0.08f, 0.1f));
        Material matBlanket = Matte("Blanket", new Color(0.06f, 0.04f, 0.08f));
        Material matPillow = Matte("Pillow", new Color(0.12f, 0.11f, 0.1f));
        Material matNightstand = Matte("Nightstand", new Color(0.09f, 0.06f, 0.04f));
        Material matMugCeramic = Matte("Mug", new Color(0.1f, 0.06f, 0.05f));
        Material matCreature = Matte("Creature", new Color(0.01f, 0.01f, 0.015f));
        Material matCurtain = Matte("Curtain", new Color(0.05f, 0.04f, 0.06f));

        // ============================================================
        // ROOM
        // ============================================================
        GameObject room = new GameObject("Room");
        room.transform.parent = root.transform;

        P("Floor", PrimitiveType.Cube, room.transform, V(0, -0.05f, 0), V3, V(5f, 0.1f, 4.5f), matFloor);
        P("LeftWall", PrimitiveType.Cube, room.transform, V(-2.5f, 1.5f, 0), V(0, 90, 0), V(4.5f, 3f, 0.1f), matWall);
        P("RightWall", PrimitiveType.Cube, room.transform, V(2.5f, 1.5f, 0), V(0, 90, 0), V(4.5f, 3f, 0.1f), matWall);
        P("Ceiling", PrimitiveType.Cube, room.transform, V(0, 3.05f, 0), V3, V(5f, 0.1f, 4.5f), matCeiling);
        // Back wall behind player (Z=+2.2)
        P("RearWall", PrimitiveType.Cube, room.transform, V(0, 1.5f, 2.2f), V3, V(5f, 3f, 0.1f), matWall);
        P("BaseFL", PrimitiveType.Cube, room.transform, V(0, 0.04f, -2.14f), V3, V(5f, 0.08f, 0.02f), matBaseboard);
        P("BaseFR", PrimitiveType.Cube, room.transform, V(-2.44f, 0.04f, 0), V(0, 90, 0), V(4.5f, 0.08f, 0.02f), matBaseboard);
        P("BaseBR", PrimitiveType.Cube, room.transform, V(2.44f, 0.04f, 0), V(0, 90, 0), V(4.5f, 0.08f, 0.02f), matBaseboard);

        // Window (LEFT wall from player POV = X=-2.5)
        GameObject window = new GameObject("Window"); window.transform.parent = room.transform;
        P("WinFrameT", PrimitiveType.Cube, window.transform, V(-2.44f, 2.15f, -0.6f), V(0, 90, 0), V(1.05f, 0.06f, 0.12f), matWindowFrame);
        P("WinFrameB", PrimitiveType.Cube, window.transform, V(-2.44f, 0.85f, -0.6f), V(0, 90, 0), V(1.05f, 0.06f, 0.12f), matWindowFrame);
        P("WinFrameL", PrimitiveType.Cube, window.transform, V(-2.44f, 1.5f, -0.1f), V(0, 90, 0), V(0.06f, 1.36f, 0.12f), matWindowFrame);
        P("WinFrameR", PrimitiveType.Cube, window.transform, V(-2.44f, 1.5f, -1.1f), V(0, 90, 0), V(0.06f, 1.36f, 0.12f), matWindowFrame);
        P("WinCenter", PrimitiveType.Cube, window.transform, V(-2.44f, 1.5f, -0.6f), V(0, 90, 0), V(0.03f, 1.3f, 0.06f), matWindowFrame);
        P("WinGlass", PrimitiveType.Cube, window.transform, V(-2.46f, 1.5f, -0.6f), V(0, 90, 0), V(0.95f, 1.24f, 0.01f), matWindowGlass);
        for (int i = 0; i < 14; i++)
            P($"Blind{i}", PrimitiveType.Cube, window.transform, V(-2.43f, 0.92f + i * 0.09f, -0.6f), V(15, 90, 0), V(0.92f, 0.005f, 0.05f), matBlinds);

        GameObject windowLight = new GameObject("WindowMoonlight");
        windowLight.transform.parent = room.transform; windowLight.transform.localPosition = V(-2.2f, 1.5f, -0.6f);
        windowLight.transform.localRotation = Quaternion.Euler(20, 90, 0);
        Light wl = windowLight.AddComponent<Light>(); wl.type = LightType.Spot; wl.color = new Color(0.15f, 0.15f, 0.3f);
        wl.intensity = 0.5f; wl.range = 8f; wl.spotAngle = 90f; wl.shadows = LightShadows.Soft;

        // Radiator (under left window)
        P("Radiator", PrimitiveType.Cube, room.transform, V(-2.3f, 0.35f, -0.6f), V(0, 90, 0), V(0.8f, 0.5f, 0.08f), matRadiator);
        for (int i = 0; i < 8; i++)
            P($"RadFin{i}", PrimitiveType.Cube, room.transform, V(-2.34f, 0.35f, -0.25f - i * 0.1f), V(0, 90, 0), V(0.02f, 0.44f, 0.06f), matRadiator);

        // Bookshelf (right wall from player POV = +X)
        GameObject shelf = new GameObject("Bookshelf"); shelf.transform.parent = room.transform; shelf.transform.localPosition = V(2.35f, 0, -0.5f);
        P("ShelfSideL", PrimitiveType.Cube, shelf.transform, V(0, 0.8f, -0.3f), V3, V(0.02f, 1.6f, 0.22f), matShelf);
        P("ShelfSideR", PrimitiveType.Cube, shelf.transform, V(0, 0.8f, 0.3f), V3, V(0.02f, 1.6f, 0.22f), matShelf);
        P("ShelfBot", PrimitiveType.Cube, shelf.transform, V(0, 0.02f, 0), V3, V(0.02f, 0.02f, 0.62f), matShelf);
        float[] shelfH = { 0.5f, 1f, 1.5f };
        foreach (float sy in shelfH) P($"ShelfBoard{sy}", PrimitiveType.Cube, shelf.transform, V(0, sy, 0), V3, V(0.02f, 0.02f, 0.6f), matShelf);
        Material[] bookMats = { matBook, matBookBlue, matBookGreen, matBook, matBookBlue };
        for (int s = 0; s < 3; s++) { float by = shelfH[s] + 0.01f; int bc = 4 + s;
            for (int b = 0; b < bc; b++) P($"Book{s}_{b}", PrimitiveType.Cube, shelf.transform,
                V(-0.01f, by + 0.08f, -0.22f + b * 0.11f), V(0, 0, Random.Range(-3f, 3f)),
                V(0.08f, 0.16f + Random.Range(-0.03f, 0.03f), 0.06f + Random.Range(-0.01f, 0.02f)), bookMats[b % bookMats.Length]); }

        // Poster
        P("PosterFrame", PrimitiveType.Cube, room.transform, V(-1.2f, 1.8f, -2.14f), V3, V(0.5f, 0.35f, 0.02f), matWindowFrame);
        P("PosterArt", PrimitiveType.Cube, room.transform, V(-1.2f, 1.8f, -2.12f), V3, V(0.44f, 0.29f, 0.005f), matPosterArt);

        // Power strip & rug
        P("PowerStrip", PrimitiveType.Cube, room.transform, V(0.6f, 0.025f, -2f), V(0, 15, 0), V(0.25f, 0.03f, 0.05f), matPowerStrip);
        P("PowerCable", PrimitiveType.Cylinder, room.transform, V(0.75f, 0.02f, -2.05f), V(0, 40, 90), V(0.008f, 0.15f, 0.008f), matCable);
        P("Rug", PrimitiveType.Cube, room.transform, V(0, 0.005f, -0.5f), V(0, 3, 0), V(1.8f, 0.01f, 1.4f), matRug);

        // ============================================================
        // FRONT WINDOW (back wall Z=-2.2, player faces this wall)
        // Far right side from player's POV
        // ============================================================
        // Back wall - single piece with cutout area covered by window
        P("BackWallMain", PrimitiveType.Cube, room.transform, V(-0.55f, 1.5f, -2.2f), V3, V(3.9f, 3f, 0.1f), matWall);
        P("BackWallFarR", PrimitiveType.Cube, room.transform, V(2.3f, 1.5f, -2.2f), V3, V(0.4f, 3f, 0.1f), matWall);
        P("BackWallWinTop", PrimitiveType.Cube, room.transform, V(1.8f, 2.7f, -2.2f), V3, V(0.65f, 0.6f, 0.1f), matWall);
        P("BackWallWinBot", PrimitiveType.Cube, room.transform, V(1.8f, 0.4f, -2.2f), V3, V(0.65f, 0.8f, 0.1f), matWall);

        // Front window frame (on back wall, far right)
        GameObject frontWin = new GameObject("FrontWindow"); frontWin.transform.parent = room.transform;
        P("FWinFrameT", PrimitiveType.Cube, frontWin.transform, V(1.8f, 2.35f, -2.2f), V3, V(0.7f, 0.06f, 0.12f), matWindowFrame);
        P("FWinFrameB", PrimitiveType.Cube, frontWin.transform, V(1.8f, 0.85f, -2.2f), V3, V(0.7f, 0.06f, 0.12f), matWindowFrame);
        P("FWinFrameL", PrimitiveType.Cube, frontWin.transform, V(1.48f, 1.6f, -2.2f), V3, V(0.06f, 1.56f, 0.12f), matWindowFrame);
        P("FWinFrameR", PrimitiveType.Cube, frontWin.transform, V(2.12f, 1.6f, -2.2f), V3, V(0.06f, 1.56f, 0.12f), matWindowFrame);
        P("FWinCenter", PrimitiveType.Cube, frontWin.transform, V(1.8f, 1.6f, -2.2f), V3, V(0.04f, 1.5f, 0.06f), matWindowFrame);

        // Window glass (dark, slightly emissive to suggest moonlight)
        Material matGlassTransparent = Emit("GlassWindow", new Color(0.02f, 0.02f, 0.04f), new Color(0.04f, 0.04f, 0.08f), 0.1f);

        P("FWinGlass", PrimitiveType.Cube, frontWin.transform, V(1.8f, 1.6f, -2.19f), V3, V(0.58f, 1.44f, 0.005f), matGlassTransparent);

        // Curtains
        P("CurtainL", PrimitiveType.Cube, frontWin.transform, V(1.52f, 1.6f, -2.16f), V(0, 5, 0), V(0.12f, 1.6f, 0.02f), matCurtain);
        P("CurtainR", PrimitiveType.Cube, frontWin.transform, V(2.08f, 1.6f, -2.16f), V(0, -5, 0), V(0.12f, 1.6f, 0.02f), matCurtain);
        P("CurtainRod", PrimitiveType.Cylinder, frontWin.transform, V(1.8f, 2.42f, -2.16f), V(0, 0, 90), V(0.012f, 0.4f, 0.012f), matWindowFrame);

        // Moonlight from behind and above the window (outside, shining in)
        GameObject frontMoonlight = new GameObject("FrontMoonlight");
        frontMoonlight.transform.parent = room.transform;
        frontMoonlight.transform.localPosition = V(1.8f, 3f, -3.5f);
        frontMoonlight.transform.localRotation = Quaternion.Euler(30, 0, 0);
        Light fml = frontMoonlight.AddComponent<Light>(); fml.type = LightType.Spot;
        fml.color = new Color(0.12f, 0.12f, 0.28f); fml.intensity = 0.6f;
        fml.range = 8f; fml.spotAngle = 65f; fml.shadows = LightShadows.Soft;

        // Moonlight strip on floor
        Material matMoonStrip = Emit("MoonStrip", new Color(0.03f, 0.03f, 0.06f), new Color(0.04f, 0.04f, 0.08f), 0.08f);
        P("MoonlightFloor", PrimitiveType.Cube, room.transform, V(1.8f, 0.006f, -1.4f), V3, V(0.5f, 0.002f, 0.4f), matMoonStrip);

        // ============================================================
        // BED (left side of room, along left wall)
        // ============================================================
        GameObject bed = new GameObject("Bed"); bed.transform.parent = room.transform; bed.transform.localPosition = V(1.2f, 0, 1.5f);
        bed.transform.localRotation = Quaternion.Euler(0, 0, 0);

        // Bed frame
        P("BedFrameBase", PrimitiveType.Cube, bed.transform, V(0, 0.15f, 0), V3, V(2f, 0.06f, 0.9f), matBedFrame);
        P("BedFrameHead", PrimitiveType.Cube, bed.transform, V(-0.95f, 0.4f, 0), V3, V(0.06f, 0.55f, 0.92f), matBedFrame);
        P("BedFrameFoot", PrimitiveType.Cube, bed.transform, V(0.95f, 0.28f, 0), V3, V(0.06f, 0.32f, 0.92f), matBedFrame);
        // Legs
        P("BedLegFL", PrimitiveType.Cube, bed.transform, V(0.92f, 0.06f, -0.4f), V3, V(0.06f, 0.12f, 0.06f), matBedFrame);
        P("BedLegFR", PrimitiveType.Cube, bed.transform, V(0.92f, 0.06f, 0.4f), V3, V(0.06f, 0.12f, 0.06f), matBedFrame);
        P("BedLegBL", PrimitiveType.Cube, bed.transform, V(-0.92f, 0.06f, -0.4f), V3, V(0.06f, 0.12f, 0.06f), matBedFrame);
        P("BedLegBR", PrimitiveType.Cube, bed.transform, V(-0.92f, 0.06f, 0.4f), V3, V(0.06f, 0.12f, 0.06f), matBedFrame);

        // Mattress
        P("Mattress", PrimitiveType.Cube, bed.transform, V(0, 0.24f, 0), V3, V(1.85f, 0.12f, 0.82f), matMattress);

        // Blanket (messy, slightly askew)
        P("Blanket", PrimitiveType.Cube, bed.transform, V(0.15f, 0.32f, -0.05f), V(2, 3, -1), V(1.4f, 0.04f, 0.75f), matBlanket);
        P("BlanketFold", PrimitiveType.Cube, bed.transform, V(-0.45f, 0.34f, -0.08f), V(-5, 2, 3), V(0.4f, 0.06f, 0.7f), matBlanket);

        // Pillow
        P("Pillow", PrimitiveType.Cube, bed.transform, V(-0.72f, 0.34f, 0), V(0, 5, 0), V(0.32f, 0.08f, 0.45f), matPillow);

        // ============================================================
        // NIGHTSTAND (next to bed)
        // ============================================================
        GameObject nightstand = new GameObject("Nightstand"); nightstand.transform.parent = room.transform;
        nightstand.transform.localPosition = V(0.1f, 0, 1.5f);

        P("NightstandBody", PrimitiveType.Cube, nightstand.transform, V(0, 0.25f, 0), V3, V(0.35f, 0.5f, 0.3f), matNightstand);
        P("NightstandTop", PrimitiveType.Cube, nightstand.transform, V(0, 0.51f, 0), V3, V(0.38f, 0.02f, 0.33f), matNightstand);
        P("NightstandDrawer", PrimitiveType.Cube, nightstand.transform, V(0, 0.3f, 0.14f), V3, V(0.28f, 0.15f, 0.01f), matBedFrame);
        P("DrawerKnob", PrimitiveType.Cube, nightstand.transform, V(0, 0.3f, 0.155f), V3, V(0.03f, 0.02f, 0.01f), matLampMetal);

        // Mug on nightstand (dark coffee mug)
        P("MugBody", PrimitiveType.Cylinder, nightstand.transform, V(0.05f, 0.56f, 0), V3, V(0.04f, 0.03f, 0.04f), matMugCeramic);
        P("MugHandle", PrimitiveType.Cube, nightstand.transform, V(0.085f, 0.56f, 0), V3, V(0.015f, 0.025f, 0.008f), matMugCeramic);

        // ============================================================
        // CREATURE (outside front window, barely visible)
        // ============================================================
        // Tall thin dark figure that slowly crosses behind the back wall window
        GameObject creature = new GameObject("WindowCreature");
        creature.transform.parent = root.transform;
        creature.transform.localPosition = V(-3f, 0, -2.5f); // starts off to the left, behind the back wall

        // Body - very thin, tall, dark, barely distinguishable from the night
        P("CreatureBody", PrimitiveType.Cube, creature.transform, V(0, 1.1f, 0), V3, V(0.15f, 1.8f, 0.1f), matCreature);
        P("CreatureHead", PrimitiveType.Sphere, creature.transform, V(0, 2.1f, 0), V3, V(0.14f, 0.18f, 0.12f), matCreature);
        // Long arms
        P("CreatureArmL", PrimitiveType.Cube, creature.transform, V(-0.12f, 0.9f, 0), V(0, 0, 5), V(0.04f, 1f, 0.04f), matCreature);
        P("CreatureArmR", PrimitiveType.Cube, creature.transform, V(0.12f, 0.85f, 0), V(0, 0, -3), V(0.04f, 1.05f, 0.04f), matCreature);
        // Thin legs
        P("CreatureLegL", PrimitiveType.Cube, creature.transform, V(-0.05f, 0.25f, 0), V3, V(0.05f, 0.5f, 0.05f), matCreature);
        P("CreatureLegR", PrimitiveType.Cube, creature.transform, V(0.05f, 0.25f, 0), V3, V(0.05f, 0.5f, 0.05f), matCreature);

        // Add the creature movement script
        creature.AddComponent<WindowCreature>();

        // ============================================================
        // DESK
        // ============================================================
        GameObject desk = new GameObject("Desk"); desk.transform.parent = root.transform; desk.transform.localPosition = V(0, 0, -1.6f);
        P("DeskTop", PrimitiveType.Cube, desk.transform, V(0, 0.72f, 0), V3, V(1.6f, 0.04f, 0.75f), matWoodTex);
        P("LegFL", PrimitiveType.Cube, desk.transform, V(-0.74f, 0.36f, 0.32f), V3, V(0.05f, 0.72f, 0.05f), matDarkWood);
        P("LegFR", PrimitiveType.Cube, desk.transform, V(0.74f, 0.36f, 0.32f), V3, V(0.05f, 0.72f, 0.05f), matDarkWood);
        P("LegBL", PrimitiveType.Cube, desk.transform, V(-0.74f, 0.36f, -0.32f), V3, V(0.05f, 0.72f, 0.05f), matDarkWood);
        P("LegBR", PrimitiveType.Cube, desk.transform, V(0.74f, 0.36f, -0.32f), V3, V(0.05f, 0.72f, 0.05f), matDarkWood);
        P("DeskBackPanel", PrimitiveType.Cube, desk.transform, V(0, 0.36f, -0.35f), V3, V(1.52f, 0.68f, 0.02f), matWoodTex);

        // Desk objects container (child of desk)
        GameObject deskObj = new GameObject("DeskObjects"); deskObj.transform.parent = desk.transform; deskObj.transform.localPosition = V3;

        // Stress ball
        GameObject stressBall = new GameObject("StressBall"); stressBall.transform.parent = deskObj.transform; stressBall.transform.localPosition = V(-0.55f, 0.755f, 0.1f);
        GameObject ballMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere); ballMesh.name = "BallMesh"; ballMesh.transform.parent = stressBall.transform;
        ballMesh.transform.localPosition = V3; ballMesh.transform.localScale = V(0.04f, 0.04f, 0.04f);
        ballMesh.GetComponent<Renderer>().sharedMaterial = matStressBall;
        InteractableObject sbInt = stressBall.AddComponent<InteractableObject>(); sbInt.interactionType = InteractableObject.InteractionType.Squeeze; sbInt.objectName = "stress ball";
        BoxCollider sbCol = stressBall.AddComponent<BoxCollider>(); sbCol.size = V(0.05f, 0.05f, 0.05f);

        // Pen
        GameObject pen = new GameObject("Pen"); pen.transform.parent = deskObj.transform;
        pen.transform.localPosition = V(0.15f, 0.74f, -0.15f); pen.transform.localRotation = Quaternion.Euler(0, 35, 90);
        P("PenBody", PrimitiveType.Cylinder, pen.transform, V3, V3, V(0.005f, 0.06f, 0.005f), matPen);
        P("PenClip", PrimitiveType.Cube, pen.transform, V(0.003f, 0.04f, 0), V3, V(0.002f, 0.025f, 0.004f), matPenClip);
        InteractableObject penInt = pen.AddComponent<InteractableObject>(); penInt.interactionType = InteractableObject.InteractionType.Spin; penInt.objectName = "pen";
        BoxCollider penCol = pen.AddComponent<BoxCollider>(); penCol.size = V(0.015f, 0.13f, 0.015f);

        // Crumpled paper
        Vector3[] paperPos = { V(0.5f, 0.75f, -0.2f), V(-0.3f, 0.75f, -0.25f) };
        Vector3[] paperRot = { V(355, 72, 3), V(3, 167, 354) };
        for (int i = 0; i < 2; i++) {
            GameObject paper = new GameObject($"CrumpledPaper{i}"); paper.transform.parent = deskObj.transform;
            paper.transform.localPosition = paperPos[i]; paper.transform.localRotation = Quaternion.Euler(paperRot[i]);
            GameObject pm = GameObject.CreatePrimitive(PrimitiveType.Sphere); pm.name = "PaperMesh"; pm.transform.parent = paper.transform;
            pm.transform.localPosition = V3; pm.transform.localScale = V(0.022f, 0.019f, 0.019f);
            pm.GetComponent<Renderer>().sharedMaterial = matPaperCrumpled;
            InteractableObject pInt = paper.AddComponent<InteractableObject>(); pInt.interactionType = InteractableObject.InteractionType.Crumple; pInt.objectName = "paper";
            BoxCollider pCol = paper.AddComponent<BoxCollider>(); pCol.size = V(0.03f, 0.025f, 0.03f);
        }

        // Sticky notes
        P("StickyYellow", PrimitiveType.Cube, deskObj.transform, V(0.2716f, 0.9257f, 0.0509f), V(0, 0, 357), V(0.04f, 0.04f, 0.001f), matStickyYellow);
        P("StickyPink", PrimitiveType.Cube, deskObj.transform, V(0.2685f, 0.9721f, 0.051f), V(0, 0, 2), V(0.035f, 0.035f, 0.001f), matStickyPink);

        // Ashtray
        GameObject ashtray = new GameObject("Ashtray"); ashtray.transform.parent = deskObj.transform; ashtray.transform.localPosition = V(-0.55f, 0.745f, 0.3f);
        P("AshtrayBase", PrimitiveType.Cylinder, ashtray.transform, V3, V3, V(0.05f, 0.008f, 0.05f), matAshtray);
        P("Ash", PrimitiveType.Cylinder, ashtray.transform, V(0, 0.005f, 0), V3, V(0.04f, 0.003f, 0.04f), matAsh);
        GameObject cig = new GameObject("Cigarette"); cig.transform.parent = ashtray.transform;
        cig.transform.localPosition = V(-0.021f, 0.0065f, 0.004f); cig.transform.localRotation = Quaternion.Euler(0, 25, 8);
        P("CigPaper", PrimitiveType.Cylinder, cig.transform, V3, V(0, 0, 90), V(0.003f, 0.02f, 0.003f), matCigPaper);
        P("CigFilter", PrimitiveType.Cylinder, cig.transform, V(-0.018f, 0, 0), V(0, 0, 90), V(0.0035f, 0.008f, 0.0035f), matCigFilter);
        P("CigEmber", PrimitiveType.Sphere, cig.transform, V(0.022f, 0, 0), V3, V(0.004f, 0.004f, 0.004f), matCigEmber);
        AddLight("EmberGlow", cig.transform, V(0.022f, 0, 0), new Color(0.8f, 0.3f, 0.05f), 0.02f, 0.1f, false);
        InteractableObject ashInt = ashtray.AddComponent<InteractableObject>(); ashInt.interactionType = InteractableObject.InteractionType.Toggle; ashInt.objectName = "cigarette";
        BoxCollider ashCol = ashtray.AddComponent<BoxCollider>(); ashCol.size = V(0.06f, 0.025f, 0.06f);

        // Lighter
        GameObject lighter = new GameObject("Lighter"); lighter.transform.parent = deskObj.transform;
        lighter.transform.localPosition = V(-0.48f, 0.745f, 0.25f); lighter.transform.localRotation = Quaternion.Euler(0, 345, 0);
        P("LighterBody", PrimitiveType.Cube, lighter.transform, V(0, 0.012f, 0), V3, V(0.015f, 0.025f, 0.008f), matLighter);
        P("LighterTop", PrimitiveType.Cube, lighter.transform, V(0, 0.026f, 0), V3, V(0.013f, 0.004f, 0.007f), matLighterMetal);
        InteractableObject lInt = lighter.AddComponent<InteractableObject>(); lInt.interactionType = InteractableObject.InteractionType.Toggle; lInt.objectName = "lighter";
        BoxCollider lCol = lighter.AddComponent<BoxCollider>(); lCol.size = V(0.02f, 0.035f, 0.012f);

        // Coins
        GameObject coins = new GameObject("CoinStack"); coins.transform.parent = deskObj.transform; coins.transform.localPosition = V(0.3f, 0.74f, -0.1f);
        P("Coin0", PrimitiveType.Cylinder, coins.transform, V(-0.0004f, 0, -0.0003f), V3, V(0.012f, 0.001f, 0.012f), matCoinStack);
        P("Coin1", PrimitiveType.Cylinder, coins.transform, V(-0.0011f, 0.002f, -0.0014f), V3, V(0.012f, 0.001f, 0.012f), matCoinStack);
        P("Coin2", PrimitiveType.Cylinder, coins.transform, V(0.0016f, 0.004f, 0.001f), V3, V(0.012f, 0.001f, 0.012f), matCoinStack);

        // USB Cable (offset from desk)
        GameObject usbCable = new GameObject("USBCable"); usbCable.transform.parent = deskObj.transform; usbCable.transform.localPosition = V(0, 0, 1.6f);
        P("CableSeg0", PrimitiveType.Cylinder, usbCable.transform, V(0.25f, 0.74f, 0.35f), V(0, 0, 90), V(0.003f, 0.015f, 0.003f), matCable);
        P("CableSeg1", PrimitiveType.Cylinder, usbCable.transform, V(0.28f, 0.74f, 0.32f), V(0, 15, 90), V(0.003f, 0.015f, 0.003f), matCable);
        P("CableSeg2", PrimitiveType.Cylinder, usbCable.transform, V(0.32f, 0.74f, 0.28f), V(0, 30, 90), V(0.003f, 0.015f, 0.003f), matCable);
        P("CableSeg3", PrimitiveType.Cylinder, usbCable.transform, V(0.35f, 0.74f, 0.25f), V(0, 45, 90), V(0.003f, 0.015f, 0.003f), matCable);
        P("CableSeg4", PrimitiveType.Cylinder, usbCable.transform, V(0.38f, 0.74f, 0.23f), V(0, 60, 90), V(0.003f, 0.015f, 0.003f), matCable);

        // ============================================================
        // MONITOR (exact from export)
        // ============================================================
        GameObject monitor = new GameObject("Monitor"); monitor.transform.parent = root.transform; monitor.transform.localPosition = V(0.05f, 0.808f, -1.75f);
        P("MonitorBody", PrimitiveType.Cube, monitor.transform, V(0, 0.18f, 0), V3, V(0.48f, 0.38f, 0.38f), matPlastic);
        GameObject monGO = new GameObject("GameObject"); monGO.transform.parent = monitor.transform; monGO.transform.localPosition = V3;
        P("MonitorBezel", PrimitiveType.Cube, monGO.transform, V(0, 0.18f, 0.19f), V3, V(0.5f, 0.4f, 0.02f), matPlastic);
        GameObject monScreen = P("MonitorScreen", PrimitiveType.Cube, monitor.transform, V(0, 0.2f, 0.195f), V3, V(0.38f, 0.28f, 0.04f), matScreenEmit);
        P("ScreenGlass", PrimitiveType.Cube, monScreen.transform, V(0, 0, 0.075f), V3, V(0.9737f, 0.9643f, 0.025f), matScreen);
        P("MonitorStand", PrimitiveType.Cube, monitor.transform, V(0, -0.02f, 0.05f), V3, V(0.22f, 0.03f, 0.25f), matMonitorStand);
        P("MonitorFoot", PrimitiveType.Cube, monitor.transform, V(0, -0.04f, 0.1f), V3, V(0.3f, 0.02f, 0.18f), matMonitorBezel);
        P("PowerButton", PrimitiveType.Cube, monitor.transform, V(0.2f, 0.03f, 0.2f), V3, V(0.02f, 0.015f, 0.008f), matKeycap);
        P("PowerLED", PrimitiveType.Cube, monitor.transform, V(0.18f, 0.03f, 0.201f), V3, V(0.006f, 0.006f, 0.002f), matLED);
        P("MonitorCable", PrimitiveType.Cylinder, monitor.transform, V(0, 0.05f, -0.22f), V(0, 0, 90), V(0.015f, 0.15f, 0.015f), matCable);

        // ============================================================
        // KEYBOARD (exact)
        // ============================================================
        GameObject keyboard = new GameObject("Keyboard"); keyboard.transform.parent = root.transform; keyboard.transform.localPosition = V(0.05f, 0.745f, -1.4f);
        P("KeyboardBase", PrimitiveType.Cube, keyboard.transform, V3, V(3, 0, 0), V(0.38f, 0.015f, 0.14f), matPlastic);
        P("KeyRow0", PrimitiveType.Cube, keyboard.transform, V(0, 0.012f, 0.045f), V(3, 0, 0), V(0.34f, 0.006f, 0.018f), matKeycap);
        P("KeyRow1", PrimitiveType.Cube, keyboard.transform, V(0, 0.012f, 0.021f), V(3, 0, 0), V(0.34f, 0.006f, 0.018f), matKeycap);
        P("KeyRow2", PrimitiveType.Cube, keyboard.transform, V(0, 0.012f, -0.003f), V(3, 0, 0), V(0.34f, 0.006f, 0.018f), matKeycap);
        P("KeyRow3", PrimitiveType.Cube, keyboard.transform, V(0, 0.012f, -0.027f), V(3, 0, 0), V(0.34f, 0.006f, 0.018f), matKeycap);
        P("KeyRow4", PrimitiveType.Cube, keyboard.transform, V(0, 0.012f, -0.051f), V(3, 0, 0), V(0.22f, 0.008f, 0.018f), matKeycap);
        P("Spacebar", PrimitiveType.Cube, keyboard.transform, V(0, 0.012f, -0.052f), V(3, 0, 0), V(0.12f, 0.006f, 0.018f), matKeycap);

        // ============================================================
        // MOUSE (exact - rotated 180)
        // ============================================================
        GameObject mouseArea = new GameObject("MouseArea"); mouseArea.transform.parent = root.transform;
        mouseArea.transform.localPosition = V(-0.274f, 0.74f, -1.3612f); mouseArea.transform.localRotation = Quaternion.Euler(0, 180, 0);
        P("MousePad", PrimitiveType.Cube, mouseArea.transform, V(0, 0.002f, 0), V3, V(0.22f, 0.003f, 0.2f), matMousePad);
        P("MouseBody", PrimitiveType.Cube, mouseArea.transform, V(0, 0.015f, 0.02f), V3, V(0.055f, 0.025f, 0.09f), matPlastic);
        P("MouseLeft", PrimitiveType.Cube, mouseArea.transform, V(-0.012f, 0.028f, 0.04f), V3, V(0.022f, 0.004f, 0.035f), matKeycap);
        P("MouseRight", PrimitiveType.Cube, mouseArea.transform, V(0.012f, 0.028f, 0.04f), V3, V(0.022f, 0.004f, 0.035f), matKeycap);
        P("ScrollWheel", PrimitiveType.Cylinder, mouseArea.transform, V(0, 0.03f, 0.04f), V(0, 0, 90), V(0.008f, 0.003f, 0.008f), matPlastic);

        // ============================================================
        // DESK LAMP (LEFT side, exact)
        // ============================================================
        GameObject lamp = new GameObject("DeskLamp"); lamp.transform.parent = root.transform; lamp.transform.localPosition = V(-0.55f, 0.74f, -1.75f);
        P("LampBase", PrimitiveType.Cylinder, lamp.transform, V(0, 0.01f, 0), V3, V(0.1f, 0.01f, 0.1f), matLampMetal);
        P("LampArm1", PrimitiveType.Cylinder, lamp.transform, V(0, 0.14f, 0), V3, V(0.012f, 0.13f, 0.012f), matLampMetal);
        P("LampArm2", PrimitiveType.Cylinder, lamp.transform, V(0.04f, 0.3f, 0.04f), V(0, 0, 335), V(0.012f, 0.1f, 0.012f), matLampMetal);
        P("LampShade", PrimitiveType.Cube, lamp.transform, V(0.08f, 0.38f, 0.08f), V(0, 0, 345), V(0.12f, 0.06f, 0.12f), matLampShade);
        P("LampBulb", PrimitiveType.Sphere, lamp.transform, V(0.08f, 0.36f, 0.08f), V3, V(0.03f, 0.03f, 0.03f), matLampBulb);
        AddLight("LampPointLight", lamp.transform, V(0.08f, 0.35f, 0.08f), new Color(0.9f, 0.7f, 0.4f), 0.3f, 2f, true);

        // ============================================================
        // ENERGY DRINK (exact)
        // ============================================================
        GameObject can = new GameObject("EnergyDrinkCan"); can.transform.parent = root.transform; can.transform.localPosition = V(-0.437f, 0.74f, -1.45f);
        P("CanBody", PrimitiveType.Cylinder, can.transform, V(0, 0.06f, 0), V3, V(0.035f, 0.06f, 0.035f), matCan);
        P("CanTop", PrimitiveType.Cylinder, can.transform, V(0, 0.121f, 0), V3, V(0.032f, 0.002f, 0.032f), matCanTop);
        P("PullTab", PrimitiveType.Cube, can.transform, V(0.005f, 0.125f, 0), V3, V(0.015f, 0.002f, 0.006f), matCanTop);
        InteractableObject canInt = can.AddComponent<InteractableObject>(); canInt.interactionType = InteractableObject.InteractionType.Sip; canInt.objectName = "energy drink";
        BoxCollider canCol = can.AddComponent<BoxCollider>(); canCol.size = V(0.035f, 0.16f, 0.035f); canCol.center = V(0, 0.04f, 0);

        // ============================================================
        // PHONE (exact - right side, rot Y=15)
        // ============================================================
        GameObject phone = new GameObject("Phone"); phone.transform.parent = root.transform;
        phone.transform.localPosition = V(0.476f, 0.74f, -1.328f); phone.transform.localRotation = Quaternion.Euler(0, 15, 0);
        GameObject phoneBody = P("PhoneBody", PrimitiveType.Cube, phone.transform, V(0, 0.005f, 0), V3, V(0.065f, 0.008f, 0.13f), matPhone);
        P("PhoneScreen", PrimitiveType.Cube, phone.transform, V(0, 0.01f, 0.01f), V3, V(0.055f, 0.002f, 0.095f), matPhoneScreen);
        phoneBody.AddComponent<BoxCollider>().size = Vector3.one;

        // ============================================================
        // CHAIR (exact)
        // ============================================================
        GameObject chair = new GameObject("Chair"); chair.transform.parent = root.transform; chair.transform.localPosition = V(0, 0, -0.7f);
        P("Seat", PrimitiveType.Cube, chair.transform, V(0, 0.42f, 0), V3, V(0.42f, 0.05f, 0.4f), matChairFabric);
        P("Backrest", PrimitiveType.Cube, chair.transform, V(0, 0.7f, 0.18f), V(355, 0, 0), V(0.4f, 0.5f, 0.04f), matChairFabric);
        P("ChairPost", PrimitiveType.Cylinder, chair.transform, V(0, 0.25f, 0), V3, V(0.04f, 0.17f, 0.04f), matChairFrame);
        Vector3[] legPos = { V(0, 0.06f, 0.22f), V(0.2092f, 0.06f, 0.068f), V(0.1293f, 0.06f, -0.178f), V(-0.1293f, 0.06f, -0.178f), V(-0.2092f, 0.06f, 0.068f) };
        float[] legRot = { 0, 288, 216, 144, 72 };
        for (int i = 0; i < 5; i++) {
            P($"ChairLeg{i}", PrimitiveType.Cube, chair.transform, legPos[i], V(0, legRot[i], 0), V(0.03f, 0.02f, 0.22f), matChairFrame);
            P($"Caster{i}", PrimitiveType.Sphere, chair.transform, V(legPos[i].x, 0.025f, legPos[i].z), V3, V(0.03f, 0.03f, 0.03f), matChairFrame);
        }

        // Loose cables (exact)
        P("MouseCable1", PrimitiveType.Cylinder, root.transform, V(-0.2659f, 0.7451f, -1.458f), V(11, 75.4f, 88.3f), V(0.0051f, 0.0767f, 0.0051f), matCable);
        P("MouseCable1 (1)", PrimitiveType.Cylinder, root.transform, V(0.1692f, 0.7488f, -1.5266f), V(0, 90, 90), V(0.0051f, 0.0767f, 0.0051f), matCable);
        P("MouseCable2", PrimitiveType.Cylinder, root.transform, V(-0.15f, 0.7431f, -1.6f), V(0, 35, 90), V(0.005f, 0.12f, 0.005f), matCable);
        P("LooseCable2", PrimitiveType.Cylinder, root.transform, V(0.3f, 0.745f, -1.7f), V(0, 340, 90), V(0.005f, 0.08f, 0.005f), matCable);

        // ============================================================
        // CAMERA (exact - rot Y=180)
        // ============================================================
        Camera existingCam = null;
        GameObject cam2 = GameObject.Find("Main Camera");
        if (cam2 != null) existingCam = cam2.GetComponent<Camera>();
        if (existingCam == null) { cam2 = new GameObject("Main Camera"); existingCam = cam2.AddComponent<Camera>(); cam2.tag = "MainCamera"; }
        cam2.transform.parent = root.transform;
        cam2.transform.localPosition = V(0.05f, 1.1f, -0.85f);
        cam2.transform.localRotation = Quaternion.Euler(12f, 180f, 0f);
        existingCam.fieldOfView = 60f; existingCam.nearClipPlane = 0.01f; existingCam.farClipPlane = 20f;
        existingCam.backgroundColor = new Color(0.01f, 0.01f, 0.02f); existingCam.clearFlags = CameraClearFlags.SolidColor;

        if (cam2.GetComponent<FirstPersonCamera>() == null) cam2.AddComponent<FirstPersonCamera>();
        if (cam2.GetComponent<PhoneInteraction>() == null) cam2.AddComponent<PhoneInteraction>();
        if (cam2.GetComponent<MonitorInteraction>() == null) cam2.AddComponent<MonitorInteraction>();
        if (cam2.GetComponent<DeskObjectInteraction>() == null) cam2.AddComponent<DeskObjectInteraction>();
        if (cam2.GetComponent<MonitorBrowser>() == null) cam2.AddComponent<MonitorBrowser>();

        if (FindAnyObjectByType<EventSystem>() == null) {
            GameObject esObj = new GameObject("EventSystem"); esObj.AddComponent<EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(); }

        // ============================================================
        // LIGHTING
        // ============================================================
        GameObject dirLight = new GameObject("AmbientMoonlight"); dirLight.transform.parent = root.transform;
        dirLight.transform.localRotation = Quaternion.Euler(50, -130, 0);
        Light dl = dirLight.AddComponent<Light>(); dl.type = LightType.Directional; dl.color = new Color(0.12f, 0.12f, 0.25f); dl.intensity = 0.08f; dl.shadows = LightShadows.Soft;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.03f, 0.03f, 0.04f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = 0.15f;
        RenderSettings.fogColor = new Color(0.01f, 0.01f, 0.02f);

        // ============================================================
        // MONITOR UI & PHONE UI
        // ============================================================
        GenerateMonitorUI(monitor, existingCam);
        GeneratePhoneUI(phone);

        Selection.activeGameObject = root;
        Debug.Log("OPENFEED Full Desk Scene generated with exact positions.");
    }

    // ============================================================
    // MONITOR UI
    // ============================================================
    static void GenerateMonitorUI(GameObject monitor, Camera cam)
    {
        GameObject co = new GameObject("MonitorCanvas"); Canvas cv = co.AddComponent<Canvas>(); cv.renderMode = RenderMode.WorldSpace;
        co.AddComponent<CanvasScaler>(); co.AddComponent<GraphicRaycaster>(); cv.worldCamera = cam;
        co.transform.SetParent(monitor.transform, false);
        RectTransform cr = co.GetComponent<RectTransform>(); cr.sizeDelta = new Vector2(760, 560);
        co.transform.localPosition = V(0.0035f, 0.2f, 0.2153f); co.transform.localRotation = Quaternion.Euler(0, 180, 0);
        co.transform.localScale = V(0.0005f, 0.0005f, 0.0005f);

        AddLight("ScreenGlow", co.transform, V(0, 0, -200), new Color(0.45f, 0.75f, 0.5f), 0.5f, 3.5f, true);
        AddLight("ScreenFill", co.transform, V(0, 0, -400), new Color(0.35f, 0.65f, 0.4f), 0.2f, 5f, false);

        GameObject bg = UI("Background", co.transform); Image bgI = bg.AddComponent<Image>(); bgI.color = new Color(0.88f, 0.92f, 0.89f); bgI.raycastTarget = true; Stretch(bg);

        // Top bar
        GameObject tb = UI("TopBar", co.transform); Image tbI = tb.AddComponent<Image>(); tbI.color = new Color(0.78f, 0.82f, 0.79f); tbI.raycastTarget = false;
        RectTransform tbR = tb.GetComponent<RectTransform>(); tbR.anchorMin = new Vector2(0, 1); tbR.anchorMax = new Vector2(1, 1); tbR.pivot = new Vector2(0.5f, 1); tbR.sizeDelta = new Vector2(0, 40);
        GameObject ad = UI("AddressBar", tb.transform); Image adI = ad.AddComponent<Image>(); adI.color = new Color(0.92f, 0.94f, 0.92f); adI.raycastTarget = false;
        RectTransform adR = ad.GetComponent<RectTransform>(); adR.anchorMin = new Vector2(0, 0.5f); adR.anchorMax = new Vector2(1, 0.5f); adR.pivot = new Vector2(0.5f, 0.5f); adR.sizeDelta = new Vector2(-100, 24);
        TXT("URLText", ad.transform, "http://openfeed.icu", 14, TextAlignmentOptions.Left, new Color(0.2f, 0.35f, 0.22f), new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 20), new Vector2(10, 0));

        // ScrollView
        GameObject sv = UI("ScrollView", co.transform); RectTransform svR = sv.GetComponent<RectTransform>();
        svR.anchorMin = Vector2.zero; svR.anchorMax = Vector2.one; svR.offsetMin = new Vector2(0, 30); svR.offsetMax = new Vector2(0, -40);
        ScrollRect scr = sv.AddComponent<ScrollRect>(); scr.horizontal = false; scr.vertical = true; scr.movementType = ScrollRect.MovementType.Elastic; scr.scrollSensitivity = 30f;
        Image svI = sv.AddComponent<Image>(); svI.color = new Color(0.88f, 0.92f, 0.89f); svI.raycastTarget = true; sv.AddComponent<Mask>().showMaskGraphic = true;

        GameObject ct = UI("Content", sv.transform); RectTransform ctR = ct.GetComponent<RectTransform>();
        ctR.anchorMin = new Vector2(0, 1); ctR.anchorMax = new Vector2(1, 1); ctR.pivot = new Vector2(0.5f, 1); ctR.sizeDelta = new Vector2(0, 900); scr.content = ctR;

        TXT("SiteTitle", ct.transform, "OPEN FEED", 42, TextAlignmentOptions.Center, new Color(0.15f, 0.2f, 0.15f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(500, 50), new Vector2(0, -30));
        TXT("Tagline", ct.transform, "live surveillance feeds - worldwide", 14, TextAlignmentOptions.Center, new Color(0.35f, 0.45f, 0.36f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(500, 24), new Vector2(0, -85));

        // Feeds
        string[] labels = { "CAM 01 - OSLO", "CAM 02 - MANILA", "CAM 03 - LAGOS", "CAM 04 - QUITO" };
        string[] status = { "LIVE", "LIVE", "OFFLINE", "LIVE" };
        Vector2[] feedPos = { new Vector2(-88.5f, -125), new Vector2(88.5f, -125), new Vector2(-88.5f, -247), new Vector2(88.5f, -247) };
        Color fN = new Color(0.1f, 0.12f, 0.1f), fH = new Color(0.15f, 0.2f, 0.16f), fP = new Color(0.08f, 0.1f, 0.08f);
        for (int i = 0; i < 4; i++) {
            GameObject f = UI($"Feed_{i}", ct.transform); Image fI = f.AddComponent<Image>(); fI.color = fN; fI.raycastTarget = true;
            RectTransform fR = f.GetComponent<RectTransform>(); fR.anchorMin = new Vector2(0.5f, 1); fR.anchorMax = new Vector2(0.5f, 1); fR.pivot = new Vector2(0.5f, 1);
            fR.sizeDelta = new Vector2(165, 110); fR.anchoredPosition = feedPos[i];
            Button btn = f.AddComponent<Button>(); ColorBlock cb = btn.colors; cb.normalColor = fN; cb.highlightedColor = fH; cb.pressedColor = fP; cb.fadeDuration = 0.1f; btn.colors = cb; btn.targetGraphic = fI;
            if (i == 2) btn.interactable = false;
            CameraFeedButton cfb = f.AddComponent<CameraFeedButton>(); cfb.feedIndex = i; cfb.feedName = labels[i]; cfb.isOnline = (i != 2);
            GameObject inn = UI($"FeedInner_{i}", f.transform); Image inI = inn.AddComponent<Image>(); inI.color = (i == 2) ? new Color(0.05f, 0.05f, 0.05f) : new Color(0.08f, 0.1f, 0.08f); inI.raycastTarget = false;
            RectTransform inR = inn.GetComponent<RectTransform>(); inR.anchorMin = Vector2.zero; inR.anchorMax = Vector2.one; inR.offsetMin = new Vector2(1, 1); inR.offsetMax = new Vector2(-1, -1);
            string hint = (i == 2) ? "NO SIGNAL" : "click to view"; Color hC = (i == 2) ? new Color(0.6f, 0.2f, 0.15f) : new Color(0.3f, 0.45f, 0.32f, 0.5f);
            TXT($"Hint{i}", inn.transform, hint, 11, TextAlignmentOptions.Center, hC, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(140, 24), Vector2.zero);
            Color sC = (i == 2) ? new Color(0.6f, 0.2f, 0.15f) : new Color(0.2f, 0.55f, 0.25f);
            TXT($"Label{i}", f.transform, labels[i], 9, TextAlignmentOptions.Left, new Color(0.55f, 0.7f, 0.57f), Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 16), new Vector2(5, 2));
            TXT($"Status{i}", f.transform, status[i], 8, TextAlignmentOptions.Right, sC, Vector2.one, Vector2.one, Vector2.one, new Vector2(50, 14), new Vector2(-5, -4));
            if (i != 2) { GameObject d = UI($"RecDot{i}", f.transform); Image dI = d.AddComponent<Image>(); dI.color = new Color(0.7f, 0.2f, 0.15f, 0.9f); dI.raycastTarget = false;
                RectTransform dR = d.GetComponent<RectTransform>(); dR.anchorMin = Vector2.one; dR.anchorMax = Vector2.one; dR.pivot = Vector2.one; dR.sizeDelta = new Vector2(5, 5); dR.anchoredPosition = new Vector2(-55, -6); }
        }

        TXT("Instructions", ct.transform, "select a feed to begin observation.\nyou are responsible for what you see.", 11, TextAlignmentOptions.Center, new Color(0.3f, 0.38f, 0.3f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(500, 40), new Vector2(0, -395));
        TXT("Disclaimer", ct.transform, "all feeds are unmonitored. proceed at your own risk.", 9, TextAlignmentOptions.Center, new Color(0.4f, 0.48f, 0.42f, 0.7f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(500, 20), new Vector2(0, -440));
        TXT("ForumLink", ct.transform, ">> community board", 12, TextAlignmentOptions.Center, new Color(0.25f, 0.45f, 0.55f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(300, 24), new Vector2(0, -520));
        TXT("FAQ", ct.transform, ">> faq / about", 12, TextAlignmentOptions.Center, new Color(0.25f, 0.45f, 0.55f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(300, 24), new Vector2(0, -548));
        TXT("Donate", ct.transform, ">> support the project", 12, TextAlignmentOptions.Center, new Color(0.25f, 0.45f, 0.55f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(300, 24), new Vector2(0, -576));
        TXT("FooterText", ct.transform, "openfeed.icu - est. 2019\n\"we only watch. we never interfere.\"\n\n12 users online", 9, TextAlignmentOptions.Center, new Color(0.4f, 0.45f, 0.42f, 0.5f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(400, 80), new Vector2(0, -630));
        TXT("VisitorCount", ct.transform, "total views: 847,203", 8, TextAlignmentOptions.Center, new Color(0.35f, 0.4f, 0.36f, 0.4f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(300, 20), new Vector2(0, -710));
        TXT("AsciiDecor", ct.transform, "- - - - - - - - - - - - -", 8, TextAlignmentOptions.Center, new Color(0.4f, 0.45f, 0.42f, 0.3f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(400, 16), new Vector2(0, -740));

        // Status bar
        GameObject sb = UI("StatusBar", co.transform); Image sbI = sb.AddComponent<Image>(); sbI.color = new Color(0.75f, 0.8f, 0.76f); sbI.raycastTarget = false;
        RectTransform sbR = sb.GetComponent<RectTransform>(); sbR.anchorMin = Vector2.zero; sbR.anchorMax = new Vector2(1, 0); sbR.pivot = new Vector2(0.5f, 0); sbR.sizeDelta = new Vector2(0, 28);
        TXT("ConnectionStatus", sb.transform, "connected", 10, TextAlignmentOptions.Left, new Color(0.2f, 0.45f, 0.25f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, 20), new Vector2(12, 0));
        TXT("Latency", sb.transform, "ping: 247ms", 10, TextAlignmentOptions.Right, new Color(0.3f, 0.4f, 0.32f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(120, 20), new Vector2(-12, 0));
        TXT("Encrypted", sb.transform, "TOR // encrypted", 10, TextAlignmentOptions.Center, new Color(0.35f, 0.45f, 0.37f, 0.6f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(150, 20), Vector2.zero);
    }

    // ============================================================
    // PHONE UI
    // ============================================================
    static void GeneratePhoneUI(GameObject phone)
    {
        GameObject co = new GameObject("PhoneCanvas"); Canvas cv = co.AddComponent<Canvas>(); cv.renderMode = RenderMode.WorldSpace;
        co.AddComponent<CanvasScaler>(); co.AddComponent<GraphicRaycaster>();
        co.transform.SetParent(phone.transform, false);
        RectTransform cr = co.GetComponent<RectTransform>(); cr.sizeDelta = new Vector2(550, 950);
        co.transform.localPosition = V(0, 0.012f, 0.01f); co.transform.localRotation = Quaternion.Euler(90, 0, 0);
        co.transform.localScale = V(0.0001f, 0.0001f, 0.0001f);

        AddLight("PhoneScreenGlow", co.transform, V(0, 0, -500), new Color(0.6f, 0.65f, 0.85f), 0.05f, 0.3f, false);
        GameObject bg = UI("Background", co.transform); Image bgI = bg.AddComponent<Image>(); bgI.color = new Color(0.12f, 0.12f, 0.18f); Stretch(bg);

        TXT("Time", co.transform, "21:37", 22, TextAlignmentOptions.Left, new Color(0.7f, 0.7f, 0.75f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(120, 40), new Vector2(20, -10));
        TXT("Battery", co.transform, "73%", 20, TextAlignmentOptions.Right, new Color(0.7f, 0.7f, 0.75f), Vector2.one, Vector2.one, Vector2.one, new Vector2(80, 40), new Vector2(-20, -10));
        TXT("NumberDisplay", co.transform, "_ _ _ - _ _ _ _", 36, TextAlignmentOptions.Center, new Color(0.8f, 0.85f, 0.9f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(450, 60), new Vector2(0, -140));
        TXT("DialLabel", co.transform, "enter number", 18, TextAlignmentOptions.Center, new Color(0.4f, 0.4f, 0.45f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(300, 30), new Vector2(0, -200));

        // Numpad
        string[,] keys = { {"1","2","3"}, {"4","5","6"}, {"7","8","9"}, {"*","0","#"} };
        string[,] subs = { {"","ABC","DEF"}, {"GHI","JKL","MNO"}, {"PQRS","TUV","WXYZ"}, {"","",""} };

        GameObject np = UI("Numpad", co.transform); RectTransform npR = np.GetComponent<RectTransform>();
        npR.anchorMin = new Vector2(0.5f, 0); npR.anchorMax = new Vector2(0.5f, 1); npR.pivot = new Vector2(0.5f, 1);
        npR.sizeDelta = new Vector2(420, 600); npR.anchoredPosition = new Vector2(0, -260);

        float[] keyY = { -20, -150, -280, -410 };
        for (int r = 0; r < 4; r++) for (int c = 0; c < 3; c++) {
            float kx = -130 + c * 130; float ky = keyY[r];
            GameObject kb = UI($"Key_{keys[r,c]}", np.transform); Image kI = kb.AddComponent<Image>(); kI.color = new Color(0.18f, 0.18f, 0.24f);
            RectTransform kR = kb.GetComponent<RectTransform>(); kR.anchorMin = new Vector2(0.5f, 1); kR.anchorMax = new Vector2(0.5f, 1); kR.pivot = new Vector2(0.5f, 0.5f);
            kR.sizeDelta = new Vector2(110, 110); kR.anchoredPosition = new Vector2(kx, ky);
            TXT($"KL_{keys[r,c]}", kb.transform, keys[r,c], 32, TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.9f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(80, 40), Vector2.zero);
            if (!string.IsNullOrEmpty(subs[r,c]))
                TXT($"KS_{keys[r,c]}", kb.transform, subs[r,c], 12, TextAlignmentOptions.Center, new Color(0.45f, 0.45f, 0.5f), new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f), new Vector2(80, 20), Vector2.zero);
        }

        GameObject callB = UI("CallButton", np.transform); Image cBI = callB.AddComponent<Image>(); cBI.color = new Color(0.1f, 0.45f, 0.15f);
        RectTransform cBR = callB.GetComponent<RectTransform>(); cBR.anchorMin = new Vector2(0.5f, 1); cBR.anchorMax = new Vector2(0.5f, 1); cBR.pivot = new Vector2(0.5f, 0.5f);
        cBR.sizeDelta = new Vector2(200, 55); cBR.anchoredPosition = new Vector2(0, -550);
        TXT("CallLabel", callB.transform, "CALL", 22, TextAlignmentOptions.Center, new Color(0.8f, 1f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(180, 45), Vector2.zero);

        GameObject delB = UI("DeleteButton", np.transform); Image dBI = delB.AddComponent<Image>(); dBI.color = new Color(0.25f, 0.14f, 0.14f);
        RectTransform dBR = delB.GetComponent<RectTransform>(); dBR.anchorMin = new Vector2(0.5f, 1); dBR.anchorMax = new Vector2(0.5f, 1); dBR.pivot = new Vector2(0.5f, 0.5f);
        dBR.sizeDelta = new Vector2(90, 55); dBR.anchoredPosition = new Vector2(130, -550);
        TXT("DelLabel", delB.transform, "<", 28, TextAlignmentOptions.Center, new Color(0.8f, 0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60, 45), Vector2.zero);

        GameObject nv = UI("NavBar", co.transform); Image nvI = nv.AddComponent<Image>(); nvI.color = new Color(0.08f, 0.08f, 0.12f);
        RectTransform nvR = nv.GetComponent<RectTransform>(); nvR.anchorMin = Vector2.zero; nvR.anchorMax = new Vector2(1, 0); nvR.pivot = new Vector2(0.5f, 0); nvR.sizeDelta = new Vector2(0, 70);
        string[] navL = { "recent", "contacts", "keypad", "voicemail" };
        float[] navX = { -206.25f, -68.75f, 68.75f, 206.25f };
        for (int n = 0; n < navL.Length; n++) {
            Color nc = (n == 2) ? new Color(0.4f, 0.6f, 0.9f) : new Color(0.4f, 0.4f, 0.45f);
            TXT($"Nav_{navL[n]}", nv.transform, navL[n], 14, TextAlignmentOptions.Center, nc, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120, 50), new Vector2(navX[n], 35)); }
    }

    // ============================================================
    // HELPERS
    // ============================================================
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    static Vector3 V3 => Vector3.zero;

    static GameObject P(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 rot, Vector3 scale, Material mat) {
        GameObject o = GameObject.CreatePrimitive(type); o.name = name; o.transform.parent = parent;
        o.transform.localPosition = pos; o.transform.localRotation = Quaternion.Euler(rot); o.transform.localScale = scale;
        if (mat != null) o.GetComponent<Renderer>().sharedMaterial = mat;
        Collider c = o.GetComponent<Collider>(); if (c != null) DestroyImmediate(c); return o; }

    static GameObject UI(string name, Transform parent) {
        GameObject o = new GameObject(name); o.transform.SetParent(parent, false); o.AddComponent<RectTransform>(); return o; }

    static void Stretch(GameObject o) {
        RectTransform r = o.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero; }

    static GameObject TXT(string name, Transform parent, string text, float size, TextAlignmentOptions align, Color color,
        Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 sizeDelta, Vector2 pos) {
        GameObject o = new GameObject(name); o.transform.SetParent(parent, false);
        TextMeshProUGUI t = o.AddComponent<TextMeshProUGUI>(); t.text = text; t.fontSize = size; t.alignment = align; t.color = color;
        t.enableAutoSizing = false; t.overflowMode = TextOverflowModes.Overflow; t.raycastTarget = false;
        RectTransform r = o.GetComponent<RectTransform>(); r.anchorMin = aMin; r.anchorMax = aMax; r.pivot = pivot; r.sizeDelta = sizeDelta; r.anchoredPosition = pos; return o; }

    static void AddLight(string name, Transform parent, Vector3 pos, Color color, float intensity, float range, bool shadows) {
        GameObject o = new GameObject(name); o.transform.SetParent(parent, false); o.transform.localPosition = pos;
        Light l = o.AddComponent<Light>(); l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = range;
        l.shadows = shadows ? LightShadows.Soft : LightShadows.None; }

    static Shader FindShader() {
        Shader s = Shader.Find("Shader Graphs/URP_PSX_PBR_Master");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        return s; }

    static Shader FindUnlitShader() {
        Shader s = Shader.Find("Shader Graphs/URP_PSX_Unlit_Master");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Unlit/Color");
        return s; }

    static Shader FindTransparentShader() {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        return s; }

    static Material Matte(string name, Color color) {
        Material m = new Material(FindShader()); m.name = name;
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        else if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        else m.color = color;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0f);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        return m; }

    static Material Emit(string name, Color baseColor, Color emitColor, float intensity) {
        Material m = new Material(FindUnlitShader()); m.name = name;
        Color combined = baseColor + emitColor * intensity;
        if (m.HasProperty("_Color")) m.SetColor("_Color", combined);
        else if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", combined);
        else m.color = combined;
        return m; }

    static Material LoadMat(string path, string fallbackName, Color fallbackColor) {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) { m.SetFloat("_Smoothness", 0f); m.SetFloat("_Metallic", 0f); return m; }
        Debug.LogWarning($"Material not found: {path}"); return Matte(fallbackName, fallbackColor); }

    static Material LoadMatTex(string name, Color tint, string texPath) {
        Material m = new Material(FindShader()); m.name = name;
        if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
        else if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        else m.color = Color.white;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0f);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex != null) {
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            else if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            else m.mainTexture = tex;
        }
        else {
            if (m.HasProperty("_Color")) m.SetColor("_Color", tint);
            else if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
            else m.color = tint;
        }
        return m; }
}