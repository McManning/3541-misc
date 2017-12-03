
LAB 5 README

Cubic Interpolation is done with Catmull-Rom splines (CatmullRomMovement.cs) between a set of GameObjects

Re-parameterization by arc length is done by creating a lookup table mapping time to a position on one of the sub-splines in the system.

Easing is done via single cubic easing to speed up as we leave the starting position and to slow down as we approach it again.

Orientation/Rotation is lazily done via Quat.LookRotation using the difference of two nearby points on the spline as a direction vector. 
