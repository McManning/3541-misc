
// Via: http://geomalgorithms.com/a07-_distance.html
bool intersectCapsule(float3 eyePos, float3 eyeRay, float3 startCap, float3 endCap, float radius)
{
	// First position is a FOV limit on the ray from the eye
	float3   u = eyeRay * 10000.0;
	float3   v = endCap - startCap;
	float3   w = eyePos - startCap;
	float    a = dot(u, u);
	float    b = dot(u, v);
	float    c = dot(v, v);
	float    d = dot(u, w);
	float    e = dot(v, w);
	float    D = a*c - b*b;
	float    sc, sN, sD = D;
	float    tc, tN, tD = D;

	float	SMALL_NUM = 0.001;

	if (D < SMALL_NUM) {
		sN = 0.0;
		sD = 1.0;
		tN = e;
		tD = c;
	}
	else {
		sN = (b*e - c*d);
		tN = (a*e - b*d);
		if (sN < 0.0) {
			sN = 0.0;
			tN = e;
			tD = c;
		}
		else if (sN > sD) {
			sN = sD;
			tN = e + b;
			tD = c;
		}
	}

	if (tN < 0.0) {
		tN = 0.0;

		if (-d < 0.0)
			sN = 0.0;
		else if (-d > a)
			sN = sD;
		else {
			sN = -d;
			sD = a;
		}
	}
	else if (tN > tD) {
		tN = tD;

		if ((-d + b) < 0.0)
			sN = 0;
		else if ((-d + b) > a)
			sN = sD;
		else {
			sN = (-d + b);
			sD = a;
		}
	}

	sc = (abs(sN) < SMALL_NUM ? 0.0 : sN / sD);
	tc = (abs(tN) < SMALL_NUM ? 0.0 : tN / tD);

	float3 dP = w + (u * sc) - (v * tc);

	return normalize(dP) < radius;
}
