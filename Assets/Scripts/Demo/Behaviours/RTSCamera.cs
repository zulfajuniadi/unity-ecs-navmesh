#region

using UnityEngine;

#endregion

namespace Demo.Behaviours
{
    [RequireComponent (typeof (Camera))]
    [AddComponentMenu ("RTS Camera")]
    public class RTSCamera : MonoBehaviour
    {
        private Transform m_Transform; //camera tranform
        public bool useFixedUpdate; //use FixedUpdate() or Update()

        #region Foldouts

#if UNITY_EDITOR

        public int lastTab;

        public bool movementSettingsFoldout;
        public bool zoomingSettingsFoldout;
        public bool rotationSettingsFoldout;
        public bool heightSettingsFoldout;
        public bool mapLimitSettingsFoldout;
        public bool targetingSettingsFoldout;
        public bool inputSettingsFoldout;

#endif

        #endregion

        #region Movement

        public bool game2D;
        public bool forMobile;
        public float keyboardMovementSpeed = 5f; //speed with keyboard movement
        public float screenEdgeMovementSpeed = 3f; //spee with screen edge movement
        public float followingSpeed = 5f; //speed when following a target
        public float rotationSped = 3f;
        public float panningSpeedPC = 10f;
        public float panningSpeedMobile = 1f;
        public float mouseRotationSpeed = 10f;

        #endregion

        #region Height

        public bool autoHeight = true;
        public LayerMask groundMask = -1; //layermask of ground or other objects that affect height

        public float maxHeight = 10f; //maximal height
        public float minHeight = 15f; //minimnal height
        public float heightDampening = 5f;
        public float keyboardZoomingSensitivity = 2f;
        public float scrollWheelZoomingSensitivity = 25f;

        private float zoomPos; //value in range (0, 1) used as t in Matf.Lerp

        #endregion

        #region MapLimits

        public bool limitMap = true;
        public Vector2 limitX = new Vector2 (0, 100); //x limit of map
        public Vector2 limitY = new Vector2 (0, 100); //z limit of map

        #endregion

        #region Targeting

        public Transform targetFollow; //target to follow
        public Vector3 targetOffset;

        /// <summary>
        ///     are we following target
        /// </summary>
        public bool FollowingTarget
        {
            get => targetFollow != null;
        }

        #endregion

        #region Input

        public bool useScreenEdgeInput = true;
        public float screenEdgeBorder = 25f;

        public bool useKeyboardInput = true;
        public string horizontalAxis = "Horizontal";
        public string verticalAxis = "Vertical";

        public bool usePanning = true;
        public KeyCode panningKey = KeyCode.Mouse2;

        public bool useKeyboardZooming = true;
        public KeyCode zoomInKey = KeyCode.E;
        public KeyCode zoomOutKey = KeyCode.Q;

        public bool useScrollwheelZooming = true;
        public string zoomingAxis = "Mouse ScrollWheel";

        public bool useKeyboardRotation = true;
        public KeyCode rotateRightKey = KeyCode.X;
        public KeyCode rotateLeftKey = KeyCode.Z;

        public bool useMouseRotation = true;
        public KeyCode mouseRotationKey = KeyCode.Mouse1;

        private Vector2 KeyboardInput
        {
            get => useKeyboardInput ? new Vector2 (Input.GetAxis (horizontalAxis), Input.GetAxis (verticalAxis)) : Vector2.zero;
        }

        private Vector2 MouseInput
        {
            get => Input.mousePosition;
        }

        private float ScrollWheel
        {
            get => Input.GetAxis (zoomingAxis);
        }

        private Vector2 MouseAxis
        {
            get => new Vector2 (Input.GetAxis ("Mouse X"), Input.GetAxis ("Mouse Y"));
        }

