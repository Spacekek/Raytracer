using OpenTK.Mathematics;
using Objects;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Template
{
    class MyApplication
    {
        public Surface screen;
        private Camera camera;
        private Scene scene;
        private Raytracer raytracer;
        private bool debug;

        public MyApplication(Surface screen)
        {
            // initialize screen, camera, scene and raytracer
            this.screen = screen;
            camera = new Camera((float)screen.width / screen.height);
            scene = new Scene();
            raytracer = new Raytracer(scene, camera, screen);
            debug = false;
        }

        public void Init()
        {
            // add scene objects and lights
            Sphere sphere = new Sphere(0.0f, 0.0f, 2f, 0.5f);
            Material material = new Material(1.0f);
            material.ambientColor = new Color4(0.2f, 0.2f, 0.2f, 1.0f);
            sphere.material = material;
            sphere.SetColor(0.0f, 0.8f, 1.0f);

            scene.Add(sphere);

            Sphere sphere2 = new Sphere(0.5f, 0.6f, 2.2f, 0.3f);
            Material material1 = new Material(1.0f);
            material1.ambientColor = new Color4(0.6f, 0.6f, 0.6f, 1.0f);
            sphere2.SetColor(1.0f, 0.0f, 0.0f);
            scene.Add(sphere2);

            Plane plane = new Plane(0.0f, -1.0f, -1f, 3.5f);
            Material material2 = new Material(1.0f);
            material2.ambientColor = new Color4(0.2f, 0.6f, 0.2f, 1.0f);
            plane.material = material2;
            plane.SetColor(0.0f, 1.0f, 0.0f);
            scene.Add(plane);

            // ground plane
            Plane plane2 = new Plane(0.0f, -1.0f, 0.0f, 1.0f);
            Material material3 = new Material(1.0f);
            material3.ambientColor = new Color4(0.5f, 0.5f, 0.5f, 1.0f);
            material3.specular = 1.0f;
            plane2.material = material3;
            plane2.SetColor(1.0f, 1.0f, 1.0f);
            scene.Add(plane2);

            scene.Add(new Light(1.0f, -1.0f, 1.5f));
        }

        public void Tick()
        {
            // every frame, clear the screen, render the scene and draw debug view
            screen.Clear(0);
            if (debug)
            {
                raytracer.Debug();
            }
            else
            {
                raytracer.Render();
            }
        }
        public void UpdateKeyboard(KeyboardState state)
        {
            // toggle debug view on key press
            if (state.IsKeyDown(Keys.Space))
            {
                debug = !debug;
            }
            if (state.IsKeyDown(Keys.W))
            {
                camera.position += camera.direction * 0.1f; // Move forward
            }
            if (state.IsKeyDown(Keys.S))
            {
                camera.position -= camera.direction * 0.1f; // Move backward
            }
            if (state.IsKeyDown(Keys.A))
            {
                Vector3 left = Vector3.Cross(camera.up, camera.direction).Normalized();
                camera.position -= left * 0.1f; // Move left
            }
            if (state.IsKeyDown(Keys.D))
            {
                Vector3 right = Vector3.Cross(camera.up, camera.direction).Normalized();
                camera.position += right * 0.1f; // Move right
            }
            if (state.IsKeyDown(Keys.Left))
            {
                camera.RotateYaw(-1.0f); // Rotate left
            }
            if (state.IsKeyDown(Keys.Right))
            {
                camera.RotateYaw(1.0f); // Rotate right
            }
            if (state.IsKeyDown(Keys.Up))
            {
                camera.RotatePitch(1.0f); // Look up
            }
            if (state.IsKeyDown(Keys.Down))
            {
                camera.RotatePitch(-1.0f); // Look down
            }
        }
    }

    class Camera
    {
        public Vector3 position;
        public Vector3 direction;
        public Vector3 up;
        public float fov;
        public Vector3[] corners;

        public Camera(float aspect)
        {
            position = new Vector3(0, 0, 0);
            direction = new Vector3(0, 0, 1);
            up = new Vector3(0, 1, 0);
            fov = 90;
            corners = new Vector3[4];
            CalculateCorners(aspect);
        }

        private void CalculateCorners(float aspect)
        {
            float d = 1 / (float)Math.Tan(fov * Math.PI / 180 / 2);
            Vector3 right = Vector3.Cross(up, direction).Normalized();
            Vector3 upAdjusted = Vector3.Cross(direction, right);
            float height = d * (float)Math.Tan(fov * Math.PI / 360);
            float width = height * aspect;
            corners = new Vector3[4];
            corners[0] = position + direction * d - right * width - upAdjusted * height;
            corners[1] = position + direction * d + right * width - upAdjusted * height;
            corners[2] = position + direction * d + right * width + upAdjusted * height;
            corners[3] = position + direction * d - right * width + upAdjusted * height;
        }

        public void DrawDebug(Surface screen, float scale, float x_offset, float y_offset)
        {
            // Recalculate corners before drawing
            CalculateCorners(screen.width / (float)screen.height);

            // Draw lines between camera and corners
            int x0 = screen.XScreen(position.X, scale, x_offset);
            int y0 = screen.YScreen(position.Z, scale, y_offset);

            for (int i = 0; i < corners.Length; i++)
            {
                int x1 = screen.XScreen(corners[i].X, scale, x_offset);
                int y1 = screen.YScreen(corners[i].Z, scale, y_offset);
                screen.Line(x0, y0, x1, y1, 0x00ff00);
                if (i < corners.Length - 1)
                {
                    int x2 = screen.XScreen(corners[i + 1].X, scale, x_offset);
                    int y2 = screen.YScreen(corners[i + 1].Z, scale, y_offset);
                    screen.Line(x1, y1, x2, y2, 0xffaa00);
                }
            }
            // Complete the loop
            screen.Line(screen.XScreen(corners[3].X, scale, x_offset), screen.YScreen(corners[3].Z, scale, y_offset),
            screen.XScreen(corners[0].X, scale, x_offset), screen.YScreen(corners[0].Z, scale, y_offset), 0xffaa00);
        }

        public void RotateYaw(float angle)
        {
            Quaternion rotation = Quaternion.FromAxisAngle(up, MathHelper.DegreesToRadians(angle));
            direction = Vector3.Transform(direction, rotation).Normalized();
        }

        public void RotatePitch(float angle)
        {
            Vector3 right = Vector3.Cross(up, direction).Normalized();
            Quaternion rotation = Quaternion.FromAxisAngle(right, MathHelper.DegreesToRadians(angle));
            direction = Vector3.Transform(direction, rotation).Normalized();
            up = Vector3.Transform(up, rotation).Normalized();
        }
    }


    public class Scene
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

        public Intersection Intersect(Vector3 origin, Vector3 direction, float epsilon = 0.0001f)
        {
            Intersection isect = null;
            // find closest intersection by checking all primitives in the scene and keeping track of the closest one
            float distance = float.MaxValue;
            foreach (Primitive p in primitives)
            {
                Intersection isect2 = p.Intersect(origin, direction);
                // if we hit an object and it is closer than the previous closest object, update the closest object
                if (isect2 != null && isect2.distance < distance && isect2.distance > epsilon)
                {
                    distance = isect2.distance;
                    isect = isect2;
                }
            }
            return isect;
        }
        
        public bool IsInShadow(Vector3 origin, Vector3 direction, float epsilon)
        {
            // check if a point is in shadow by casting a ray from the point to the light source
            foreach (Primitive p in primitives)
            {
                Intersection isect = p.Intersect(origin, direction);
                if (isect != null && isect.distance > epsilon)
                {
                    return true;
                }
            }
            return false;
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
            int bounces = 0;
            int raysPerPixel = 1;
            // pixels can be calculated in parallel
            Parallel.For(0, h, y =>
            {
                Parallel.For(0, w, x =>
                {
                    Color4[] colors = CalculateColors(x, y, bounces, raysPerPixel);
                    Color4 blendedColor = BlendColors(colors);
                    pixels[y * w + x] = GetPixelColor(blendedColor);
                });
            });
        }

        private Color4[] CalculateColors(int x, int y, int bounces, int raysPerPixel)
        {
            int w = screen.width;
            int h = screen.height;
            Color4[] colors = new Color4[(bounces + 1) * raysPerPixel];
            for (int ray = 0; ray < raysPerPixel; ray++)
            {
                Vector3 origin = GetCameraOrigin();
                Vector3 direction = GetCameraDirection(x, y, w, h);

                // // optional code for simple anti-aliasing, works by blurring the image
                // // add a small random offset to the direction to create multiple rays per pixel
                // Random rand = new Random();
                // direction += new Vector3((float)rand.NextDouble() * 0.0025f, (float)rand.NextDouble() * 0.0025f, (float)rand.NextDouble() * 0.0025f);

                for (int i = 0; i < bounces + 1; i++)
                {
                    Intersection isect = scene.Intersect(origin, direction);
                    if (isect != null)
                    {
                        Primitive prim = isect.prim;
                        colors[i * raysPerPixel + ray] = prim.Shade(isect.hitPoint, direction, scene.lights, scene, 0);
                        origin = isect.hitPoint;
                        // direction = prim.Bounce(direction, isect.hitPoint);
                        continue;
                    }
                    colors[i] = new Color4(0, 0, 0, 1);
                    break;
                }
            }
            return colors;
        }

        private Color4 BlendColors(Color4[] colors)
        {
            float r = 0, g = 0, b = 0;
            for (int i = 0; i < colors.Length; i++)
            {
            r += colors[i].R;
            g += colors[i].G;
            b += colors[i].B;
            }
            return new Color4(r / colors.Length, g / colors.Length, b / colors.Length, 1);
        }

        private int GetPixelColor(Color4 color)
        {
            return Primitive.MixColor(color.R, color.G, color.B);
        }

        private Vector3 GetCameraOrigin()
        {
            return new Vector3(camera.position.X, camera.position.Y, camera.position.Z);
        }

        private Vector3 GetCameraDirection(int x, int y, int w, int h)
        {
            // Calculate the aspect ratio of the screen
            float aspectRatio = w / (float)h;

            // Horizontal and vertical angles based on the field of view
            float angleH = MathHelper.DegreesToRadians(camera.fov * aspectRatio);
            float angleV = MathHelper.DegreesToRadians(camera.fov);

            // Calculate the relative screen position from -1 to 1
            float screenX = (x / (float)w) * 2 - 1; // normalize x to range -1 to 1
            float screenY = (y / (float)h) * 2 - 1; // normalize y to range -1 to 1

            // Adjust screen positions based on the aspect ratio
            screenX *= (float)Math.Tan(angleH / 2);
            screenY *= (float)Math.Tan(angleV / 2);

            // Direction vector from the camera to the pixel
            Vector3 rayDirection = camera.direction +
                                   camera.up * screenY +
                                   Vector3.Cross(camera.up, camera.direction).Normalized() * screenX;
            rayDirection.Normalize();
            
            return rayDirection;
        }

        public void Debug()
        {
            int screenCamX = screen.XScreen(camera.position.X, scale, x_offset);
            int screenCamY = screen.YScreen(camera.position.Z, scale, y_offset);
            for (int x = 0; x < screen.width; x++)
            {
                // draw rays from camera to first intersection point
                if (x % 10 == 0)
                {
                    float dx = camera.corners[0].X + (camera.corners[1].X - camera.corners[0].X) * x / screen.width;
                    float dz = camera.corners[0].Z + (camera.corners[1].Z - camera.corners[0].Z) * x / screen.width;
                    int intersectX = screen.XScreen(dx * 20, scale, x_offset);
                    int intersectY = screen.YScreen(dz * 20, scale, y_offset);
                    Vector3 direction = new Vector3(dx, 0, dz);
                    direction.Normalize();
                    Intersection isect = scene.Intersect(camera.position, direction);
                    if (isect != null)
                    {
                        intersectX = screen.XScreen(isect.hitPoint.X, scale, x_offset);
                        intersectY = screen.YScreen(isect.hitPoint.Z, scale, y_offset);
                        // draw rays from hitpoint to next intersection point
                        Vector3 origin = isect.hitPoint;
                        Vector3 direction2 = isect.prim.Bounce(direction, isect.hitPoint);
                        // line in direction of bounce
                        int x2 = screen.XScreen(origin.X + direction2.X * 10, scale, x_offset);
                        int y2 = screen.YScreen(origin.Z + direction2.Z * 10, scale, y_offset);
                        screen.Line(intersectX, intersectY, x2, y2, 0x00ffff);
                    }
                    screen.Line(screenCamX, screenCamY, intersectX, intersectY, 0xff0000);
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
