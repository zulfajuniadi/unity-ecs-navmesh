using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

///-----------------------------------------------------------------------------------------
///   Namespace:      BE
///   Class:          MobileRTSCam
///   Description:    classes to manage camera input  control
///   Usage :		  
///   Author:         BraveElephant inc.                    
///   Version: 		  v1.3 (2016-12-06)
/// 				  - Add Rotation while shift keydown in Editor
/// 				  v1.2 (2016-11-24)
/// 				  - Prevent Camerashaking while on touch
/// 				  - X < Y Rotation Use variable added
/// 				  - GraphicRaycaster related code removed
/// 				  v1.1 (2016-09-21)
///                   - border draw gizmo error fix
/// 				  - prevent camera shaking while touch after drag
///                   v1.0 (2016-02-15)
///-----------------------------------------------------------------------------------------
namespace BE
{

	public interface MobileRTSCamListner
	{
		void OnTouchDown (Ray ray);
		void OnTouchUp (Ray ray);
		void OnTouch (Ray ray);
		void OnDragStart (Ray ray);
		void OnDragEnd (Ray ray);
		void OnDrag (Ray ray);
		void OnLongPress (Ray ray);
		void OnMouseWheel (float fValue);
	}

	public enum PinchType
	{
		None = -1,
		Zoom = 0,
		Rotate = 1,
		Up = 2,
		Max = 3,
	}

	public enum BorderType
	{
		None = -1,
		Rect = 0,
		Circle = 1,
	}

	public class MobileRTSCam : MonoBehaviour
	{

		public static MobileRTSCam instance;

		public MobileRTSCamListner Listner = null;

		private Transform trCamera = null; // transform for zoom
		private Transform trCameraRoot = null; // transform for move(panning)
		[HideInInspector]
		public bool camPanningUse = true;
		public BorderType borderType = BorderType.None;
		public bool BorderUse = true;
		public float XMin = -30.0f; // Camera panning x limit
		public float XMax = 30.0f; // Camera panning x limit
		public float ZMin = -30.0f; // Camera panning z limit
		public float ZMax = 30.0f; // Camera panning z limit
		public float CircleBorderRadius = 30.0f; // Camera panning circle limit
		public float zoomMax = 50.0f;
		public float zoomMin = 20.0f;
		public float zoomCurrent = -34.0f;
		public float zoomSpeed = 2.0f;

		private bool bInTouch = false;
		private Vector3 vCamRootPosOld = Vector3.zero;
		private Vector3 mousePosStart = Vector3.zero;
		private Vector3 mousePosPrev = Vector3.zero;
		private Camera camMain;
		private bool Dragged = false;

		public Plane xzPlane;
		public Ray ray;

		private Vector3 vPickStart;
		private Vector3 vPickOld;
		private Vector3 vCameraPanDir;

		// Camera Panning Inertia Movement 
		public bool InertiaUse = true;
		private bool InertiaActive = false;
		private Vector3 InertiaSpeed;
		private float InertiaAge = 0.0f;

		private bool InZoom = false;

		// variables for long touch down check
		private bool LongTabCheck = true;
		public float LongTabPeriod = 0.5f;
		private float ClickAfter = 0.0f;

		// disable cam panning for limited time
		private float fCamPanLimit = 0.0f;

		private bool InPinch = false;
		private PinchType pinchType = PinchType.None;
		private Vector3 vPinchDirStart = Vector3.zero;
		private float fPinchDistanceStart = 0.0f;
		private Vector3 vPinchPickCenterStart = Vector3.zero;
		private Vector3 vCamRootRotStart = Vector3.zero;
		private Vector3 vCamRootRot = Vector3.zero;
		private Vector2[] vTouchPosStart = new Vector2[2];
		private float ZoomStart = 0.0f;

		// disable ui while camera is in dragging
		//private GraphicRaycaster	gr;

		public bool UseYRotation = true;
		public bool UseXRotation = true;

		// for editor
		private Vector3 vEDCamRootRotStart;
		private Vector3 vEDCamRootRot;
		private Vector3 vEDMouseStart;
		private Vector3 vEDMouseMove;
		private bool vEDInRotation = false;

		public float fDragCheckMin = 0.1f;
		public float fInertiaCheckMin = 0.1f;

		void Awake ()
		{
			instance = this;
			trCamera = transform.Find ("Main Camera").transform;
			trCameraRoot = transform;
			//gr = GameObject.Find ("Canvas").GetComponent<GraphicRaycaster>();
			xzPlane = new Plane (new Vector3 (0f, 1f, 0f), 0f); // set base plane to xzplane with height zero
			camMain = trCamera.GetComponent<Camera> ();
			zoomCurrent = -trCamera.localPosition.z;
		}

