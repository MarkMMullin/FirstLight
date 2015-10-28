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

using System;
using System.Collections.Generic;
using System.Linq;
using Tango;
using UnityEngine;
using UnityEngine.UI;

#endregion

namespace Assets.Scripts
{
    /// <summary>
    /// Primary System simulator - this drives the simulation
    /// </summary>
    public class SolarSystem : MonoBehaviour
    {
        #region Public Fields

        /// <summary>
        /// Handy singleton reference
        /// </summary>
        public static SolarSystem SSingleton;

        public Quaternion CraftAttitude = Quaternion.identity;
        public Vector3 CraftLocation = Vector3.zero;
        public float EarthAtmosphereMult = 1.1f;
        public float EarthAtmosphereSpin = 1.0f;
        public bool EarthAtmosphereVisible = true;
        /// <summary>
        /// Used to make everything go faster and slower
        /// </summary>
        public float TimeBase = 1;

        #endregion Public Fields

        #region Private Fields

        private readonly Dictionary<string, OrbitalSystem> mSolarBodies = new Dictionary<string, OrbitalSystem>();
        private int mADFIndex;

        private bool mADFInitialized;

        /// <summary>
        /// Voice controlled command module
        /// </summary>
        private AutoPilot mAutopilot;
        private Credits mCredits;
        private Text mCreditsDisplay;

        private bool mInitialCameraJumpPerformed;
        private bool mIsListening;

        private bool mIsStereoView = true;
        private Text mListeningStatus;
        private GameObject mMonoCamera;
        private Text mSelectedADF;
        private int mSnapCounter = 1;
        private bool mSnapRestoreToStereo;
        /// <summary>
        /// Internal reference to the individual using the software - they're what's in motion
        /// </summary>
        private GameObject mSpacecraft;

        private int mSpeechDelay;
        private GameObject mStereoCameraLeft;
        private GameObject mStereoCameraRight;
        private int mTakesnapshot;
        private Text mViewText;

        #endregion Private Fields

        #region Public Properties

        public Text AutopilotStatus { get; private set; }

        public GameObject[] OrbitalElements { get; private set; }

        public List<OrbitalSystem> OrbitalSystems
        {
            get { return mSolarBodies.Values.ToList(); }
        }

        /// <summary>
        /// Connection to Tango, and hence to attitude control
        /// Future credit to someone adding translational control
        /// nb. Translational control at a 1:1 ratio is probably not gonna work
        /// </summary>
        public PoseController PoseController { get; private set; }

