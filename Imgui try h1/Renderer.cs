using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ClickableTransparentOverlay;
using ImGuiNET;

namespace Imgui_try_h1
{
    public class Renderer : Overlay
    {
        //Render Variables
        public Vector2 screenSize = new Vector2(1920, 1080); //ScreenSize of your Screen
        
        //entities copy , using more Threads is a safer method.
        private ConcurrentQueue<Entity> entities = new ConcurrentQueue<Entity>();
        private Entity localPlayer = new Entity();
        private readonly object entityLock = new object();

        // GUI Elements Variables
        private bool enableESP = true;
        private bool enableLine = false;
        public bool enableHealth = true;
        public bool enableName = true;
        private bool enableVisibilityCheck = true;
        public bool enableFOV = false;

        public int fov = 60; //default fov

        //collors
        private Vector4 enemyColor = new Vector4(1, 0, 0, 1); //Default Red
        private Vector4 teamColor = new Vector4(0, 1, 0, 1); //Default Green
        private Vector4 hiddenColor = new Vector4(0, 0, 0, 1); //Default Black
        private Vector4 nameColor = new Vector4(1, 1, 1, 1); //Default White
        private Vector4 boneColor = new Vector4(1, 1, 1, 1); //Default White

        float boneThickness = 4;

        //draw list
        ImDrawListPtr drawList;

        protected override void Render()
        {
            //ImGui menu

            ImGui.Begin("Basic Cheat");
            ImGui.Checkbox("ESP", ref  enableESP);
            ImGui.Checkbox("Name", ref  enableName);
            ImGui.Checkbox("VisCheck", ref  enableVisibilityCheck);
            ImGui.Checkbox("Lines", ref enableLine);
            ImGui.Checkbox("Health", ref enableHealth);
            ImGui.Checkbox("FOV", ref enableFOV);

            ImGui.SliderInt("FOV",ref fov, 60, 140); //current, min, max


            //enemy color
            if (ImGui.CollapsingHeader("Enemy Color"))
                ImGui.ColorPicker4("##Enemy", ref enemyColor);

            //bone color
            if (ImGui.CollapsingHeader("Bone Color"))
                ImGui.ColorPicker4("##Enemy", ref boneColor);


            //Draw Overlay
            DrawOverlay(screenSize);
            drawList = ImGui.GetWindowDrawList();

            //draw stuff
            if (enableESP)
            {
                foreach(var entity in entities)
                {
                    if (EntityOnScreen(entity) && localPlayer.team != entity.team) {
                                            //Check If Entity On Screen
                        if (EntityOnScreen(entity))
                    {
                        //Draw Methods
                        DrawBox(entity);
                        DrawLine(entity);
                        DrawHealthBar(entity);
                        DrawName(entity,20);
                        DrawBones(entity);
                    
                    }

                    }
                }
            }
        }

        //Check Position
        bool EntityOnScreen(Entity entity)
        {
            if (entity.position2D.X > 0 && entity.position2D.X < screenSize.X && entity.position2D.Y > 0 && entity.position2D.Y < screenSize.Y)
            {
                return true;
            }
            return false;
        }


        //Drawing Methods

        private void DrawBones(Entity entity)
        {
            uint uintColor = ImGui.ColorConvertFloat4ToU32(boneColor);

            float currentBoneThickness = boneThickness / entity.distance;

            drawList.AddLine(entity.bones2d[1], entity.bones2d[2], uintColor, currentBoneThickness); //neckt to head
            drawList.AddLine(entity.bones2d[1], entity.bones2d[3], uintColor, currentBoneThickness); //neck to left shoulder
            drawList.AddLine(entity.bones2d[1], entity.bones2d[6], uintColor, currentBoneThickness); //neck to right shoulder
            drawList.AddLine(entity.bones2d[3], entity.bones2d[4], uintColor, currentBoneThickness); //shoulderRight to ar
            drawList.AddLine(entity.bones2d[6], entity.bones2d[7], uintColor, currentBoneThickness); //shoulderLeft to ar
            drawList.AddLine(entity.bones2d[4], entity.bones2d[5], uintColor, currentBoneThickness); //armLeft to handLeft
            drawList.AddLine(entity.bones2d[7], entity.bones2d[8], uintColor, currentBoneThickness); //armRight to rightHand
            drawList.AddLine(entity.bones2d[1], entity.bones2d[0], uintColor, currentBoneThickness); //neck to waist
            drawList.AddLine(entity.bones2d[0], entity.bones2d[9], uintColor, currentBoneThickness); //waist to kneeLeft
            drawList.AddLine(entity.bones2d[0], entity.bones2d[11], uintColor, currentBoneThickness); //waist to kneeRight
            drawList.AddLine(entity.bones2d[9], entity.bones2d[10], uintColor, currentBoneThickness); //kneeLeft to feetLeft
            drawList.AddLine(entity.bones2d[11], entity.bones2d[12], uintColor, currentBoneThickness); //knee to idk
            drawList.AddCircle(entity.bones2d[2], 3 + currentBoneThickness, uintColor);
        }


