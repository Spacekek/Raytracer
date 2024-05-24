using System.Security.Cryptography.X509Certificates;
using OpenTK.Mathematics;
using Template;

namespace Objects
{
    public class Material
    {
        public Vector4 diffuseColor;
        public Vector4 glossyColor;
        public Vector4 ambientColor;
        public float diffuse;
        public float specular;
        public bool useCheckerboard;

        public Material(float diffuse)
        {
            this.diffuse = diffuse;
            specular = 0.0f;
            glossyColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            diffuseColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            ambientColor = diffuseColor * 0.1f * diffuse;
            useCheckerboard = false;
        }
    }
    public abstract class Primitive
    {
        public Vector3 position;
        public Material material = new Material(1.0f);
        public abstract Intersection? Intersect(Vector3 origin, Vector3 direction);
        public abstract void DrawDebug(Surface screen, float scale, float x_offset, float y_offset);

        public static int MixColor(Vector4 color)
        {
            int red = (int)(color.X * 255);
            int green = (int)(color.Y * 255);
            int blue = (int)(color.Z * 255);
            return (red << 16) + (green << 8) + blue;
        }
        public abstract Vector3 GetNormal(Vector3 hitPoint);

        public Color4 Shade(Vector3 hitPoint, Vector3 viewDir, List<Light> lights, Scene scene, int depth)
        {
            Vector3 normal = GetNormal(hitPoint);
            Vector4 finalColor = Vector4.Zero;

            foreach (Light light in lights)
            {
                Vector3 shadowRay = (light.position - hitPoint).Normalized();
                float distance = (light.position - hitPoint).Length;
                float diffuse = Math.Max(Vector3.Dot(normal, shadowRay), 0);
                float glossy = 1.0f;

                if (scene.IsInShadow(hitPoint, shadowRay, 0.0001f, distance))
                {
                    diffuse = 0;
                    glossy = 0;
                }

                Vector4 diffuseColor = material.diffuseColor * diffuse;

                // Use checkerboard color if the material's flag is true
                if (material.useCheckerboard && this is Plane plane)
                {
                    diffuseColor = plane.GetCheckerboardColor(hitPoint) * diffuse;
                }

                Vector3 reflection = shadowRay - 2 * Vector3.Dot(shadowRay, normal) * normal;
                float n = 250;
                Vector4 glossyColor = (float)Math.Pow(Math.Max(Vector3.Dot(reflection, viewDir), 0), n) * material.glossyColor * glossy;

                if (material.specular != 0)
                {
                    Vector3 specularDir = (viewDir - 2 * Vector3.Dot(viewDir, normal) * normal).Normalized();
                    Intersection? intersection = scene.Intersect(hitPoint, specularDir);
                    if (intersection != null && depth < 5)
                    {
                    Primitive prim = intersection.prim;
                    finalColor += (Vector4)prim.Shade(intersection.hitPoint, specularDir, lights, scene, depth + 1) * material.specular * 1 / (distance * distance);
                    }
                }

                finalColor += light.color * 1 / (distance * distance) * (diffuseColor + glossyColor) + material.ambientColor;
            }

            finalColor = Vector4.Clamp(finalColor, Vector4.Zero, Vector4.One);
            return (Color4)finalColor;
        }
    }

    public class Sphere : Primitive
    {
        public float radius;

        public Sphere(float x, float y, float z, float radius)
        {
            position = new Vector3(x, y, z);
            this.radius = radius;
        }

        public override Intersection? Intersect(Vector3 origin, Vector3 direction)
        {
            // ray-sphere intersection
            float a = Vector3.Dot(direction, direction);
            float b = 2 * Vector3.Dot(direction, origin - position);
            float c = Vector3.Dot(origin - position, origin - position) - radius * radius;

            // The discriminant < 0 means no intersection, = 0 means one intersection (ray touches the sphere), > 0 means two intersections (ray goes through sphere)
            float discriminant = b * b - 4 * a * c;

            if (discriminant < 0) return null;
            // Calculate the distance to the intersection point
            float distance = (-b - (float)Math.Sqrt(discriminant)) / (2 * a);
            // Calculate the intersection point
            Vector3 hitPoint = new Vector3(origin + direction * distance);

            return new Intersection(this, distance, hitPoint);
        }

        public override Vector3 GetNormal(Vector3 hitPoint)
        {
            return (hitPoint - position).Normalized();
        }

        public override void DrawDebug(Surface screen, float scale, float x_offset, float y_offset)
        {
            int x = screen.XScreen(position.X, scale, x_offset);
            int y = screen.YScreen(position.Z, scale, y_offset);
            int r = (int)(radius * screen.width / scale);
            screen.Circle(x, y, r, MixColor(material.diffuseColor));
        }
    }

