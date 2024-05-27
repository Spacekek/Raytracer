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
uniform int NUM_LIGHTS;
uniform int NUM_PRIMS;
uniform float gpu;
uniform int maxDepth;
uniform vec3[10] lightPositions;
uniform vec4[10] lightColors;
uniform vec3[10] primPositions;
uniform vec3[10] primNormals;
uniform vec4[10] primDiffuseColors;
uniform vec4[10] primGlossyColors;
uniform vec4[10] primAmbientColors;
uniform float[10] primSpecular;
uniform float[10] primRadius;
uniform float[10] primD;
uniform vec3[10] primV0;
uniform vec3[10] primV1;
uniform vec3[10] primV2;
uniform int[10] primCheckerboard;
uniform int[10] primTypes;
uniform int width;
uniform int height;


// shader output
out vec4 outputColor;

// fragment shader
struct Material {
    vec4 diffuseColor;
    vec4 glossyColor;
    vec4 ambientColor;
    float specular;
    int checkerboard;
};

struct Primitive {
    int type; // 0 = sphere, 1 = plane, 2 = triangle
    vec3 position;
    vec3 normal; // only used for planes
    Material material;
    float radius; // only used for spheres
    float d; // only used for planes
    vec3 v0, v1, v2; // only used for triangles
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
    // Ray-sphere intersection
    float a = dot(dir, dir);
    float b = 2.0 * dot(dir, orig - prim.position);
    float c = dot(orig - prim.position, orig - prim.position) - prim.radius * prim.radius;

    // The discriminant < 0 means no intersection, = 0 means one intersection (ray touches the sphere), > 0 means two intersections (ray goes through sphere)
    float discriminant = b * b - 4.0 * a * c;

    if (discriminant < 0.0) return Intersection(false, prim, vec3(0.0), 0);

    // Calculate both distances to the intersection points
    float sqrtDiscriminant = sqrt(discriminant);
    float t1 = (-b - sqrtDiscriminant) / (2.0 * a);
    float t2 = (-b + sqrtDiscriminant) / (2.0 * a);

    // Find the nearest positive intersection distance
    float dist = (t1 > 0.0) ? t1 : ((t2 > 0.0) ? t2 : -1.0);

