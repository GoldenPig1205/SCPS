using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SCPS
{
    public static class Extensions
    {
        public static (ushort horizontal, ushort vertical) ToClientUShorts(this Quaternion rotation)
        {
            const float ToHorizontal = ushort.MaxValue / 360f;
            const float ToVertical = ushort.MaxValue / 176f;

            float fixVertical = -rotation.eulerAngles.x;

            if (fixVertical < -90f)
            {
                fixVertical += 360f;
            }
            else if (fixVertical > 270f)
            {
                fixVertical -= 360f;
            }

            float horizontal = Mathf.Clamp(rotation.eulerAngles.y, 0f, 360f);
            float vertical = Mathf.Clamp(fixVertical, -88f, 88f) + 88f;

            return ((ushort)Math.Round(horizontal * ToHorizontal), (ushort)Math.Round(vertical * ToVertical));
        }
    }
}
