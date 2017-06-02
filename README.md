# ComputeShaderTutorial

This project has been designed to teach an aspiring Unity VR developer the very basics of working with ComputeShaders in VR. The project has three relevant files:

* ComputeExample.cs
* ComputeExample.compute
* Compute_Graphics.shader

# ComputeExample.cs

![ComputeExample.cs Inspector](https://puu.sh/w3l3G/1a86dd6e17.png)

This is the only piece of C# script in the project. There are four important functions:

* InitialiseBuffers(), responsible for creating the ComputeBuffer and initialising the Compute kernel that will be editing the buffer
* FillBuffers(), responsible for filling the ComputeBuffer with data and assigning the ComputeBuffer to both the Compute and Graphics shaders.
* SetData(), called before running the Compute Shader, which updates the Compute Shader with the controller positions, any relevant inputs (in our case, the trigger axis), and any other information (in our case, some additional physics data)
* OnRenderObject(), called automatically by Unity, in this function we call SetData, run Dispatch() (which actually executes our Compute Shader code), and then draw the buffer using SetPass() and Graphics.DrawProcedural()

# ComputeExample.compute

This is our compute shader. The most confusing thing is probably the thread groups - the reason GPUs are so great is that they are amazing at parallel processing. We tell our GPU how many thread groups to run (and how many threads within each group), and this allows us to run massively parallel operations. In our example we run with 10 thread groups in the x, y and z dimensions, for a total of 1000 thread groups. Within each thread groups we set up 10 threads in the x, y and z dimensions, for 1000 thread per thread group, or 1,000,000 threads in total! This allows us a maximum of 1,000,000 particles.

The compute shader in our example project has one function (or kernel) called ParticleFunction. This function has a single parameter, an array of three ints which refers to the current thread that we are working on. In line 35, you can see how to use this parameter to get the index in our buffer and so get an individual particle to run operations on.

From there, it's pretty self-explanatory, we run a simple implementation of Coloumb's Law on each particle using our controllers as charged particles. Note that the particles don't exert a force on each other - this is called an n-body simulation and is a shiteload more complicated.

# Compute_Graphics.shader

If you already know how to write vertex-fragment shaders, this file will look very familiar. The only different is now, rather than passing in a set of vertex data to our vertex function, we pass in an index that refers to an individual item in our buffer, and we run our vertex shader on that instead.

Compute Shaders become much more interesting and powerful when you run them in combination with a Geometry Shader. I'll write a tutorial project for GeometryShaders later. For now, we just output the results of our compute shader as points. With Geometry Shaders, we could output more interesting and complicated geometry.

# Other handy resources

[CG programming wikibook](https://en.wikibooks.org/wiki/Cg_Programming/Unity) - a really good resource for learning the basics of vertex/fragment shader combos in Unity, and a generally good entry point for thinking about shaders

[Simple example of a geometry shader](http://answers.unity3d.com/questions/744622/constructing-a-cube-primitive-using-shader.html) - from a thread where a guy was wanting to make one. Check the last post. It's a good example of the basics of how geometry shaders work, how to generate normals, vertices and triangles and append them to the buffer, and how everything interacts with the vertex/fragment shaders
