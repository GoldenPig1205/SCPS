using UnityEngine;
using Newtonsoft.Json;

namespace SCPS.Locations
{
    public sealed class ScpsVector
    {
        public ScpsVector()
        {
        }

        public ScpsVector(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        public static implicit operator Vector3(ScpsVector value)
            => value == null ? Vector3.zero : new Vector3(value.X, value.Y, value.Z);

        public static implicit operator ScpsVector(Vector3 value)
            => new ScpsVector(value.x, value.y, value.z);
    }

    public sealed class ScpsLocation
    {
        public string Key { get; set; } = string.Empty;

        public string NameEnglish { get; set; } = string.Empty;

        public string NameKorean { get; set; } = string.Empty;

        public bool Captured { get; set; }

        public ScpsVector Position { get; set; } = new ScpsVector();

        public ScpsVector Rotation { get; set; } = new ScpsVector();

        [JsonIgnore]
        public Quaternion Quaternion => Quaternion.Euler((Vector3)Rotation);
    }
}
