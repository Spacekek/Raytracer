// Only used in Modern OpenGL

#version 330

// shader input
in vec2 uv;			        // interpolated texture coordinates
in vec3 rayDirection;		// interpolated ray direction
uniform vec3 cameraPosition = vec3(0.0, 0.0, 0.0); // camera position
uniform sampler2D pixels;   // texture sampler
uniform vec3 cameraDirection;
uniform vec3 cameraUp;
uniform float fov;
uniform vec3 corners[4];
uniform int NUM_LIGHTS = 2;
uniform int NUM_PRIM = 10;
uniform float gpu;

// shader output
out vec4 outputColor;

// fragment shader
struct Material {
    vec4 diffuseColor;
    vec4 glossyColor;
    vec4 ambientColor;
    float specular;
};

struct Primitive {
    int type; // 0 = sphere, 1 = plane
    vec3 position;
    vec3 normal; // only used for planes
    Material material;
    float radius; // only used for spheres
    float d; // only used for planes
};

struct Light {
    vec3 position;
    vec4 color;
};

struct Intersection {
    bool hit;
    Primitive prim;
    vec3 hitPoint;
    float dist;
};

vec3 GetRayDirection(int x, int y, int w, int h) {
	// Calculate the aspect ratio of the screen
	float aspectRatio = float(w) / float(h);

	// Horizontal and vertical angles based on the field of view
	float angleH = radians(fov * aspectRatio);
	float angleV = radians(fov);

	// Calculate the relative screen position from -1 to 1
	float screenX = float(x) / float(w) * 2.0 - 1.0; // normalize x to range -1 to 1
	float screenY = float(y) / float(h) * 2.0 - 1.0; // normalize y to range -1 to 1

	// Adjust screen positions based on the aspect ratio
	screenX *= tan(angleH / 2.0);
	screenY *= tan(angleV / 2.0);

	// Direction vector from the camera to the pixel
	vec3 rayDirection = cameraDirection +
						cameraUp * screenY +
						cross(cameraUp, cameraDirection) * screenX;
	rayDirection = normalize(rayDirection);

	return rayDirection;
}

Intersection SphereIntersect(vec3 orig, vec3 dir, Primitive prim) {
    // ray-sphere intersection
    float a = dot(dir, dir);
    float b = 2.0 * dot(dir, orig - prim.position);
    float c = dot(orig - prim.position, orig - prim.position) - prim.radius * prim.radius;

    // The discriminant < 0 means no intersection, = 0 means one intersection (ray touches the sphere), > 0 means two intersections (ray goes through sphere)
    float discriminant = b * b - 4.0 * a * c;

    if (discriminant < 0.0) return Intersection(false, prim, vec3(0.0), 0);
    // Calculate the distance to the intersection point
    float dist = (-b - sqrt(discriminant)) / (2.0 * a);
    // Calculate the intersection point
    vec3 hitPoint = orig + dir * dist;

    return Intersection(true, prim, hitPoint, dist);
}

Intersection PlaneIntersect(vec3 origin, vec3 direction, Primitive prim) {
    float denom = dot(prim.normal, direction);
    if (abs(denom) <= 0.0001)
        return Intersection(false, prim, vec3(0.0), 0);

    float t = (-dot(prim.normal, origin) - prim.d) / denom;

    if (t <= 0.0001)
        return Intersection(false, prim, vec3(0.0), 0);

    vec3 hitPoint = origin + direction * t;

    return Intersection(true, prim, hitPoint, t);
}

bool IsInShadow(vec3 origin, vec3 direction, Primitive[10] primitives, float epsilon, float dist) {

    // check if a point is in shadow by casting a ray from the point to the light source
    // foreach (Primitive p in primitives)
    // {
    //     Intersection? isect = p.Intersect(origin, direction);
    //     if (isect != null && isect.distance > epsilon && isect.distance < distance)
    //         return true;
    // }
    // return false;

    for (int i = 0; i < NUM_PRIM; i++) {
        Primitive prim = primitives[i];
        if (prim.type == 0) { // sphere
            Intersection intersect = SphereIntersect(origin, direction, prim);
            if (intersect.hit && intersect.dist > epsilon && intersect.dist < dist) {
                return true;
            }
        }
        if (prim.type == 1) { // plane
            Intersection intersect = PlaneIntersect(origin, direction, prim);
            if (intersect.hit && intersect.dist > epsilon && intersect.dist < dist) {
                return true;
            }
        }
    }
    return false;
}

