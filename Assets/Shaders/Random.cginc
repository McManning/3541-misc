
uint rng_state = 0;

/**
* Xorshift random number generator for a value [0, 1)
*
* @author http://www.reedbeta.com/blog/quick-and-easy-gpu-random-numbers-in-d3d11/
*/
float random()
{    
	// Xorshift algorithm from George Marsaglia's paper
	rng_state ^= (rng_state << 13);
	rng_state ^= (rng_state >> 17);
	rng_state ^= (rng_state << 5);

	// Bind [0, 1)
	return float(rng_state) * (1.0 / 4294967296.0);
}

uint wangHash(uint seed)
{
	seed = (seed ^ 61) ^ (seed >> 16);
	seed *= 9;
	seed = seed ^ (seed >> 4);
	seed *= 0x27d4eb2d;
	seed = seed ^ (seed >> 15);
	return seed;
}

float randomBetween(float min, float max) 
{
	return min + random() * (max - min);
}
