using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Note that this struct has to match EXACTLY the struct defined in our shaders.
public struct ParticleData {
	public Vector3 pos;
	public Vector3 velocity;
	public Color color;
}

public class ComputeExample : MonoBehaviour {

	//Just SteamVR stuff - reference to our controllers so we can interact with the particles
	[Header("SteamVR Controllers")]
	public SteamVR_TrackedObject LeftController;
	public SteamVR_TrackedObject RightController;
	private SteamVR_Controller.Device RightDevice;
	private SteamVR_Controller.Device LeftDevice;

	[Header("Shaders")]
	public ComputeShader compute;
	public Material graphics;

	[Header("Shader Parameters")]
	[Range(0, 1000000)] [Tooltip("Number of particles")] 
	public int count = 50000;

	[Space(10)]

	[Range(-0.5f, 0.5f)] [Tooltip("Charge (in nC) of the controllers when triggers are fully pressed")] 
	public float ControllerMaxCharge = -0.1f;

	[Range(0f, 0.02f)] [Tooltip("Amount of damping to apply each frame")] 
	public float Damping = 0.005f;

	[Range(0f, 0.001f)] [Tooltip("Charge (in nC) of each particle")]
	public float ParticleCharge = 0.0001f;

	[Range(0f, 10f)] [Tooltip("Mass (in kg) of each particle")]
	public float ParticleMass = 1f;

	[Range(0f, 1f)] [Tooltip("Softening factor to limit force amplitudes")] 
	public float SofteningFactor = 0.1f;

	//shader info
	ComputeBuffer Buffer;
	ParticleData[] Data;
    int Stride;
    int KernelIndex;

	void Start () {
		//Set up all the data
		InitialiseBuffers();
		FillBuffers();
	}

	//This can fail a time or two on start while SteamVR initialises the controllers
	void TryGetLeftDevice() {
		try {
			LeftDevice = SteamVR_Controller.Input((int)LeftController.index);
		} catch (System.Exception e) {
			Debug.Log("Failed getting left controller: " + e.Message);
		}
	}

	void TryGetRightDevice() {
		try {
			RightDevice = SteamVR_Controller.Input((int)RightController.index);
		} catch (System.Exception e) {
			Debug.Log("Failed getting right controller: " + e.Message);
		}
	}

	//Make sure we release the data when the program closes!
	void OnDestroy() {
		Buffer.Release();
		Buffer.Dispose();
	}

	void InitialiseBuffers() {
		//Calculate 'Stride' or how much data the GPU should get for each particle
		//We calculate this by determining the size, in bytes, of a single ParticleData struct instance
		int vector3Stride = sizeof(float) * 3;
		int colorStride = sizeof(int) * 4;
		Stride = vector3Stride * 2 + colorStride;

		//Then we initialise the buffer and get the KernelIndex using the kernel name in the first line of our compute shader
		Buffer = new ComputeBuffer(count, Stride);
		KernelIndex = compute.FindKernel("ParticleFunction");
	}

	void FillBuffers() {
		//initialise our data array
		Data = new ParticleData[count];

		//give it some starting data. For now, we'll make a bunch of white particles spawning in a sphere 1m off the ground
		for(int i = 0; i < Data.Length; i++) {
			Data[i] = new ParticleData();

			Data[i].pos = Random.insideUnitSphere * 0.5f + new Vector3(0f, 1f, 0f);
			Data[i].velocity = Vector3.zero;
			Data[i].color = Color.white;
		}

		//then put the data array into our ComputeBuffer
		Buffer.SetData(Data);

		//And assign it to our ComputeShader and our Graphics shader!
		compute.SetBuffer(KernelIndex, "outputBuffer", Buffer);
		graphics.SetBuffer("inputBuffer", Buffer);
	}

	//Whenever we do a render pass
	void OnRenderObject() {
		//Update the parameters of our compute shader, especially the controller positions / trigger axes for input
		SetData();

		//Actually run the compute shader to update our Particle Data
		compute.Dispatch(KernelIndex, 10, 10, 10);

		//And then draw it using our graphics material.
		graphics.SetPass(0);
		Graphics.DrawProcedural(MeshTopology.Points, Buffer.count);
	}

	void SetData() {
		//apply general physics parameters
		//note that we don't have to do this every frame, unless we allow players to change these values in real-time
		compute.SetFloat("Damping", Damping);
		compute.SetFloat("ParticleCharge", ParticleCharge);
		compute.SetFloat("ParticleMass", ParticleMass);
		compute.SetFloat("SofteningFactor", SofteningFactor);

		//Make sure we actually have the devices before we do anything with them
		if(LeftDevice == null) {
			TryGetLeftDevice();
		} else {
			//Create a four-dimensional vector - XYZ will be the controller position, and W will be the 'charge' of the controller, attached to the trigger axis
			Vector3 LeftPosition = LeftController.transform.position;
			float LeftCharge = Mathf.Lerp(0f, ControllerMaxCharge, LeftDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x);
			compute.SetVector("LeftController", new Vector4(LeftPosition.x, LeftPosition.y, LeftPosition.z, LeftCharge));
		}

		//Make sure we actually have the devices before we do anything with them
		if(RightDevice == null) {
			TryGetRightDevice();
		} else {
			//Create a four-dimensional vector - XYZ will be the controller position, and W will be the 'charge' of the controller, attached to the trigger axis
			Vector3 RightPosition = RightController.transform.position;
			float RightCharge = Mathf.Lerp(0f, ControllerMaxCharge, RightDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x);
			compute.SetVector("RightController", new Vector4(RightPosition.x, RightPosition.y, RightPosition.z, RightCharge));
		}
	}
}
