using OpenTK.Mathematics;
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
            // TODO implement intersection
            return null;
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

        public Intersection(Primitive prim, float t, Vector3 hitPoint)
        {
            this.prim = prim;
            this.distance = t;
            this.hitPoint = hitPoint;
        }
    }
}