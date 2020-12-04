using GameX.Properties;
using OpenTK;
using System.Numerics;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using OpenTK.Windowing;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;

using Vector3 = System.Numerics.Vector3;
using Vector2 = System.Numerics.Vector2;
using MathHelper = OpenTK.Mathematics.MathHelper;
using System;
using ImGuiNET;
using System.Drawing;
using System.IO;
using System.ComponentModel;
using GameX.Editor;
using System.Collections.Generic;
using System.Diagnostics;
using Vector4 = System.Numerics.Vector4;

namespace GameX
{
    public class EditorWindow : GameWindow
    {
        public ProjectLoader ProjectLoader;
        private PerformanceMonitor PerformanceCounter;
        

        // Editor style properties
        public Color4 ClearColor = new Color4(0.2f, 0.3f, 0.3f, 1.0f);

        // Scene view properties
        public Camera SceneCamera;
        private AxisRenderer AxisRenderer;
        private LineGridRenderer GridRenderer;
        private LineShapeRenderer ColliderRenderer;
        private Skybox Skybox;
        public bool ShowSky = true;
        public bool ShowGrid = true;
        public bool ShowAxis = true;
        public bool ShowColliders = true;
        public bool SceneViewOpen = true;

        public bool GameViewOpen = true;
        public bool ChangeGameViewResolution = false;
        public Vector2 NextWindowSize;

        public bool EnableDocking = true;
        public bool InspectorViewOpen = true;
        public Action InspectorViewDrawPtr;
        internal AssetMenuItem InspectorActiveItem;

        public bool HierarchyOpen = true;
        public List<GameObject> SelectedGameObjects = new List<GameObject>();

        private bool OpenContextMenu = false;
        private string ContextMenuName;
        public Dictionary<string, Texture2D> AssetItemIconCache = new Dictionary<string, Texture2D>();
        public List<AssetMenuItem> SelectedItems = new List<AssetMenuItem>();
        public AssetMenuItem CurrentAssetRoot;

        public bool ShowProfilerWindow = false;
        public bool EnableProfiling = false;
        public Color4 ProfilerGridColor = new Color4(1.0f, 1.0f, 1.0f, 0.1f);
        public Vector3 SceneCameraRotation = new Vector3(0.0f, 0.0f, 0.0f);

        ImGuiController GUIController;
        //private MeshRenderer TestMesh;
        //float speed = 10.0f;
        Vector2 LastMousePosition;
        

        public EditorWindow(string projectPath) : base(
            new GameWindowSettings
            {
                IsMultiThreaded = false,
                RenderFrequency = 60.0,
                UpdateFrequency = 60.0,
            },
            new NativeWindowSettings
            {
                Flags = ContextFlags.Default | ContextFlags.Debug,
                Profile = ContextProfile.Core,
                StartFocused = true,
                WindowBorder = WindowBorder.Resizable,
                Title = "GameX Editor",
                Size = new Vector2i(Settings.Default.EditorSize.Width, Settings.Default.EditorSize.Height),
            }
        )
        {
            ProjectLoader = new ProjectLoader(projectPath);
            PerformanceCounter = new PerformanceMonitor();
            PerformanceCounter.Register("Init");

            if (Settings.Default.EditorLocation.X >= 0 && Settings.Default.EditorLocation.Y >= 0)
                Location = new Vector2i(Settings.Default.EditorLocation.X, Settings.Default.EditorLocation.Y);
        }

        protected override void OnLoad()
        {
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.ClearColor(ClearColor);

            // Code goes here            

            SceneCamera = new Camera(ViewportRect.Default, Rectangle.Empty);
            AxisRenderer = new AxisRenderer();
            GridRenderer = new LineGridRenderer();
            ColliderRenderer = new LineShapeRenderer();
            Skybox = new Skybox();
            
            base.OnLoad();

            string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeui.ttf");
            GUIController = new ImGuiController(fontPath, Size.X, Size.Y);

            MousePosition = new OpenTK.Mathematics.Vector2(Size.X / 2, Size.Y / 2);

            LastMousePosition = new Vector2(MouseState.X, MouseState.Y);
            PerformanceCounter.Update("Init");

            ProjectLoader.Load();

            PerformanceCounter.Register("Update");
            PerformanceCounter.Register("SceneView");
            PerformanceCounter.Register("SceneLoad");
            PerformanceCounter.Register("GameView");
            
            Scene emptyScene = new Scene();
            Scene.Load(emptyScene);
            PerformanceCounter.Update("SceneLoad");

            Physics.Run();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Settings.Default.Save();
            base.OnClosing(e);
        }

