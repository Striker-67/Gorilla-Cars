﻿using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using Valve.VR;
using UnityEngine.InputSystem;
using Photon.Pun;
using GorillaNetworking;
using ExitGames.Client.Photon;
using GorillaCars.Patches;

namespace GorillaCars
{
    public class LocalCarManager : MonoBehaviourPunCallbacks
    {
        public static LocalCarManager Instance { get; set; }

        bool IsCarOn;

        private float touchTime = 0f;
        private const float debounceTime = 0.25f;

        private float touchTime1 = 0f;
        private const float debounceTime1 = 0.25f;

        bool sitting;

        public WheelCollider frontleft;
        public WheelCollider frontright;
        public WheelCollider backleft;
        public WheelCollider backright;

        public float acclration = 500f;
        public float breakforce = 200f;

        Vector2 leftStick;
        bool Drving = true;

        public static int layer = 29, layerMask = 1 << layer;
        private LayerMask baseMask;
        private const float horizontalMultiplier = 60f, verticalMultiplier = 50f;

        GameObject raycastsphere;

        Transform frontLeftWheel;
        Transform frontRightWheel;
        Transform rearLeftWheel;
        Transform rearRightWheel;

        public GameObject driver;
        public GameObject passenger;
        public GameObject backdriver;
        public GameObject backpassenger;

        GameObject PowerOnCar;
        GameObject EngineStart;
        GameObject EngineStop;
        GameObject EngineLoop;

        CustomCarDescripter CarDescriptor;
        //bool setup; (commented out because its never used)
        bool guiEnabled = true;

        void Awake()
        {
            Instance = this;
        }
        public override void OnJoinedRoom()
        {
            DontDestroyOnLoad(new GameObject("CarNetworkManager", typeof(NetWorkManager)));
        }
        public void Setup2()
        {
            try
            {
                CarDescriptor = GetComponentInParent<CustomCarDescripter>();
                frontleft = CarDescriptor.LeftFront;
                frontLeftWheel = CarDescriptor.LeftFrontwheel.transform;

                frontright = CarDescriptor.RightFront;
                frontRightWheel = CarDescriptor.RightFrontwheel.transform;

                backleft = CarDescriptor.RearLeft;
                rearLeftWheel = CarDescriptor.RearLeftwheel.transform;

                backright = CarDescriptor.RearRight;
                rearRightWheel = CarDescriptor.RearLeftwheel.transform;


            }
            catch
            {
                Debug.LogError("the wheel colliders are shit!!");
            }
            try
            {
                acclration = CarDescriptor.accleractionforce;
                breakforce = CarDescriptor.breakforce;
                driver = CarDescriptor.DriverSeat;
                passenger = CarDescriptor.PassengerSeat;
                backdriver = CarDescriptor.BackDriverSide;
                backpassenger = CarDescriptor.BackPassengerSide;

                CarDescriptor.DoorDriverSeat.name = "DriverSeat";
                CarDescriptor.DoorPassengerSeat.name = "PassengerSeat";
                CarDescriptor.DoorBackDriverSide.name = "BackDriverSeat";
                CarDescriptor.DoorBackPassengerSide.name = "BackPassengerSeat";

                PowerOnCar = CarDescriptor.poweroncar;
                PowerOnCar.name = "PowerOnCar";
                EngineStart = CarDescriptor.EngineStart.gameObject;
                EngineStop = CarDescriptor.EngineStop.gameObject;
                EngineLoop = CarDescriptor.EngineLoop.gameObject;

            }
            catch
            {
                Debug.Log("SKILL ISSUE");
            }

            try
            {
                raycastsphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(raycastsphere.GetComponent<SphereCollider>());
                raycastsphere.GetComponent<MeshRenderer>().material.shader = GorillaTagger.Instance.offlineVRRig.mainSkin.GetComponent<SkinnedMeshRenderer>().material.shader;
                raycastsphere.transform.localScale = new Vector3(.1f, .1f, .1f);
            }
            catch
            {
                Debug.LogError("Fuck you raycastsphere.");
            }
        }

        public void Setup()
        {
            Plugin.Instance.CarGameObject.SetActive(true);

            // setup = true;
        }

        public void UndoMySetup()
        {
            Plugin.Instance.CarGameObject.SetActive(false);

            // setup = false;
        }

        public void FixedUpdate()
        {

            if (raycastsphere == null)
                Setup2();
            if (GorillaLocomotion.Player.Instance != null && Physics.Raycast(GorillaLocomotion.Player.Instance.leftControllerTransform.position, GorillaLocomotion.Player.Instance.leftControllerTransform.forward, out RaycastHit hit, 100))
            {
                if (hit.collider != null)
                    raycastsphere.transform.position = hit.point;
                else
                    raycastsphere.transform.position = Vector3.zero;
            }
        }

        public void Update()
        {
            if (ControllerInputPoller.instance.leftControllerPrimaryButton)
            {
                transform.position = raycastsphere.transform.position + new Vector3(0, 1.5f, 0);
                transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
                GetComponent<Rigidbody>().velocity = Vector3.zero;
            }
            if (Plugin.IsSteamVr)
            {
                leftStick = SteamVR_Actions.gorillaTag_LeftJoystick2DAxis.GetAxis(SteamVR_Input_Sources.LeftHand);
            }
            else
            {
                ControllerInputPoller.instance.leftControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out leftStick);
            }

