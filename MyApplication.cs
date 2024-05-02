namespace Template
{
    class MyApplication
    {
        // member variables
        public Surface screen;
        public Camera camera;
        public Scene scene;
        public Raytracer raytracer;
        // constructor
        public MyApplication(Surface screen)
        {
            this.screen = screen;
        }
        // initialize
        public void Init()
        {
            // create camera
            camera = new Camera((float)screen.width / screen.height);
            // create scene
            scene = new Scene();
            Sphere sphere = new Sphere(1, 0, 0, 0.1f);
            sphere.SetColor(1, 0, 1);
            scene.Add(sphere);
            scene.Add(new Light(1, 1, -1, 1, 1, 1));
            // create raytracer
            raytracer = new Raytracer(scene, camera, screen);
        }
        // tick: renders one frame
        public void Tick()
        {
            screen.Clear(0);
            raytracer.Render();
            raytracer.Debug();
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
        public Camera(float aspect)
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

            // set corners based on fov, direction, up and screen aspect ratio
            float angle = (float)(fov * Math.PI / 180 / 2);
            float x0 = (float)(Math.Tan(angle) * aspect);
            float y0 = (float)(Math.Tan(angle));

        }
    }

    abstract class Primitive
    {
        public float x, y, z;
        public float r, g, b;
        // constructor
        public Primitive()
        {
            r = g = b = 1;
        }
        // intersect
        public abstract Intersection Intersect(float ox, float oy, float oz, float dx, float dy, float dz, ref float t);
        // draw debug
        public virtual void DrawDebug(Surface screen)
        {
            // draw a box on the xz plane
            int width = screen.width;
            int height = screen.height;
            // for now we assume axes are between -2 and 2
            // screen.box takes pixel coordinates instead of world coordinates
            int x0 = (int)(x * width / 4 + width / 2 - 5);
            int y0 = (int)(z * height / 4 + height / 2 - 5);
            int x1 = (int)(z * width / 4 + width / 2 + 5);
            int y1 = (int)(z * height / 4 + height / 2 + 5);
            
            screen.Box(x0, y0, x1, y1, MixColor(r, g, b));
        }
        public int MixColor(float r, float g, float b)
        {
            int red = (int)(r * 255);
            int green = (int)(g * 255);
            int blue = (int)(b * 255);
            return (red << 16) + (green << 8) + blue;
        }
        public void SetColor(float r, float g, float b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }
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
        public override Intersection Intersect(float ox, float oy, float oz, float dx, float dy, float dz, ref float t)
        {
            // #TODO implement sphere intersection
            return null;
        }
        public override void DrawDebug(Surface screen)
        {
            // draw a circle on the xz plane
            int width = screen.width;
            int height = screen.height;
            int x0 = (int)(x * width / 4 + width / 2);
            int y0 = (int)(z * height / 4 + height / 2);
            int r = (int)(radius * width / 4);
            // draw circle by drawing lines between points on the circle
            for (int i = 0; i < 360; i += 10)
            {
                float a0 = (float)(i * Math.PI / 180);
                float a1 = (float)((i + 10) * Math.PI / 180);
                int x1 = (int)(x0 + r * Math.Cos(a0));
                int y1 = (int)(y0 + r * Math.Sin(a0));
                int x2 = (int)(x0 + r * Math.Cos(a1));
                int y2 = (int)(y0 + r * Math.Sin(a1));
                screen.Line(x1, y1, x2, y2, MixColor(r, g, b));
            }
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
        public override Intersection Intersect(float ox, float oy, float oz, float dx, float dy, float dz, ref float t)
        {
            // TODO: implement plane intersection
            return null;
        }
    }
    // Light stores location and intensity of a light source
    class Light
    {
        public float x, y, z;
        public float r, g, b;
        public Light(float x, float y, float z, float r, float g, float b)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.r = r;
            this.g = g;
            this.b = b;
        }
    }
    // Scene, which stores a list of primitives and light sources. It implements a scene-level Intersect method, which loops over the primitives and returns the closest intersection4
    class Scene
    {
        public List<Primitive> primitives;
        public List<Light> lights;
        public Scene()
        {
            primitives = new List<Primitive>();
            lights = new List<Light>();
        }
        public void Add(Primitive p)
        {
            primitives.Add(p);
        }
        public void Add(Light l)
        {
            lights.Add(l);
        }
        public Intersection Intersect(float ox, float oy, float oz, float dx, float dy, float dz, ref Primitive prim, ref float t)
        {
            Intersection isect = null;
            t = float.MaxValue;
            foreach (Primitive p in primitives)
            {
                float t2 = 0;
                Intersection isect2 = p.Intersect(ox, oy, oz, dx, dy, dz, ref t2);
                if (isect2 != null && t2 < t)
                {
                    t = t2;
                    isect = isect2;
                    prim = p;
                }
            }
            return isect;
        }
    }
    // Intersection, which stores the result of an intersection. Apart from the intersection distance, you will at least want to store the nearest primitive, but perhaps also the normal at the intersection point.
    class Intersection
    {
        public Primitive prim;
        public float t;
        public float nx, ny, nz;
        public Intersection(Primitive prim, float t, float nx, float ny, float nz)
        {
            this.prim = prim;
            this.t = t;
            this.nx = nx;
            this.ny = ny;
            this.nz = nz;
        }
    }
    class Raytracer
    {
        public Scene scene;
        public Camera camera;
        public Surface screen;
        public Raytracer(Scene scene, Camera camera, Surface screen)
        {
            this.scene = scene;
            this.camera = camera;
            this.screen = screen;
        }
        public void Render()
        {
            int w = screen.width;
            int h = screen.height;
            int[] pixels = screen.pixels;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // calculate ray direction, normalize and check for intersections
                    // # TODO
                    // set pixel color to background color
                    screen.Plot(x, y, 0x000000);
                }
            }
        }
        // debug view For the middle row of pixels, it generates debug output by visualizing every Nth ray (where N is e.g. 10).
        public void Debug()
        {
            int w = screen.width;
            int h = screen.height;

            // draw bar at camera location
            int xc = (int)camera.x * w / 4 + w / 2;
            int zc = (int)camera.z * h / 4 + h / 2;
            screen.Bar(xc -2, zc - 2, xc + 2, zc + 2, 0x00ff00);
            // draw triangle representing the field of view keeping direction of camera in mind
            // xz angle of camera
            float cameradir = (float)Math.Atan2(camera.dx, camera.dz);
            float angle = (float)(cameradir + camera.fov * Math.PI / 180 / 2);
            int x0 = (int)(xc + (50 * Math.Cos(angle)));
            int z0 = (int)(zc + (50 * Math.Sin(angle)));
            int x1 = (int)(xc + (50 * Math.Cos(-angle)));
            int z1 = (int)(zc + (50 * Math.Sin(-angle)));
            screen.Line(xc, zc, x0, z0, 0x00ff00);
            screen.Line(xc, zc, x1, z1, 0x00ff00);
            screen.Line(x0, z0, x1, z1, 0x00ff00);


            // draw rays
            for (int x = 0; x < w; x++)
            {
                if (x % 10 == 0)
                {
                    // calculate ray direction and draw line
                }
            }
            // draw lights
            foreach (Light l in scene.lights)
            {
                int xl = (int)l.x * w / 4 + w / 2;
                int zl = (int)l.z * h / 4 + h / 2;
                screen.Bar(xl - 2, zl - 2, xl + 2, zl + 2, 0xffff00);
            }
            // draw primitives
            foreach (Primitive p in scene.primitives)
            {
                p.DrawDebug(screen);
            }
        }
    }
}