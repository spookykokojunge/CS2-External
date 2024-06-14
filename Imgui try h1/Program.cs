using Imgui_try_h1;
using Swed64;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Concurrent;

// Main Logic

// Init swed
Swed swed = new Swed("cs2");

// Get Client Module
IntPtr client = swed.GetModuleBase("client.dll");

// Init Render
Renderer renderer = new Renderer();
Thread renderThread = new Thread(new ThreadStart(renderer.Start().Wait));
renderThread.Start();

// Get Screen Size From Renderer
Vector2 screenSize = renderer.screenSize;

// Store Entities
ConcurrentBag<Entity> entities = new ConcurrentBag<Entity>();
Entity localPlayer = new Entity();

// Create reusable buffers
float[] viewMatrix = new float[16];
Vector3 localPlayerPosition = new Vector3();
IntPtr[] listEntries = new IntPtr[64];
IntPtr[] currentControllers = new IntPtr[64];
int[] pawnHandles = new int[64];
IntPtr[] currentPawns = new IntPtr[64];
int[] lifeStates = new int[64];
bool[] isSpotted = new bool[64];

// Now ESP Loop
while (true)
{
    entities = new ConcurrentBag<Entity>(); // Clean List

    // Get Entity List
    IntPtr entityList = swed.ReadPointer(client, Offsets.dwEntityList);

    // Make Entry
    IntPtr listEntry = swed.ReadPointer(entityList, 0x10);

    // Get Localplayer
    IntPtr loaclPlayerPawn = swed.ReadPointer(client, Offsets.dwLocalPlayerPawn);

    // Get Team, So We Can Compare With Other Entities
    localPlayer.team = swed.ReadInt(loaclPlayerPawn, Offsets.m_iTeamNum);

    // Get Local Player Position
    localPlayer.position = swed.ReadVec(loaclPlayerPawn, Offsets.m_vOldOrigin);

    // Read view matrix once
    viewMatrix = swed.ReadMatrix(client + Offsets.dwViewMatrix);

    // Loop Through Entity List in parallel
    Parallel.For(0, 64, i =>
    {
        // Get Current Controller
        currentControllers[i] = swed.ReadPointer(listEntry, i * 0x78);
        if (currentControllers[i] == IntPtr.Zero) return; // Check

        // Get Pawn Handle
        pawnHandles[i] = swed.ReadInt(currentControllers[i], Offsets.m_hPlayerPawn);
        if (pawnHandles[i] == 0) return;

        // Get Current Pawn, Make Second Entry
        IntPtr listEntry2 = swed.ReadPointer(entityList, 0x8 * ((pawnHandles[i] & 0x7FFF) >> 9) + 0x10);

        // Get Current Pawn
        currentPawns[i] = swed.ReadPointer(listEntry2, 0x78 * (pawnHandles[i] & 0x1FF));
        if (currentPawns[i] == IntPtr.Zero) return;

        // Check If Lifestate
        lifeStates[i] = swed.ReadInt(currentPawns[i], Offsets.m_lifeState);
        if (lifeStates[i] != 256) return;

        // Populate Entity
        Entity entity = new Entity();
        IntPtr sceneNode = swed.ReadPointer(currentPawns[i], Offsets.m_pGameSceneNode);
        IntPtr boneMatrix = swed.ReadPointer(sceneNode, Offsets.m_modelState + 0x80); // 0x80 would be dwBoneMatrix

        entity.name = swed.ReadString(currentControllers[i], Offsets.m_iszPlayerName, 16).Split("\0")[0]; // read name
        entity.team = swed.ReadInt(currentPawns[i], Offsets.m_iTeamNum);
        entity.spotted = swed.ReadBool(currentPawns[i], Offsets.m_entitySpottedState + Offsets.m_bSpotted); // Read If Entity Is Spotted Compared To The Localplayer
        entity.health = swed.ReadInt(currentPawns[i], Offsets.m_iHealth); // read current entity health
        entity.position = swed.ReadVec(currentPawns[i], Offsets.m_vOldOrigin); // read current entity position
        entity.viewOffset = swed.ReadVec(currentPawns[i], Offsets.m_vecViewOffset);
        entity.position2D = Calculate.WorldToScreen(viewMatrix, entity.position, screenSize);
        entity.viewPositon2D = Calculate.WorldToScreen(viewMatrix, Vector3.Add(entity.position, entity.viewOffset), screenSize);
        entity.distance = Vector3.Distance(entity.position, localPlayer.position);
        entity.bones = Calculate.ReadBones(boneMatrix, swed);
        entity.bones2d = Calculate.ReadBones2d(entity.bones, viewMatrix, screenSize);

        entities.Add(entity);
    });

    // Update Renderer Data
    renderer.UpdateLocalPlayer(localPlayer);
    renderer.UpdateEntities(entities.ToList());

    // Thread.Sleep(1); // <--Optonal Thread
}