        /// <summary>
        /// Enclosing sphere upon which have been painted some stars
        /// </summary>
        public GameObject StarSphere { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public OrbitalSystem GetOrbitalSystem(string theName)
        {
            OrbitalSystem os;
            mSolarBodies.TryGetValue(theName, out os);
            return os;
        }

        public void NextADF()
        {
            if (!mADFInitialized)
            {
                initializeADFGui();
                mADFInitialized = true;
            }
            mADFIndex++;
            UUID_list l = PoseProvider.GetCachedADFList();
            if (mADFIndex >= l.Count)
                mADFIndex = 0;
            UUIDUnityHolder adfId = l.GetADFAtIndex(mADFIndex);

            Dictionary<string, string> mdInfo = adfId.uuidMetaData.GetMetaDataKeyValues();
            mSelectedADF.text = mdInfo["name"];
            mSelectedADF.color = Color.red;
        }

        public void NormalSpeed()
        {
            TimeBase = 1;
        }

        public void PickADF()
        {
            UUID_list l = PoseProvider.GetCachedADFList();
            if (mADFIndex >= l.Count)
                mADFIndex = 0;
            PoseController.TangoApplication.InitProviders(l.GetADFAtIndex(mADFIndex).GetStringDataUUID());
            mSelectedADF.color = Color.green;
        }

        public void Prog1()
        {
            mAutopilot.Velocity *= 1.1f;
        }

        public void Prog2()
        {
            mAutopilot.Velocity *= 0.9f;
        }

        public void Prog3()
        {
            mAutopilot.OrbitElevation *= 1.1f;
        }

        public void Prog4()
        {
            mAutopilot.OrbitElevation *= 0.9f;
        }

        public void Prog5()
        {
            mAutopilot.HandleOrbitalTransition(null, false);
        }

        public void RunCredits()
        {
            mCredits = new Credits();
        }

        public void SetViewToStereo(bool? viewState)
        {
            if (viewState.HasValue)
                mIsStereoView = viewState.Value;
            else
                mIsStereoView = !mIsStereoView;

            if (mIsStereoView)
            {
                mStereoCameraLeft.SetActive(true);
                mStereoCameraRight.SetActive(true);
                mMonoCamera.SetActive(false);
                mIsStereoView = true;
                mViewText.text = "Handheld";
            }
            else
            {
                mStereoCameraLeft.SetActive(false);
                mStereoCameraRight.SetActive(false);
                mMonoCamera.SetActive(true);
                mIsStereoView = false;
                mViewText.text = "Headset";
            }
        }

        public void SwitchView()
        {
            SetViewToStereo(null);
        }

        public void TakeSnapshot()
        {
            mSnapRestoreToStereo = mIsStereoView;
            SetViewToStereo(false);
            mTakesnapshot = 2;
        }

        public void TestDeOrbit()
        {
            mAutopilot.HandleOrbitalTransition(null, false);
        }

        public void TestFlight()
        {
            mAutopilot.FlyTo(mSolarBodies["mercury"]);
        }

        public void TestLookAt()
        {
            TimeBase = 8;
        }

        public void TestOrbit()
        {
            mAutopilot.HandleOrbitalTransition(mSolarBodies["earth"], true);
            //GameObject saturnRing = GameObject.Find("SaturnRing");
            //mSpacecraft.transform.position = saturnRing.transform.position + new Vector3(0,500,0);
        }

        public void TestWhat()
        {
            mAutopilot.WhatsThat();
        }

        #endregion Public Methods

        #region Private Methods

        private void initializeADFGui()
        {
            int numItems = PoseProvider.RefreshADFList();
            mADFIndex = 0;
            if (numItems != 0)
            {
                UUID_list l = PoseProvider.GetCachedADFList();
                mSelectedADF.text = l.GetADFAtIndex(mADFIndex).GetStringDataUUID();
                mSelectedADF.color = Color.red;
            }
            else
            {
                mSelectedADF.text = "NO ADFS";
                mSelectedADF.color = Color.red;
            }
        }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        private void LateUpdate()
        {
            if (mTakesnapshot > 0)
            {
                --mTakesnapshot;
                if (mTakesnapshot > 0)
                    return;
                string filename;
                if (Application.platform == RuntimePlatform.Android)
                {
                    filename = "FirstLight" + mSnapCounter++ + ".png";
                }
                else
                {
                    filename = "C:/temp/FirstLight-" + mSnapCounter++ + ".png";
                }
                Application.CaptureScreenshot(filename);
                mTakesnapshot = -10;
            }
            else if (mTakesnapshot < 0)
            {
                ++mTakesnapshot;
                if (mTakesnapshot == 0)
                    SetViewToStereo(mSnapRestoreToStereo);
            }
        }

        // Use this for initialization
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        private void Start()
        {
            mMonoCamera = GameObject.Find("Camera_mono");
            mMonoCamera.SetActive(false);
            mStereoCameraLeft = GameObject.Find("Camera_left");
            mStereoCameraRight = GameObject.Find("Camera_right");
            SSingleton = this;
            StarSphere = GameObject.Find("StarSphere");
            GameObject controller = GameObject.Find("Dive_Camera");
            PoseController = controller.GetComponent<PoseController>();
            StarSphere.transform.localScale = new Vector3(9990, 9990, 9990);

            OrbitalSystem.SetOrbitRange(1, 57910000.0f, 5913520000.0f); // planetary orbits
            OrbitalSystem.SetOrbitRange(2, 9000, 12952000); // lunar orbits
            OrbitalSystem.SetMassRange(1, 1.27E+22f, 1.9E+27f); // pluto to jupiter
            OrbitalSystem.SetMassRange(2, 1.80e15f, 1.48e23f); // deimos to ganymede
            OrbitalSystem.SetRadiusRange(1, 1137, 71492); // pluto to jupiter
            OrbitalSystem.SetRadiusRange(2, 6, 2631); // deimos to ganymede

            mSolarBodies["sun"] = new OrbitalSystem(0, "SunBody", null, 1.989e30f, 0.0f, 695000f, 0.0f, 0.0f, 1.17f, 28 * 9.78f, 1.0f,
                GameObject.Find("Sun"));
            mSolarBodies["mercury"] = new OrbitalSystem(1, "MercuryBody", mSolarBodies["sun"], 3.30000E+23f, 57910000f, 2.44000E+03f, 7f, 87.9f, 1407.6f,
                3.7f, 6.59026E-01f, GameObject.Find("Mercury"));
            mSolarBodies["venus"] = new OrbitalSystem(1, "VenusBody", mSolarBodies["sun"], 9.869E+23f, 108200000, 6051.8f, 3.39f, 224.701f, 5832.5f, 8.87f,
                0.987144169f, GameObject.Find("Venus"));
            mSolarBodies["earth"] = new OrbitalSystem(1, "EarthBody", mSolarBodies["sun"], 5.972E+24f, 179600000, 6378.15f, 9, 365.256f, -23.9345f, 9.78f,
                0.96712689f, GameObject.Find("Earth"));
            //earth->SetAtmosphere("clouds",-1,0.25,0.01 * DEGREEMULT,0,sm_textures[EarthClouds]);
            //CDIB* specChannel = new CDIB(theApp.GetFilePath() + "\\textures\\" +  RESPATH + "EarthBody-seas.bmp");
            //earth->SetHighlightMap(specChannel);

            mSolarBodies["mars"] = new OrbitalSystem(1, "MarsBody", mSolarBodies["sun"], 6.4219E+24f, 427940000, 3397, 1.85f, 686.980f, 24.6229f, 3.69f,
                0.82905297f, GameObject.Find("Mars"));
            mSolarBodies["jupiter"] = new OrbitalSystem(1, "Jupiter", mSolarBodies["sun"], 1.9E+27f, 778330000, 71492, 1.305f, 4332.589f, 9.925f, 23.12f,
                0.907598039f, GameObject.Find("Jupiter"));
            mSolarBodies["saturn"] = new OrbitalSystem(1, "Saturn", mSolarBodies["sun"], 5.68E+26f, 1429400000, 60268, 2.484f, 10759.22f, 10.500f, 8.96f,
                0.894583112f, GameObject.Find("Saturn"));
            //saturn->GenerateRing(96,41,80,true,sm_textures[SaturnRing]);

            mSolarBodies["uranus"] = new OrbitalSystem(1, "Uranus", mSolarBodies["sun"], 8.683E+25f, 2870990000, 25559, 0.7f, 30685.4f, 17.24f, 8.69f,
                0.909756422f, GameObject.Find("Uranus"));
            mSolarBodies["neptune"] = new OrbitalSystem(1, "Neptune", mSolarBodies["sun"], 1.0247E+26f, 4504000000, 24766, 1.7f, 60189, 16.11f, 11,
                0.982977597f, GameObject.Find("Neptune"));
            mSolarBodies["pluto"] = new OrbitalSystem(1, "Pluto", mSolarBodies["sun"], 1.27E+22f, 5913520000, 1137, 17.4f, 90465, 153.2928f, 0.66f,
                0.602313987f, GameObject.Find("Pluto"));

            //OrbitalSystem *moon = new OrbitalSystem(2,"MoonBody",earth,7.35e22,384000,1738,5.145, 27.322,655.728,1.62,sm_textures[Moon]);
            mSolarBodies["moon"] = new OrbitalSystem(2, "MoonBody", mSolarBodies["earth"], 7.35e22f, 384000.0f / 4.0f, 1738, 5.145f, -27.322f, 655.728f, 1.62f,
                0.98f, GameObject.Find("Moon"));

            mSolarBodies["phobos"] = new OrbitalSystem(2, "Phobos", mSolarBodies["mars"], 1.08e16f, 7000, 800, 1.08f, 0.31891f, 0.31891f, 0.01f, 0.98f,
                GameObject.Find("Phobos"));
            mSolarBodies["deimos"] = new OrbitalSystem(2, "Deimos", mSolarBodies["mars"], 1.80e15f, 18000, 400, 1.79f, 1.26244f, 0.07f, 0.01f, 0.98f,
                GameObject.Find("Deimos"));

            //OrbitalSystem amalthea = new OrbitalSystem(2,"Amalthea",jupiter,7.17e18,181000,98,1,0.498179,0.07,0.01,0.98,MercuryBody);
            mSolarBodies["io"] = new OrbitalSystem(2, "Io", mSolarBodies["jupiter"], 8.94e22f, 422000, 1815, 2, 1.769138f, 0.07f, 0.183f * 9.78f, 0.98f,
                GameObject.Find("Io"));
            mSolarBodies["europa"] = new OrbitalSystem(2, "Europa", mSolarBodies["jupiter"], 4.80e22f, 671000, 1569, 3, 3.551810f, 0.07f, 0.145f * 9.78f,
                0.98f, GameObject.Find("Europa"));
            mSolarBodies["ganymede"] = new OrbitalSystem(2, "Ganymede", mSolarBodies["jupiter"], 1.48e23f, 1070000, 2631, 4, 7.154553f, 0.07f, 0.145f * 9.78f,
                0.98f, GameObject.Find("Ganymede"));
            mSolarBodies["callisto"] = new OrbitalSystem(2, "Callisto", mSolarBodies["jupiter"], 1.08e23f, 1883000, 2400, 5, 16.689018f, 0.07f, 0.127f * 9.78f,
                0.98f, GameObject.Find("Callisto"));

            mSolarBodies["mimas"] = new OrbitalSystem(2, "mimas", mSolarBodies["saturn"], 3.80e19f, 186000, 196, 1, 0.9424218f, 0.07f, 0.008f * 9.78f, 0.98f,
                GameObject.Find("Mimas"));
            mSolarBodies["enceladus"] = new OrbitalSystem(2, "enceladus", mSolarBodies["saturn"], 8.40e19f, 238000, 260, 19, 1.370218f, 0.07f, 0.008f * 9.78f,
                0.98f, GameObject.Find("Enceladus"));
            mSolarBodies["tethys"] = new OrbitalSystem(2, "tethys", mSolarBodies["saturn"], 7.55e20f, 295000, 530, 3, 1.887802f, 0.07f, 0.018f * 9.78f, 0.98f,
                GameObject.Find("Tethys"));
            mSolarBodies["dione"] = new OrbitalSystem(2, "dione", mSolarBodies["saturn"], 1.05e21f, 377000, 560, 4, 2.736915f, 0.07f, 0.223f, 0.98f,
                GameObject.Find("Dione"));
            mSolarBodies["rhea"] = new OrbitalSystem(2, "rhea", mSolarBodies["saturn"], 2.49e21f, 527000, 765, 5, 4.517500f, 0.07f, 0.029f * 9.78f, 0.98f,
                GameObject.Find("Rhea"));
            mSolarBodies["titan"] = new OrbitalSystem(2, "titan", mSolarBodies["saturn"], 1.35e23f, 1222000, 2575, 6, 15.945421f, 0.07f, 9.78f * (1.0f / 7.0f),
                0.98f, GameObject.Find("Titan"));
            //OrbitalSystem hyperion = new OrbitalSystem(2,"hyperion",saturn,1.77e19,1481000,143,7,21.276609,0.07,0.107 ,0.98,MercuryBody);
            mSolarBodies["iapetus"] = new OrbitalSystem(2, "iapetus", mSolarBodies["saturn"], 1.88e21f, 3561000, 730, 8, 463, 0.07f, 0.107f, 0.98f,
                GameObject.Find("Iapetus"));
            //OrbitalSystem *phobe = new OrbitalSystem(2,"phobe",saturn,4.00e18,12952000,110,9,79.330183,0.07,0.107 ,0.98,PhobeBody);
            //solarRoot.mSimulationRadius = 400;

            // max distance --- 1222000
            // min orbital distance -- 7000
            // max radius -- 695000
            // min radius -- 196
            // get the spacecraft root frame of reference
            mSpacecraft = GameObject.Find("Spacecraft");
            //mSpacecraft.transform.position = new Vector3(1643.308f, -59.1503f, -83.91498f);
            //Quaternion q = Quaternion.Euler(new Vector3(356.3696f, 81.03871f, 0));
            //mSpacecraft.transform.rotation = q;

            // get the HUD status displays
            GameObject ss = GameObject.Find("SpeechStatus");
            mListeningStatus = ss.GetComponent<Text>();
            ss = GameObject.Find("ADFDisplay");
            mSelectedADF = ss.GetComponent<Text>();
            ss = GameObject.Find("ViewButtonText");
            mViewText = ss.GetComponent<Text>();
            ss = GameObject.Find("AutopilotStatus");
            AutopilotStatus = ss.GetComponent<Text>();
            ss = GameObject.Find("CreditsPanel");
            mCreditsDisplay = ss.GetComponent<Text>();
            mAutopilot = new AutoPilot { SolarBodies = mSolarBodies, SpaceCraft = mSpacecraft, SolarSystem = this };
            OrbitalElements = GameObject.FindGameObjectsWithTag("OrbitalElement");

            OrbitalSystem s = GetOrbitalSystem("saturn");
            GameObject saturnRing = GameObject.Find("SaturnRing");
            MeshFilter viewedModelFilter = (MeshFilter)saturnRing.GetComponent("MeshFilter");
            var mesh = viewedModelFilter.mesh;
            Vector2[] uvs = new Vector2[mesh.vertices.Length];
            double minDist = double.MaxValue;
            double maxDist = double.MinValue;
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                Vector3 aVertex = mesh.vertices[i];
                double a1 = Math.Atan2(aVertex.x, aVertex.y);
                Vector2 p = new Vector2(aVertex.x, aVertex.y);
                double pd = p.magnitude;
                if (pd < minDist)
                    minDist = pd;
                if (pd > maxDist)
                    maxDist = pd;
                double a2 = (a1 + Math.PI) / (Math.PI * 2);
                float ringAngle = (float)Math.Atan2(aVertex.y, aVertex.x);
                float orbitAngle = (float)Math.Atan2(s.Root.transform.position.z, s.Root.transform.position.x);
                float lightAngle = ringAngle - orbitAngle;
                if (lightAngle > Math.PI)
                    lightAngle = -Mathf.PI + (lightAngle - Mathf.PI);
                else if (lightAngle < -Math.PI)
                    lightAngle = Mathf.PI + (lightAngle - -Mathf.PI);
                if (lightAngle > 0)
                {
                    uvs[i] = new Vector2((float)a2, (pd < 0.5f) ? 0f : 1f);
                    //uvs[i] = new Vector2((float)a2, (pd < 0.5f) ? 0f : (ang > 0.2 || ang < -2) ? 0f : 1f);
                }
                else
                    uvs[i] = new Vector2(1, (pd < 0.5f) ? 0f : 1f);
            }
            mesh.uv = uvs;
            // scale the ring

            saturnRing.transform.position = s.Root.transform.position;
            saturnRing.transform.parent = s.Root.transform;
            saturnRing.transform.localScale = new Vector3(s.SimulationRadius * 1.6f, s.SimulationRadius * 1.6f, s.SimulationRadius * 1.6f);

            OrbitalSystem earth = GetOrbitalSystem("earth");
            earth.AddLiveLightingTexture(Resources.Load<Texture2D>("Textures/earth-land"),
                Resources.Load<Texture2D>("Textures/lightsatnight"), 220, 3);
            earth.AddAtmosphere(mSpacecraft, 2.05f, 0, 0, false, Resources.Load<Texture2D>("Textures/cloud-alpha"));
        }
        // Update is called once per frame
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        private void Update()
        {
            // run the speech system
            //#if ANDROID_DEVICE
            if (mSpeechDelay <= 0)
            {
                if (!mIsListening)
                {
                    mListeningStatus.color = Color.green;
                    mListeningStatus.text = "Listening";
                    mIsListening = true;
                    mSpeechDelay = 15;
                    SpeechToText.Listen();
                }
                else if (!SpeechToText.IsListening())
                {
                    string theString = SpeechToText.SpeechResult();
                    mListeningStatus.text = theString;
                    mListeningStatus.color = Color.red;
                    AutoPilot.ECommandCompletionState state = mAutopilot.ParseCommand(theString);
                    switch (state)
                    {
                        case AutoPilot.ECommandCompletionState.Ok:
                            mListeningStatus.color = Color.green;
                            break;

                        case AutoPilot.ECommandCompletionState.How:

                            if (UnityEngine.Random.Range(1, 20) <= 3)
                                SpeechToText.HowMessage();
                            mListeningStatus.color = Color.blue;
                            break;

                        case AutoPilot.ECommandCompletionState.What:
                            if (UnityEngine.Random.Range(1, 20) <= 1)
                                SpeechToText.WhatMessage();
                            mListeningStatus.color = Color.red;
                            break;
                    }

                    mSpeechDelay = 30;
                    mIsListening = false;
                }
            }
            else
            {
                --mSpeechDelay;
            }
            ////check if the left mouse has been pressed down this frame
            //if (Input.GetMouseButtonDown(0)) {
            //    //empty RaycastHit object which raycast puts the hit details into
            //    RaycastHit hit = new RaycastHit(); //ray shooting out of the camera from where the mouse is
            //    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            //     if (Physics.Raycast(ray,out hit,5000f))
            //     {
            //         GameObject target = hit.collider.gameObject;
            //         if (target == TrackTarget)
            //         {
            //             FollowDistance = (target.transform.position - Camera.main.transform.position).magnitude;
            //             FollowTarget = target;
            //         }
            //         else
            //             TrackTarget = target;
            //     }
            //     else
            //     {
            //         //TrackTarget = null;
            //         //FollowTarget = null;
            //     }
            //}
            mSolarBodies["sun"].AdvancePosition(mSpacecraft, TimeBase);
            mAutopilot.Update(TimeBase);

            if (!mInitialCameraJumpPerformed)
            {
                mAutopilot.HandleOrbitalTransition(mSolarBodies["earth"], true);
                //mAutopilot.Track = mSolarBodies["earth"];
                mInitialCameraJumpPerformed = true;
            }
            if (mCredits != null)
                if (!mCredits.Advance(mCreditsDisplay))
                    mCredits = null;
        }

        #endregion Private Methods
    }
}