        protected override void OnUnload()
        {
            Physics.Stop();
            
            foreach (KeyValuePair<string, Texture2D> icon in AssetItemIconCache)
                icon.Value.Dispose();

            Scene.CurrentScene.Close();
            Skybox.Dispose();
            ColliderRenderer.Dispose();
            GridRenderer.Dispose();
            AxisRenderer.Dispose();
            GUIController.Dispose();
            base.OnUnload();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            Physics.Step((float)args.Time);

            foreach(GameObject obj in Physics.PhysicsObjects)
                if(obj.PhysicsBodyInitialized)
                    obj.UpdatePhysicsBody();

        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            Time.EditorDelta = (float)e.Time;
            GUIController.Update(this, (float)e.Time);
            
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            
            // Draw main menu bar
            DrawMenuBar();

            if (ProjectLoader.IsLoading)
            {
                ImGui.SetNextWindowSize(new Vector2(Size.X / 3, Size.Y / 8));
                if (ImGui.Begin("Loading", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoDocking))
                {
                    ImGui.Text("Loading Assets...");
                    ImGui.ProgressBar(0.0f);

                    ImGui.End();
                }

                ImGui.SetWindowFocus("Loading");
            }
            else
            {
                // Enable window docking
                if (EnableDocking)
                    ImGui.DockSpaceOverViewport();

                if(SceneViewOpen)
                    DrawSceneView();

                if (InspectorViewOpen)
                    DrawInspectorView();


                DrawAssetView();

                if (HierarchyOpen)
                    DrawSceneHierarchy();

                if (ShowProfilerWindow)
                    DrawPerfMonitorWindow();


                if (GameViewOpen)
                    DrawGameView();


                if (OpenContextMenu)
                {
                    ImGui.OpenPopup(ContextMenuName);
                    OpenContextMenu = false;
                }


                if (ImGui.BeginPopupContextItem("AssetViewContextItem"))
                {
                    if (ImGui.MenuItem("Show in Explorer"))
                    {

                    }

                    if (ImGui.MenuItem("Delete"))
                    {
                        AssetMenuItem selectedItem = SelectedItems[0];

                        if (selectedItem.Type == AssetItemType.Folder)
                            Directory.Delete(selectedItem.SystemPath, true);
                        else
                            File.Delete(selectedItem.SystemPath);

                        selectedItem.Parent.Children.Remove(selectedItem);
                        SelectedItems.Clear();
                    }

                    ImGui.Separator();

                    ImGui.MenuItem("Import New Asset...");
                    if (ImGui.BeginMenu("Import Package"))
                    {
                        ImGui.EndMenu();
                    }

                    ImGui.MenuItem("Refresh");
                    ImGui.MenuItem("Reimport");

                    ImGui.EndPopup();
                }


                if (ImGui.BeginPopupContextItem("AssetViewContext"))
                {
                    if (ImGui.BeginMenu("Create"))
                    {
                        if (ImGui.MenuItem("New Folder"))
                        {
                            AssetMenuItem folderItem = new AssetMenuItem();
                            string folderName = GetNewFolderName(CurrentAssetRoot.SystemPath);
                            folderItem.Type = AssetItemType.Folder;
                            folderItem.Parent = CurrentAssetRoot;
                            folderItem.Name = folderName;
                            folderItem.SystemPath = Path.Combine(CurrentAssetRoot.SystemPath, folderName);
                            folderItem.Renaming = true;

                            if (!Directory.Exists(folderItem.SystemPath))
                                Directory.CreateDirectory(folderItem.SystemPath);

                            CurrentAssetRoot.Children.Add(folderItem);

                            SelectedItems.Clear();
                            SelectedItems.Add(folderItem);
                        }

                        ImGui.Separator();

                        if (ImGui.MenuItem("C# Script"))
                        {
                            Console.WriteLine("Yes!");
                        }

                        ImGui.Separator();

                        if (ImGui.MenuItem("Material"))
                        {

                        }

                        if (ImGui.MenuItem("Shader"))
                        {

                        }

                        if (ImGui.MenuItem("Sprite"))
                        {

                        }

                        ImGui.Separator();

                        if (ImGui.MenuItem("Scene"))
                        {

                        }

                        ImGui.EndMenu();
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Show in Explorer"))
                    {
                        Process.Start("explorer.exe", $"\"{CurrentAssetRoot.SystemPath}\"");
                    }

                    ImGui.Separator();

                    ImGui.MenuItem("Import New Asset...");
                    if (ImGui.BeginMenu("Import Package"))
                    {
                        ImGui.EndMenu();
                    }

                    ImGui.MenuItem("Export Package...");

                    ImGui.Separator();

                    if (ImGui.MenuItem("Refresh"))
                    {

                    }
                    ImGui.MenuItem("Reimport");

                    ImGui.EndPopup();
                }
                

            }

            // Draw GUI
            GUIController.Render();
            GLException.CheckError("End of frame");

            Context.SwapBuffers();
            base.OnRenderFrame(e);

            if(EnableProfiling)
                PerformanceCounter.Update("Update");
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            //Scene.CurrentScene.MainCamera.SetScreenBounds(e.Width, e.Height);
            GUIController.WindowResized(e.Width, e.Height);

            Settings.Default.EditorSize = new Size(e.Width, e.Height);
        }

        protected override void OnMove(WindowPositionEventArgs e)
        {
            base.OnMove(e);
            Settings.Default.EditorLocation = new Point(e.X, e.Y);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            
            GUIController.PressChar((char)e.Unicode);
            
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            GUIController.MouseScroll(e.Offset);
        }


        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);



        }

        public void ShowContextMenu(string name)
        {
            OpenContextMenu = true;
            ContextMenuName = name;
        }

        private void DrawMenuBar()
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Load Scene"))
                    {
                        Scene.Load(Scene.FromFile("test.scene", ProjectLoader.AssetPath));
                    }