Intersection sceneIntersect(vec3 orig, vec3 dir, Primitive[10] primitives, float epsilon) {
    float closest = 1000000.0;
    Primitive closestPrim;
    Intersection intersection = Intersection(false, closestPrim, vec3(0.0), 0);
    for (int i = 0; i < NUM_PRIM; i++) {
        Primitive prim = primitives[i];
        Intersection intersect;
        if (prim.type == 0) { // sphere
            intersect = SphereIntersect(orig, dir, prim);
        }
        if (prim.type == 1) { // plane
            intersect = PlaneIntersect(orig, dir, prim);
        }
        if (intersect.hit && length(intersect.hitPoint - orig) < closest && length(intersect.hitPoint - orig) > epsilon) {
            closest = length(intersect.hitPoint - orig);
            intersection = intersect;
        }
    }
    return intersection;
}

vec4 CalculateLighting(vec3 hitPoint, vec3 viewDir, vec3 normal, Material material, Primitive[10] primitives, Light[10] lights, float epsilon) {
    vec4 finalColor = vec4(0.0);
    for (int i = 0; i < NUM_LIGHTS; i++) {
        vec3 shadowRay = normalize(lights[i].position - hitPoint);
        float dist = length(lights[i].position - hitPoint);
        float diffuse = max(dot(normal, shadowRay), 0.0);
        float glossy = 1.0;

        if (IsInShadow(hitPoint, shadowRay, primitives, epsilon, dist)) {
            diffuse = 0.0;
            glossy = 0.0;
        }

        vec4 diffuseColor = material.diffuseColor * diffuse;

        vec3 reflection = shadowRay - 2.0 * dot(shadowRay, normal) * normal;
        float n = 250.0;
        vec4 glossyColor = pow(max(dot(reflection, viewDir), 0.0), n) * material.glossyColor * glossy;

        finalColor += lights[i].color * 1.0 / (dist * dist) * (diffuseColor + glossyColor) + material.ambientColor;
    }
    return finalColor;
}


vec4 SpecularColor(float specular, int maxDepth, vec3 viewDir, vec3 normal, vec3 hitPoint, Primitive[10] primitives, Light[10] lights, float epsilon)
{
    vec4 finalColor = vec4(0.0);
    float prevSpecular = specular;
    int depth = 0;
    while (depth < maxDepth) {
        vec3 specularDir = normalize(viewDir - 2.0 * dot(viewDir, normal) * normal);
        Intersection intersection = sceneIntersect(hitPoint, specularDir, primitives, epsilon);
        if (!intersection.hit) { break; }
        Primitive prim = intersection.prim;
        hitPoint = intersection.hitPoint;
        Material material = prim.material;
        if (prim.type == 0) { // sphere
            normal = normalize(hitPoint - prim.position);
        }
        if (prim.type == 1) { // plane
            normal = prim.normal;
        }
        finalColor += CalculateLighting(hitPoint, specularDir, normal, material, primitives, lights, epsilon) * prevSpecular;
        prevSpecular = material.specular;
        if (prevSpecular == 0.0) { break; }
        viewDir = specularDir;

        depth++;
    }
    return finalColor * specular;
}

vec4 Shade(Primitive prim, vec3 hitPoint, vec3 viewDir, Light[10] lights, Primitive[10] primitives, int maxDepth) {
    float epsilon = 0.0001;

    vec3 normal = vec3(0.0);
    Material material = prim.material;
    if (prim.type == 0) { // sphere
        normal = normalize(hitPoint - prim.position);
    }
    if (prim.type == 1) { // plane
        normal = prim.normal;
    }
    vec4 finalColor = vec4(0.0);
    finalColor += CalculateLighting(hitPoint, viewDir, normal, material, primitives, lights, epsilon);
    finalColor += SpecularColor(material.specular, maxDepth, viewDir, normal, hitPoint, primitives, lights, epsilon);

    return finalColor;
}

