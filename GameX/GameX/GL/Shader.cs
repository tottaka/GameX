using System;
using System.Collections.Generic;
using System.Numerics;
using System.IO;
using GameX.Properties;
using OpenTK.Graphics.OpenGL4;

using Matrix4 = OpenTK.Mathematics.Matrix4;

namespace GameX
{
    public struct UniformFieldInfo
    {
        public int Location;
        public string Name;
        public int Size;
        public ActiveUniformType Type;
    }

    public class Shader : IDisposable
    {
        public static Shader Default = new Shader(Resources.default_vert_shader, Resources.default_frag_shader, true);
        
        public int Program { get; private set; }
        private readonly Dictionary<string, int> UniformCache = new Dictionary<string, int>();
        private readonly Dictionary<string, int> AttribCache = new Dictionary<string, int>();

        internal string RelativeVertexShader, RelativeFragmentShader;

        public bool IsDisposed { get; private set; }

        internal Shader(string vertRelative, string fragRelative, bool build = false)
        {
            if (!build)
            {
                RelativeVertexShader = vertRelative;
                RelativeFragmentShader = fragRelative;
                return;
            }

            int vertShader = CompileShader(ShaderType.VertexShader, vertRelative);
            GLException.CheckError("Compile vertex shader");

            int fragShader = CompileShader(ShaderType.FragmentShader, fragRelative);
            GLException.CheckError("Compile fragment shader");

            Program = GL.CreateProgram();
            GLException.CheckError("Create shader program");

            GL.AttachShader(Program, vertShader);
            GL.AttachShader(Program, fragShader);
            GL.LinkProgram(Program);
            GL.GetProgram(Program, GetProgramParameterName.LinkStatus, out int Success);
            if (Success == 0)
                throw new GLException(ErrorCode.InvalidOperation, "GL.LinkProgram had info log: " + GL.GetProgramInfoLog(Program));

            GL.DeleteShader(vertShader);
            GL.DeleteShader(fragShader);
            GLException.CheckError("Shader");

        }

        ~Shader()
        {
            Dispose();
        }

        internal void Load(string basePath)
        {
            string vertexSource = File.ReadAllText(Path.Combine(basePath, RelativeVertexShader));
            int vertShader = CompileShader(ShaderType.VertexShader, vertexSource);
            GLException.CheckError("Compile vertex shader");

            string fragmentSource = File.ReadAllText(Path.Combine(basePath, RelativeFragmentShader));
            int fragShader = CompileShader(ShaderType.FragmentShader, fragmentSource);
            GLException.CheckError("Compile fragment shader");

            Program = GL.CreateProgram();
            GLException.CheckError("Create shader program");

            GL.AttachShader(Program, vertShader);
            GL.AttachShader(Program, fragShader);
            GL.LinkProgram(Program);
            GL.GetProgram(Program, GetProgramParameterName.LinkStatus, out int Success);
            if (Success == 0)
                throw new GLException(ErrorCode.InvalidOperation, "GL.LinkProgram had info log: " + GL.GetProgramInfoLog(Program));

            GL.DeleteShader(vertShader);
            GL.DeleteShader(fragShader);
            GLException.CheckError("Shader");
        }

        public void UseShader()
        {
            GL.UseProgram(Program);
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                GL.DeleteProgram(Program);
                IsDisposed = true;
            }

        }

        public UniformFieldInfo[] GetUniforms()
        {
            GL.GetProgram(Program, GetProgramParameterName.ActiveUniforms, out int UnifromCount);
            UniformFieldInfo[] Uniforms = new UniformFieldInfo[UnifromCount];

            for (int i = 0; i < UnifromCount; i++)
            {
                string Name = GL.GetActiveUniform(Program, i, out int Size, out ActiveUniformType Type);

                UniformFieldInfo FieldInfo;
                FieldInfo.Location = GetUniformLocation(Name);
                FieldInfo.Name = Name;
                FieldInfo.Size = Size;
                FieldInfo.Type = Type;

                Uniforms[i] = FieldInfo;
            }

            return Uniforms;
        }

        public void SetInt(string name, int value)
        {
            int location = GetUniformLocation(name);
            GL.Uniform1(location, value);
        }

        public void SetFloat(string name, float value)
        {
            int location = GetUniformLocation(name);
            GL.Uniform1(location, value);
        }

        public void SetMatrix(string name, ref Matrix4 matrix, bool transpose = false)
        {
            int location = GetUniformLocation(name);
            GL.UniformMatrix4(location, transpose, ref matrix);
        }

        public void SetVector3(string name, ref Vector3 value)
        {
            int location = GetUniformLocation(name);
            GL.Uniform3(location, value.X, value.Y, value.Z);
        }

        public void SetVector3(string name, float x, float y, float z)
        {
            int location = GetUniformLocation(name);
            GL.Uniform3(location, x, y, z);
        
        }

        public int GetUniformLocation(string uniform)
        {
            if (!UniformCache.TryGetValue(uniform, out int location))
            {
                location = GL.GetUniformLocation(Program, uniform);
                UniformCache.Add(uniform, location);

                if (location == -1)
                    throw new GLException(ErrorCode.InvalidValue, $"The uniform '{uniform}' does not exist in this shader!");
            }

            return location;
        }

        public int GetAttributeLocation(string attribute)
        {
            if (!AttribCache.TryGetValue(attribute, out int location))
            {
                location = GL.GetAttribLocation(Program, attribute);
                AttribCache.Add(attribute, location);

                if (location == -1)
                    throw new GLException(ErrorCode.InvalidValue, $"The uniform '{attribute}' does not exist in this shader!");
            }

            return location;
        }

        private int CompileShader(ShaderType type, string source)
        {
            GLException.CheckError("Before CreateShader(" + type + ")");
            int Shader = GL.CreateShader(type);
            GL.ShaderSource(Shader, 1, new string[] { source }, new int[] { source.Length });
            GL.CompileShader(Shader);
            GL.GetShader(Shader, ShaderParameter.CompileStatus, out int success);
            if (success != 1)
            {
                GL.GetShaderInfoLog(Shader, out string infoLog);
                throw new GLException(ErrorCode.InvalidOperation, $"GL.CompileShader for shader [{type}] had info log: {infoLog}");
            }

            return Shader;
        }
    }
}
