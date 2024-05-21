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

/**
Basic FXAA implementation based on the code on geeks3d.com with the
modification that the texture2DLod stuff was removed since it's
unsupported by WebGL.

--

From:
https://github.com/mitsuhiko/webgl-meincraft

Copyright (c) 2011 by Armin Ronacher.

Some rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:

    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above
      copyright notice, this list of conditions and the following
      disclaimer in the documentation and/or other materials provided
      with the distribution.

    * The names of the contributors may not be used to endorse or
      promote products derived from this software without specific
      prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

#ifndef FXAA_REDUCE_MIN
    #define FXAA_REDUCE_MIN   (1.0/ 128.0)
#endif
#ifndef FXAA_REDUCE_MUL
    #define FXAA_REDUCE_MUL   (1.0 / 8.0)
#endif
#ifndef FXAA_SPAN_MAX
    #define FXAA_SPAN_MAX     8.0
#endif

//optimized version for mobile, where dependent 
//texture reads can be a bottleneck
vec4 fxaa(sampler2D tex, vec2 fragCoord, vec2 resolution){
    vec4 color;
    mediump vec2 inverseVP = vec2(1.0 / resolution.x, 1.0 / resolution.y);
    vec2 v_rgbNW = (fragCoord + vec2(-1.0, -1.0)) * inverseVP;
	vec2 v_rgbNE = (fragCoord + vec2(1.0, -1.0)) * inverseVP;
	vec2 v_rgbSW = (fragCoord + vec2(-1.0, 1.0)) * inverseVP;
	vec2 v_rgbSE = (fragCoord + vec2(1.0, 1.0)) * inverseVP;
	vec2 v_rgbM = vec2(fragCoord * inverseVP);
    vec3 rgbNW = texture2D(tex, v_rgbNW).xyz;
    vec3 rgbNE = texture2D(tex, v_rgbNE).xyz;
    vec3 rgbSW = texture2D(tex, v_rgbSW).xyz;
    vec3 rgbSE = texture2D(tex, v_rgbSE).xyz;
    vec4 texColor = texture2D(tex, v_rgbM);
    vec3 rgbM  = texColor.xyz;
    vec3 luma = vec3(0.299, 0.587, 0.114);
    float lumaNW = dot(rgbNW, luma);
    float lumaNE = dot(rgbNE, luma);
    float lumaSW = dot(rgbSW, luma);
    float lumaSE = dot(rgbSE, luma);
    float lumaM  = dot(rgbM,  luma);
    float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
    float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));
    
    mediump vec2 dir;
    dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
    dir.y =  ((lumaNW + lumaSW) - (lumaNE + lumaSE));
    
    float dirReduce = max((lumaNW + lumaNE + lumaSW + lumaSE) *
                          (0.25 * FXAA_REDUCE_MUL), FXAA_REDUCE_MIN);
    
    float rcpDirMin = 1.0 / (min(abs(dir.x), abs(dir.y)) + dirReduce);
    dir = min(vec2(FXAA_SPAN_MAX, FXAA_SPAN_MAX),
              max(vec2(-FXAA_SPAN_MAX, -FXAA_SPAN_MAX),
              dir * rcpDirMin)) * inverseVP;
    
    vec3 rgbA = 0.5 * (
        texture2D(tex, fragCoord * inverseVP + dir * (1.0 / 3.0 - 0.5)).xyz +
        texture2D(tex, fragCoord * inverseVP + dir * (2.0 / 3.0 - 0.5)).xyz);
    vec3 rgbB = rgbA * 0.5 + 0.25 * (
        texture2D(tex, fragCoord * inverseVP + dir * -0.5).xyz +
        texture2D(tex, fragCoord * inverseVP + dir * 0.5).xyz);

    float lumaB = dot(rgbB, luma);
    if ((lumaB < lumaMin) || (lumaB > lumaMax))
        color = vec4(rgbA, texColor.a);
    else
        color = vec4(rgbB, texColor.a);
    return color;
}

void main() {
    if (gpu < 0) {
        outputColor = fxaa(pixels, vec2(uv.x * width, uv.y * height), vec2(width, height));
        return;
    }

    Light[10] lights;
    for (int i = 0; i < NUM_LIGHTS; i++) {
        lights[i] = Light(lightPositions[i], lightColors[i]);
    }

    Primitive[10] primitives;
    for (int i = 0; i < NUM_PRIMS; i++) {
        if (primTypes[i] == 0) {
            primitives[i] = Primitive(0, primPositions[i], vec3(0.0), Material(primDiffuseColors[i], primGlossyColors[i], primAmbientColors[i], primSpecular[i]), primRadius[i], 0.0, vec3(0.0), vec3(0.0), vec3(0.0));
        }
        if (primTypes[i] == 1) {
            primitives[i] = Primitive(1, vec3(0.0), primNormals[i], Material(primDiffuseColors[i], primGlossyColors[i], primAmbientColors[i], primSpecular[i]), 0.0, primD[i], vec3(0.0), vec3(0.0), vec3(0.0));
        }
        if (primTypes[i] == 2) {
            primitives[i] = Primitive(2, vec3(0.0), primNormals[i], Material(primDiffuseColors[i], primGlossyColors[i], primAmbientColors[i], primSpecular[i]), 0.0, 0.0, primV0[i], primV1[i], primV2[i]);
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
