using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using GameX.Properties;
using System.IO;

namespace GameX
{
    /// <summary>
    /// Manages input for ImGui and handles rendering ImGui's DrawLists with <see cref="OpenTK"/>.
    /// </summary>
    public class ImGuiController : IDisposable
    {
        private bool _frameBegun;

        private int _vertexArray;
        private int _vertexBuffer;
        private int _vertexBufferSize;
        private int _indexBuffer;
        private int _indexBufferSize;

        public Texture2D IconAtlas;
        private Texture2D _fontTexture;
        private Shader _shader;

        private int _windowWidth;
        private int _windowHeight;

        private System.Numerics.Vector2 _scaleFactor = System.Numerics.Vector2.One;
        private readonly List<char> PressedChars = new List<char>();
        private bool BlendEnabled, ScissorEnabled, CullFaceEnabled, DepthTestEnabled;

        public delegate bool CustomDrawEvent(object data);
        public static List<CustomDrawEvent> ImGuiDelegates = new List<CustomDrawEvent>();
        public static List<object> ImGuiDelegateArgs = new List<object>();

        public static class EditorIcons
        {
            public static System.Numerics.Vector2 ImageSize = new System.Numerics.Vector2(1024.0f, 1024.0f);

            public static EditorIcon Folder = new EditorIcon(new System.Numerics.Vector2(0.0f, 0.0f), new System.Numerics.Vector2(80.0f, 72.0f), ImageSize);
            public static EditorIcon CodeFile = new EditorIcon(new System.Numerics.Vector2(80.0f, 0.0f), new System.Numerics.Vector2(72.0f, 80.0f), ImageSize);
            public static EditorIcon TextFile = new EditorIcon(new System.Numerics.Vector2(152.0f, 0.0f), new System.Numerics.Vector2(72.0f, 80.0f), ImageSize);
            public static EditorIcon ObjFile = new EditorIcon(new System.Numerics.Vector2(224.0f, 0.0f), new System.Numerics.Vector2(72.0f, 80.0f), ImageSize);
            public static EditorIcon CameraLens = new EditorIcon(new System.Numerics.Vector2(296.0f, 0.0f), new System.Numerics.Vector2(80.0f, 80.0f), ImageSize);
            public static EditorIcon CircleGeneric = new EditorIcon(new System.Numerics.Vector2(376.0f, 0.0f), new System.Numerics.Vector2(80.0f, 80.0f), ImageSize);
            public static EditorIcon Play = new EditorIcon(new System.Numerics.Vector2(456.0f, 0.0f), new System.Numerics.Vector2(46.0f, 60.0f), ImageSize);
            public static EditorIcon Pause = new EditorIcon(new System.Numerics.Vector2(454.0f, 0.0f), new System.Numerics.Vector2(48.0f, 56.0f), ImageSize);
            public static EditorIcon SquareGeneric = new EditorIcon(new System.Numerics.Vector2(502.0f, 0.0f), new System.Numerics.Vector2(128.0f, 128.0f), ImageSize);
            public static EditorIcon AxisArrow = new EditorIcon(new System.Numerics.Vector2(630.0f, 0.0f), new System.Numerics.Vector2(178.0f, 178.0f), ImageSize);
            public static EditorIcon Grid = new EditorIcon(new System.Numerics.Vector2(808.0f, 0.0f), new System.Numerics.Vector2(96.0f, 96.0f), ImageSize);
            public static EditorIcon Lightbulb = new EditorIcon(new System.Numerics.Vector2(810.0f, 96.0f), new System.Numerics.Vector2(28.0f, 38.0f), ImageSize);
            public static EditorIcon Cloud = new EditorIcon(new System.Numerics.Vector2(838.0f, 96.0f), new System.Numerics.Vector2(48.0f, 32.0f), ImageSize);
        }

        public struct EditorIcon
        {
            public System.Numerics.Vector2 uvMin, uvMax;
            public EditorIcon(System.Numerics.Vector2 start, System.Numerics.Vector2 size, System.Numerics.Vector2 imageSize)
            {
                uvMin = new System.Numerics.Vector2(start.X / imageSize.X, start.Y / imageSize.Y);
                uvMax = new System.Numerics.Vector2(uvMin.X + (size.X / imageSize.X), uvMin.Y + (size.Y / imageSize.Y));
            }
        }


        /// <summary>
        /// Constructs a new ImGuiController.
        /// </summary>
        public ImGuiController(string fontPath, int width, int height)
        {
            _windowWidth = width;
            _windowHeight = height;

            IntPtr context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);