            if (sitting)
            {
                GorillaLocomotion.Player.Instance.bodyCollider.attachedRigidbody.drag = 6000f;
                GorillaTagger.Instance.mainCamera.transform.parent.transform.position = driver.transform.position;
                GorillaTagger.Instance.mainCamera.transform.parent.rotation = driver.transform.rotation;
            }
            else if (!sitting)
            {
                GorillaLocomotion.Player.Instance.bodyCollider.attachedRigidbody.drag = 0f;
            }
            if (Drving)
            {
                backleft.motorTorque = ControllerInputPoller.instance.leftControllerIndexFloat * acclration;
                backright.motorTorque = ControllerInputPoller.instance.leftControllerIndexFloat * acclration;
            }
            else if (!Drving)
            {
                backleft.motorTorque = -ControllerInputPoller.instance.leftControllerIndexFloat * acclration;
                backright.motorTorque = -ControllerInputPoller.instance.leftControllerIndexFloat * acclration;
            }

            frontleft.brakeTorque = ControllerInputPoller.instance.rightControllerGripFloat * breakforce;
            frontright.brakeTorque = ControllerInputPoller.instance.rightControllerGripFloat * breakforce;
            backleft.brakeTorque = ControllerInputPoller.instance.leftControllerGripFloat * breakforce;
            backright.brakeTorque = ControllerInputPoller.instance.leftControllerGripFloat * breakforce;

            frontleft.steerAngle = leftStick.x * 35f;
            frontright.steerAngle = leftStick.x * 35f;

            Updatewheel(frontleft, frontLeftWheel);
            Updatewheel(frontright, frontRightWheel);
            Updatewheel(backright, rearRightWheel);
            Updatewheel(backleft, rearLeftWheel);
        }

        public void Updatewheel(WheelCollider collider, Transform wheel)
        {
            collider.GetWorldPose(out Vector3 wheeltrans, out Quaternion wheelrot);
            wheel.transform.position = wheeltrans;
            wheel.transform.rotation = wheelrot;
        }

        public void clicked(string BtnName)
        {
            switch (BtnName)
            {
                case "DriverSeat":
                    if (touchTime + debounceTime >= Time.time)
                    {
                        if (!sitting)
                        {
                            if (ControllerInputPoller.instance.rightControllerGripFloat > 0f || ControllerInputPoller.instance.leftControllerGripFloat > 0f)
                            {
                                sitting = true;
                                baseMask = GorillaLocomotion.Player.Instance.locomotionEnabledLayers;
                                GorillaLocomotion.Player.Instance.locomotionEnabledLayers = layerMask;
                                GorillaLocomotion.Player.Instance.bodyCollider.isTrigger = true;
                                GorillaLocomotion.Player.Instance.headCollider.isTrigger = true;


                                if (PhotonNetwork.LocalPlayer.CustomProperties != null)
                                {
                                    var HT = new ExitGames.Client.Photon.Hashtable();
                                    HT.AddOrUpdate("Sitting", true);
                                    PhotonNetwork.SetPlayerCustomProperties(HT);

                                }

                            }
                        }
                        else if (sitting)
                        {
                            if (ControllerInputPoller.instance.rightControllerGripFloat > 0f || ControllerInputPoller.instance.leftControllerGripFloat > 0f)
                            {
                                GorillaTagger.Instance.mainCamera.transform.parent.rotation = Quaternion.Euler(0f, 47.9593f, 0f);
                                sitting = false;
                                GorillaLocomotion.Player.Instance.locomotionEnabledLayers = baseMask;
                                GorillaLocomotion.Player.Instance.bodyCollider.isTrigger = false;
                                GorillaLocomotion.Player.Instance.headCollider.isTrigger = false;

                            }
                            if (PhotonNetwork.LocalPlayer.CustomProperties != null)
                            {
                                var HT = new ExitGames.Client.Photon.Hashtable();
                                HT.AddOrUpdate("Sitting", false);
                                PhotonNetwork.SetPlayerCustomProperties(HT);

                            }
                        }




                    }
                    touchTime = Time.time;
                    break;

                case "PowerOnCar":
                    if (touchTime1 + debounceTime1 >= Time.time)
                    {
                        if (!IsCarOn)
                        {
                            IsCarOn = true;
                            PowerOnCar.GetComponent<MeshRenderer>().material.color = Color.green;
                            EngineStart.GetComponent<AudioSource>().Play();
                            EngineLoop.GetComponent<AudioSource>().Play();
                        }
                        else
                        {
                            IsCarOn = false;
                            PowerOnCar.GetComponent<MeshRenderer>().material.color = Color.red;
                            EngineLoop.GetComponent<AudioSource>().Stop();
                            EngineStart.GetComponent<AudioSource>().Stop();
                            EngineStop.GetComponent<AudioSource>().Play();
                        }
                    }
                    touchTime1 = Time.time;
                    break;
                case "Drive":
                    Drving = true;
                    break;
                case "Rerverse":
                    Drving = false;
                    break;
            }
        }
    }
}