    public class Plane : Primitive
    {
        private Vector3 normal;
        public float d;

        public Plane(float nx, float ny, float nz, float d)
        {
            normal = new Vector3(nx, ny, nz).Normalized();
            this.d = d;
            position = new Vector3(d / normal.X, d / normal.Y, d / normal.Z);
        }

        public override Intersection? Intersect(Vector3 origin, Vector3 direction)
        {
            float denom = Vector3.Dot(normal, direction);
            if (Math.Abs(denom) <= 0.0001f)
                return null;
            
            float t = (-Vector3.Dot(normal, origin) - d) / denom;

            if (t <= 0.0001f)
                return null;

            Vector3 hitPoint = origin + direction * t;

            return new Intersection(this, t, hitPoint);
        }

        public override Vector3 GetNormal(Vector3 hitPoint)
        {
            return normal;
        }

        public Vector4 GetCheckerboardColor(Vector3 hitPoint)
        {
            float scale = 1.0f; // Size of the checkerboard squares
            int x = (int)Math.Floor(hitPoint.X / scale);
            int z = (int)Math.Floor(hitPoint.Z / scale);

            if ((x + z) % 2 == 0)
                return new Vector4(1.0f, 1.0f, 1.0f, 1.0f); // White color
            else
                return new Vector4(0.0f, 0.0f, 0.0f, 1.0f); // Black color
        }

        public override void DrawDebug(Surface screen, float scale, float x_offset, float y_offset)
        {
            int x = screen.XScreen(position.X, scale, x_offset);
            int y = screen.YScreen(position.Z, scale, y_offset);
            screen.Line(x - 10, y, x + 10, y, 0xff0000);
            screen.Line(x, y - 10, x, y + 10, 0xff0000);
        }
    }
    public class Triangle : Primitive
    {
        public Vector3 v0, v1, v2;
        public Vector3 normal;

        public Triangle(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            this.v0 = v0;
            this.v1 = v1;
            this.v2 = v2;
            normal = Vector3.Cross(v1 - v0, v2 - v0).Normalized();
        }

        public override Intersection? Intersect(Vector3 origin, Vector3 direction)
        {
            Vector3 e1 = v1 - v0;
            Vector3 e2 = v2 - v0;
            Vector3 h = Vector3.Cross(direction, e2);
            float a = Vector3.Dot(e1, h);

            if (a > -0.0001f && a < 0.0001f)
                return null;

            float f = 1 / a;
            Vector3 s = origin - v0;
            float u = f * Vector3.Dot(s, h);

            if (u < 0 || u > 1)
                return null;

            Vector3 q = Vector3.Cross(s, e1);
            float v = f * Vector3.Dot(direction, q);

            if (v < 0 || u + v > 1)
                return null;

            float t = f * Vector3.Dot(e2, q);

            if (t > 0.0001f)
            {
                Vector3 hitPoint = origin + direction * t;
                return new Intersection(this, t, hitPoint);
            }

            return null;
        }

        public override Vector3 GetNormal(Vector3 hitPoint)
        {
            return normal;
        }

        public override void DrawDebug(Surface screen, float scale, float x_offset, float y_offset)
        {
            int x0 = screen.XScreen(v0.X, scale, x_offset);
            int y0 = screen.YScreen(v0.Z, scale, y_offset);
            int x1 = screen.XScreen(v1.X, scale, x_offset);
            int y1 = screen.YScreen(v1.Z, scale, y_offset);
            int x2 = screen.XScreen(v2.X, scale, x_offset);
            int y2 = screen.YScreen(v2.Z, scale, y_offset);
            screen.Line(x0, y0, x1, y1, 0xff0000);
            screen.Line(x1, y1, x2, y2, 0xff0000);
            screen.Line(x2, y2, x0, y0, 0xff0000);
        }
    }
    public class Light
    {
        public Vector3 position;
        public Vector4 color;

        public Light(float x, float y, float z)
        {
            position = new Vector3(x, y, z);
            color = new Vector4(2.0f, 2.0f, 2.0f, 1.0f);
        }
        public void DrawDebug(Surface screen, float scale, float x_offset, float y_offset)
        {
            int x = screen.XScreen(position.X, scale, x_offset);
            int y = screen.YScreen(position.Z, scale, y_offset);
            screen.Circle(x, y, 5, 0xffffff);
        }
    }

    public class Intersection
    {
        public Primitive prim;
        public float distance;
        public Vector3 hitPoint;

        public Intersection(Primitive prim, float distance, Vector3 hitPoint)
        {
            this.prim = prim;
            this.distance = distance;
            this.hitPoint = hitPoint;
        }
    }
}