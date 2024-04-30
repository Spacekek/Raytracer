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
            camera = new Camera();
            camera.x = 0;
            camera.y = 0;
            camera.z = -5;
            camera.dx = 0;
            camera.dy = 0;
            camera.dz = 1;
            camera.ux = 0;
            camera.uy = 1;
            camera.uz = 0;
            camera.fov = 90;
            // create scene
            scene = new Scene();
            scene.Add(new Sphere(0, 0, 0, 1));
            scene.Add(new Plane(0, 1, 0, 1));
            scene.Add(new Light(2, 2, 0, 1, 1, 1));
            // create raytracer
            raytracer = new Raytracer(scene, camera, screen);
        }
        // tick: renders one frame
        public void Tick()
        {
            screen.Clear(0);
            raytracer.Render();
            // raytracer.Debug();
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
        public abstract Intersection Intersect(float ox, float oy, float oz, float dx, float dy, float dz, ref float t);
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
        public override Intersection Intersect(float ox, float oy, float oz, float dx, float dy, float dz, ref float t)
        {
            float a = dx * dx + dy * dy + dz * dz;
            float b = 2 * (dx * (ox - x) + dy * (oy - y) + dz * (oz - z));
            float c = (ox - x) * (ox - x) + (oy - y) * (oy - y) + (oz - z) * (oz - z) - radius * radius;
            float det = b * b - 4 * a * c;
            if (det > 0)
            {
                det = (float)Math.Sqrt(det);
                float t0 = (-b - det) / (2 * a);
                float t1 = (-b + det) / (2 * a);
                if (t0 > 0)
                {
                    t = t0;
                    return new Intersection(this, t0, 0, 0, 0);
                }
                if (t1 > 0)
                {
                    t = t1;
                    return new Intersection(this, t1, 0, 0, 0);
                }
            }
            return null;
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
        public override Intersection Intersect(float ox, float oy, float oz, float dx, float dy, float dz, ref float t)
        {
            float denom = nx * dx + ny * dy + nz * dz;
            if (denom > 0) return null;
            t = (nx * (x - ox) + ny * (y - oy) + nz * (z - oz)) / denom;
            return new Intersection(this, t, nx, ny, nz);
        }
        public override (float, float, float) Normal(float x, float y, float z, ref float nx, ref float ny, ref float nz)
        {
            nx = this.nx;
            ny = this.ny;
            nz = this.nz;
            return (nx, ny, nz);
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
                    float u = (2 * x - w) / (float)w;
                    float v = (2 * y - h) / (float)h;
                    float ox = camera.x;
                    float oy = camera.y;
                    float oz = camera.z;
                    float dx = camera.dx + u * camera.corners[0] + v * camera.corners[3];
                    float dy = camera.dy + u * camera.corners[1] + v * camera.corners[4];
                    float dz = camera.dz + u * camera.corners[2] + v * camera.corners[5];
                    float t = 0;
                    Primitive prim = null;
                    Intersection isect = scene.Intersect(ox, oy, oz, dx, dy, dz, ref prim, ref t);
                    if (isect != null)
                    {
                        float nx = 0, ny = 0, nz = 0;
                        prim.Normal(ox + t * dx, oy + t * dy, oz + t * dz, ref nx, ref ny, ref nz);
                        float r = 0, g = 0, b = 0;
                        foreach (Light light in scene.lights)
                        {
                            float ldx = light.x - ox;
                            float ldy = light.y - oy;
                            float ldz = light.z - oz;
                            float ldist = (float)Math.Sqrt(ldx * ldx + ldy * ldy + ldz * ldz);
                            ldx /= ldist;
                            ldy /= ldist;
                            ldz /= ldist;
                            float dt = ldx * nx + ldy * ny + ldz * nz;
                            if (dt > 0)
                            {
                                float shade = dt;
                                r += light.r * shade;
                                g += light.g * shade;
                                b += light.b * shade;
                            }
                        }
                        pixels[x + y * w] = ((int)(r * 255) << 16) | ((int)(g * 255) << 8) | (int)(b * 255);
                    }
                }
            }
        }
        // debug view For the middle row of pixels (typically line 256 for a 512x512 window), it generates debug output by visualizing every Nth ray (where N is e.g. 10).
        public void Debug()
        {
            int w = screen.width;
            int h = screen.height;
            int[] pixels = screen.pixels;
            for (int x = 0; x < w; x++)
            {
                float u = (2 * x - w) / (float)w;
                float v = 0;
                float ox = camera.x;
                float oy = camera.y;
                float oz = camera.z;
                float dx = camera.dx + u * camera.corners[0] + v * camera.corners[3];
                float dy = camera.dy + u * camera.corners[1] + v * camera.corners[4];
                float dz = camera.dz + u * camera.corners[2] + v * camera.corners[5];
                float t = 0;
                Primitive prim = null;
                Intersection isect = scene.Intersect(ox, oy, oz, dx, dy, dz, ref prim, ref t);
                if (isect != null)
                {
                    pixels[x + 256 * w] = 0xffffff;
                }
            }
        }
    }
}