
LAB 4 README

Agent motion:
	- Shark (predator) follows two splines* (one for a period of idle time, the other for "hunting").
		Shark ignores prey while idle, but will attack the first it sees while hunting
	- Fish (prey) move toward a shared objective while adhering to flocking behavior. 
		If they see a shark in their FOV, they'll run in the opposite direction, ignoring flocking.

Agent vision:
	- Shark has a FOV of 120 degrees in front of it, with a radius of 20 units. 
	- Fish have a FOV of 300 degrees around them. Radius differs based on objective, 
		but for fleeing from predators it's at 20 units.

Additional features:
	- Fish are flocking. It's not as smooth as I'd like though.
	- Fish have an objective (Fishbait) that moves along a spline
	- 


*	Bezier spline implementation was done prior to us talking about it in class so I didn't realize we were going to go through it.
	I'll write a different implementation if required by lab 5. 
	
	Spline implementation is not my own, it's a simplified version of a tutorial: http://catlikecoding.com/unity/tutorials/curves-and-splines/
	I mostly wanted to learn how to build custom inspector/scene gizmo components in Unity. Plus, ya know, splines for movement paths.
