using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Core : MonoBehaviour {	
	public class CloudParticle {
		public Vector3 pos;
		public float time;

		public CloudParticle(Vector3 Pos, float Time) {
			pos = Pos;
			time = Time;
		}
	}

	private SF45Device _lidar;
	private List<CloudParticle> _cloudParticles = new List<CloudParticle>();

	public GameObject TopAssembly;
	public ParticleSystem PointCloud;
	public float ParticleLifetime = 1.0f;

	void Start() {
		_lidar = new SF45Device();
		_lidar.Connect("COM6", 921600);

		// All commands are referenced from: http://support.lightware.co.za/sf45b/#/commands

		// Disable streaming.
		_lidar.SendWriteInt32(30, 0);
		
		// Disable scanning.
		_lidar.SendScanDisable();

		// Get product name.
		_lidar.SendGetProduct();

		// Set low angle limit.
		_lidar.SendWriteFloat(98, -90);

		// Set high angle limit.
		_lidar.SendWriteFloat(99, 90);

		// Configure update rate to 400/s.
		_lidar.SendWriteInt8(66, 4);

		// Configure cycle update speed to fastest.
		_lidar.SendWriteInt16(85, 5);

		// Configure bitmask of distance info to receive.
		_lidar.SendWriteInt32(27, 0x109);

		// Set stream to output distance data.
		_lidar.SendWriteInt32(30, 5);

		// Start scanning.
		_lidar.SendScanEnable();
	}

	private void _UpdateParticles() {
		// Remove particles that are older than specified lifetime.
		for (int i = 0; i < _cloudParticles.Count; ++i) {
			if (_cloudParticles[i].time <= Time.time - ParticleLifetime) {
				_cloudParticles.RemoveAt(i);
				--i;
			}
		}
		
		ParticleSystem.Particle[] newParticles = new ParticleSystem.Particle[_cloudParticles.Count];

		Color32 cA = new Color32(255, 255, 255, 255);
		Color32 cB = new Color32(255, 255, 255, 0);

		for (int i = 0; i < _cloudParticles.Count; ++i) {	
			float t = (Time.time - _cloudParticles[i].time) / ParticleLifetime;

			newParticles[i].position = _cloudParticles[i].pos;
			newParticles[i].startSize = 0.02f;
			newParticles[i].startColor = Color32.Lerp(cA, cB, t);
			newParticles[i].startLifetime = 1000.0f;
			newParticles[i].remainingLifetime = 1000.0f;
		}
		
		PointCloud.SetParticles(newParticles, newParticles.Length, 0);
	}

	void Update() {
		// Update serial port and read incoming packets.
		_lidar.Update();

		// Read and clear currently accumulated results.
		List<DistanceResult> distances = _lidar.PopDistanceResults();

		if (distances.Count > 0) {
			float finalAngle = 0.0f;
			Vector3 finalPos = new Vector3();

			// Add new distances to the display buffer.
			for (int i = 0; i < distances.Count; ++i) {
				_cloudParticles.Add(new CloudParticle(distances[i].position, Time.time));
				finalAngle = distances[i].angle;
				finalPos = distances[i].position;
			}

			TopAssembly.transform.rotation = Quaternion.Euler(0, finalAngle + 180, 0);
			Debug.DrawLine(TopAssembly.transform.position, TopAssembly.transform.position + finalPos, Color.blue, 0.01f);

			_UpdateParticles();
		}
	}
}