        private void DrawName(Entity entity, int yOffset)
        {
            if (enableName) 
            {    
            Vector2 textLocation = new Vector2(entity.viewPositon2D.X, entity.viewPositon2D.Y - yOffset); //Get render location of name
            drawList.AddText(textLocation, ImGui.ColorConvertFloat4ToU32(nameColor), $"{entity.name}"); //Draw on screen
            }

        }




        private void DrawHealthBar(Entity entity)
        {
            if (enableHealth)
            {
                //calculate bar height
                float entityHeight = entity.position2D.Y - entity.viewPositon2D.Y;


                //get box location
                float boxLeft = entity.viewPositon2D.X - entityHeight / 3;
                float boxRight = entity.position2D.X + entityHeight / 3;


                //calculate health bar width
                float barPercentWidth = 0.05f; // 5% ore 1/20 of box width
                float barPixelWidth = barPercentWidth * (boxRight - boxLeft);


                //calculate bar height after health
                float barHeight = entityHeight * (entity.health / 100f);


                //calculate bar rectange, two vertors
                Vector2 barTop = new Vector2(boxLeft - barPixelWidth, entity.position2D.Y - barHeight);
                Vector2 barBottom = new Vector2(boxLeft, entity.position2D.Y);


                Vector4 barColor = new Vector4(0, 1, 0, 1);


                //draw health bar
                drawList.AddRectFilled(barTop, barBottom, ImGui.ColorConvertFloat4ToU32(barColor));

            }
            


        }


        private void DrawBox(Entity entity)
        {
            //Calculate Box Height
            float entityHeight = entity.position2D.Y - entity.viewPositon2D.Y;

            //Calculate Box Dimensions
            Vector2 rectTop = new Vector2(entity.viewPositon2D.X - entityHeight / 3, entity.viewPositon2D.Y);

            Vector2 rectBottom = new Vector2(entity.position2D.X + entityHeight / 3, entity.position2D.Y);

            //Get Correct Color
            Vector4 boxColor = localPlayer.team == entity.team ? teamColor : enemyColor;

            //Check If Player Is Visible Ore Hidden
            if (enableVisibilityCheck)
                boxColor = entity.spotted == true ? boxColor : hiddenColor; //Set Color To Black If Not Spotted


            drawList.AddRect(rectTop, rectBottom, ImGui.ColorConvertFloat4ToU32(boxColor));
        }




        private void DrawLine(Entity entity)
        {
            if (enableLine)
            {
                //Get Correct Color
                Vector4 lineColor = localPlayer.team == entity.team ? teamColor : enemyColor;

                //Draw Line
                drawList.AddLine(new Vector2(screenSize.X / 2, screenSize.Y), entity.position2D, ImGui.ColorConvertFloat4ToU32(lineColor));

            }

        }






        //transfer entity methods


        public void UpdateEntities(IEnumerable<Entity> newEntities) // update entities
        {
            entities = new ConcurrentQueue<Entity>(newEntities);
        }


        public void UpdateLocalPlayer(Entity newEntity) //Update Localplayer
        {
            localPlayer = newEntity;
        }


        public Entity GetLocalPlayer() //Get Localplayer
        {
            lock(entityLock)
            {
                return localPlayer;
            }
        }


        void DrawOverlay(Vector2 screeSize) // Overlay Window 
        {

            ImGui.SetNextWindowSize(screenSize);
            ImGui.SetNextWindowPos(new Vector2(0, 0)); // in the beninguing
            ImGui.Begin("Overlay", ImGuiWindowFlags.NoDecoration
                | ImGuiWindowFlags.NoBackground
                | ImGuiWindowFlags.NoBringToFrontOnFocus
                | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoInputs
                | ImGuiWindowFlags.NoCollapse
                | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse
                );
        }
    }
}