    if (dist < 0.0) return Intersection(false, prim, vec3(0.0), 0);

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

Intersection TriangleIntersect(vec3 orig, vec3 dir, Primitive prim) {
    vec3 e1 = prim.v1 - prim.v0;
    vec3 e2 = prim.v2 - prim.v0;
    vec3 h = cross(dir, e2);
    float a = dot(e1, h);
    if (a > -0.0001f && a < 0.0001f)
        return Intersection(false, prim, vec3(0.0), 0.0);

    float f = 1.0 / a;
    vec3 s = orig - prim.v0;
    float u = f * dot(s, h);
    if (u < 0.0 || u > 1.0)
        return Intersection(false, prim, vec3(0.0), 0.0);

    vec3 q = cross(s, e1);
    float v = f * dot(dir, q);
    if (v < 0.0 || u + v > 1.0)
        return Intersection(false, prim, vec3(0.0), 0.0);

    float t = f * dot(e2, q);
    if (t > 0.0001f) {
        vec3 hitPoint = orig + dir * t;
        return Intersection(true, prim, hitPoint, t);
    }

    return Intersection(false, prim, vec3(0.0), 0.0);
}


bool IsInShadow(vec3 origin, vec3 direction, Primitive[10] primitives, float epsilon, float dist) {
    for (int i = 0; i < NUM_PRIMS; i++) {
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
        if (prim.type == 2) { // triangle
            Intersection intersect = TriangleIntersect(origin, direction, prim);
            if (intersect.hit && intersect.dist > epsilon && intersect.dist < dist) {
                return true;
            }
        }
    }
    return false;
}

Intersection SceneIntersect(vec3 orig, vec3 dir, Primitive[10] primitives, float epsilon) {
    float closest = 1000000.0;
    Primitive closestPrim;
    Intersection intersection = Intersection(false, closestPrim, vec3(0.0), 0);
    for (int i = 0; i < NUM_PRIMS; i++) {
        Primitive prim = primitives[i];
        Intersection intersect;
        if (prim.type == 0) { // sphere
            intersect = SphereIntersect(orig, dir, prim);
        }
        if (prim.type == 1) { // plane
            intersect = PlaneIntersect(orig, dir, prim);
        }
        if (prim.type == 2) { // triangle
            intersect = TriangleIntersect(orig, dir, prim);
        }
        if (intersect.hit && length(intersect.hitPoint - orig) < closest && length(intersect.hitPoint - orig) > epsilon) {
            closest = length(intersect.hitPoint - orig);
            intersection = intersect;
        }
    }
    return intersection;
}

vec4 CalculateLighting(vec3 hitPoint, vec3 viewDir, vec3 normal, Material material, Primitive[10] primitives, Light light, float epsilon) {
    vec4 finalColor = vec4(0.0);
    vec3 shadowRay = normalize(light.position - hitPoint);
    float dist = length(light.position - hitPoint);
    float diffuse = max(dot(normal, shadowRay), 0.0);
    float glossy = 1.0; 

    if (IsInShadow(hitPoint, shadowRay, primitives, epsilon, dist)) {
        diffuse = 0.0;
        glossy = 0.0;
    }

    vec4 diffuseColor = material.diffuseColor * diffuse;

    if (material.checkerboard == 1) {
        float scale = 7.5;
        float sines = sin(hitPoint.x * scale) * sin(hitPoint.y * scale) * sin(hitPoint.z * scale);
        if (sines < 0.0) {
            diffuseColor = vec4(0.0);
        }
    }

    vec3 reflection = shadowRay - 2.0 * dot(shadowRay, normal) * normal;
    float n = 250.0;
    vec4 glossyColor = pow(max(dot(reflection, viewDir), 0.0), n) * material.glossyColor * glossy;

    return light.color * 1.0 / (dist * dist) * (diffuseColor + glossyColor) + material.ambientColor;
}


vec4 SpecularColor(float specular, int maxDepth, vec3 viewDir, vec3 normal, vec3 hitPoint, Primitive[10] primitives, Light light, float epsilon)
{
    vec4 finalColor = vec4(0.0);
    float dist = length(light.position - hitPoint);
    while (specular != 0.0 && maxDepth > 0) {
        vec3 specularDir = normalize(viewDir - 2.0 * dot(viewDir, normal) * normal);
        Intersection intersection = SceneIntersect(hitPoint, specularDir, primitives, epsilon);
        if (!intersection.hit) { break; }
        Primitive prim = intersection.prim;
        if (prim.type == 0){
            normal = normalize(intersection.hitPoint - prim.position);
        }
        if (prim.type == 1){
            normal = prim.normal;
        }
        if (prim.type == 2){
            normal = prim.normal;
        }
        finalColor += CalculateLighting(intersection.hitPoint, specularDir, normal, prim.material, primitives, light, epsilon) * specular;
        specular = prim.material.specular;
        hitPoint = intersection.hitPoint;
        viewDir = specularDir;
        maxDepth--;
    }

    return finalColor * 1.0 / (dist * dist);
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
    if (prim.type == 2) { // triangle
        normal = prim.normal;
    }
    vec4 finalColor = vec4(0.0);
    for (int i = 0; i < NUM_LIGHTS; i++) {
        finalColor += CalculateLighting(hitPoint, viewDir, normal, material, primitives, lights[i], epsilon);
        finalColor += SpecularColor(material.specular, maxDepth, viewDir, normal, hitPoint, primitives, lights[i], epsilon);
    }

    return finalColor;
}

vec3 PlanePos(vec3 normal, float d) {
    vec3 position = vec3(d/ normal.x, d / normal.y, d / normal.z);
    return position;
}

void main() {
    if (gpu < 0) {
        outputColor = texture(pixels, uv);
        return;
    }

    Light[10] lights;
    for (int i = 0; i < NUM_LIGHTS; i++) {
        lights[i] = Light(lightPositions[i], lightColors[i]);
    }

    Primitive[10] primitives;
    for (int i = 0; i < NUM_PRIMS; i++) {
        if (primTypes[i] == 0) {
            primitives[i] = Primitive(0, primPositions[i], vec3(0.0), Material(primDiffuseColors[i], primGlossyColors[i], primAmbientColors[i], primSpecular[i], primCheckerboard[i]), primRadius[i], 0.0, vec3(0.0), vec3(0.0), vec3(0.0));
        }
        if (primTypes[i] == 1) {
            primitives[i] = Primitive(1, vec3(0.0), primNormals[i], Material(primDiffuseColors[i], primGlossyColors[i], primAmbientColors[i], primSpecular[i], primCheckerboard[i]), 0.0, primD[i], vec3(0.0), vec3(0.0), vec3(0.0));
        }
        if (primTypes[i] == 2) {
            primitives[i] = Primitive(2, vec3(0.0), primNormals[i], Material(primDiffuseColors[i], primGlossyColors[i], primAmbientColors[i], primSpecular[i], primCheckerboard[i]), 0.0, 0.0, primV0[i], primV1[i], primV2[i]);
        }
    }

    vec3 orig = cameraPosition;
    vec3 dir = GetRayDirection(int(uv.x * width), int(uv.y * height), width, height);
    Intersection intersection = SceneIntersect(orig, dir, primitives, 0.0001);
    if (intersection.hit) {
        outputColor = Shade(intersection.prim, intersection.hitPoint, dir, lights, primitives, 5);
    } else {
        outputColor = vec4(0.0);
    }
}
