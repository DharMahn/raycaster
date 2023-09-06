using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ray_marching
{
    class HitResult
    {
        public Color Color;
        public Vector2 EndPoint;
        public float distanceToScene;
    }
}
