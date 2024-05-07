using OpenTK.Mathematics;
using OpenTK;
using Template;

namespace Objects
{
    public abstract class Primitive
    {
        public Vector3 position;
        public Color4 color;

        public Primitive()
        {
            color = new Color4(1.0f, 1.0f, 1.0f, 1);
        }

        public abstract Intersection Intersect(float ox, float oy, float oz, float dx, float dy, float dz);

        public virtual void DrawDebug(Surface screen, float scale, float x_offset, float y_offset)
        {
            int x = screen.XScreen(position.X, scale, x_offset);
            int y = screen.YScreen(position.Z, scale, y_offset);
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

    public class Sphere : Primitive
    {
        private float radius;

        public Sphere(float x, float y, float z, float radius)
        {
            position = new Vector3(x, y, z);
            this.radius = radius;
        }

        public override Intersection Intersect(float ox, float oy, float oz, float dx, float dy, float dz)
        {
            // ray-sphere intersection
            float a = Vector3.Dot(new Vector3(dx, dy, dz), new Vector3(dx, dy, dz));
            float b = 2 * (dx * (ox - position.X) + dy * (oy - position.Y) + dz * (oz - position.Z));
            float c = position.X * position.X + position.Y * position.Y + position.Z * position.Z + ox * ox + oy * oy + oz * oz - 2 * (position.X * ox + position.Y * oy + position.Z * oz) - radius * radius;

            // The discriminant < 0 means no intersection, = 0 means one intersection (ray touches the sphere), > 0 means two intersections (ray goes through sphere)
            float discriminant = b * b - 4 * a * c;

            if (discriminant < 0) return null;
            // Calculate the distance to the intersection point
            float distance = (-b - (float)System.Math.Sqrt(discriminant)) / (2 * a);
            // Calculate the intersection point
            Vector3 hitPoint = new Vector3(ox + distance * dx, oy + distance * dy, oz + distance * dz);

            return new Intersection(this, distance, hitPoint);
        }

        public override void DrawDebug(Surface screen, float scale, float x_offset, float y_offset)
        {
            int x = screen.XScreen(position.X, scale, x_offset);
            int y = screen.YScreen(position.Z, scale, y_offset);
            int r = (int)(radius * screen.width / scale);
            screen.Circle(x, y, r, MixColor(color.R, color.G, color.B));
        }
    }

    public class Plane : Primitive
    {
        private Vector3 normal;
        private float d;

        public Plane(float nx, float ny, float nz, float d)
        {
            normal = new Vector3(nx, ny, nz);
            this.d = d;
        }

        public override Intersection Intersect(float ox, float oy, float oz, float dx, float dy, float dz)
        {
            // TODO: implement intersection
            return null;
        }
    }
    public class Light
    {
        public Vector3 position;
        private Color4 color;

        public Light(float x, float y, float z, float r, float g, float b)
        {
            position = new Vector3(x, y, z);
            color = new Color4(r, g, b, 1);
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