                    if (ImGui.MenuItem("Save Scene"))
                    {
                        Scene.CurrentScene.Save("test.scene");
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Window"))
                {
                    if (ImGui.RadioButton("Show Scene View", SceneViewOpen))
                        SceneViewOpen = !SceneViewOpen;

                    if (ImGui.RadioButton("Show Inspector", InspectorViewOpen))
                        InspectorViewOpen = !InspectorViewOpen;

                    if (ImGui.RadioButton("Enable Docking", EnableDocking))
                        EnableDocking = !EnableDocking;

                    ImGui.EndMenu();
                }

                ImGui.EndMainMenuBar();
            }
        }

        private void DrawPerfMonitorWindow()
        {
            if(ImGui.Begin("Performance Monitor", ref ShowProfilerWindow))
            {
                float gridSize = 32.0f;

                if (ImGui.BeginChild("Profilers", new Vector2(0.0f, 0.0f), true))
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0.0f, 0.0f));
                    foreach (KeyValuePair<string, PerformanceMonitor.PerformanceDelta> monitor in PerformanceCounter.PerformanceCounters)
                    {
                        if (ImGui.BeginChild(monitor.Key, new Vector2(0.0f, 1.0f + gridSize * 3)))
                        {
                            Vector2 windowPos = ImGui.GetWindowPos();
                            Vector2 windowSize = ImGui.GetWindowSize();

                            float deltaTime = monitor.Value.DeltaTime * 1000.0f;
                            float hertz = MathF.Round(1000.0f / deltaTime, 0);
                            ImGui.Text(monitor.Key);
                            ImGui.Text($"{deltaTime} ms / {hertz} Hz");

                            ImDrawListPtr bgDrawList = ImGui.GetWindowDrawList();
                            for (float x = gridSize; x < windowSize.X; x += gridSize)
                            {
                                Vector2 pos = new Vector2(windowPos.X + x, windowPos.Y);
                                bgDrawList.AddLine(pos, pos + new Vector2(0.0f, windowSize.Y), (uint)ProfilerGridColor.ToArgb());
                            }

                            for (float y = 0.0f; y < windowSize.Y + gridSize; y += gridSize)
                            {
                                Vector2 pos = new Vector2(windowPos.X, windowPos.Y + y);
                                bgDrawList.AddLine(pos, pos + new Vector2(windowPos.X + windowSize.X, 0.0f), (uint)ProfilerGridColor.ToArgb());
                            }


                            //ImGui.PlotLines("", ref monitor.Value.UpdateTimes[0], PerformanceMonitor.CacheTime, monitor.Value.UpdateIndex);

                            ImGui.EndChild();
                        }
                    }
                    ImGui.PopStyleVar();
                    ImGui.EndChild();
                }
                
                ImGui.End();
            }
        }

        private void DrawSceneView()
        {
            if (ImGui.Begin("Scene View", ref SceneViewOpen, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.MenuBar))
            {
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("Gizmos"))
                    {
                        if (ImGui.RadioButton("Show Grid", ShowGrid))
                            ShowGrid = !ShowGrid;

                        if (ImGui.RadioButton("Show Axis", ShowAxis))
                            ShowAxis = !ShowAxis;

                        if (ImGui.RadioButton("Show Skybox", ShowSky))
                            ShowSky = !ShowSky;

                        if (ImGui.RadioButton("Show Colliders", ShowColliders))
                            ShowColliders = !ShowColliders;

                        ImGui.EndMenu();
                    }

                    ImGui.EndMenuBar();
                }

                Vector2 windowPos = ImGui.GetWindowPos();
                Vector2 windowSize = ImGui.GetWindowSize();
                Rectangle windowRect = new Rectangle((int)windowPos.X + 1, Size.Y - ((int)windowPos.Y + (int)windowSize.Y) + 1, (int)windowSize.X - 2, (int)windowSize.Y - 54);

                ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                ImGuiController.AddCallback(drawList, data =>
                {

                    GUIController.UnsetRenderSettings();

                    GL.Viewport(windowRect);
                    SceneCamera.SetScreenBounds(windowRect);

                    Vector2 windowMin = new Vector2(windowRect.Left, windowRect.Top);
                    Vector2 windowMax = new Vector2(windowRect.Right, windowRect.Bottom);
                    if (ImGui.IsMouseDragging(ImGuiMouseButton.Right) && ImGui.IsMouseHoveringRect(windowMin, windowMax, false))
                    {

                        float mouseSensitivity = 2.0f;
                        float speed = 10.0f;

                        if (KeyboardState.IsKeyDown(Keys.LeftShift))
                            speed *= 3.0f;

                        if (KeyboardState.IsKeyDown(Keys.W))
                            SceneCamera.transform.Position += SceneCamera.transform.Forward * speed * Time.EditorDelta; //Forward 

                        if (KeyboardState.IsKeyDown(Keys.S))
                            SceneCamera.transform.Position -= SceneCamera.transform.Forward * speed * Time.EditorDelta; //Backwards

                        if (KeyboardState.IsKeyDown(Keys.A))
                            SceneCamera.transform.Position -= Vector3.Normalize(Vector3.Cross(SceneCamera.transform.Forward, SceneCamera.transform.Up)) * speed * Time.EditorDelta; //Left

                        if (KeyboardState.IsKeyDown(Keys.D))
                            SceneCamera.transform.Position += Vector3.Normalize(Vector3.Cross(SceneCamera.transform.Forward, SceneCamera.transform.Up)) * speed * Time.EditorDelta; //Right

                        if (KeyboardState.IsKeyDown(Keys.Space))
                            SceneCamera.transform.Position += SceneCamera.transform.Up * speed * Time.EditorDelta; //Up 

                        if (KeyboardState.IsKeyDown(Keys.LeftControl))
                            SceneCamera.transform.Position -= SceneCamera.transform.Up * speed * Time.EditorDelta; //Down

                        SceneCameraRotation.X += (MouseState.X - LastMousePosition.X) * mouseSensitivity;
                        SceneCameraRotation.Y -= (MouseState.Y - LastMousePosition.Y) * mouseSensitivity;


                        System.Numerics.Quaternion rotation = System.Numerics.Quaternion.CreateFromAxisAngle(new Vector3(1.0f, 0.0f, 0.0f), MathHelper.DegreesToRadians(SceneCameraRotation.X));
                        //rotation *= System.Numerics.Quaternion.CreateFromAxisAngle(new Vector3(0.0f, 1.0f, 0.0f), MathHelper.DegreesToRadians(SceneCameraRotation.Y));
                        SceneCamera.transform.Rotation = System.Numerics.Quaternion.Identity * rotation;
                    }


                    LastMousePosition.X = MouseState.X;
                    LastMousePosition.Y = MouseState.Y;

                    // Draw the Skybox (needs optimization)
                    if (ShowSky)
                        Skybox.Draw(SceneCamera);

                    // Draw the 3D scene
                    for (int i = 0; i < Scene.CurrentScene.gameObjects.Count; i++)
                    {
                        if (Scene.CurrentScene.Cameras.Contains(i))
                            continue;

                        GameObject obj = Scene.CurrentScene.gameObjects[i];

                        if (obj.Renderer != null)
                            obj.Renderer.Draw(SceneCamera, obj.transform, obj.Material ?? Material.Default);

                        if(ShowColliders && obj.PhysicsBodyInitialized)
                        {
                            // Draw collider outline in different cool shader
                            ColliderRenderer.Draw(SceneCamera, obj);
                        }

                    }

                    // Draw Gizmos
                    if (ShowGrid)
                        GridRenderer.Draw(SceneCamera);

                    if (ShowAxis)
                        AxisRenderer.Draw(SceneCamera);




                    // return true to reset ImGui Render settings
                    return true;

                });

                /*
                if (AxisRenderer.Target != null) {
                    OpenTK.Mathematics.Vector2 projection = SceneCamera.WorldToScreen(AxisRenderer.Target.Position);

                    Vector2 screenPos = new Vector2(projection.X, projection.Y);
                    drawList.AddRectFilled(screenPos, screenPos + new Vector2(16.0f, 16.0f), (uint)Color4.WhiteSmoke.ToArgb());
                }
                */

                ImGui.End();
                if (EnableProfiling)
                    PerformanceCounter.Update("SceneView");
            }


        }

        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360.0f)
                angle += 360F;
            if (angle > 360.0f)
                angle -= 360F;

            return Mathf.Clamp(angle, min, max);
        }

        private void DrawGameView()
        {
            if (ChangeGameViewResolution)
            {
                ImGui.SetNextWindowSize(NextWindowSize);
                ChangeGameViewResolution = false;
            }

            if (ImGui.Begin("Game View", ref GameViewOpen, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.MenuBar))
            {
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("Aspect Ratio"))
                    {
                        if(ImGui.Selectable("800 x 600"))
                        {
                            NextWindowSize = new Vector2(800, 600);
                            ChangeGameViewResolution = true;
                        }
                        

                        ImGui.EndMenu();
                    }

                    ImGui.EndMenuBar();
                }


                if (Scene.CurrentScene.Cameras.Count > 0)
                {
                    Vector2 windowPos = ImGui.GetWindowPos();
                    Vector2 windowSize = ImGui.GetWindowSize();
                    Rectangle windowRect = new Rectangle((int)windowPos.X + 1, Size.Y - ((int)windowPos.Y + (int)windowSize.Y) + 1, (int)windowSize.X - 2, (int)windowSize.Y - 54);

                    ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                    ImGuiController.AddCallback(drawList, data =>
                    {
                        GUIController.UnsetRenderSettings();

                        // Get all cameras in this scene
                        for (int i = 0; i < Scene.CurrentScene.Cameras.Count; i++)
                        {
                            // when a new scene loads, not all CAMERAS are removed so it returns them here which is bad stuff
                            Camera cam = Scene.CurrentScene.gameObjects[i] as Camera;
                            cam.SetScreenBounds(windowRect);

                            Rectangle viewport = cam.Viewport.GetScaledViewport(windowRect);
                            GL.Scissor(viewport.X, viewport.Y, viewport.Width, viewport.Height);// cam.Viewport.v_Left, cam.Viewport.v_Top, cam.Viewport.v_Width, cam.Viewport.v_Height);
                            GL.Viewport(viewport);
                            GL.Enable(EnableCap.ScissorTest);

                            GL.ClearColor(cam.ClearColor);
                            GL.Clear(ClearBufferMask.ColorBufferBit);

                            if (cam.ClearMode == ClearFlags.Skybox)
                            {
                                // Draw the Skybox (needs optimization)
                                Skybox.Draw(cam);
                            }

                            for (int j = 0; j < Scene.CurrentScene.gameObjects.Count; j++)
                            {
                                if (Scene.CurrentScene.Cameras.Contains(j))
                                    continue;

                                GameObject obj = Scene.CurrentScene.gameObjects[j];
                                if (obj.Renderer != null)
                                    obj.Renderer.Draw(cam, obj.transform, obj.Material ?? Material.Default);
                            }

                        }

                        GL.Disable(EnableCap.ScissorTest);

                        // return true to reset ImGui Render settings
                        return true;

                    });
                }
                else
                {
                    ImGui.Text("There are no Cameras to view this Scene.");
                }

                ImGui.Text("Body Count: " + Physics.simulator.Bodies.ActiveSet.Count);

                ImGui.End();
                if (EnableProfiling)
                    PerformanceCounter.Update("GameView");
            }


        }

        private void DrawInspectorView()
        {
            if (ImGui.Begin("Inspector", ref InspectorViewOpen, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse))
            {
                InspectorViewDrawPtr?.Invoke();

                ImGui.End();
            }
        }

        private void DrawSceneHierarchy()
        {
            if (ImGui.Begin("Hierarchy", ref InspectorViewOpen))
            {
                if (ImGui.BeginMenu("Create"))
                {
                    if (ImGui.MenuItem("Empty GameObject"))
                    {
                        GameObject gameObject = new GameObject { Name = "New GameObject", transform = new Transform { Position = Vector3.Zero, Rotation = System.Numerics.Quaternion.Identity, Scale = Vector3.One } };
                        //gameObject.SetCollider();
                        Scene.CurrentScene.gameObjects.Add(gameObject);
                        SetHierarchySelection(gameObject);
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Particle System", false))
                    {

                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Camera"))
                    {
                        Camera cam = new Camera(ViewportRect.Default, Rectangle.Empty) { Name = "Camera" };
                        Scene.CurrentScene.gameObjects.Add(cam);
                        Scene.CurrentScene.Cameras.Add(Scene.CurrentScene.gameObjects.Count);
                        SetHierarchySelection(cam);
                    }

                    ImGui.EndMenu();
                }

                ImGui.Separator();
                if (ImGui.BeginChild("SceneObjects", Vector2.Zero, false))
                {
                    if (Scene.CurrentScene != null)
                    {
                        for (int i = 0; i < Scene.CurrentScene.gameObjects.Count; i++)
                        {
                            GameObject currentObject = Scene.CurrentScene.gameObjects[i];
                            if (ImGui.Selectable(currentObject.Name, SelectedGameObjects.Contains(currentObject)))
                            {
                                SetHierarchySelection(currentObject);
                            }

                            if (ImGui.BeginDragDropTarget())
                            {
                                ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("Mesh");
                                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && InspectorActiveItem != null)
                                {
                                    if (currentObject.Renderer == null)
                                    {
                                        (currentObject.Renderer = new MeshRenderer(InspectorActiveItem.RelativePath))
                                            .SetMesh(Scene.LoadMeshData(currentObject.Renderer.RelativeMeshPath, ProjectLoader.AssetPath));
                                    }
                                    else
                                    {
                                        currentObject.Renderer.RelativeMeshPath = InspectorActiveItem.RelativePath;
                                        currentObject.Renderer.SetMesh(Scene.LoadMeshData(currentObject.Renderer.RelativeMeshPath, ProjectLoader.AssetPath));
                                    }

                                    InspectorActiveItem = null;
                                }

                                ImGui.EndDragDropTarget();
                            }

                        }

                    }

                    ImGui.EndChild();
                }

                if (ImGui.BeginDragDropTarget())
                {
                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("Mesh");
                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && InspectorActiveItem != null)
                    {
                        GameObject gameObject = new GameObject {
                            Name = "New GameObject",
                            transform = new Transform {
                                Position = Vector3.Zero,
                                Rotation = System.Numerics.Quaternion.Identity,
                                Scale = Vector3.One
                            }
                        };

                        (gameObject.Renderer = new MeshRenderer(InspectorActiveItem.RelativePath))
                            .SetMesh(Scene.LoadMeshData(gameObject.Renderer.RelativeMeshPath, ProjectLoader.AssetPath));
                        
                        Scene.CurrentScene.gameObjects.Add(gameObject);
                        SetHierarchySelection(gameObject);
                    }

                    ImGui.EndDragDropTarget();
                }

                ImGui.End();
            }

        }

        private void DrawAssetView()
        {
            if (CurrentAssetRoot == null)
                CurrentAssetRoot = ProjectLoader.AssetRoot;

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(3.0f, 1.0f));
            if (ImGui.Begin("Assets", ref InspectorViewOpen))
            {
                if (CurrentAssetRoot != ProjectLoader.AssetRoot)
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 8.0f);
                    ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(6.0f, 2.0f));
                    ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 2.0f);
                    ImGui.SetWindowFontScale(0.75f);
                    if (ImGui.BeginChild("AssetPath", new Vector2(0.0f, 20.0f), false, ImGuiWindowFlags.HorizontalScrollbar))
                    {
                        foreach(AssetMenuItem pathItem in GetAssetBrowserTree(CurrentAssetRoot))
                        {
                            if(ImGui.Button(pathItem.Name, new Vector2(0, 20.0f)))
                                CurrentAssetRoot = pathItem;
                            ImGui.SameLine();
                        }

                        ImGui.EndChild();
                    }
                    ImGui.PopStyleVar();
                    ImGui.PopStyleVar();
                    ImGui.PopStyleVar();
                    ImGui.SetWindowFontScale(1.0f);

                    ImGui.Separator();
                    ImGui.Spacing();
                }

                ImGui.BeginChild("Assets", new Vector2(0, -34.0f), false, ImGuiWindowFlags.None);

                if( CurrentAssetRoot != ProjectLoader.AssetRoot )
                {
                    if (ImGui.Selectable(".."))
                        CurrentAssetRoot = CurrentAssetRoot.Parent;
                }

                foreach(AssetMenuItem child in CurrentAssetRoot.Children)
                    DrawAssetMenuItem(child);

                ImGui.EndChild();
                
                if(ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    ShowContextMenu("AssetViewContext");
                }

                ImGui.Separator();
                float scale = 0.0f;

                if(ImGui.SliderFloat("Scale", ref scale, 0.0f, 1.0f))
                {

                }

                ImGui.End();
            }
            ImGui.PopStyleVar();
        }

        private void DrawAssetMenuItem(AssetMenuItem item)
        {
            if (item.Type == AssetItemType.Folder)
            {

                ImGui.Image((IntPtr)GUIController.IconAtlas.GLTexture, new Vector2(20.0f, 18.0f), ImGuiController.EditorIcons.Folder.uvMin, ImGuiController.EditorIcons.Folder.uvMax);
                ImGui.SameLine();

                if(item.Renaming)
                {
                    string name = item.Name;
                    if(ImGui.InputText("", ref name, 20, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        string parentDir = item.SystemPath.Substring(0, item.SystemPath.LastIndexOf(Path.DirectorySeparatorChar));
                        string newPath = Path.Combine(parentDir, item.Name);
                        item.Renaming = false;

                        if(newPath != item.SystemPath)
                            Directory.Move(item.SystemPath, newPath);
                    }
                    ImGui.SetKeyboardFocusHere();
                    item.Name = name;
                }
                else
                {
                    if (ImGui.Selectable(item.Name, SelectedItems.Contains(item)))
                    {
                        if (!KeyboardState.IsKeyDown(Keys.LeftShift))
                            SelectedItems.Clear();

                        SelectedItems.Add(item);

                    }
                    else if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        if (!KeyboardState.IsKeyDown(Keys.LeftShift))
                            SelectedItems.Clear();

                        SelectedItems.Add(item);
                        ShowContextMenu("AssetViewContextItem");
                    }
                    else if (ImGui.IsItemClicked(ImGuiMouseButton.Left) && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    {
                        // Go into this folder...
                        CurrentAssetRoot = item;
                    }
                }

            }
            else if( item.Type == AssetItemType.Mesh )
            {
                ImGui.Image((IntPtr)GUIController.IconAtlas.GLTexture, new Vector2(18.0f, 20.0f), ImGuiController.EditorIcons.ObjFile.uvMin, ImGuiController.EditorIcons.ObjFile.uvMax);
                ImGui.SameLine();

                if (item.Renaming)
                {
                    string name = item.Name;
                    if (ImGui.InputText("", ref name, 20, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        string parentDir = item.SystemPath.Substring(0, item.SystemPath.LastIndexOf(Path.DirectorySeparatorChar));
                        string newPath = Path.Combine(parentDir, item.Name);
                        item.Renaming = false;

                        if (newPath != item.SystemPath)
                            File.Move(item.SystemPath, newPath);
                    }
                    ImGui.SetKeyboardFocusHere();
                    item.Name = name;
                }
                else
                {
                    if (item.Children.Count > 0)
                    {
                        if (ImGui.TreeNode(item.Name))
                        {
                            foreach (AssetMenuItem ai in item.Children)
                                AssetView_DrawSubItem(ai);

                            ImGui.TreePop();
                        }
                    }
                    else if(ImGui.Selectable(item.Name, SelectedItems.Contains(item)))
                    {
                        if (!KeyboardState.IsKeyDown(Keys.LeftShift))
                            SelectedItems.Clear();

                        SelectedItems.Add(item);
                    }

                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        if (!KeyboardState.IsKeyDown(Keys.LeftShift))
                            SelectedItems.Clear();

                        SelectedItems.Add(item);
                        ShowContextMenu("AssetViewContextItem");
                    }

                    if (ImGui.BeginDragDropSource())
                    {
                        if (ImGui.SetDragDropPayload("Mesh", IntPtr.Zero, 0))
                        {
                            InspectorActiveItem = item;
                        }

                        ImGui.EndDragDropSource();
                    }
                }
            }
            else if(item.Type == AssetItemType.Texture)
            {
                Texture2D icon = GetAssetIconThumbnailPreview(item);
                ImGui.Image((IntPtr)icon.GLTexture, new Vector2(18.0f, 20.0f));
                ImGui.SameLine();

                if (item.Renaming)
                {
                    string name = item.Name;
                    if (ImGui.InputText("", ref name, 20, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        string parentDir = item.SystemPath.Substring(0, item.SystemPath.LastIndexOf(Path.DirectorySeparatorChar));
                        string newPath = Path.Combine(parentDir, item.Name);
                        item.Renaming = false;

                        if (newPath != item.SystemPath)
                            File.Move(item.SystemPath, newPath);
                    }
                    ImGui.SetKeyboardFocusHere();
                    item.Name = name;
                }
                else
                {
                    if (ImGui.Selectable(item.Name, SelectedItems.Contains(item)))
                    {
                        if (!KeyboardState.IsKeyDown(Keys.LeftShift))
                            SelectedItems.Clear();

                        SelectedItems.Add(item);
                    }
                    else if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        if (!KeyboardState.IsKeyDown(Keys.LeftShift))
                            SelectedItems.Clear();

                        SelectedItems.Add(item);
                        ShowContextMenu("AssetViewContextItem");
                    }

                    if (ImGui.BeginDragDropSource())
                    {
                        if (ImGui.SetDragDropPayload("Texture", IntPtr.Zero, 0))
                        {
                            InspectorActiveItem = item;
                        }

                        ImGui.EndDragDropSource();
                    }
                }
            }
            else
            {
                ImGui.Image((IntPtr)GUIController.IconAtlas.GLTexture, new Vector2(18.0f, 20.0f), ImGuiController.EditorIcons.TextFile.uvMin, ImGuiController.EditorIcons.TextFile.uvMax);
                ImGui.SameLine();

                if (item.Renaming)
                {
                    string name = item.Name;
                    if (ImGui.InputText("", ref name, 20, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        string parentDir = item.SystemPath.Substring(0, item.SystemPath.LastIndexOf(Path.DirectorySeparatorChar));
                        string newPath = Path.Combine(parentDir, item.Name);
                        item.Renaming = false;

                        if (newPath != item.SystemPath)
                            File.Move(item.SystemPath, newPath);
                    }
                    ImGui.SetKeyboardFocusHere();
                    item.Name = name;
                }
                else
                {
                    if (ImGui.Selectable(item.Name, SelectedItems.Contains(item)))
                    {
                        if (!KeyboardState.IsKeyDown(Keys.LeftShift))
                            SelectedItems.Clear();

                        SelectedItems.Add(item);
                    }
                    else if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        if (!KeyboardState.IsKeyDown(Keys.LeftShift))
                            SelectedItems.Clear();

                        SelectedItems.Add(item);
                        ShowContextMenu("AssetViewContextItem");
                    }

                    if (ImGui.BeginDragDropSource())
                    {
                        if (ImGui.SetDragDropPayload("Object", IntPtr.Zero, 0))
                        {
                            InspectorActiveItem = item;
                        }

                        ImGui.EndDragDropSource();
                    }
                }
            }
        }

        private void AssetView_DrawSubItem(AssetMenuItem item)
        {
            if (item.Type == AssetItemType.Mesh)
            {
                ImGui.Image((IntPtr)GUIController.IconAtlas.GLTexture, new Vector2(18.0f, 20.0f), ImGuiController.EditorIcons.ObjFile.uvMin, ImGuiController.EditorIcons.ObjFile.uvMax);
                ImGui.SameLine();

                if (item.Children.Count > 0)
                {
                    if (ImGui.TreeNode(item.Name))
                    {
                        foreach (AssetMenuItem ai in item.Children)
                            AssetView_DrawSubItem(ai);

                        ImGui.TreePop();
                    }
                }
                else
                {
                    if (ImGui.Selectable(item.Name, false))
                    {
                        
                    }
                }
            }
        }

        private void Inspector_DrawGameObject()
        {
            if(SelectedGameObjects.Count == 0)
            {
                InspectorViewDrawPtr = null;
                return;
            }

            GameObject gameObject = SelectedGameObjects[0];

            string name = gameObject.Name;
            if(ImGui.InputText("Name", ref name, 32))
            {
                gameObject.Name = name;
            }

            ImGui.Separator();

            // Draw Transform editor
            Vector3 pos = gameObject.transform.Position;
            if (ImGui.DragFloat3("Position", ref pos))
            {
                if (gameObject.PhysicsBodyInitialized)
                {
                    gameObject.BodyReference.Pose.Position = pos;
                    gameObject.BodyReference.Awake = true;
                }
                else
                {
                    gameObject.transform.Position = pos;
                }
            }

            Vector3 rot = gameObject.transform.EulerAngles;
            if (ImGui.DragFloat3("Rotation", ref rot))
            {
                if (gameObject.PhysicsBodyInitialized)
                {
                    gameObject.BodyReference.Pose.Orientation = System.Numerics.Quaternion.CreateFromYawPitchRoll(rot.X, rot.Y, rot.Z);
                    gameObject.BodyReference.Awake = true;
                }
                else
                {
                    gameObject.transform.EulerAngles = rot;// System.Numerics.Quaternion.CreateFromYawPitchRoll(MathHelper.DegreesToRadians(rot.Y), MathHelper.DegreesToRadians(rot.X), MathHelper.DegreesToRadians(rot.Z));
                }
            }

            Vector3 scale = gameObject.transform.Scale;
            if (ImGui.DragFloat3("Scale", ref scale))
                gameObject.transform.Scale = scale;

            ImGui.Separator();

            if(gameObject is Camera)
            {
                Camera cam = gameObject as Camera;
                if (ImGui.CollapsingHeader("Camera"))
                {
                    float fov = cam.FOV;
                    if(ImGui.SliderFloat("Field of View", ref fov, 45.0f, 90.0f))
                        cam.FOV = fov;

                    ImGui.Separator();

                    ImGui.Text("Viewport");
                    ImGui.DragFloat("X", ref cam.Viewport.Left, 0.01f, 0.0f, 1.0f);
                    ImGui.DragFloat("Y", ref cam.Viewport.Top, 0.01f, 0.0f, 1.0f);
                    ImGui.DragFloat("Width", ref cam.Viewport.Width, 0.01f, 0.01f, 1.0f);
                    ImGui.DragFloat("Height", ref cam.Viewport.Height, 0.01f, 0.01f, 1.0f);

                    ImGui.Separator();

                    ImGui.DragFloat("zNear", ref cam.zNear, 0.1f, 0.01f, 1.0f);
                    ImGui.DragFloat("zFar", ref cam.zFar, 0.1f, 1.0f, 100000.0f);

                    ImGui.Separator();

                    int depth = cam.Depth;
                    if(ImGui.DragInt("Depth", ref depth, 1.0f, -10, 10))
                        cam.Depth = depth;
                }
            }
            else
            {
                if (ImGui.CollapsingHeader("Renderer"))
                {
                    if (ImGui.SmallButton("Set"))
                    {

                    }
                    ImGui.SameLine();
                    ImGui.Text(gameObject.Renderer?.RelativeMeshPath ?? "undefined");
                }

                if (ImGui.BeginDragDropTarget())
                {
                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("Mesh");
                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && InspectorActiveItem != null)
                    {
                        if (gameObject.Renderer == null)
                        {
                            (gameObject.Renderer = new MeshRenderer(InspectorActiveItem.RelativePath))
                                .SetMesh(Scene.LoadMeshData(gameObject.Renderer.RelativeMeshPath, ProjectLoader.AssetPath));
                        }
                        else
                        {
                            gameObject.Renderer.RelativeMeshPath = InspectorActiveItem.RelativePath;
                            gameObject.Renderer.SetMesh(Scene.LoadMeshData(gameObject.Renderer.RelativeMeshPath, ProjectLoader.AssetPath));
                        }

                        InspectorActiveItem = null;
                    }

                    ImGui.EndDragDropTarget();
                }

                if (ImGui.CollapsingHeader("Material"))
                {
                    ImGui.Button("Hello");
                }

                if (ImGui.BeginDragDropTarget())
                {
                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("Material");
                    /*
                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && InspectorActiveItem != null)
                    {





                        if (gameObject.Material == null)
                        {
                            gameObject.Material = new Material();
                        }
                        else
                        {
                            gameObject.Renderer.RelativeMeshPath = InspectorActiveItem.RelativePath;
                            gameObject.Renderer.SetMesh(Scene.LoadMeshData(gameObject.Renderer.RelativeMeshPath, ProjectLoader.AssetPath));
                        }

                        if (gameObject.Material == null)
                            gameObject.Material = Material.Default;

                        InspectorActiveItem = null;
                    }
                    */

                    ImGui.EndDragDropTarget();
                }

                //bool physicsEnabled = gameObject.EnablePhysics;
                if (ImGui.CollapsingHeader("Physics"))
                {
                    //if(ImGui.Checkbox("Enable Collisions", ref physicsEnabled))
                        //gameObject.EnablePhysics = physicsEnabled;

                    int selectedIndex = (int)gameObject.PhysicsType;
                    string[] items = Enum.GetNames(typeof(PhysicsType));
                    if (ImGui.Combo("Collision Type", ref selectedIndex, items, items.Length))
                        gameObject.SetCollider((PhysicsType)selectedIndex);

                }

            }

        }

        public void SetHierarchySelection(GameObject @object)
        {
            SelectedGameObjects.Clear();
            SelectedGameObjects.Add(@object);
            InspectorViewDrawPtr = Inspector_DrawGameObject;
            AxisRenderer.SetTarget(@object.transform);
        }

        public Texture2D GetAssetIconThumbnailPreview(AssetMenuItem item)
        {
            if(!AssetItemIconCache.ContainsKey(item.RelativePath))
            {
                using (System.Drawing.Image img = System.Drawing.Image.FromFile(item.SystemPath))
                    AssetItemIconCache.Add(item.RelativePath, Texture2D.GetThumbnailImage(img, new Vector2(32f, 32f)));
            }

            return AssetItemIconCache[item.RelativePath];
        }

        internal List<AssetMenuItem> GetAssetBrowserTree(AssetMenuItem root)
        {
            List<AssetMenuItem> tree = new List<AssetMenuItem>();

            AssetMenuItem currentItem = root;
            while(currentItem.Parent != null)
            {
                tree.Add(currentItem);
                currentItem = currentItem.Parent;
            }
            tree.Add(currentItem);
            tree.Reverse();

            return tree;
        }

        private string GetNewFolderName(string baseDirectory, string defaultName = "New Folder")
        {
            string currentName = defaultName;
            int count = 1;
            while (Directory.Exists(Path.Combine(baseDirectory, currentName))) {
                currentName = defaultName.Trim() + $" ({count})";
                count++;
            }

            return currentName;
        }

    }
}
