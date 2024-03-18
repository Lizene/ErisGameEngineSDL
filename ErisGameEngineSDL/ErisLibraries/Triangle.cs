using ErisMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDL2;

namespace ErisLibraries
{
    internal struct Triangle
    {
        public int[] indices;
        public Vec3 normal, color;
        public Triangle(int[] indices, Vec3 normal, Vec3 color) 
        {
            this.indices = indices;
            this.normal = normal;
            this.color = color;
        }
    }
}
