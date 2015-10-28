/*
 * Copyright (c) 2014-2015, Mark Mullin
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 *  * Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 *
 *  * Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 *
 *  * Neither the name of Tango Tricorder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#region References

using System.Collections;
using Tango;
using UnityEngine;

#endregion

namespace Assets.Scripts
{
    /// <summary>
    /// This is a basic movement controller based on
    /// pose estimation returned from the Tango Service.
    /// </summary>
    public class PoseController : MonoBehaviour, ITangoPose
    {
        public enum TrackingTypes
        {
            None,
            Motion,
            ADF,
            Relocalized
        }

        private Matrix4x4 mDTuc;

        [HideInInspector]
        public int FrameCount;

        [HideInInspector]
        public float FrameDeltaTime;

        private float mPrevFrameTimestamp;
    

        [HideInInspector]
        public TangoEnums.TangoPoseStatusType Status;

        private Vector3 mTangoPosition;

        // Tango pose data.
        private Quaternion mTangoRotation;

        // Tango pose data for debug logging and transform update.
        [HideInInspector]
        public string TangoServiceVersionName = string.Empty;

        // We use couple of matrix transformation to convert the pose from Tango coordinate
        // frame to Unity coordinate frame.
        // The full equation is:
        //     Matrix4x4 uwTuc = m_uwTss * ssTd * m_dTuc;
        //
        // uwTuc: Unity camera with respect to Unity world, this is the desired matrix.
        // m_uwTss: Constant matrix converting start of service frame to Unity world frame.
        // ssTd: Device frame with repect to start of service frame, this matrix denotes the
        //       pose transform we get from pose callback.
        // m_dTuc: Constant matrix converting Unity world frame frame to device frame.
        //
        // Please see the coordinate system section online for more information:
        //     https://developers.google.com/project-tango/overview/coordinate-systems
        private Matrix4x4 mUwTss;

        private readonly Quaternion mAttitudeBaseline = Quaternion.identity;
        public bool UsePosition = true;
        public Quaternion CorrectedRotation { get { return transform.rotation * mAttitudeBaseline; } }
        public Quaternion Rotation { get { return transform.rotation; } }

        public Vector3 AbsoluteForward
        {
            get
            {
                return transform.forward;
            }
        }

        public Transform Transform { get { return transform; } }

        public TangoApplication TangoApplication { get; private set; }

        /// <summary>
        /// Handle the callback sent by the Tango Service
        /// when a new pose is sampled.
        /// DO NOT USE THE UNITY API FROM INSIDE THIS FUNCTION!
        /// </summary>
        /// <param name="pose">Current tango pose information</param>
        public void OnTangoPoseAvailable(TangoPoseData pose)
        {
            // Get out of here if the pose is null
            if (pose == null)
            {
                Debug.Log("TangoPoseDate is null.");
                return;
            }

            // The callback pose is for device with respect to start of service pose.
            if (pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE &&
                pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE)
            {
                // Update the stats for the pose for the debug text
                Status = pose.status_code;
                if (pose.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
                {
                    // Create new Quaternion and Vec3 from the pose data received in the event.
                    mTangoPosition = new Vector3((float)pose.translation[0],
                        (float)pose.translation[1],
                        (float)pose.translation[2]);

                    mTangoRotation = new Quaternion((float)pose.orientation[0],
                        (float)pose.orientation[1],
                        (float)pose.orientation[2],
                        (float)pose.orientation[3]);
                    // Reset the current status frame count if the status code changed.
                    if (pose.status_code != Status)
                    {
                        FrameCount = 0;
                    }
                    FrameCount++;

                    // Compute delta frame timestamp.
                    FrameDeltaTime = (float)pose.timestamp - mPrevFrameTimestamp;
                    mPrevFrameTimestamp = (float)pose.timestamp;

                    // Construct the start of service with respect to device matrix from the pose.
                    Matrix4x4 ssTd = Matrix4x4.TRS(mTangoPosition, mTangoRotation, Vector3.one);

                    // Converting from Tango coordinate frame to Unity coodinate frame.
                    Matrix4x4 uwTuc = mUwTss * ssTd * mDTuc;
                    // Extract new local position
                    if (UsePosition)
                        transform.localPosition = uwTuc.GetColumn(3);

                    // Extract new local rotation
                    transform.localRotation = Quaternion.LookRotation(uwTuc.GetColumn(2), uwTuc.GetColumn(1));
                }
                else // if the current pose is not valid we set the pose to identity
                {
                    mTangoPosition = Vector3.zero;
                    mTangoRotation = Quaternion.identity;
                }
            }
        }

        /// <summary>
        /// Initialize the controller.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        private void Awake()
        {
            // Constant matrix converting start of service frame to Unity world frame.
            mUwTss = new Matrix4x4();
            mUwTss.SetColumn(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
            mUwTss.SetColumn(1, new Vector4(0.0f, 0.0f, 1.0f, 0.0f));
            mUwTss.SetColumn(2, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
            mUwTss.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            // Constant matrix converting Unity world frame frame to device frame.
            mDTuc = new Matrix4x4();
            mDTuc.SetColumn(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
            mDTuc.SetColumn(1, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
            mDTuc.SetColumn(2, new Vector4(0.0f, 0.0f, -1.0f, 0.0f));
            mDTuc.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            FrameDeltaTime = -1.0f;
            mPrevFrameTimestamp = -1.0f;
            FrameCount = -1;
            Status = TangoEnums.TangoPoseStatusType.NA;
            mTangoRotation = Quaternion.identity;
            mTangoPosition = Vector3.zero;
        }

        /// <summary>
        /// Start this instance.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        private void Start()
        {
            TangoApplication = FindObjectOfType<TangoApplication>();

            if (TangoApplication != null)
            {
                if (AndroidHelper.IsTangoCorePresent())
                {
                    // Request Tango permissions
                    TangoApplication.RegisterPermissionsCallback(_OnTangoApplicationPermissionsEvent);
                    TangoApplication.RequestNecessaryPermissionsAndConnect();
                    TangoApplication.Register(this);
                    TangoServiceVersionName = TangoApplication.GetTangoServiceVersion();
                }
                else
                {
                    // If no Tango Core is present let's tell the user to install it!
                    StartCoroutine(_InformUserNoTangoCore());
                }
            }
            else
            {
                Debug.Log("No Tango Manager found in scene.");
            }
        }

        /// <summary>
        /// Informs the user that they should install Tango Core via Android toast.
        /// </summary>
        private IEnumerator _InformUserNoTangoCore()
        {
            AndroidHelper.ShowAndroidToastMessage("Please install Tango Core", false);
            yield return new WaitForSeconds(2.0f);
            Application.Quit();
        }

        /// <summary>
        /// Apply any needed changes to the pose.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        private void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if(m_tangoApplication != null)
            {
                m_tangoApplication.Shutdown();
            }

            // This is a temporary fix for a lifecycle issue where calling
            // Application.Quit() here, and restarting the application immediately,
            // results in a hard crash.
            AndroidHelper.AndroidQuit();
        }
#else
            Vector3 tempPosition = transform.position;
            Quaternion tempRotation = transform.rotation;
            PoseProvider.GetMouseEmulation(ref tempPosition, ref tempRotation);
            transform.rotation = tempRotation;
            if (UsePosition)
                transform.position = tempPosition;

#endif
        }

        /// <summary>
        /// Unity callback when application is paused.
        /// </summary>
        ///     
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnApplicationPause(bool pauseStatus)
        {
            FrameDeltaTime = -1.0f;
            mPrevFrameTimestamp = -1.0f;
            FrameCount = -1;
            Status = TangoEnums.TangoPoseStatusType.NA;
            mTangoRotation = Quaternion.identity;
            mTangoPosition = Vector3.zero;
        }

        public void CancelRotation()
        {
            //mAttitudeBaseline = Quaternion.Inverse(transform.localRotation);
        }

        public void ZeroPosition()
        {
            transform.position = Vector3.zero;
        }

        public void ZeroPositionRotation()
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }

        private void _OnTangoApplicationPermissionsEvent(bool permissionsGranted)
        {
            if (permissionsGranted)
            {
                TangoApplication.InitApplication();
                TangoApplication.InitProviders(string.Empty);
                TangoApplication.ConnectToService();
            }
            else
            {
                AndroidHelper.ShowAndroidToastMessage("Motion Tracking Permissions Needed", true);
            }
        }
    }
}