        private int ZoomDirection
        {
            get
            {
                var zoomIn = Input.GetKey (zoomInKey);
                var zoomOut = Input.GetKey (zoomOutKey);
                if (zoomIn && zoomOut)
                {
                    return 0;
                }

                if (!zoomIn && zoomOut)
                {
                    return 1;
                }

                if (zoomIn && !zoomOut)
                {
                    return -1;
                }

                return 0;
            }
        }

        private int RotationDirection
        {
            get
            {
                var rotateRight = Input.GetKey (rotateRightKey);
                var rotateLeft = Input.GetKey (rotateLeftKey);
                if (rotateLeft && rotateRight)
                {
                    return 0;
                }

                if (rotateLeft && !rotateRight)
                {
                    return -1;
                }

                if (!rotateLeft && rotateRight)
                {
                    return 1;
                }

                return 0;
            }
        }

        #endregion

        #region Unity_Methods

        private void Start ()
        {
            m_Transform = transform;

            if (game2D)
            {
                transform.rotation = Quaternion.identity;
            }

#if UNITY_STANDALONE || UNITY_EDITOR
            forMobile = false;
#else
            forMobile = true;
#endif
        }

        private void Update ()
        {
            if (!useFixedUpdate)
            {
                CameraUpdate ();
            }
        }

        private void FixedUpdate ()
        {
            if (useFixedUpdate)
            {
                CameraUpdate ();
            }
        }

        #endregion

        #region RTSCamera_Methods

        /// <summary>
        ///     update camera movement and rotation
        /// </summary>
        private void CameraUpdate ()
        {
            if (FollowingTarget)
            {
                FollowTarget ();
            }
            else
            {
                Move ();
            }

            HeightCalculation ();
            Rotation ();
            LimitPosition ();
        }

        /// <summary>
        ///     move camera with keyboard or with screen edge
        /// </summary>
        private void Move ()
        {
            if (useKeyboardInput)
            {
                Vector3 desiredMove;

                if (!game2D)
                {
                    desiredMove = new Vector3 (KeyboardInput.x, 0, KeyboardInput.y);
                }
                else
                {
                    desiredMove = new Vector3 (KeyboardInput.x, KeyboardInput.y, 0);
                }

                desiredMove *= keyboardMovementSpeed;
                desiredMove *= Time.deltaTime;
                desiredMove = Quaternion.Euler (new Vector3 (0f, transform.eulerAngles.y, 0f)) * desiredMove;
                desiredMove = m_Transform.InverseTransformDirection (desiredMove);

                m_Transform.Translate (desiredMove, Space.Self);
            }

            if (useScreenEdgeInput)
            {
                var desiredMove = new Vector3 ();

                var leftRect = new Rect (0, 0, screenEdgeBorder, Screen.height);
                var rightRect = new Rect (Screen.width - screenEdgeBorder, 0, screenEdgeBorder, Screen.height);
                var upRect = new Rect (0, Screen.height - screenEdgeBorder, Screen.width, screenEdgeBorder);
                var downRect = new Rect (0, 0, Screen.width, screenEdgeBorder);

                desiredMove.x = leftRect.Contains (MouseInput) ? -1 : rightRect.Contains (MouseInput) ? 1 : 0;

                if (!game2D)
                {
                    desiredMove.z = upRect.Contains (MouseInput) ? 1 : downRect.Contains (MouseInput) ? -1 : 0;
                }
                else
                {
                    desiredMove.y = upRect.Contains (MouseInput) ? 1 : downRect.Contains (MouseInput) ? -1 : 0;
                }

                desiredMove *= screenEdgeMovementSpeed;
                desiredMove *= Time.deltaTime;
                desiredMove = Quaternion.Euler (new Vector3 (0f, transform.eulerAngles.y, 0f)) * desiredMove;
                desiredMove = m_Transform.InverseTransformDirection (desiredMove);

                m_Transform.Translate (desiredMove, Space.Self);
            }

            // For PC
            if (usePanning && Input.GetKey (panningKey) && MouseAxis != Vector2.zero && !forMobile)
            {
                Vector3 desiredMove;

                if (!game2D)
                {
                    desiredMove = new Vector3 (-MouseAxis.x, 0, -MouseAxis.y);
                }
                else
                {
                    desiredMove = new Vector3 (-MouseAxis.x, -MouseAxis.y, 0);
                }

                desiredMove *= panningSpeedPC;
                desiredMove *= Time.deltaTime;
                desiredMove = Quaternion.Euler (new Vector3 (0f, transform.eulerAngles.y, 0f)) * desiredMove;
                desiredMove = m_Transform.InverseTransformDirection (desiredMove);

                m_Transform.Translate (desiredMove, Space.Self);
            }

            // For Mobile
            if (Input.touchCount > 0 && Input.GetTouch (0).phase == TouchPhase.Moved)
            {
                var touchDeltaPosition = Input.GetTouch (0).deltaPosition;
                transform.Translate (-touchDeltaPosition.x * panningSpeedMobile * Time.deltaTime, -touchDeltaPosition.y * panningSpeedMobile * Time.deltaTime, 0);
            }
        }

