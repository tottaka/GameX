using System;
using System.Collections.Generic;
using System.Numerics;
using Matrix4 = OpenTK.Mathematics.Matrix4;
using MathHelper = OpenTK.Mathematics.MathHelper;

namespace GameX
{
    public sealed class Transform
    {

        public Vector3 Position = Vector3.Zero;

        public Quaternion Rotation;

        public Vector3 Scale = Vector3.One;

        public Vector3 Forward
        {
            get
            {
                float yaw = Rotation.X;
                float pitch = Rotation.Y;

                return new Vector3((float)Math.Cos(yaw) * (float)Math.Cos(pitch), (float)Math.Sin(pitch), (float)Math.Sin(yaw) * (float)Math.Cos(pitch));
            }
        }

        public Vector3 Up = Vector3.UnitY;

        /// <summary>
        /// Rotation angles expressed in degrees.
        /// </summary>
        public Vector3 EulerAngles
        {
            get
            {
                return Rotation.ToOTK().ToEulerAngles().ToNumeric().ToDegrees();
            }

            set
            {
                Rotation = OpenTK.Mathematics.Quaternion.FromEulerAngles(value.ToRadians().ToOTK()).ToNumeric();
            }
        }

        public Matrix4 GetModelMatrix()
        {
            return Matrix4.CreateScale(Scale.X, Scale.Y, Scale.X) * Matrix4.CreateFromQuaternion(Rotation.ToOTK()) * Matrix4.CreateTranslation(Position.X, Position.Y, Position.Z);
        }

        public Matrix4 LookAt(Vector3 direction)
        {
            return Matrix4.LookAt(Position.X, Position.Y, Position.Z, Position.X + direction.X, Position.Y + direction.Y, Position.Z + direction.Z, Up.X, Up.Y, Up.Z);
        }

    }

    public static class MathExt
    {
        public static OpenTK.Mathematics.Quaternion ToOTK(this Quaternion quaternion) => new OpenTK.Mathematics.Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        public static OpenTK.Mathematics.Vector3 ToOTK(this Vector3 axis) => new OpenTK.Mathematics.Vector3(axis.X, axis.Y, axis.Z);
        public static Quaternion ToNumeric(this OpenTK.Mathematics.Quaternion quaternion) => new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);

        public static Vector3 ToDegrees(this Vector3 radians) => new Vector3(MathHelper.RadiansToDegrees(radians.X), MathHelper.RadiansToDegrees(radians.Y), MathHelper.RadiansToDegrees(radians.Z));
        public static Vector3 ToRadians(this Vector3 degrees) => new Vector3(MathHelper.DegreesToRadians(degrees.X), MathHelper.DegreesToRadians(degrees.Y), MathHelper.DegreesToRadians(degrees.Z));

    }

}