vec3 PlanePos(vec3 normal, float d) {
    vec3 position = vec3(d/ normal.x, d / normal.y, d / normal.z);
    return position;
}


void main()
{
    if (gpu < 0) 
    {
        outputColor = texture(pixels, uv);
        return;
    }
    Material cyan = Material(vec4(0.75, 0.95, 1.0, 1.0), vec4(1.0, 1.0, 1.0, 1.0), vec4(0.02, 0.05, 0.1, 1.0), 0.0);
    Material green = Material(vec4(0.8, 1.0, 0.8, 1.0), vec4(1.0, 1.0, 1.0, 1.0), vec4(0.06, 0.08, 0.06, 1.0), 0.0);
    Material mirror = Material(vec4(0.0, 0.0, 0.0, 1.0), vec4(1.0, 1.0, 1.0, 1.0), vec4(0.0, 0.0, 0.0, 1.0), 1.0);
    Material orange = Material(vec4(1.0, 0.85, 0.7, 1.0), vec4(1.0, 1.0, 1.0, 1.0), vec4(0.07, 0.05, 0.05, 1.0), 0.2);
    Material pink = Material(vec4(1.0, 0.8, 0.95, 1.0), vec4(1.0, 1.0, 1.0, 1.0), vec4(0.07, 0.05, 0.05, 1.0), 0.0);

    Primitive[10] primitives;
    // spheres
    primitives[0] = Primitive(0, vec3(0.0, 0.0, 2.0), vec3(0.0), mirror, 0.5, 0.0);
    primitives[1] = Primitive(0, vec3(0.5, 0.6, 2.2), vec3(0.0), green, 0.3, 0.0);
    // planes
    primitives[2] = Primitive(1, PlanePos(vec3(0.0, 0.0, -1.0), 3.5), normalize(vec3(0.0, 0.0, -1.0)), cyan, 0.0, 3.5);
    primitives[3] = Primitive(1, PlanePos(vec3(0.0, 0.0, 1.0), 2.0), normalize(vec3(0.0, 0.0, 1.0)), cyan, 0.0, 2.0);
    primitives[4] = Primitive(1, PlanePos(vec3(0.0, -1.0, 1.0), 1.0), normalize(vec3(0.0, -1.0, 0.0)), orange, 0.0, 1.0);
    primitives[5] = Primitive(1, PlanePos(vec3(0.0, 1.0, 0.0), 4.0), normalize(vec3(0.0, 1.0, 0.0)), orange, 0.0, 4.0);
    primitives[6] = Primitive(1, PlanePos(vec3(1.0, 0.0, 0.0), 4.0), normalize(vec3(1.0, 0.0, 0.0)), green, 0.0, 4.0);
    primitives[7] = Primitive(1, PlanePos(vec3(-1.0, 0.0, 0.0), 4.0), normalize(vec3(-1.0, 0.0, 0.0)), pink, 0.0, 4.0);

    Light[10] lights;
    lights[0] = Light(vec3(1.0, -1.0, 1.5), vec4(2.0, 2.0, 2.0, 1.0));
    lights[1] = Light(vec3(-1.0, -1.0, 0.5), vec4(2.0, 2.0, 2.0, 1.0));

    vec3 orig = cameraPosition;
    vec3 dir = GetRayDirection(int(uv.x * 640.0), int(uv.y * 640.0), 640, 640);
    Intersection intersection = sceneIntersect(orig, dir, primitives, 0.0001);
    if (intersection.hit) {
        outputColor = Shade(intersection.prim, intersection.hitPoint, dir, lights, primitives, 5);
    }
    else {
        outputColor = vec4(0.0);
    }
}