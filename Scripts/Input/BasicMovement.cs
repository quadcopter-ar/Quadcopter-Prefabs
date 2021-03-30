using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class BasicMovement : MonoBehaviour
{
    float deadZoneAmount = 0.5f;

    private XRNode leftControllerNode = XRNode.LeftHand;
    private List<InputDevice> leftInputDevices = new List<InputDevice>();

    private InputDevice leftController;
    private XRNode rightControllerNode = XRNode.LeftHand;
    private List<InputDevice> rightInputDevices = new List<InputDevice>();
    private InputDevice rightController;

    void Start() {
        //Lets get all the devices we can find.
        GetDevices();
    }

    void Update() {
        if (leftController == null) {
            GetControllerDevices(leftControllerNode, leftController, leftInputDevices);
        }

        if (rightController == null) {
            GetControllerDevices(rightControllerNode, rightController, rightInputDevices);
        }

        CheckForChanges();
    }

    void CheckForChanges() {

    }

    void GetDevices() {
        //Gets the Right Controller Devices
        GetControllerDevices(leftControllerNode, leftController, leftInputDevices);

        //Gets the Right Controller Devices
        GetControllerDevices(rightControllerNode, rightController, rightInputDevices);

    }

    void GetControllerDevices(XRNode controllerNode, InputDevice controller,List<InputDevice> inputDevices) {
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(controllerNode, inputDevices);

        if (inputDevices.Count == 1){
            controller = inputDevices[0];
            Debug.Log(string.Format("Device name '{0}' with characteristics '{1}'", controller.name, controller.characteristics));
        }

        if (inputDevices.Count > 1) {
            Debug.LogAssertion("More than one device found with the same input characteristics");
        }
    }

}
