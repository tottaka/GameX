using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace GameX
{
    /// <summary>
	/// Exception thrown by Egl class.
	/// </summary>
	public sealed class GLException : Exception
    {
        public ErrorCode errorCode = ErrorCode.NoError;

        /// <summary>
        /// Construct a GLException.
        /// </summary>
        /// <param name="errorCode">
        /// A <see cref="Int32"/> that specifies the error code.
        /// </param>
        public GLException(ErrorCode error) : this(error, GetErrorMessage(error)) { }
        public GLException(ErrorCode error, string message) : base(message)
        {
            errorCode = error;
        }

        /// <summary>
        /// Returns a description of the error code.
        /// </summary>
        /// <param name="errorCode">
        /// A <see cref="Int32"/> that specifies the error code.
        /// </param>
        /// <returns>
        /// It returns a description of <paramref name="errorCode"/>.
        /// </returns>
        private static string GetErrorMessage(ErrorCode errorCode)
        {
            switch (errorCode)
            {
                default:
                    return $"Unknown error";
                case ErrorCode.NoError:
                    return $"No error";
                case ErrorCode.OutOfMemory:
                    return $"Out of memory";
                case ErrorCode.TableTooLarge:
                    return $"Table too large";
                case ErrorCode.TextureTooLargeExt:
                    return $"Texture too large";
                case ErrorCode.InvalidValue:
                    return $"Invalid value";
                case ErrorCode.InvalidOperation:
                    return $"Invalid operation";
                case ErrorCode.InvalidFramebufferOperation:
                    return $"Invalid framebuffer operation";
                case ErrorCode.InvalidEnum:
                    return $"Invalid enum";
                case ErrorCode.ContextLost:
                    return $"Context lost";
            }
        }

        public static void CheckError(string context = "")
        {
            ErrorCode code = GL.GetError();
            if (code != ErrorCode.NoError)
            {
                if (string.IsNullOrWhiteSpace(context))
                    throw new GLException(code);
                else throw new GLException(code, string.Format("{0}: {1}", context, GetErrorMessage(code)));
            }
        }
    }
}