            ImGuiIOPtr io = ImGui.GetIO();
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            if ( File.Exists(fontPath) )
                io.Fonts.AddFontFromFileTTF(fontPath, 20.0f);
            else
                io.Fonts.AddFontDefault();

            CreateDeviceResources();
            SetKeyMappings();

            SetPerFrameImGuiData(1f / 60f);

            ImGui.NewFrame();
            _frameBegun = true;
        }

        public void WindowResized(int width, int height)
        {
            _windowWidth = width;
            _windowHeight = height;
            
        }

        public void DestroyDeviceObjects()
        {
            Dispose();
        }

        public void CreateDeviceResources()
        {
            GL.CreateVertexArrays(1, out _vertexArray);

            _vertexBufferSize = 10000;
            _indexBufferSize = 2000;

            GL.CreateBuffers(1, out _vertexBuffer);
            GL.CreateBuffers(1, out _indexBuffer);
            GL.NamedBufferData(_vertexBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.NamedBufferData(_indexBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            RecreateFontDeviceTexture();

            _shader = new Shader(Resources.imgui_vert_shader, Resources.imgui_frag_shader, true);

            GL.VertexArrayVertexBuffer(_vertexArray, 0, _vertexBuffer, IntPtr.Zero, Unsafe.SizeOf<ImDrawVert>());
            GL.VertexArrayElementBuffer(_vertexArray, _indexBuffer);

            GL.EnableVertexArrayAttrib(_vertexArray, 0);
            GL.VertexArrayAttribBinding(_vertexArray, 0, 0);
            GL.VertexArrayAttribFormat(_vertexArray, 0, 2, VertexAttribType.Float, false, 0);

            GL.EnableVertexArrayAttrib(_vertexArray, 1);
            GL.VertexArrayAttribBinding(_vertexArray, 1, 0);
            GL.VertexArrayAttribFormat(_vertexArray, 1, 2, VertexAttribType.Float, false, 8);

            GL.EnableVertexArrayAttrib(_vertexArray, 2);
            GL.VertexArrayAttribBinding(_vertexArray, 2, 0);
            GL.VertexArrayAttribFormat(_vertexArray, 2, 4, VertexAttribType.UnsignedByte, true, 16);



            IconAtlas = new Texture2D(Resources.editor_icon_atlas, false, TextureUnit.Texture0, false);

            GLException.CheckError("ImGui Setup");
        
        
        
        }

        /// <summary>
        /// Recreates the device texture used to render text.
        /// </summary>
        public void RecreateFontDeviceTexture()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

            _fontTexture = new Texture2D(width, height, pixels, TextureUnit.Texture0);
            _fontTexture.SetMagFilter(TextureMagFilter.Linear);
            _fontTexture.SetMinFilter(TextureMinFilter.Linear);

            io.Fonts.SetTexID((IntPtr)_fontTexture.GLTexture);

            io.Fonts.ClearTexData();
        }

        /// <summary>
        /// Renders the ImGui draw list data.
        /// This method requires a <see cref="GraphicsDevice"/> because it may create new DeviceBuffers if the size of vertex
        /// or index data has increased beyond the capacity of the existing buffers.
        /// A <see cref="CommandList"/> is needed to submit drawing and resource update commands.
        /// </summary>
        public void Render()
        {
            if (_frameBegun)
            {
                _frameBegun = false;
                ImGui.Render();
                RenderImDrawData(ImGui.GetDrawData());

                ImGuiDelegates.Clear();
                ImGuiDelegateArgs.Clear();

            }
        }

        /// <summary>
        /// Updates ImGui input and IO configuration state.
        /// </summary>
        public void Update(GameWindow wnd, float deltaSeconds)
        {
            if (_frameBegun)
                ImGui.Render();

            SetPerFrameImGuiData(deltaSeconds);
            UpdateImGuiInput(wnd);

            _frameBegun = true;
            ImGui.NewFrame();
        }

        /// <summary>
        /// Sets per-frame data based on the associated window.
        /// This is called by Update(float).
        /// </summary>
        private void SetPerFrameImGuiData(float deltaSeconds)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = new System.Numerics.Vector2(_windowWidth / _scaleFactor.X, _windowHeight / _scaleFactor.Y);
            io.DisplayFramebufferScale = _scaleFactor;
            io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
        }

