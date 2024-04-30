namespace Template
{
    class MyApplication
    {
        // member variables
        public Surface screen;
        // constructor
        public MyApplication(Surface screen)
        {
            this.screen = screen;
        }
        // initialize
        public void Init()
        {

        }
        // tick: renders one frame
        public void Tick()
        {
            screen.Clear(0);
            screen.Print("hello world", 2, 2, 0xffffff);
            screen.Line(2, 20, 160, 20, 0xff0000);
        }
    }
    class Camera
    {
        // camera position
        public float x, y, z;
        // look at direction
        public float dx, dy, dz;
        // up direction
        public float ux, uy, uz;
        // field of view
        public float fov;
        // screen plane specified by four corner points
        public float[] corners;

        public Camera()
        {
            x = y = z = 0;
            dx = 0; dy = 0; dz = 1;
            ux = 0; uy = 1; uz = 0;
            fov = 90;
            corners = new float[8];

            // initialize corners
            for (int i = 0; i < 8; i++)
            {
                corners[i] = 0;
            }

            // set corners
            corners[0] = -1; corners[1] = -1; corners[2] = 1;
            corners[3] = 1; corners[4] = -1; corners[5] = 1;
            corners[6] = 1; corners[7] = 1;
        }
    }

    abstract class Primitive
    {
        public float x, y, z;
        public float r, g, b;
        // intersect
        public abstract bool Intersect(float ox, float oy, float oz, float dx, float dy, float dz, ref float t);
        // normal
        public abstract (float, float, float) Normal(float x, float y, float z, ref float nx, ref float ny, ref float nz);
    }
    // a sphere is defined by a center and a radius
    class Sphere : Primitive
    {
        public float radius;
        public Sphere(float x, float y, float z, float radius)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.radius = radius;
        }
        public override bool Intersect(float ox, float oy, float oz, float dx, float dy, float dz, ref float t)
        {
            float l = x - ox;
            float m = y - oy;
            float n = z - oz;
            float b = l * dx + m * dy + n * dz;
            float c = l * l + m * m + n * n - radius * radius;
            if (c > 0 && b > 0) return false;
            float discr = b * b - c;
            if (discr < 0) return false;
            t = -b - (float)Math.Sqrt(discr);
            if (t < 0) t = 0;
            return true;
        }
        public override (float, float, float) Normal(float x, float y, float z, ref float nx, ref float ny, ref float nz)
        {
            nx = x - this.x;
            ny = y - this.y;
            nz = z - this.z;
            float len = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
            nx /= len;
            ny /= len;
            nz /= len;
            return (nx, ny, nz);
        }
    }
    
    // a plane is defined by a normal and a distance to the origin
    class Plane : Primitive
    {
        public float nx, ny, nz, d;
        public Plane(float nx, float ny, float nz, float d)
        {
            this.nx = nx;
            this.ny = ny;
            this.nz = nz;
            this.d = d;
        }
        public override bool Intersect(float ox, float oy, float oz, float dx, float dy, float dz, ref float t)
        {
            float denom = nx * dx + ny * dy + nz * dz;
            if (denom > 0) return false;
            t = -(nx * ox + ny * oy + nz * oz + d) / denom;
            return true;
        }
        public override (float, float, float) Normal(float x, float y, float z, ref float nx, ref float ny, ref float nz)
        {
            nx = this.nx;
            ny = this.ny;
            nz = this.nz;
            return (nx, ny, nz);
        }
    }
}