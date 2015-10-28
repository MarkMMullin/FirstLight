#region References

using UnityEngine;

#endregion

namespace Assets.Scripts
{
    public class FlyCam : MonoBehaviour
    {
        /*
        EXTENDED FLYCAM
            Desi Quintans (CowfaceGames.com), 17 August 2012.
            Based on FlyThrough.js by Slin (http://wiki.unity3d.com/index.php/FlyThrough), 17 May 2011.

        LICENSE
            Free as in speech, and free as in beer.

        FEATURES
            WASD/Arrows:    Movement
                      Q:    Climb
                      E:    Drop
                          Shift:    Move faster
                        Control:    Move slower
                            End:    Toggle cursor locking to screen (you can also press Ctrl+P to toggle play mode on and off).
        */

        #region Public Fields

        public float cameraSensitivity = 90;
        public float climbSpeed = 4;
        public float fastMoveFactor = 3;
        public float normalMoveSpeed = 10;

        public float slowMoveFactor = 0.25f;

        #endregion Public Fields

        #region Private Fields

        private float rotationX;
        private float rotationY;

        #endregion Private Fields

        #region Private Methods

        private void Start()
        {
            //Screen.lockCursor = true;
        }

        private void Update()
        {
            rotationX += Input.GetAxis("Mouse X") * cameraSensitivity * Time.deltaTime;
            rotationY += Input.GetAxis("Mouse Y") * cameraSensitivity * Time.deltaTime;
            rotationY = Mathf.Clamp(rotationY, -90, 90);

            transform.localRotation = Quaternion.AngleAxis(rotationX, Vector3.up);
            transform.localRotation *= Quaternion.AngleAxis(rotationY, Vector3.left);

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                transform.position += transform.forward * (normalMoveSpeed * fastMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
                transform.position += transform.right * (normalMoveSpeed * fastMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
            }
            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                transform.position += transform.forward * (normalMoveSpeed * slowMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
                transform.position += transform.right * (normalMoveSpeed * slowMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
            }
            else
            {
                transform.position += transform.forward * normalMoveSpeed * Input.GetAxis("Vertical") * Time.deltaTime;
                transform.position += transform.right * normalMoveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.Q)) { transform.position += transform.up * climbSpeed * Time.deltaTime; }
            if (Input.GetKey(KeyCode.E)) { transform.position -= transform.up * climbSpeed * Time.deltaTime; }

            if (Input.GetKeyDown(KeyCode.End))
            {
                Screen.lockCursor = (Screen.lockCursor == false) ? true : false;
            }
        }

        #endregion Private Methods
    }
}