        private void UpdateImGuiInput(GameWindow wnd)
        {
            ImGuiIOPtr io = ImGui.GetIO();

            MouseState MouseState = wnd.MouseState;
            KeyboardState KeyboardState = wnd.KeyboardState;

            io.MouseDown[0] = MouseState[MouseButton.Left];
            io.MouseDown[1] = MouseState[MouseButton.Right];
            io.MouseDown[2] = MouseState[MouseButton.Middle];
            io.MousePos.X = MouseState.X;
            io.MousePos.Y = MouseState.Y;

            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                if (key == Keys.Unknown)
                    continue;

                io.KeysDown[(int)key] = KeyboardState.IsKeyDown(key);
            }

            foreach (var c in PressedChars)
                io.AddInputCharacter(c);
            PressedChars.Clear();

            io.KeyCtrl = KeyboardState.IsKeyDown(Keys.LeftControl) || KeyboardState.IsKeyDown(Keys.RightControl);
            io.KeyAlt = KeyboardState.IsKeyDown(Keys.LeftAlt) || KeyboardState.IsKeyDown(Keys.RightAlt);
            io.KeyShift = KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift);
            io.KeySuper = KeyboardState.IsKeyDown(Keys.LeftSuper) || KeyboardState.IsKeyDown(Keys.RightSuper);
        }

        internal void PressChar(char keyChar)
        {
            PressedChars.Add(keyChar);
        }

        internal void MouseScroll(Vector2 offset)
        {
            ImGuiIOPtr io = ImGui.GetIO();

            io.MouseWheel = offset.Y;
            io.MouseWheelH = offset.X;
        }

        private static void SetKeyMappings()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.KeyMap[(int)ImGuiKey.Tab] = (int)Keys.Tab;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Keys.Left;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Keys.Right;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Keys.Up;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Keys.Down;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int)Keys.PageUp;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int)Keys.PageDown;
            io.KeyMap[(int)ImGuiKey.Home] = (int)Keys.Home;
            io.KeyMap[(int)ImGuiKey.End] = (int)Keys.End;
            io.KeyMap[(int)ImGuiKey.Delete] = (int)Keys.Delete;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int)Keys.Backspace;
            io.KeyMap[(int)ImGuiKey.Enter] = (int)Keys.Enter;
            io.KeyMap[(int)ImGuiKey.Escape] = (int)Keys.Escape;
            io.KeyMap[(int)ImGuiKey.A] = (int)Keys.A;
            io.KeyMap[(int)ImGuiKey.C] = (int)Keys.C;
            io.KeyMap[(int)ImGuiKey.V] = (int)Keys.V;
            io.KeyMap[(int)ImGuiKey.X] = (int)Keys.X;
            io.KeyMap[(int)ImGuiKey.Y] = (int)Keys.Y;
            io.KeyMap[(int)ImGuiKey.Z] = (int)Keys.Z;
        }

        private void RenderImDrawData(ImDrawDataPtr draw_data)
        {
            if (draw_data.CmdListsCount == 0)
                return;

            for (int i = 0; i < draw_data.CmdListsCount; i++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

                int vertexSize = cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
                if (vertexSize > _vertexBufferSize)
                {
                    int newSize = (int)Math.Max(_vertexBufferSize * 1.5f, vertexSize);
                    GL.NamedBufferData(_vertexBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                    _vertexBufferSize = newSize;

                    //Console.WriteLine($"Resized dear imgui vertex buffer to new size {_vertexBufferSize}");
                }

                int indexSize = cmd_list.IdxBuffer.Size * sizeof(ushort);
                if (indexSize > _indexBufferSize)
                {
                    int newSize = (int)Math.Max(_indexBufferSize * 1.5f, indexSize);
                    GL.NamedBufferData(_indexBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                    _indexBufferSize = newSize;

                    //Console.WriteLine($"Resized dear imgui index buffer to new size {_indexBufferSize}");
                }
            }

            // Setup orthographic projection matrix into our constant buffer
            ImGuiIOPtr io = ImGui.GetIO();
            Matrix4 mvp = Matrix4.CreateOrthographicOffCenter(0.0f, io.DisplaySize.X, io.DisplaySize.Y, 0.0f, -1.0f, 1.0f);
            
            _shader.UseShader();
            _shader.SetMatrix("projection_matrix", ref mvp);
            _shader.SetInt("in_fontTexture", 0);
            GLException.CheckError("ImGui Projection");

            GL.BindVertexArray(_vertexArray);
            GLException.CheckError("ImGui VAO");
            draw_data.ScaleClipRects(io.DisplayFramebufferScale);

            SetRenderSettings(io);

            // Render command lists
            for (int n = 0; n < draw_data.CmdListsCount; n++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];

                GL.NamedBufferSubData(_vertexBuffer, IntPtr.Zero, cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmd_list.VtxBuffer.Data);
                GLException.CheckError($"ImGui Data Vert {n}");

                GL.NamedBufferSubData(_indexBuffer, IntPtr.Zero, cmd_list.IdxBuffer.Size * sizeof(ushort), cmd_list.IdxBuffer.Data);
                GLException.CheckError($"ImGui Data Idx {n}");

                int vtx_offset = 0;
                int idx_offset = 0;

                for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
                {
                    ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                    if (pcmd.UserCallback != IntPtr.Zero)
                    {
                        int callback = (int)pcmd.UserCallback;
                        int callbackData = (int)pcmd.UserCallbackData;
                        if( ImGuiDelegates[callback - 1](ImGuiDelegateArgs[callbackData - 1]) )
                        {
                            _shader.UseShader();
                            _shader.SetMatrix("projection_matrix", ref mvp);
                            _shader.SetInt("in_fontTexture", 0);
                            GLException.CheckError("ImGui Projection");

                            GL.BindVertexArray(_vertexArray);
                            GLException.CheckError("ImGui VAO");
                            draw_data.ScaleClipRects(io.DisplayFramebufferScale);

                            // Reset ImGui values
                            SetRenderSettings(io);
                        }
                    }
                    else
                    {
                        GL.ActiveTexture(TextureUnit.Texture0);
                        GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);
                        GLException.CheckError("ImGui Texture");

                        // We do _windowHeight - (int)clip.W instead of (int)clip.Y because gl has flipped Y when it comes to these coordinates
                        var clip = pcmd.ClipRect;
                        GL.Scissor((int)clip.X, _windowHeight - (int)clip.W, (int)(clip.Z - clip.X), (int)(clip.W - clip.Y));
                        GLException.CheckError("ImGui Scissor");

                        if ((io.BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0)
                            GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (IntPtr)(idx_offset * sizeof(ushort)), vtx_offset);
                        else
                            GL.DrawElements(BeginMode.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (int)pcmd.IdxOffset * sizeof(ushort));
                    }

                    GLException.CheckError("ImGui Draw");
                    idx_offset += (int)pcmd.ElemCount;
                }

                vtx_offset += cmd_list.VtxBuffer.Size;
            }

            UnsetRenderSettings();
        }


        public static void AddCallback(ImDrawListPtr drawList, CustomDrawEvent callback, object args = null)
        {
            int index = ImGuiDelegates.Count + 1;
            int dataIndex = ImGuiDelegateArgs.Count + 1;
            ImGuiDelegates.Add(callback);
            ImGuiDelegateArgs.Add(args);
            drawList.AddCallback((IntPtr)index, (IntPtr)dataIndex);
        }

        internal void SetRenderSettings(ImGuiIOPtr io)
        {
            GL.Viewport(0, 0, (int)io.DisplaySize.X, (int)io.DisplaySize.Y);


            BlendEnabled = GL.IsEnabled(EnableCap.Blend);
            ScissorEnabled = GL.IsEnabled(EnableCap.ScissorTest);
            CullFaceEnabled = GL.IsEnabled(EnableCap.CullFace);
            DepthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);

            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.ScissorTest);

            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);
        }

        /// <summary>
        /// Only call this AFTER calling <see cref="SetRenderSettings(ImGuiIOPtr)"/>
        /// </summary>
        internal void UnsetRenderSettings()
        {
            //GL.BlendEquation(BlendEquationMode.FuncAdd);
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            if (!BlendEnabled)
                GL.Disable(EnableCap.Blend);

            if (!ScissorEnabled)
                GL.Disable(EnableCap.ScissorTest);

            if (CullFaceEnabled)
                GL.Enable(EnableCap.CullFace);

            if (DepthTestEnabled)
                GL.Enable(EnableCap.DepthTest);
        }

        /// <summary>
        /// Frees all graphics resources used by the renderer.
        /// </summary>
        public void Dispose()
        {
            GL.DeleteBuffer(_indexBuffer);
            GL.DeleteVertexArray(_vertexArray);
            GL.DeleteBuffer(_vertexBuffer);

            IconAtlas.Dispose();
            _fontTexture.Dispose();
            _shader.Dispose();
        }
    }
}
