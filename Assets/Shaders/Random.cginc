
uint rng_state;

/**
* Xorshift random number generator
*
* @author http://www.reedbeta.com/blog/quick-and-easy-gpu-random-numbers-in-d3d11/
*/
float random()
{
	rng_state ^= (rng_state << 13);
	rng_state ^= (rng_state >> 17);
	rng_state ^= (rng_state << 5);

	return float(rng_state) * (1.0 / 4294967296.0);
}