        /// <summary>
        ///     calcualte height
        /// </summary>
        private void HeightCalculation ()
        {
            var distanceToGround = DistanceToGround ();
            if (useScrollwheelZooming)
            {
                zoomPos += ScrollWheel * Time.deltaTime * scrollWheelZoomingSensitivity;
            }

            if (useKeyboardZooming)
            {
                zoomPos += ZoomDirection * Time.deltaTime * keyboardZoomingSensitivity;
            }

            zoomPos = Mathf.Clamp01 (zoomPos);

            var targetHeight = Mathf.Lerp (minHeight, maxHeight, zoomPos);
            float difference = 0;

            if (distanceToGround != targetHeight)
            {
                difference = targetHeight - distanceToGround;
            }

            if (!game2D)
            {
                m_Transform.position = Vector3.Lerp (m_Transform.position,
                    new Vector3 (m_Transform.position.x, targetHeight, m_Transform.position.z),
                    Time.deltaTime * heightDampening);
            }
        }

        /// <summary>
        ///     rotate camera
        /// </summary>
        private void Rotation ()
        {
            if (useKeyboardRotation)
            {
                transform.Rotate (Vector3.up, RotationDirection * Time.deltaTime * rotationSped, Space.World);
            }

            if (useMouseRotation && Input.GetKey (mouseRotationKey))
            {
                m_Transform.Rotate (Vector3.up, -MouseAxis.x * Time.deltaTime * mouseRotationSpeed, Space.World);
            }
        }

        /// <summary>
        ///     follow targetif target != null
        /// </summary>
        private void FollowTarget ()
        {
            var targetPos = new Vector3 (targetFollow.position.x, m_Transform.position.y, targetFollow.position.z) + targetOffset;
            m_Transform.position = Vector3.MoveTowards (m_Transform.position, targetPos, Time.deltaTime * followingSpeed);
        }

        /// <summary>
        ///     limit camera position
        /// </summary>
        private void LimitPosition ()
        {
            if (!limitMap)
            {
                return;
            }

            m_Transform.position = new Vector3 (Mathf.Clamp (m_Transform.position.x, limitX.x, limitX.y),
                m_Transform.position.y,
                Mathf.Clamp (m_Transform.position.z, limitY.x, limitY.y));
        }

        /// <summary>
        ///     set the target
        /// </summary>
        /// <param name="target"></param>
        public void SetTarget (Transform target)
        {
            targetFollow = target;
        }

        /// <summary>
        ///     reset the target (target is set to null)
        /// </summary>
        public void ResetTarget ()
        {
            targetFollow = null;
        }

        /// <summary>
        ///     calculate distance to ground
        /// </summary>
        /// <returns></returns>
        private float DistanceToGround ()
        {
            var ray = new Ray (m_Transform.position, Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast (ray, out hit, groundMask.value))
            {
                return (hit.point - m_Transform.position).magnitude;
            }

            return 0f;
        }

        #endregion
    }
}