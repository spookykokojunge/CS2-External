using Imgui_try_h1;
using Swed64;
using System.Numerics;
using System.Reflection;

//Main Logic

//init swed
Swed swed = new Swed("cs2");

//Get Client Module
IntPtr client = swed.GetModuleBase("client.dll");

//Init Render
Renderer renderer = new Renderer();
Thread renderThread = new Thread(new ThreadStart(renderer.Start().Wait));
renderThread.Start();

//Get Screen Size From Renderer
Vector2 screenSize = renderer.screenSize;

//Store Entities
List<Entity> entities = new List<Entity>();
Entity localPlayer = new Entity();

//Offsets

//Offsets.cs <-- these normally update every game patch
int dwEntityList = 0x18C7F98;
int dwViewMatrix = 0x1929430;
int dwLocalPlayerPawn = 0x173B568;

//Client.dll.cs
int m_vOldOrigin = 0x127C; // Vector
int m_iTeamNum = 0x3CB; // uint8_t
int m_lifeState = 0x338; // uint8_t
int m_hPlayerPawn = 0x7E4; // CHandle<C_CSPlayerPawn>
int m_vecViewOffset = 0xC58; // CNetworkViewOffsetVector
int m_iHealth = 0x334;
int m_entitySpottedState = 0x1698;
int m_bSpotted = 0x8;
int m_iszPlayerName = 0x638;
int m_pCameraServices = 0x1138;
int m_iFOV = 0x210;
int m_bIsScoped = 0x1400;
int m_flFlashBangTime = 0x14B8;
int m_pGameSceneNode = 0x318;
int m_modelState = 0x160;


//Now ESP Loop

while (true)
{
    entities.Clear(); //Clean List

    //Get Entity List
    IntPtr entityList = swed.ReadPointer(client, dwEntityList);

    //Make Entry
    IntPtr listEntry = swed.ReadPointer(entityList, 0x10);

    //Get Localplayer
    IntPtr loaclPlayerPawn = swed.ReadPointer(client, dwLocalPlayerPawn);

    //Get Team, So We Can Compare With Other Entities
    localPlayer.team = swed.ReadInt(loaclPlayerPawn, m_iTeamNum);

    //Loop Through Entity List

    for (int i = 0; i < 64; i++)
    {
        //Populate Entity
        Entity entity = new Entity();

        //Get Current Controller
        IntPtr currentController = swed.ReadPointer(listEntry, i * 0x78);

        if (currentController == IntPtr.Zero) continue; //Check

        //Get Pawn Handle
        int pawnHandle = swed.ReadInt(currentController, m_hPlayerPawn);
        if (pawnHandle == 0) continue;

        //Get Current Pawn, Make Second Entry
        IntPtr listEntry2 = swed.ReadPointer(entityList, 0x8 * ((pawnHandle & 0x7FFF) >> 9) + 0x10);

        //Get Current Pawn
        IntPtr currentPawn = swed.ReadPointer(listEntry2, 0x78 * (pawnHandle & 0x1FF));
        if (currentPawn == IntPtr.Zero) continue;

        //Check If Lifestate
        int lifeState = swed.ReadInt(currentPawn, m_lifeState);
        if (lifeState != 256) continue;

        //Get Matrix
        float[] viewMatrix = swed.ReadMatrix(client + dwViewMatrix);
        
        IntPtr sceneNode = swed.ReadPointer(currentPawn, m_pGameSceneNode);
        IntPtr boneMatrix = swed.ReadPointer(sceneNode, m_modelState + 0x80); // 0x80 would be dwBoneMatrix))

        entity.name = swed.ReadString(currentController, m_iszPlayerName, 16).Split("\0")[0]; //read name
        entity.team = swed.ReadInt(currentPawn, m_iTeamNum);
        entity.spotted = swed.ReadBool(currentPawn, m_entitySpottedState + m_bSpotted); //Read If Entity Is Spotted Compared To The Localplayer
        entity.health = swed.ReadInt(currentPawn, m_iHealth); //read current entity health
        entity.position = swed.ReadVec(currentPawn, m_vOldOrigin); //read current entity position
        entity.viewOffset = swed.ReadVec(currentPawn, m_vecViewOffset);
        entity.position2D = Calculate.WorldToScreen(viewMatrix, entity.position, screenSize);
        entity.viewPositon2D = Calculate.WorldToScreen(viewMatrix, Vector3.Add(entity.position, entity.viewOffset), screenSize);
        entity.distance  = Vector3.Distance(entity.position, localPlayer.position);
        entity.bones = Calculate.ReadBones(boneMatrix, swed);
        entity.bones2d = Calculate.ReadBones2d(entity.bones, viewMatrix, screenSize);


        entities.Add(entity);

        uint desiredFov = (uint)renderer.fov;


        IntPtr cameraServices = swed.ReadPointer(currentPawn, m_pCameraServices);

        uint currentFov = swed.ReadUInt(cameraServices, m_iFOV);

        bool isScoped = swed.ReadBool(dwLocalPlayerPawn, m_bIsScoped);

        bool enableFOV1 = (bool)renderer.enableFOV;

        if (enableFOV1)
        {
            if (!isScoped && currentFov != desiredFov)
            {
                swed.WriteUInt(cameraServices + m_iFOV, desiredFov);

            }


        }




    }
    //Update Renderer Data
    renderer.UpdateLocalPlayer(localPlayer);
    renderer.UpdateEntities(entities);

    //Thread.Sleep(1); //<--Optonal Thread
}
