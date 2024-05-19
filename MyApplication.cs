using OpenTK.Mathematics;
using Objects;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Template
{
    class MyApplication
    {
        public Surface screen;
        public Camera camera;
        private Scene scene;
        private Raytracer raytracer;
        private bool debug;
        public float gpu;

        public MyApplication(Surface screen)
        {
            // initialize screen, camera, scene and raytracer
            this.screen = screen;
            gpu = -1.0f;
            camera = new Camera((float)screen.width / screen.height);
            scene = new Scene();
            raytracer = new Raytracer(scene, camera, screen);
            debug = false;
        }

        public void Init()
        {
            Material cyan = new Material(1.0f)
            {
                diffuseColor = new Vector4(0.75f, 0.95f, 1.0f, 1.0f),
                ambientColor = new Vector4(0.02f, 0.05f, 0.1f, 1.0f)
            };
            Material green = new Material(1.0f)
            {
                diffuseColor = new Vector4(0.8f, 1.0f, 0.8f, 1.0f),
                ambientColor = new Vector4(0.06f, 0.08f, 0.06f, 1.0f)
            };
            Material mirror = new Material(0.0f)
            {
                specular = 1.0f,
                glossyColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
                ambientColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f),
                diffuseColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f)
            };
            Material orange = new Material(1.0f)
            {
                diffuseColor = new Vector4(1.0f, 0.85f, 0.7f, 1.0f),
                ambientColor = new Vector4(0.07f, 0.05f, 0.05f, 1.0f),
                specular = 0.5f
            };
            Material pink = new Material(1.0f)
            {
                diffuseColor = new Vector4(1.0f, 0.8f, 0.95f, 1.0f),
                ambientColor = new Vector4(0.07f, 0.05f, 0.05f, 1.0f),
            };

            // add scene objects and lights
            scene.Add(new Sphere(0.0f, 0.0f, 2f, 0.5f) { material = mirror });
            scene.Add(new Sphere(0.5f, 0.6f, 2.2f, 0.3f) { material = green });

            // scene.Add(new Triangle(new Vector3(-2.0f, 0.0f, 1.0f), new Vector3(-1.5f, 0.0f, 2.0f), new Vector3(-2.0f, -1.5f, 2.0f)) { material = cyan });

            scene.Add(new Plane(0.0f, 0.0f, -1.0f, 3.5f) { material = cyan });
            scene.Add(new Plane(0.0f, 0.0f, 1.0f, 2.0f) { material = cyan });
            scene.Add(new Plane(0.0f, -1.0f, 0.0f, 1.0f) { material = orange });
            scene.Add(new Plane(0.0f, 1.0f, 0.0f, 4.0f) { material = orange });
            scene.Add(new Plane(1.0f, 0.0f, 0.0f, 4.0f) { material = green });
            scene.Add(new Plane(-1.0f, 0.0f, 0.0f, 4.0f) { material = pink });

            scene.Add(new Light(1.0f, -1.0f, 1.5f));
            scene.Add(new Light(-1.0f, -1.0f, 0.5f));
        }

        public void Tick()
        {
            // every frame, clear the screen, render the scene and draw debug view
            screen.Clear(0);
            if (gpu > 0.0)
                return;
            else
            {
                if (debug)
                    raytracer.Debug();
                else
                    raytracer.Render();
            }
        }
        public void UpdateKeyboard(KeyboardState state)
        {
            // toggle debug view on key press
            debug = state.IsKeyDown(Keys.Space) ? !debug : debug;

            // toggle gpu
            gpu = state.IsKeyDown(Keys.G) ? gpu * -1 : gpu;

            // Move forward/backward
            float moveSpeed = 0.1f;
            camera.position += state.IsKeyDown(Keys.W) ? camera.direction * moveSpeed : Vector3.Zero;
            camera.position -= state.IsKeyDown(Keys.S) ? camera.direction * moveSpeed : Vector3.Zero;

            // Move left/right
            Vector3 left = Vector3.Cross(camera.up, camera.direction).Normalized();
            camera.position -= state.IsKeyDown(Keys.A) ? left * moveSpeed : Vector3.Zero;
            camera.position += state.IsKeyDown(Keys.D) ? left * moveSpeed : Vector3.Zero;

            // Rotate left/right
            float rotateSpeed = 5.0f;
            camera.RotateYaw(state.IsKeyDown(Keys.Left) ? -rotateSpeed : 0.0f);
            camera.RotateYaw(state.IsKeyDown(Keys.Right) ? rotateSpeed : 0.0f);

            // Look up/down
            camera.RotatePitch(state.IsKeyDown(Keys.Up) ? rotateSpeed : 0.0f);
            camera.RotatePitch(state.IsKeyDown(Keys.Down) ? -rotateSpeed : 0.0f);
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
        public void Add(Primitive p){primitives.Add(p);}
        public void Add(Light l){lights.Add(l);}

        public Intersection? Intersect(Vector3 origin, Vector3 direction, float epsilon = 0.0001f)
        {
            Intersection? isect = null;
            // find closest intersection by checking all primitives in the scene and keeping track of the closest one
            float distance = float.MaxValue;
            foreach (Primitive p in primitives)
            {
                Intersection? isect2 = p.Intersect(origin, direction);
                // if we hit an object and it is closer than the previous closest object, update the closest object
                if (isect2 != null && isect2.distance < distance && isect2.distance > epsilon)
                {
                    distance = isect2.distance;
                    isect = isect2;
                }
            }
            return isect;
        }
        
        public bool IsInShadow(Vector3 origin, Vector3 direction, float epsilon, float distance)
        {
            // check if a point is in shadow by casting a ray from the point to the light source
            foreach (Primitive p in primitives)
            {
                Intersection? isect = p.Intersect(origin, direction);
                if (isect != null && isect.distance > epsilon && isect.distance < distance)
                    return true;
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
            // pixels can be calculated in parallel
            Parallel.For(0, h, y =>
            {
                Parallel.For(0, w, x =>
                {
                    Vector3 origin = camera.position;
                    Vector3 direction = GetRayDirection(x, y, w, h);

                    Intersection? isect = scene.Intersect(origin, direction);
                    if (isect != null)
                    {
                        Primitive prim = isect.prim;
                        Color4 color = prim.Shade(isect.hitPoint, direction, scene.lights, scene, 0);
                        pixels[y * w + x] = Primitive.MixColor((Vector4)color);
                    }
                    else
                        pixels[y * w + x] = 0;
                });
            });
        }

        private Vector3 GetRayDirection(int x, int y, int w, int h)
        {
            // Calculate the aspect ratio of the screen
            float aspectRatio = w / (float)h;

            // Horizontal and vertical angles based on the field of view
            float angleH = MathHelper.DegreesToRadians(camera.fov * aspectRatio);
            float angleV = MathHelper.DegreesToRadians(camera.fov);

            // Calculate the relative screen position from -1 to 1
            float screenX = x / (float)w * 2 - 1; // normalize x to range -1 to 1
            float screenY = y / (float)h * 2 - 1; // normalize y to range -1 to 1

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

            for (int x = 0; x < screen.width; x += 10)
            {
                Vector3 direction = GetRayDirection(x, 320, screen.width, screen.height);
                int intersectX = screen.XScreen(camera.position.X + direction.X * 100, scale, x_offset);
                int intersectY = screen.YScreen(camera.position.Z + direction.Z * 100, scale, y_offset);
                Intersection? isect = scene.Intersect(camera.position, direction);

                if (isect != null)
                {
                    intersectX = screen.XScreen(isect.hitPoint.X, scale, x_offset);
                    intersectY = screen.YScreen(isect.hitPoint.Z, scale, y_offset);
                    Vector3 origin = isect.hitPoint;
                    Vector3 normal = isect.prim.GetNormal(isect.hitPoint);
                    Vector3 direction2 = direction - 2 * Vector3.Dot(direction, normal) * normal;
                    int x2 = screen.XScreen(origin.X + direction2.X * 10, scale, x_offset);
                    int y2 = screen.YScreen(origin.Z + direction2.Z * 10, scale, y_offset);
                    screen.Line(intersectX, intersectY, x2, y2, 0x00ffff);
                }
                screen.Line(screenCamX, screenCamY, intersectX, intersectY, 0xff0000);
            }

            camera.DrawDebug(screen, scale, x_offset, y_offset);

            foreach (Light l in scene.lights)
                l.DrawDebug(screen, scale, x_offset, y_offset);
            foreach (Primitive p in scene.primitives)
                p.DrawDebug(screen, scale, x_offset, y_offset);
        }
    }
}