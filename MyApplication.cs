using OpenTK.Mathematics;
using Objects;

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
            // initialize screen, camera, scene and raytracer
            this.screen = screen;
            camera = new Camera((float)screen.width / screen.height);
            scene = new Scene();
            raytracer = new Raytracer(scene, camera, screen);
        }

        public void Init()
        {
            // add scene objects and lights
            Sphere sphere = new Sphere(0.0f, 0.0f, 2f, 0.1f);
            sphere.SetColor(0.0f, 0.0f, 1.0f);
            scene.Add(sphere);

            Plane plane = new Plane(0.0f, 0.5f, 1f, 3f);
            plane.SetColor(0.0f, 1.0f, 0.0f);
            scene.Add(plane);

            scene.Add(new Light(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f));
        }

        public void Tick()
        {
            // every frame, clear the screen, render the scene and draw debug view
            screen.Clear(0);
            raytracer.Render();
            // raytracer.Debug();
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
            // distance of camera plane is dependent on field of view
            float d = 1 / (float)Math.Tan(fov * Math.PI / 180 / 2);
            corners[0] = new Vector3(-aspect / 5, -0.2f, d / 5);
            corners[1] = new Vector3(aspect / 5, -0.2f, d / 5);
            corners[2] = new Vector3(aspect / 5, 0.2f, d / 5);
            corners[3] = new Vector3(-aspect / 5, 0.2f, d / 5);
        }

        public void DrawDebug(Surface screen, float scale, float x_offset, float y_offset)
        {
            // draw lines between camera corners to form a triangle
            int x0 = screen.XScreen(position.X, scale, x_offset);
            int y0 = screen.YScreen(position.Z, scale, y_offset);
            int x1 = screen.XScreen(corners[0].X, scale, x_offset);
            int y1 = screen.YScreen(corners[0].Z, scale, y_offset);
            int x2 = screen.XScreen(corners[1].X, scale, x_offset);
            int y2 = screen.YScreen(corners[1].Z, scale, y_offset);
            screen.Line(x0, y0, x1, y1, 0x00ff00);
            screen.Line(x0, y0, x2, y2, 0x00ff00);
            screen.Line(x1, y1, x2, y2, 0xffaa00);
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

        public Intersection Intersect(Vector3 origin, Vector3 direction)
        {
            Intersection isect = null;
            // find closest intersection by checking all primitives in the scene and keeping track of the closest one
            float distance = float.MaxValue;
            foreach (Primitive p in primitives)
            {
                Intersection isect2 = p.Intersect(origin, direction);
                // if we hit an object and it is closer than the previous closest object, update the closest object
                if (isect2 != null && isect2.distance < distance)
                {
                    distance = isect2.distance;
                    isect = isect2;
                }
            }
            return isect;
        }
    }

    class Raytracer
    {
        private Scene scene;
        private Camera camera;
        private Surface screen;
        private float x_offset;
        private float y_offset;
        private float scale;

        public Raytracer(Scene scene, Camera camera, Surface screen)
        {
            this.scene = scene;
            this.camera = camera;
            this.screen = screen;
            x_offset = 2.0f;
            y_offset = 4.0f;
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
                    // ray from camera through camera plane for every pixel
                    float dx = camera.corners[0].X + (camera.corners[1].X - camera.corners[0].X) * x / w;
                    float dz = camera.corners[0].Z + (camera.corners[1].Z - camera.corners[0].Z) * x / w;
                    float dy = camera.corners[0].Y + (camera.corners[3].Y - camera.corners[0].Y) * y / h;
                    Intersection isect = scene.Intersect(new Vector3(camera.position.X, camera.position.Y, camera.position.Z), new Vector3(dx, dy, dz));
                    if (isect != null)
                    {
                        // if we hit an object, set color to object color
                        Primitive prim = isect.prim;
                        pixels[y * w + x] = prim.MixColor(prim.color.R, prim.color.G, prim.color.B);
                        continue;
                    }
                    // if we hit nothing, set color to black
                    pixels[y * w + x] = 0;
                }
            }
        }

        public void Debug()
        {
            int screenCamX = screen.XScreen(camera.position.X, scale, x_offset);
            int screenCamY = screen.YScreen(camera.position.Z, scale, y_offset);
            for (int x = 0; x < screen.width; x++)
            {
                // draw rays from camera to camera plane for every 10th pixel
                if (x % 10 == 0)
                {
                    float dx = camera.corners[0].X + (camera.corners[1].X - camera.corners[0].X) * x / screen.width;
                    float dz = camera.corners[0].Z + (camera.corners[1].Z - camera.corners[0].Z) * x / screen.width;
                    int screenRayX = screen.XScreen(dx * 20, scale, x_offset);
                    int screenRayY = screen.YScreen(dz * 20, scale, y_offset);
                    screen.Line(screenCamX, screenCamY, screenRayX, screenRayY, 0xff0000);
                }
            }
            // draw camera triangle
            camera.DrawDebug(screen, scale, x_offset, y_offset);
            // draw lights as yellow circles
            foreach (Light l in scene.lights)
            {
                int xl = screen.XScreen(l.position.X, scale, x_offset);
                int yl = screen.YScreen(l.position.Z, scale, y_offset);
                screen.Circle(xl, yl, 5, 0xffff00);
            }
            // draw primitives as whatever they want to draw
            foreach (Primitive p in scene.primitives)
            {
                p.DrawDebug(screen, scale, x_offset, y_offset);
            }
        }
    }
}
