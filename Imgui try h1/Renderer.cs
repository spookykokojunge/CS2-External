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
        // Render Variables
        public Vector2 screenSize = new Vector2(1920, 1080); // Screen size

        // Entities, using concurrent collection for thread safety
        private ConcurrentQueue<Entity> entities = new ConcurrentQueue<Entity>();
        private Entity localPlayer = new Entity();
        private readonly object entityLock = new object();

        // GUI Elements Variables
        private bool enableESP = true;
        private bool enableLine = false;
        private bool enableHealth = true;
        private bool enableName = false;
        private bool enableVisibilityCheck = false;
        private bool enableBox = false;
        private bool enableBone = true;
        private bool enableTeammateESP = false; // New variable to enable/disable ESP for teammates

        // Colors
        private Vector4 enemyColor = new Vector4(1, 0, 0, 1); // Red
        private Vector4 teamColor = new Vector4(0, 1, 0, 1); // Green
        private Vector4 hiddenColor = new Vector4(0, 0, 0, 1); // Black
        private Vector4 nameColor = new Vector4(1, 1, 1, 1); // White
        private Vector4 boneColor = new Vector4(1, 1, 1, 1); // White
        private Vector4 lowHealthColor = new Vector4(1, 1, 0, 1); // Yellow
        private Vector4 criticalHealthColor = new Vector4(1, 0.5f, 0, 1); // Orange

        float boneThickness = 4;

        // Draw list
        ImDrawListPtr drawList;

        protected override void Render()
        {
            ImGuiStylePtr style = ImGui.GetStyle();

            style.WindowBorderSize = 1f;
            style.WindowRounding = 8f;
            style.Colors[(int)ImGuiCol.Border] = new Vector4(252 / 255f, 106 / 255f, 0 / 255f, 1f);
            style.Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(252 / 255f, 106 / 255f, 0 / 255f, 1f);
            style.Colors[(int)ImGuiCol.Text] = new Vector4(1,1,1,1f);
            style.Colors[(int)ImGuiCol.Tab] = new Vector4(252 / 255f, 106 / 255f, 0 / 255f, 1f);
            style.Colors[(int)ImGuiCol.TabActive] = new Vector4(252 / 255f, 106 / 255f, 0 / 255f, 1f);
            style.Colors[(int)ImGuiCol.TabHovered] = new Vector4(232 / 255f, 90 / 255f, 0 / 255f, 1f);

            ImGui.Begin("Zynx - Sookyisnice");

            if (ImGui.BeginTabBar("SettingsTabs"))
            {
                if (ImGui.BeginTabItem("ESP"))
                {
                    RenderESPSettings();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Colors"))
                {
                    RenderColorSettings();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            DrawOverlay(screenSize);
            drawList = ImGui.GetWindowDrawList();

            if (enableESP)
            {
                foreach (var entity in entities)
                {
                    bool isTeammate = localPlayer.team == entity.team;
                    if (EntityOnScreen(entity) && (!isTeammate || (isTeammate && enableTeammateESP)))
                    {
                        DrawEntity(entity);
                    }
                }
            }
        }

        private void RenderESPSettings()
        {
            ImGui.Checkbox("ESP", ref enableESP);
            ImGui.Checkbox("Box", ref enableBox);
            ImGui.Checkbox("Bone", ref enableBone);
            ImGui.Checkbox("Name", ref enableName);
            ImGui.Checkbox("VisCheck", ref enableVisibilityCheck);
            ImGui.Checkbox("Lines", ref enableLine);
            ImGui.Checkbox("Health", ref enableHealth);
            ImGui.Checkbox("Teammate ESP", ref enableTeammateESP); // New checkbox for teammate ESP
        }

        private void RenderColorSettings()
        {
            ImGui.ColorEdit4("Enemy", ref enemyColor, ImGuiColorEditFlags.PickerMask | ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Bone", ref boneColor, ImGuiColorEditFlags.PickerMask | ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Name", ref nameColor, ImGuiColorEditFlags.PickerMask | ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Low Health", ref lowHealthColor, ImGuiColorEditFlags.PickerMask | ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Critical Health", ref criticalHealthColor, ImGuiColorEditFlags.PickerMask | ImGuiColorEditFlags.NoInputs);
        }

        private void DrawEntity(Entity entity)
        {
            DrawBox(entity);
            DrawLine(entity);
            DrawHealthBar(entity);
            DrawName(entity, 20);
            DrawBones(entity);
        }

        private bool EntityOnScreen(Entity entity)
        {
            return entity.position2D.X > 0 && entity.position2D.X < screenSize.X &&
                   entity.position2D.Y > 0 && entity.position2D.Y < screenSize.Y;
        }

        private void DrawBones(Entity entity)
        {
            if (!enableBone) return;

            uint color = ImGui.ColorConvertFloat4ToU32(boneColor);
            float thickness = boneThickness / entity.distance;

            drawList.AddLine(entity.bones2d[1], entity.bones2d[2], color, thickness);
            drawList.AddLine(entity.bones2d[1], entity.bones2d[3], color, thickness);
            drawList.AddLine(entity.bones2d[1], entity.bones2d[6], color, thickness);
            drawList.AddLine(entity.bones2d[3], entity.bones2d[4], color, thickness);
            drawList.AddLine(entity.bones2d[6], entity.bones2d[7], color, thickness);
            drawList.AddLine(entity.bones2d[4], entity.bones2d[5], color, thickness);
            drawList.AddLine(entity.bones2d[7], entity.bones2d[8], color, thickness);
            drawList.AddLine(entity.bones2d[1], entity.bones2d[0], color, thickness);
            drawList.AddLine(entity.bones2d[0], entity.bones2d[9], color, thickness);
            drawList.AddLine(entity.bones2d[0], entity.bones2d[11], color, thickness);
            drawList.AddLine(entity.bones2d[9], entity.bones2d[10], color, thickness);
            drawList.AddLine(entity.bones2d[11], entity.bones2d[12], color, thickness);
            drawList.AddCircle(entity.bones2d[2], 3 + thickness, color);
        }

        private void DrawName(Entity entity, int yOffset)
        {
            if (!enableName) return;

            Vector2 textLocation = new Vector2(entity.viewPositon2D.X, entity.viewPositon2D.Y - yOffset);
            drawList.AddText(textLocation, ImGui.ColorConvertFloat4ToU32(nameColor), entity.name);
        }

        private void DrawHealthBar(Entity entity)
        {
            if (!enableHealth) return;

            // Calculate bar height
            float entityHeight = entity.position2D.Y - entity.viewPositon2D.Y;

            // Get box location
            float boxLeft = entity.viewPositon2D.X - entityHeight / 3;
            float boxRight = entity.position2D.X + entityHeight / 3;

            // Calculate health bar width
            float barPercentWidth = 0.05f; // 5% or 1/20 of box width
            float barPixelWidth = barPercentWidth * (boxRight - boxLeft);

            // Calculate bar height after health
            float barHeight = entityHeight * (entity.health / 100f);

            // Calculate bar rectangle, two vectors
            Vector2 barTop = new Vector2(boxLeft - barPixelWidth, entity.position2D.Y - barHeight);
            Vector2 barBottom = new Vector2(boxLeft, entity.position2D.Y);

            // Determine bar color based on health
            Vector4 barColor;
            if (entity.health < 10)
            {
                barColor = new Vector4(1, 0, 0, 1); // Red
            }
            else if (entity.health < 25)
            {
                barColor = new Vector4(1, 0.5f, 0, 1); // Orange
            }
            else if (entity.health < 50)
            {
                barColor = new Vector4(1, 1, 0, 1); // Yellow
            }
            else if (entity.health < 75)
            {
                barColor = new Vector4(0.5f, 1, 0, 1); // Lime Green
            }
            else
            {
                barColor = new Vector4(0, 1, 0, 1); // Green
            }

            // Draw health bar
            drawList.AddRectFilled(barTop, barBottom, ImGui.ColorConvertFloat4ToU32(barColor));

            // Draw health text
            string healthText = entity.health.ToString();
            Vector2 textSize = ImGui.CalcTextSize(healthText);
            Vector2 textPosition = new Vector2(barTop.X - textSize.X - 2, barTop.Y - textSize.Y / 2);

            // Determine text color based on health
            Vector4 textColor;
            if (entity.health < 20)
            {
                textColor = new Vector4(1, 0, 0, 1); // Red
            }
            else if (entity.health < 40)
            {
                textColor = new Vector4(1, 0.5f, 0, 1); // Orange
            }
            else if (entity.health < 60)
            {
                textColor = new Vector4(1, 1, 0, 1); // Yellow
            }
            else if (entity.health < 80)
            {
                textColor = new Vector4(0.5f, 1, 0, 1); // Lime Green
            }
            else
            {
                textColor = new Vector4(1, 1, 1, 1); // White
            }

            // Draw health text
            drawList.AddText(textPosition, ImGui.ColorConvertFloat4ToU32(textColor), healthText);
        }



        private void DrawBox(Entity entity)
        {
            if (!enableBox) return;

            float entityHeight = entity.position2D.Y - entity.viewPositon2D.Y;
            Vector2 rectTop = new Vector2(entity.viewPositon2D.X - entityHeight / 3, entity.viewPositon2D.Y);
            Vector2 rectBottom = new Vector2(entity.position2D.X + entityHeight / 3, entity.position2D.Y);

            Vector4 boxColor = localPlayer.team == entity.team ? teamColor : enemyColor;

            if (enableVisibilityCheck)
                boxColor = entity.spotted ? boxColor : hiddenColor;

            drawList.AddRect(rectTop, rectBottom, ImGui.ColorConvertFloat4ToU32(boxColor));
        }

        private void DrawLine(Entity entity)
        {
            if (!enableLine) return;

            Vector4 lineColor = localPlayer.team == entity.team ? teamColor : enemyColor;
            drawList.AddLine(new Vector2(screenSize.X / 2, screenSize.Y), entity.position2D, ImGui.ColorConvertFloat4ToU32(lineColor));
        }

        public void UpdateEntities(IEnumerable<Entity> newEntities)
        {
            entities = new ConcurrentQueue<Entity>(newEntities);
        }

        public void UpdateLocalPlayer(Entity newEntity)
        {
            lock (entityLock)
            {
                localPlayer = newEntity;
            }
        }

        public Entity GetLocalPlayer()
        {
            lock (entityLock)
            {
                return localPlayer;
            }
        }

        private void DrawOverlay(Vector2 screenSize)
        {
            ImGui.SetNextWindowSize(screenSize);
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.Begin("Overlay", ImGuiWindowFlags.NoDecoration
                | ImGuiWindowFlags.NoBackground
                | ImGuiWindowFlags.NoBringToFrontOnFocus
                | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoInputs
                | ImGuiWindowFlags.NoCollapse
                | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse);
        }
    }
}
