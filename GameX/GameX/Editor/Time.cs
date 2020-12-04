using System;
using System.Collections.Generic;
using System.Text;

namespace GameX.Editor
{
    public static class Time
    {
        /// <summary>
        /// The number of seconds elapsed since the previous frame update.
        /// </summary>
        public static float DeltaTime = 0.0f;

        /// <summary>
        /// The number of seconds elapsed since the previous frame update in the <see cref="GameX"/> Editor.
        /// </summary>
        public static float EditorDelta = 0.0f;

    }
}
