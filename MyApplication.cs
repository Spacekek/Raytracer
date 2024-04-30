using System.Security.Cryptography.X509Certificates;

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
        // intersect
        public abstract bool Intersect(float ox, float oy, float oz, float dx, float dy, float dz, ref float t);
        // normal
        public abstract (float, float, float) Normal(float x, float y, float z, ref float nx, ref float ny, ref float nz);
    }
}