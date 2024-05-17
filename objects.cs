using OpenTK.Mathematics;
using Template;

namespace Objects
{
    public class Material
    {
        public Color4 diffuseColor;
        public Color4 glossyColor;
        public Color4 ambientColor;
        public float diffuse;
        public float specular;

        public Material(float diffuse)
        {
            this.diffuse = diffuse;
            specular = 0.0f;
            glossyColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
            diffuseColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
            ambientColor = new Color4(0.0f, 0.0f, 0.0f, 1.0f);
        }
    }
    public abstract class Primitive
    {
        public Vector3 position;
        public Material material;

        public Primitive()
        {
            material = new Material(1.0f);
        }
        public abstract Intersection? Intersect(Vector3 origin, Vector3 direction);
        public abstract void DrawDebug(Surface screen, float scale, float x_offset, float y_offset);

        public static int MixColor(float r, float g, float b)
        {
            int red = (int)(r * 255);
            int green = (int)(g * 255);
            int blue = (int)(b * 255);
            return (red << 16) + (green << 8) + blue;
        }

        public void SetColor(float r, float g, float b)
        {
            material.diffuseColor = new Color4(r, g, b, 1);
        }

        public abstract Vector3 GetNormal(Vector3 hitPoint);

        public virtual Color4 Shade(Vector3 hitPoint, Vector3 viewDir, List<Light> lights, Scene scene, int depth)
        {
            Vector3 normal = GetNormal(hitPoint);
            Vector4 finalColor = new Vector4(0, 0, 0, 1);

            foreach (Light light in lights)
            {
                Vector3 shadowRay = (light.position - hitPoint).Normalized();
                // distance to lightsource
                float distance = (light.position - hitPoint).Length;
                float diffuse = Math.Max(Vector3.Dot(normal, shadowRay), 0);
                float glossy = 1.0f;
                // if the shadowRay intersects with any object, then the point is in shadow
                if (scene.IsInShadow(hitPoint, shadowRay, 0.0001f, distance))
                {
                    diffuse = 0;
                    glossy = 0;
                }
                Vector4 diffusecolor = (Vector4)material.diffuseColor * diffuse;

                // reflection
                Vector3 reflection = shadowRay - 2 * Vector3.Dot(shadowRay, normal) * normal;
                float n = 250;
                Vector4 glossyColor = (float)Math.Pow(Math.Max(Vector3.Dot(reflection, viewDir), 0), n) * (Vector4)material.glossyColor * glossy;
                if (material.specular != 0)
                {
                    Vector3 specularDir = (viewDir - 2 * Vector3.Dot(viewDir, normal) * normal).Normalized();
                    Intersection intersection = scene.Intersect(hitPoint, specularDir);
                    if (intersection != null && depth < 5)
                    {
                        var prim = intersection.prim;
                        finalColor += (Vector4)prim.Shade(intersection.hitPoint, specularDir, lights, scene, depth + 1) * material.specular * 1/(distance * distance);
                    }
                }

                finalColor += (Vector4)light.color * 1/(distance * distance) * (diffusecolor + glossyColor) + (Vector4)material.ambientColor;
            }
            finalColor = Vector4.Clamp(finalColor, Vector4.Zero, Vector4.One);
            return (Color4)finalColor;
        }
    }

    public class Sphere : Primitive
    {
        private float radius;

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
            screen.Circle(x, y, r, MixColor(material.diffuseColor.R, material.diffuseColor.G, material.diffuseColor.B));
        }
    }

    public class Plane : Primitive
    {
        private Vector3 normal;
        private float d;

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

        public override void DrawDebug(Surface screen, float scale, float x_offset, float y_offset)
        {
            int x = screen.XScreen(position.X, scale, x_offset);
            int y = screen.YScreen(position.Z, scale, y_offset);
            screen.Line(x - 10, y, x + 10, y, 0xff0000);
            screen.Line(x, y - 10, x, y + 10, 0xff0000);
        }
    }
    public class Light
    {
        public Vector3 position;
        public Color4 color;

        public Light(float x, float y, float z)
        {
            position = new Vector3(x, y, z);
            color = new Color4(2.0f, 2.0f, 2.0f, 1.0f);
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