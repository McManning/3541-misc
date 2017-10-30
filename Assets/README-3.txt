
Lab 3 README

DX11+ is very required.

Tests were done under:
	GTX 980 Ti / i5 @ 3.3 GHz
	Integrated Intel HD 5500 / i7 @ 2.4 GHz 

Current emitter settings are optimized for the integrated GPU.

There are 3 different particle emitters implemented for this lab. Each one runs off the same 
GPU Particle Emitter script. You can enable them one at a time in the scene hierarchy.

* Elastic Particle Emitter
	Super simple emitter that should check all the boxes for this lab. 
	Colored balls bounce around the scene, reacting to a sphere and a plane while fading out as they age. 

	Relevant emitter settings:
	- Particle Count, Emitter Radius, Min Life, Max Life
	- Initial Acceleration - Acceleration applied after spawning
	- Constant Acceleration - Gravity direction/force
	- Damping Ratio - Energy loss per bounce

* Curl Noise Particle Emitter
	My favorite. Calculates a vector toward an attractor and applies a curl function against the velocity 
	using a perlin noise function for randomness. Will also react with the collider sphere in not a
	particularly realistic way, but it looks cool. 

	Relevant emitter settings:
	- Particle Count, Emitter Radius, Min Life, Max Life
	- Constant Acceleration - Gravity direction/force
	- Attractor
	- Attractor Acceleration - Magnitude of attraction vector
	- Curl Acceleration - Magnitude of curl vector
	- Noise Scale - Scaling applied to perlin noise generator
	- Start Color - Duh
	- End Color - Duh
	- Color Bias - How much of end color to use over start color

* Bridson Curl Particle Emitter
	Utilizes the technique described in Bridson's Siggraph 2007 paper on procedural fluid flow
	(https://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph2007-curlnoise.pdf)

	Relevant emitter settings:
	- Same as curl noise

	Not fully functioning on time. Removed from final package.

