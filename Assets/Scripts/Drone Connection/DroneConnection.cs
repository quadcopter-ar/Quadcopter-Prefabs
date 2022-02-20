using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading;
using System.Text;
using System;
using System.Net.Sockets;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Linq;

[Serializable]
public class Position
{
	public float x;
	public float y;
	public float z;
}

[Serializable]
public struct Orientation
{
	public float w;
	public float x;
	public float y;
	public float z;
}

[Serializable]
public class PoseJSON
{
	public float x;
	public float y;
	public float z;
	public float pitch;
	public float roll;
	public float yaw;
}

public class TargetJSON
{
	public int status;
	public float x;
	public float y;
	public float z;
	public float yaw;
}

public class DroneConnection : MonoBehaviour
{

	public string remoteIP;
	public bool isSimulation;
	public GameObject droneObject;
	public Button takeoffLandButton;

	private Thread clientReceiveThread;
	private TcpClient socketConnection;

	private int status = 0;

	private Vector3 current_position = new Vector3(0, 0, 0);
	private Vector3 current_orientation = new Vector3(0, 0, 0);
	private Vector3 target_displacement = new Vector3(0, 0, 0);
	private Vector3 target_position = new Vector3(0, 1, 0);
	private Vector3 target_orientation = new Vector3(0, 0, 0);

	private XRNode controllerNode = XRNode.RightHand;
	private List<InputDevice> devices = new List<InputDevice>();
	private InputDevice controller;

	// Use this for initialization
	void Start()
	{
		takeoffLandButton.transform.GetComponentInChildren<Text>().text = "Takeoff";
		takeoffLandButton.GetComponent<Button>().onClick.AddListener(TakeoffLandButtonEvent);
		ConnectToTcpServer();

		InputDevices.GetDevicesAtXRNode(controllerNode, devices);
		controller = devices.FirstOrDefault();
	}

	// Update is called once per frame
	void Update()
	{
		droneObject.transform.position = current_position + new Vector3(0, 2, -19);
		droneObject.transform.eulerAngles = current_orientation;

		// Reset
		UpdateTarget();


	}

	private void UpdateTarget()
	{
		target_position = new Vector3(current_position.x, current_position.y, current_position.z);
		target_orientation = new Vector3(current_orientation.x, current_orientation.y, current_orientation.z);

		Vector3 dir = new Vector3(0, 0, 0);

		Vector2 touchCoords;

		InputFeatureUsage<Vector2> primary2DVector = CommonUsages.primary2DAxis;

		float deadZoneAmt = 0.5f;

		/*
		if (controller.TryGetFeatureValue(primary2DVector, out touchCoords) && touchCoords != Vector2.zero)
		{

			Debug.Log("primary2DAxisClick is pressed " + touchCoords);

			if (touchCoords.x < -deadZoneAmt)
			{
				// touching left side, strafe left
				dir -= droneObject.transform.right * 0.5f;
			}
			else if (touchCoords.x > deadZoneAmt)
			{
				// touching right side, strafe right
				dir += droneObject.transform.right * 0.5f;
			}
		}

		*/
		
		

		if (Input.GetKey(KeyCode.W))
		{
			dir += droneObject.transform.forward * 0.5f;
		}
		if (Input.GetKey(KeyCode.S))
		{
			dir -= droneObject.transform.forward * 0.5f;
		}
		if (Input.GetKey(KeyCode.A))
		{
			dir -= droneObject.transform.right * 0.5f;
		}
		if (Input.GetKey(KeyCode.D))
		{
			dir += droneObject.transform.right * 0.5f;
		}

		dir.y = 0f;

		if (Input.GetKey(KeyCode.R))
		{
			dir.y += 0.5f;
		}
		if (Input.GetKey(KeyCode.F))
		{
			dir.y -= 0.5f;
		}
		/*
		if (controller.TryGetFeatureValue(primary2DVector, out touchCoords) && touchCoords != Vector2.zero)
		{ 
			if (touchCoords.y < -deadZoneAmt)
			{
				// touching bottom side, move backwards
				dir.y -= 0.5f;

			}
			else if (touchCoords.y > deadZoneAmt)
			{
				// touching top side, move forward
				dir.y += 0.5f;
			}

			// print(touchCoords);
		}
		*/
		target_position += dir;

		if (Input.GetKey(KeyCode.E))
		{
			target_orientation.y += 10f;
		}
		if (Input.GetKey(KeyCode.Q))
		{
			target_orientation.y -= 10f;
		}


		target_position.x = Mathf.Clamp(target_position.x, -1.5f, 1.5f);
		target_position.z = Mathf.Clamp(target_position.z, -0.5f, 0.5f);
		target_position.y = Mathf.Clamp(target_position.y, 0.5f, 1.5f);
		// target_position = new Vector3(0, 1.5f, 0);
		target_orientation.y = 0f;
	}

	private void ConnectToTcpServer()
	{
		try
		{
			clientReceiveThread = new Thread(new ThreadStart(ListenForData));
			clientReceiveThread.IsBackground = true;
			clientReceiveThread.Start();
		}
		catch (Exception e)
		{
			Debug.Log("On client connect exception " + e);
		}
	}

	public void TakeoffLandButtonEvent()
	{
		Text btn_text = takeoffLandButton.transform.GetComponentInChildren<Text>();
		print(btn_text.text);
		if (btn_text.text == "Takeoff")
		{
			status = 1;
			btn_text.text = "Land";
		}
		else if (btn_text.text == "Land")
		{
			status = 0;
			btn_text.text = "Takeoff";
		}
	}

	private void ListenForData()
	{
		try
		{
			socketConnection = new TcpClient(remoteIP, 13579);

			if (socketConnection.Connected)
				Debug.Log("TCP Server connected.");
			while (true)
			{
				using (NetworkStream stream = socketConnection.GetStream())
				{
					Byte[] bytes = new Byte[1024];


					// Read incomming stream into byte arrary.                  
					while (true)
					{
						try
						{
							TargetJSON t = new TargetJSON();
							t.status = status;
							t.x = target_position.x;
							t.y = target_position.y;
							t.z = -target_position.z;
							// t.yaw = target_orientation.y / 180f * Mathf.PI;
							t.yaw = target_orientation.y;
							Byte[] send_msg = Encoding.UTF8.GetBytes(JsonUtility.ToJson(t));
							// Byte[] send_msg = Encoding.UTF8.GetBytes("RECV");
							stream.Write(send_msg, 0, send_msg.Length);
							Array.Clear(bytes, 0, bytes.Length);
							stream.Read(bytes, 0, bytes.Length);
							string json_str = Encoding.UTF8.GetString(bytes);
							// print(json_str);
							PoseJSON p = JsonUtility.FromJson<PoseJSON>(json_str);
							current_position.x = p.x;
							current_position.y = p.y;
							current_position.z = -p.z;
							current_orientation.x = -p.pitch * 180f / Mathf.PI;
							current_orientation.y = p.yaw * 180f / Mathf.PI;
							current_orientation.z = p.roll * 180f / Mathf.PI;
						}
						catch (Exception e)
						{
							stream.Flush();
							// Debug.Log(e);
						}
					}
				}
			}
		}

		catch (SocketException socketException)
		{
			Debug.Log("Socket exception: " + socketException);
		}
	}

	void OnDestroy()
	{
		clientReceiveThread.Abort();
		Debug.Log("OnDestroy1");
	}
}
