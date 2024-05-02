using OpenTK.Mathematics;

namespace Template
{
    class MyApplication
    {
        public Surface screen;
        private Camera camera;
        private Scene scene;
        private Raytracer raytracer;

        public MyApplication(Surface screen)
        {
            this.screen = screen;
        }

        public void Init()
        {
            camera = new Camera((float)screen.width / screen.height);
            scene = new Scene();
            Sphere sphere = new Sphere(0, 0, 1.0f, 0.1f);
            sphere.SetColor(1.0f, 0.0f, 1.0f);
            scene.Add(sphere);
            scene.Add(new Light(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f));
            raytracer = new Raytracer(scene, camera, screen);
        }

        public void Tick()
        {
            screen.Clear(0);
            raytracer.Render();
            raytracer.Debug();
        }
    }

    class Camera
    {
        public Vector3 position;
        private Vector3 direction;
        private Vector3 up;
        private float fov;
        public Vector3[] corners;

        public Camera(float aspect)
        {
            position = new Vector3(0, 0, 0);
            direction = new Vector3(0, 0, 1);
            up = new Vector3(0, 1, 0);
            fov = 90;
            corners = new Vector3[4];

            float d = 1 / (float)Math.Tan(fov * Math.PI / 180 / 2);
            corners[0] = new Vector3(-aspect / 5, -0.2f, d / 5);
            corners[1] = new Vector3(aspect / 5, -0.2f, d / 5);
            corners[2] = new Vector3(aspect / 5, 0.2f, d / 5);
            corners[3] = new Vector3(-aspect / 5, 0.2f, d / 5);
        }

        public void DrawDebug(Surface screen, float scale = 4, float offset = 2)
        {
            int x0 = screen.XScreen(position.X, scale, offset);
            int y0 = screen.YScreen(position.Z, scale, offset);
            int x1 = screen.XScreen(corners[0].X, scale, offset);
            int y1 = screen.YScreen(corners[0].Z, scale, offset);
            int x2 = screen.XScreen(corners[1].X, scale, offset);
            int y2 = screen.YScreen(corners[1].Z, scale, offset);
            screen.Line(x0, y0, x1, y1, 0x00ff00);
            screen.Line(x0, y0, x2, y2, 0x00ff00);
            screen.Line(x1, y1, x2, y2, 0xffaa00);
        }
    }

    abstract class Primitive
    {
        public Vector3 position;
        public Color4 color;

        public Primitive()
        {
            color = new Color4(1.0f, 1.0f, 1.0f, 1);
        }

        public abstract Intersection Intersect(float ox, float oy, float oz, float dx, float dy, float dz, ref float t);

        public virtual void DrawDebug(Surface screen, float scale, float offset)
        {
            int x = screen.XScreen(position.X, scale, offset);
            int y = screen.YScreen(position.Z, scale, offset);
            screen.Box(x - 2, y - 2, x + 2, y + 2, MixColor(color.R, color.G, color.B));
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
            color = new Color4(r, g, b, 1);
        }
    }

    class Sphere : Primitive
    {
        private float radius;

        public Sphere(float x, float y, float z, float radius)
        {
            position = new Vector3(x, y, z);
            this.radius = radius;
        }

        public override Intersection Intersect(float ox, float oy, float oz, float dx, float dy, float dz, ref float t)
        {
            return null;
        }

        public override void DrawDebug(Surface screen, float scale, float offset)
        {
            int x = screen.XScreen(position.X, scale, offset);
            int y = screen.YScreen(position.Z, scale, offset);
            int r = (int)(radius * scale);
            screen.Circle(x, y, 10, MixColor(color.R, color.G, color.B));
        }
    }

    class Plane : Primitive
    {
        private Vector3 normal;
        private float d;

        public Plane(float nx, float ny, float nz, float d)
        {
            normal = new Vector3(nx, ny, nz);
            this.d = d;
        }

        public override Intersection Intersect(float ox, float oy, float oz, float dx, float dy, float dz, ref float t)
        {
            return null;
        }
    }

    class Light
    {
        public Vector3 position;
        private Color4 color;

        public Light(float x, float y, float z, float r, float g, float b)
        {
            position = new Vector3(x, y, z);
            color = new Color4(r, g, b, 1);
        }
    }

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

    class Intersection
    {
        public Primitive prim;
        public float t;
        public Vector3 hitPoint;

        public Intersection(Primitive prim, float t, Vector3 hitPoint)
        {
            this.prim = prim;
            this.t = t;
            this.hitPoint = hitPoint;
        }
    }

    class Raytracer
    {
        private Scene scene;
        private Camera camera;
        private Surface screen;
        private float offset;
        private float scale;

        public Raytracer(Scene scene, Camera camera, Surface screen)
        {
            this.scene = scene;
            this.camera = camera;
            this.screen = screen;
            offset = 2.0f;
            scale = 4.0f;
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
                    screen.Plot(x, y, 0x000000);
                }
            }
        }

        public void Debug()
        {
            int screenCamX = screen.XScreen(camera.position.X, scale, offset);
            int screenCamY = screen.YScreen(camera.position.Z, scale, offset);
            for (int x = 0; x < screen.width; x++)
            {
                if (x % 10 == 0)
                {
                    float dx = camera.corners[0].X + (camera.corners[1].X - camera.corners[0].X) * x / screen.width;
                    float dz = camera.corners[0].Z + (camera.corners[1].Z - camera.corners[0].Z) * x / screen.width;
                    int screenRayX = screen.XScreen(dx * 20, scale, offset);
                    int screenRayY = screen.YScreen(dz * 20, scale, offset);
                    screen.Line(screenCamX, screenCamY, screenRayX, screenRayY, 0xff0000);
                }
            }
            camera.DrawDebug(screen, scale, offset);
            foreach (Light l in scene.lights)
            {
                int xl = screen.XScreen(l.position.X, scale, offset);
                int yl = screen.YScreen(l.position.Z, scale, offset);
                screen.Circle(xl, yl, 5, 0xffff00);
            }
            foreach (Primitive p in scene.primitives)
            {
                p.DrawDebug(screen, scale, offset);
            }
        }
    }
}