		void Start ()
		{

		}

		void Update ()
		{

			//inertia camera panning
			if (InertiaUse)
			{
				if (InertiaActive && (InertiaSpeed.magnitude > fInertiaCheckMin))
				{
					SetCameraPosition (trCameraRoot.position - InertiaSpeed);
					InertiaSpeed = Vector3.Lerp (InertiaSpeed, Vector3.zero, InertiaAge);
					InertiaAge += Time.smoothDeltaTime;
				}
				else
				{
					InertiaActive = false;
				}
			}

			if (fCamPanLimit > 0.0f)
				fCamPanLimit -= Time.deltaTime;

			if (Input.touchCount < 2)
			{
				if (InPinch)
				{
					InPinch = false;
					bInTouch = false;
					fCamPanLimit = 0.1f;
					pinchType = PinchType.None;
					camPanningUse = true;
				}
			}

			Vector3 vTouch = Input.mousePosition;
			ray = Camera.main.ScreenPointToRay (vTouch);
			float enter;

			//if left MouseButton down
			if (Input.GetMouseButton (0))
			{

				if (EventSystem.current && EventSystem.current.IsPointerOverGameObject ())
				{
					//Debug.Log("left-click over a GUI element!");
					return;
				}

				if (Input.GetKey (KeyCode.LeftShift))
				{
					//Debug.Log ("left shift key is held down");

					if (!vEDInRotation)
					{
						vEDInRotation = true;
						vEDMouseStart = vTouch;
						vEDCamRootRotStart = trCameraRoot.localRotation.eulerAngles;
						//Debug.Log ("editor rotation start");
					}
					else
					{
						if (Vector3.Distance (vTouch, vEDMouseStart) > fDragCheckMin)
						{
							//Debug.Log ("change rotation start vTouch:"+vTouch+" vEDMouseStart:"+vEDMouseStart+" vEDCamRootRotStart:"+vEDCamRootRotStart);
							vEDMouseMove = vTouch - vEDMouseStart;
							if (UseXRotation)
							{
								vEDCamRootRot.x = vEDCamRootRotStart.x - vEDMouseMove.y * 0.1f;
								vEDCamRootRot.x = Mathf.Clamp (vEDCamRootRot.x, 10.0f, 90.0f);
							}
							else
							{
								vEDCamRootRot.x = vEDCamRootRotStart.x;
							}
							if (UseYRotation)
							{
								vEDCamRootRot.y = vEDCamRootRotStart.y + vEDMouseMove.x * 0.1f;
							}
							else
							{
								vEDCamRootRot.y = vEDCamRootRotStart.y;
							}
							vEDCamRootRot.z = 0;
							trCameraRoot.localRotation = Quaternion.Euler (vEDCamRootRot);
							//Debug.Log ("change rotation : "+vEDCamRootRot);
						}
					}
				}
				else
				{
					vEDInRotation = false;
				}

				xzPlane.Raycast (ray, out enter);

				if (!bInTouch)
				{
					bInTouch = true;
					//gr.enabled = false;
					ClickAfter = 0.0f;
					LongTabCheck = true;
					Dragged = false;
					mousePosPrev = mousePosStart = vTouch;

					if (Listner != null)
						Listner.OnTouchDown (ray);

					// Get Picking Position
					xzPlane.Raycast (ray, out enter);
					vPickStart = ray.GetPoint (enter) - trCameraRoot.position;
					vPickOld = vPickStart;
					vCamRootPosOld = trCameraRoot.position;

					if (InertiaUse)
					{
						InertiaActive = false;
						InertiaAge = 0.0f;
						InertiaSpeed = Vector3.zero;
					}
					//Debug.Log ("Update buildingSelected:"+((buildingSelected != null) ? buildingSelected.name : "none"));
				}
				else
				{

					if (Input.touchCount < 2)
					{

						//Mouse Button is in pressed & mouse move certain diatance
						if (Vector3.Distance (vTouch, mousePosStart) > fDragCheckMin)
						{

							// set drag flag on
							if (!Dragged)
							{
								Dragged = true;

								if (Listner != null) Listner.OnDragStart (ray);
							}

							if (!Input.GetKey (KeyCode.LeftShift))
							{
								// prevent camera shaking while touch pressed after drag.
								if (Vector3.Distance (vTouch, mousePosPrev) > fDragCheckMin)
								{

									if (Listner != null) Listner.OnDrag (ray);

									if (camPanningUse)
									{
										Vector3 vPickNew = ray.GetPoint (enter) - trCameraRoot.position;
										if (InertiaUse)
										{
											InertiaSpeed = 0.3f * InertiaSpeed + 0.7f * (vPickNew - vPickOld);
										}
										vCameraPanDir = vPickNew - vPickStart;
										//Debug.Log ("vCameraPanDir:"+vCameraPanDir);
										SetCameraPosition (vCamRootPosOld - vCameraPanDir);
										vPickOld = vPickNew;
									}
								}
							}
						}
						// Not Move
						else
						{

							if (Dragged)
							{

								if (Listner != null) Listner.OnDrag (ray);

								if (camPanningUse)
								{
									Vector3 vPickNew = ray.GetPoint (enter) - trCameraRoot.position;
									if (InertiaUse)
									{
										InertiaSpeed = 0.3f * InertiaSpeed + 0.7f * (vPickNew - vPickOld);
									}
									vPickOld = vPickNew;
								}
							}
							else
							{
								if (!Dragged)
								{
									ClickAfter += Time.deltaTime;

									if (LongTabCheck && (ClickAfter > LongTabPeriod))
									{
										LongTabCheck = false;
										if (Listner != null) Listner.OnLongPress (ray);
									}
								}
							}
						}

						mousePosPrev = vTouch;
					}
				}
			}
			else
			{

				if (vEDInRotation)
				{
					vEDInRotation = false;
				}

				//Release MouseButton
				if (bInTouch)
				{
					bInTouch = false;
					//gr.enabled = true;

					if (Listner != null) Listner.OnTouchUp (ray);

					// if in drag state
					if (Dragged)
					{

						if (InertiaUse && (InertiaSpeed.magnitude > fInertiaCheckMin))
							InertiaActive = true;

						if (Listner != null) Listner.OnDragEnd (ray);
					}
					else
					{
						if (Listner != null) Listner.OnTouch (ray);
					}
				}
			}

			//if (EventSystem.current && !EventSystem.current.IsPointerOverGameObject()) {
			//zoom with mouse wheel
			float fInputValue = Input.GetAxis ("Mouse ScrollWheel");
			if (Listner != null) Listner.OnMouseWheel (fInputValue);
			if (fInputValue != 0.0f)
			{

				if (!InZoom)
				{
					mousePosStart = vTouch;
					xzPlane.Raycast (ray, out enter);
					vPickStart = ray.GetPoint (enter) - trCameraRoot.position;
					vCamRootPosOld = trCameraRoot.position;
					InZoom = true;
				}

				float zoomDelta = fInputValue * zoomSpeed;
				SetCameraZoom (zoomCurrent - zoomDelta);
				UpjustPickPos (vTouch, vPickStart);
			}
			else
			{
				if (InZoom)
					InZoom = false;
			}
			//}

			// pinch zoom for mobile touch input
			if (Input.touchCount != 2)
				return;

			Touch touchZero = Input.GetTouch (0);
			Touch touchOne = Input.GetTouch (1);

			Vector3 vPinchDir = touchOne.position - touchZero.position;
			float fPinchDistance = vPinchDir.magnitude;
			vPinchDir.Normalize ();

			Vector3 vPinchTouchCenter = (touchOne.position - touchZero.position) * 0.5f + touchZero.position;
			ray = Camera.main.ScreenPointToRay (vPinchTouchCenter);
			xzPlane.Raycast (ray, out enter);
			Vector3 vPinchPickCenter = ray.GetPoint (enter) - trCameraRoot.position;

			if (!InPinch)
			{
				vTouchPosStart[0] = touchZero.position;
				vTouchPosStart[1] = touchOne.position;
				ZoomStart = zoomCurrent;
				vCamRootRotStart = trCameraRoot.localRotation.eulerAngles;
				vCamRootRot = vCamRootRotStart;
				vCamRootPosOld = trCameraRoot.position;

				vPinchDirStart = vPinchDir;
				fPinchDistanceStart = fPinchDistance;
				vPinchPickCenterStart = vPinchPickCenter;
				InPinch = true;
				camPanningUse = false;
			}
			else
			{

				Vector2 vTouchZeroDelta = touchZero.position - vTouchPosStart[0];
				Vector2 vTouchOneDelta = touchOne.position - vTouchPosStart[1];
				if ((vTouchZeroDelta.magnitude > 1.0f) && (vTouchOneDelta.magnitude > 1.0f))
				{

					float angleWithUp = Vector2.Angle (vTouchOneDelta, Vector2.up);
					float angleBetweenTouches = Vector2.Angle (vTouchZeroDelta, vTouchOneDelta);
					//Debug.Log ("angleWithUp:"+angleWithUp+"angleBetweenTouches:"+angleBetweenTouches);

					// check if pinch up
					if (((angleWithUp < 30.0f) || (150.0f < angleWithUp)) && (angleBetweenTouches < 50.0f))
					{
						pinchType = PinchType.Up;
					}
					else if ((angleBetweenTouches < 30.0f) || (150.0f < angleBetweenTouches))
					{
						pinchType = PinchType.Zoom;
					}
					else
					{
						pinchType = PinchType.Rotate;
					}
				}

				if (pinchType == PinchType.Up)
				{
					if (UseXRotation)
					{
						//rotate x
						float fDelta = touchZero.deltaPosition.y * Time.deltaTime * 10.0f;
						vCamRootRot.x = Mathf.Clamp (vCamRootRot.x - fDelta, 10.0f, 90.0f);
						trCameraRoot.localRotation = Quaternion.Euler (vCamRootRot);
						Debug.Log ("change rotation 1");
					}
				}
				else
				{
					//zoom
					float fDelta = fPinchDistance - fPinchDistanceStart;
					SetCameraZoom (ZoomStart - fDelta * zoomSpeed * 0.05f);

					if (UseYRotation)
					{
						// rotate y
						Vector3 v1 = vPinchDirStart;
						Vector3 v2 = vPinchDir;
						float dot = v1.x * v2.x + v1.y * v2.y; //# dot product
						float det = v1.x * v2.y - v1.y * v2.x; // # determinant
						float angle = Mathf.Atan2 (det, dot); //# atan2(y, x) or atan2(sin, cos)
						angle *= Mathf.Rad2Deg;

						vCamRootRot.y = vCamRootRotStart.y + angle;
						trCameraRoot.localRotation = Quaternion.Euler (vCamRootRot);
						Debug.Log ("change rotation 2");
					}
				}

				if ((pinchType == PinchType.Zoom) || (pinchType == PinchType.Rotate))
				{
					UpjustPickPos (vPinchTouchCenter, vPinchPickCenterStart);
				}
			}
		}

