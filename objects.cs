using OpenTK.Mathematics;
using Template;

namespace Objects
{
    public class Material
    {
        public Color4 diffuseColor;
        public Color4 specularColor;
        public float diffuse;
        public float specular;
        public float reflection;

        public Material(float diffuse, float specular, float reflection)
        {
            this.diffuse = diffuse;
            this.specular = specular;
            this.reflection = reflection;
            specularColor = new Color4(1, 1, 1, 1);
            diffuseColor = new Color4(1, 1, 1, 1);
        }
    }
    public abstract class Primitive
    {
        public Vector3 position;
        public Material material;

        public Primitive()
        {
            material = new Material(1, 0, 0);
        }

        public abstract Intersection Intersect(Vector3 origin, Vector3 direction);
        public abstract Vector3 Bounce(Vector3 direction, Vector3 hitPoint);

        public virtual void DrawDebug(Surface screen, float scale, float x_offset, float y_offset)
        {
            // int x = screen.XScreen(position.X, scale, x_offset);
            // int y = screen.YScreen(position.Z, scale, y_offset);
            // screen.Box(x - 2, y - 2, x + 2, y + 2, MixColor(color.R, color.G, color.B));
        }

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

        public virtual Color4 Shade(Vector3 hitPoint, Vector3 viewDir, List<Light> lights)
        {
            Vector3 normal = GetNormal(hitPoint);
            Vector4 finalColor = new Vector4(0, 0, 0, 1);

            foreach (Light light in lights)
            {
                Vector3 lightDir = (light.position - hitPoint).Normalized();
                float diffuse = Math.Max(Vector3.Dot(normal, lightDir), 0);
                Vector3 reflectDir = Bounce(-lightDir, hitPoint);
                float specular = (float)Math.Pow(Math.Max(Vector3.Dot(reflectDir, viewDir), 0), 32);

                Vector4 lightColor = new Vector4(
                    material.diffuseColor.R * diffuse * material.diffuse + material.specularColor.R * specular * material.specular,
                    material.diffuseColor.G * diffuse * material.diffuse + material.specularColor.G * specular * material.specular,
                    material.diffuseColor.B * diffuse * material.diffuse + material.specularColor.B * specular * material.specular,
                    1);

                finalColor += lightColor;
            }

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

        public override Intersection Intersect(Vector3 origin, Vector3 direction)
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

        public override Vector3 Bounce(Vector3 direction, Vector3 hitPoint)
        {
            return GetNormal(hitPoint);
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

        public override Intersection Intersect(Vector3 origin, Vector3 direction)
        {
            // ray-plane intersection
            float denominator = Vector3.Dot(-normal, direction);
            if (denominator > 0.0001f)
            {
                float t = (d - Vector3.Dot(-normal, origin)) / denominator;
                if (t >= 0)
                {
                    Vector3 hitPoint = new Vector3(origin + direction * t);
                    return new Intersection(this, t, hitPoint);
                }
            }
            return null;
        }

        public override Vector3 Bounce(Vector3 direction, Vector3 hitPoint)
        {
            return direction - 2 * Vector3.Dot(direction, normal) * normal;
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

        public Light(float x, float y, float z)
        {
            position = new Vector3(x, y, z);
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