		public void UpjustPickPos (Vector3 vTouch, Vector3 vPickStart)
		{
			Ray ray = Camera.main.ScreenPointToRay (vTouch);
			float enter;
			xzPlane.Raycast (ray, out enter);
			Vector3 vPickNew = ray.GetPoint (enter) - trCameraRoot.position;
			vCameraPanDir = vPickNew - vPickStart;
			SetCameraPosition (vCamRootPosOld - vCameraPanDir);
		}

		public void SetCameraPosition (Vector3 vPos)
		{
			if (borderType == BorderType.Rect)
			{
				vPos.x = Mathf.Clamp (vPos.x, XMin, XMax);
				vPos.z = Mathf.Clamp (vPos.z, ZMin, ZMax);
			}
			else if (borderType == BorderType.Circle)
			{
				Vector3 vDir = vPos;
				vDir.y = 0.0f;
				float fLength = vDir.magnitude;
				if (fLength > CircleBorderRadius)
				{
					vDir.Normalize ();
					vPos = vDir * CircleBorderRadius;
				}
			}
			else { }

			trCameraRoot.position = vPos;
		}

		public void SetCameraZoom (float value)
		{
			zoomCurrent = Mathf.Clamp (value, zoomMin, zoomMax);
			if (camMain.orthographic)
			{
				camMain.orthographicSize = zoomCurrent;
			}
			else
			{
				trCamera.localPosition = new Vector3 (0, 0, -zoomCurrent);
			}
		}

		// Set Zoom value with 0.0 ~ 1.0 ratio of min to max value
		public void SetCameraZoomRatio (float fRatio)
		{
			float fRealValue = (zoomMax - zoomMin) * fRatio + zoomMin;
			SetCameraZoom (fRealValue);
		}

		void OnDrawGizmos ()
		{
			Gizmos.color = Color.red;

			if (borderType == BorderType.Rect)
			{
				Gizmos.DrawLine (new Vector3 (XMin, 0, ZMin), new Vector3 (XMax, 0, ZMin));
				Gizmos.DrawLine (new Vector3 (XMin, 0, ZMax), new Vector3 (XMax, 0, ZMax));
				Gizmos.DrawLine (new Vector3 (XMin, 0, ZMin), new Vector3 (XMin, 0, ZMax));
				Gizmos.DrawLine (new Vector3 (XMax, 0, ZMin), new Vector3 (XMax, 0, ZMax));
			}
			else if (borderType == BorderType.Circle)
			{
				Gizmos.DrawWireSphere (Vector3.zero, CircleBorderRadius);
			}
			else { }
		}
	}

}