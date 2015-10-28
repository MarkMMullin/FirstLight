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
using UnityEngine;

#endregion

namespace Assets.Scripts
{
    public class OrbitalSystem
    {
        #region Public Fields

        public const float DEGREEMULT = 0.0174532925f;

        public const float G_K = 6.67e-11f;

        //C2DTextureShader* COrbitalSystem::sm_textures[kNumPlanetTextures];
        /// <summary>
        ///     Use this to control the size of the virtual system
        /// </summary>
        public const float KMAXSYSTEMRADIUS = 50000;

        //@cmember radius of the virtual object
        public float mSimulationRadius;

        #endregion Public Fields

        #region Private Fields

        private static readonly float[,] MASS_RANGE = new float[8, 2];

        private static readonly float[,] ORBIT_RANGE = new float[8, 2];

        // internal
        private static readonly float[,] RADIUS_RANGE = new float[8, 2];

        //@cmember X defines the max siblings, Y defines max depth
        private static Vector2 smMaxDimensions;

        //@cmember distance of atmosphere shell from body surface
        private readonly List<Atmosphere> mAtmospheres;

        //@cmember centerpoint bias for orbit calculations
        private readonly Vector3 mBias;

        //@cmember reported depth information
        private readonly int mDepth;

        //@cmember image for pad display
        private readonly Texture2D mImage;

        //@cmember distance from parent object center to orbital object center
        //@cmember gravitational field in M/s^2, measured at the surface
        //@cmember distance
        private readonly float mObjectDistance;

        //@cmember angular orbital velocity
        private readonly float mOrbitalVelocity;

        //@@cmember float ratio of width over breadth for orbit eccentricity
        private readonly float mOrbitEccentricity;

        //@cmember List of orbital system objects that are orbiting this object
        private readonly List<OrbitalSystem> mOrbitObjects;

        //@cmember The parent orbital system
        private readonly OrbitalSystem mParentObject;

        //@cmember texture bound to this object
        private readonly Material mTexture;

        //@cmember the object representing the orbital track
        private readonly GameObject mTrackObject;

        //@cmember last orbital angle
        private float mLastOrbitAngle;

        private Texture2D mLiveDayTexture;

        private float mLiveLightingDistance;

        private Texture2D mLiveLightingTexture;

        private bool mLiveLightingTextureActive;

        private Texture2D mLiveNightTexture;

        //@cmember base name of the orbital system
        private string mName;

        private float mObserverDistance;

        private Vector3 mObserverPosition;

        //@cmember hoursPerRevolution rate of root object
        //@cmember orbital period offset
        private float mOrbitalPeriodOffset;

        //@cmember the u and v rotation angles for the orbital plane
        private Vector2 mOrbitalPlane;

        //@cmember cached child image
        private Texture2D mSiblingImage;

        #endregion Private Fields

        #region Public Constructors

        public OrbitalSystem()
        {
            RotationColumnOffset = 0;
            mOrbitalVelocity = 0;
            Spin = 0;
            Radius = 0;
            mParentObject = null;
            mTexture = null;
            mName = "";
            mBias = Vector3.zero;
            mOrbitalPlane = Vector2.zero;
            mOrbitEccentricity = 0;
            mObjectDistance = 0;
            mSimulationRadius = 0;
            Root = null;
            mTrackObject = null;
            mImage = null;
            mSiblingImage = null;
            mDepth = 0;
            mOrbitalPeriodOffset = 0;
        }

        public OrbitalSystem(int odepth, string theName, OrbitalSystem root, float mass, float dist, float rad, float inc,
                    float orbitalPeriodDays, float hoursPerRevolution, float gravMPS, float eccRat, GameObject body)
        {
            RotationColumnOffset = 0;
            mOrbitalPeriodOffset = 0;
            mDepth = odepth;
            mImage = null;
            mOrbitalVelocity = convertOrbitalPeriodToRadSec(orbitalPeriodDays);
            mOrbitalPeriodOffset = 0;
            // rotational velocity expressed as orbital period in earth sidereal hours
            Spin = convertOrbitalRevolutionHoursToRadSec(hoursPerRevolution);

            mOrbitObjects = new List<OrbitalSystem>();
            mAtmospheres = new List<Atmosphere>();
            mSiblingImage = null;
            var distance = dist;
            Radius = rad;
            mParentObject = root;
            var inclination = DEGREEMULT * inc;
            mName = theName;
            mBias = Vector3.zero;
            mOrbitalPlane = Vector2.zero;
            mOrbitEccentricity = eccRat;
            mLastOrbitAngle = 0;
            if (distance != 0)
            {
                mObjectDistance = (distance / mParentObject.Radius) + mParentObject.mSimulationRadius;
                mSimulationRadius = (Radius / mParentObject.Radius) * mParentObject.mSimulationRadius;
            }
            else
            {
                mObjectDistance = 0;
                mSimulationRadius = 4800;
            }
            if (mParentObject != null)
                mParentObject.mOrbitObjects.Add(this);

            Root = new GameObject();
            //Construct();
            //Root.transform.position = new Vector3(0, 0, m_objectDistance);
            if (mParentObject != null)
            {
                Root.transform.parent = mParentObject.Root.transform;
                Root.transform.localPosition = new Vector3(0, 0, mObjectDistance);
            }
            else
            {
                Root.transform.position = new Vector3(0, 0, mObjectDistance);
            }
            if (mTrackObject != null)
            {
                mTrackObject.transform.parent = mParentObject.Root.transform;
                var pp = mParentObject.Root.transform.position;
                mTrackObject.transform.position = pp;
            }
            Body = body;
            Body.transform.parent = Root.transform;
            Body.transform.localScale = new Vector3(mSimulationRadius, mSimulationRadius, mSimulationRadius);
            Body.transform.localPosition = new Vector3(0, 0, 0);
            var vector = Quaternion.AngleAxis(inclination / DEGREEMULT, Vector3.right) * Vector3.up;
            Body.transform.up = vector;
            Root.name = mName + "_root";

            //Renderer rend = Body.GetComponent<Renderer>();
            //rend.material.shader = Shader.Find("Specular");
            //rend.material.SetColor("_SpecColor", Color.black);
        }

        #endregion Public Constructors

        #region Public Enums

        /// <summary>
        ///     Defines all of the predefined planets, moons, etc
        /// </summary>
        public enum EOrbitalElements
        {
            Mercury = 0,
            Venus = 1,
            Earth = 2,
            Moon = 3,
            Mars = 4,
            Jupiter = 5,
            Saturn = 6,
            Uranus = 7,
            Neptune = 8,
            Pluto = 9,
            Sun = 10,
            Stars = 11,
            Phobos = 12,
            Deimos = 13,
            Io = 14,
            Europa = 15,
            Ganymede = 16,
            Callisto = 17,
            Mimas = 18,
            Enceladus = 19,
            Tethys = 20,
            Dione = 21,
            Rhea = 22,
            Iapetus = 23,
            SaturnRing = 24,
            EarthClouds = 25
        };

        #endregion Public Enums

        #region Public Properties

        public GameObject Body { get; set; }

        public bool IsMoon
        {
            get { return GetParent() != null && GetParent().GetParent() != null; }
        }

        public bool IsPlanet
        {
            get { return GetParent() != null && GetParent().GetParent() == null; }
        }

        public Vector3 LastAbsoluteDisplacement { get; private set; }

        public Vector3 LastRelativeMove { get; private set; }

        //@cmember radius of the physical object
        public float Radius { get; private set; }

        public GameObject Root { get; set; }

        public int RotationColumnOffset { get; set; }

        public float SimulationRadius
        {
            get { return mSimulationRadius; }
            set
            {
                if (mSimulationRadius == value) return;

                Body.transform.localScale = new Vector3(value, value, value);
                mSimulationRadius = value;
            }
        }

        public float Speed
        {
            get { return (float)((mOrbitalVelocity / (2 * Math.PI)) * (2 * Math.PI * mObjectDistance)); }
        }

        /// <summary>
        ///     Return the angular velocity of the hoursPerRevolution
        /// </summary>
        public float Spin { get; private set; }
        public float SumOfAbsoluteDisplacementDistance
        {
            get
            {
                float sumDisplacement = 0;
                var current = this;
                while (current != null)
                {
                    sumDisplacement += current.Speed;
                    current = current.GetParent();
                }
                return sumDisplacement;
            }
        }

        #endregion Public Properties

        #region Public Methods

        public static GameObject CreateFreemanSphere()
        {
            return null;
        }

        public static Vector3 OrbitRotationVector(ref float lastOrbitalAngle, float objectRadius, float velocity,
            float orbitalDistance)
        {
            var velocityRatio = (float)(velocity / (2.0f * Math.PI * objectRadius));
            lastOrbitalAngle = (float)((lastOrbitalAngle + velocityRatio) % (2 * Math.PI));
            return new Vector3((float)Math.Cos(lastOrbitalAngle) * orbitalDistance, 0,
                (float)Math.Sin(lastOrbitalAngle) * orbitalDistance);
        }

        public static void SetMassRange(int orbitIndex, float minrg, float maxrg)
        {
            MASS_RANGE[orbitIndex, 0] = minrg;
            MASS_RANGE[orbitIndex, 1] = maxrg;
        }

        public static void SetOrbitRange(int orbitIndex, float minrg, float maxrg)
        {
            ORBIT_RANGE[orbitIndex, 0] = minrg;
            ORBIT_RANGE[orbitIndex, 1] = maxrg;
        }

        public static void SetRadiusRange(int orbitIndex, float minrg, float maxrg)
        {
            RADIUS_RANGE[orbitIndex, 0] = minrg;
            RADIUS_RANGE[orbitIndex, 1] = maxrg;
        }

        public int AddAtmosphere(GameObject spaceCraft, float surfDist, float spin, float spinOffset, bool enterable,
            Texture2D ts = null, AtmosphereTriggerScript.AtmosphereEntryHandler entryHandler = null)
        {
            var atmosphere = new Atmosphere(spaceCraft, this, GetName() + "Atmosphere" + mAtmospheres.Count,
                mAtmospheres.Count, surfDist, spin, spinOffset, Vector3.zero, enterable, ts, entryHandler);
            mAtmospheres.Add(atmosphere);

            //SortAtmospheres();
            return mAtmospheres.Count - 1;
        }

        public void AddLiveLightingTexture(Texture2D dayTexture, Texture2D nightTexture, int rotationColumnOffset,
            float activeDistance)
        {
            RotationColumnOffset = rotationColumnOffset;
            mLiveLightingDistance = activeDistance * SimulationRadius;
            mLiveDayTexture = dayTexture;
            mLiveNightTexture = nightTexture;
            mLiveLightingTexture = new Texture2D(mLiveDayTexture.width, mLiveDayTexture.height, TextureFormat.RGB24, true);
        }

        public void AdvancePosition(GameObject observer, float tstate)
        {
            mObserverPosition = observer.transform.position;
            var originalPosition = Root.transform.position;
            var angmotion = (float)(((tstate * -mOrbitalVelocity) + mLastOrbitAngle) % (2 * 3.141592));
            var rv = new Vector3((float)Math.Cos(angmotion + mOrbitalPeriodOffset) * mObjectDistance, 0,
                (float)Math.Sin(angmotion + mOrbitalPeriodOffset) * (mObjectDistance * mOrbitEccentricity));
            LastRelativeMove = rv;
            var q = Quaternion.identity;
            if (mOrbitalPlane.x != 0.0)
                q = q * Quaternion.AngleAxis(mOrbitalPlane.x, Vector3.right);
            if (mOrbitalPlane.y != 0.0)
                q = q * Quaternion.AngleAxis(mOrbitalPlane.y, Vector3.forward);
            var pp = (mParentObject == null) ? Vector3.zero : mParentObject.Root.transform.position;
            var abspos = rv + pp + mBias;

            Root.transform.position = abspos;
            Root.transform.rotation = q;
            mObserverDistance = (mObserverPosition - abspos).magnitude;

            mLastOrbitAngle = angmotion;
            var spinAngle = RotationalSpin(tstate);
            Body.transform.Rotate(0, spinAngle / DEGREEMULT, 0);
            if (mLiveLightingTexture != null)
            {
                updateLiveLightingTexture(Body.transform.eulerAngles.y * DEGREEMULT);
            }
            // Body.transform.Rotate(Body.transform.up, spinAngle/DEGREEMULT);
            LastAbsoluteDisplacement = abspos - originalPosition;

            foreach (OrbitalSystem t in mOrbitObjects)
                t.AdvancePosition(observer, tstate);
        }

        public Vector2 ComputeTreeDimensions(int theDepth)
        {
            var nChildren = mOrbitObjects.Count;
            if (smMaxDimensions.x < nChildren) smMaxDimensions.x = nChildren;
            if (smMaxDimensions.y < theDepth)
                smMaxDimensions.y = theDepth;
            for (var i = 0; i < nChildren; i++)
                mOrbitObjects[i].ComputeTreeDimensions(theDepth + 1);
            return smMaxDimensions;
        }

        public void DropCachedSiblings()
        {
            foreach (OrbitalSystem t in mOrbitObjects)
                t.DropCachedSiblings();
        }

        public OrbitalSystem GetChild(int i)
        {
            return mOrbitObjects[i];
        }

        public int GetDepth()
        {
            return mDepth;
        }

        public Texture2D GetImage()
        {
            if (mImage != null)
                return mImage;
            return (mTexture != null) ? (Texture2D)mTexture.mainTexture : null;
        }

        public string GetName()
        {
            return mName;
        }

        public GameObject GetObject()
        {
            return Body;
        }

        public OrbitalSystem GetParent()
        {
            return mParentObject;
        }

        public Texture2D GetSiblingImage()
        {
            return mSiblingImage;
        }

        public GameObject GetTrack()
        {
            return mTrackObject;
        }

        public bool IsBody(GameObject o)
        {
            var theName = o.name;
            var tst = "_body";
            return theName.EndsWith(tst);
        }

        public bool IsTrack(GameObject o)
        {
            var theName = o.name;
            var tst = "_track";
            return theName.EndsWith(tst);
        }

        public int NumChildren()
        {
            return mOrbitObjects.Count;
        }

        public float RotationalSpin(float tstate)
        {
            var ttlspin = tstate * Spin;
            ttlspin = ttlspin % (2.0f * 3.141592f);
            return ttlspin;
        }

        public void SetName(string theName)
        {
            var trackname = theName + "_track";
            if (Body)
                Body.name = theName;
            if (mTrackObject)
                mTrackObject.name = trackname;
            mName = theName;
        }

        public void SetOrbitalPeriodOffset(float ofs)
        {
            if (ofs != mOrbitalPeriodOffset)
            {
                var diff = ofs - mOrbitalPeriodOffset;
                var rv = new Vector3((float)Math.Cos(diff) * mObjectDistance, 0,
                    (float)Math.Sin(diff) * (mObjectDistance * mOrbitEccentricity));
                var q = Quaternion.identity;
                if (mOrbitalPlane.x != 0.0)
                    q = q * Quaternion.AngleAxis(mOrbitalPlane.x, Vector3.right);
                if (mOrbitalPlane.y != 0.0)
                    q = q * Quaternion.AngleAxis(mOrbitalPlane.y, Vector3.forward);
                var pp = (mParentObject == null) ? Vector3.zero : mParentObject.Root.transform.position;
                var abspos = rv + pp + mBias;
                Root.transform.position = abspos;
                mOrbitalPeriodOffset = ofs;
            }
        }

        public void SetSiblingImage(Texture2D dib)
        {
            mSiblingImage = dib;
        }

        #endregion Public Methods

        #region Private Methods

        private static float gcf(float x, float y)
        {
            float t;
            x = Math.Abs(x);
            y = Math.Abs(y);
            t = x % y;
            while (t > 0)
            {
                x = y;
                y = t;
                t = x % y;
            }
            return y;
        }

        private float convertOrbitalPeriodToRadSec(float orbitalPeriod)
        {
            if (orbitalPeriod == 0)
                return 0;
            var inADay = (float)(2 * Math.PI / orbitalPeriod);
            var inAnHour = inADay / 24;
            var inAMinute = inAnHour / 60;
            var inASecond = inAMinute / 60;
            return inASecond;
        }

        private float convertOrbitalRevolutionHoursToRadSec(float spinPeriodHours)
        {
            var spinPerHour = (2 * (float)Math.PI) / spinPeriodHours;
            var spinPerMin = spinPerHour / 60;
            var spinPerSec = spinPerMin / 60;
            return spinPerSec;
        }
        // Use this for initialization
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        private void Start()
        {
        }

        // Update is called once per frame
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        private void Update()
        {
        }

        private void updateLiveLightingTexture(float spin)
        {
            // test for most common case and bail - i.e. its too far away to matter
            if (!mLiveLightingTextureActive && (mObserverDistance > mLiveLightingDistance))
                return;

            var renderer = (Renderer)Body.GetComponent("Renderer");
            if (mLiveLightingTextureActive && (mObserverDistance > mLiveLightingDistance))
            {
                // disable live lighting
                renderer.material.mainTexture = mLiveDayTexture;
                renderer.material.shader = Shader.Find("Standard");
                mLiveLightingTextureActive = false;
                return;
            }
            if (!mLiveLightingTextureActive)
            {
                mLiveLightingTextureActive = true;
                renderer.material.shader = Shader.Find("Unlit/Texture");
            }

            var rotationColumn = (int)((spin / (Mathf.PI * 2)) * 1024) + RotationColumnOffset;
            if (rotationColumn > 1023)
                rotationColumn = rotationColumn - 1024;
            // start at the rotation column
            // paint 512 columns of night, wrap as necessary
            // paint 512 columns of day, wrap as necessary
            // paint leading twilight
            var twilightColumn = 0;
            while (twilightColumn < 64)
            {
                var night = mLiveNightTexture.GetPixels(rotationColumn, 0, 1, 512);
                var day = mLiveDayTexture.GetPixels(rotationColumn, 0, 1, 512);
                var twilight = new Color[day.Length];
                for (var i = 0; i < night.Length; i++)
                {
                    var nc = new Vector3(night[i].r, night[i].g, night[i].b);
                    var dc = new Vector3(day[i].r, day[i].g, day[i].b);
                    var tc = dc + ((nc - dc) * (twilightColumn / 64.0f));
                    twilight[i] = new Color(tc.x, tc.y, tc.z);
                }
                mLiveLightingTexture.SetPixels(rotationColumn++, 0, 1, 512, twilight);
                if (rotationColumn > 1023)
                    rotationColumn = 0;
                ++twilightColumn;
            }

            // paint night
            if (rotationColumn + 384 > 1023) // need to wrap
            {
                var remainder = (rotationColumn + 384) - 1024;
                var src = mLiveNightTexture.GetPixels(rotationColumn, 0, 384 - remainder, 512);
                mLiveLightingTexture.SetPixels(rotationColumn, 0, 384 - remainder, 512, src);
                rotationColumn = 0;
                src = mLiveNightTexture.GetPixels(rotationColumn, 0, remainder, 512);
                mLiveLightingTexture.SetPixels(rotationColumn, 0, remainder, 512, src);
                mLiveLightingTexture.Apply();
                rotationColumn = remainder;
            }
            else
            {
                mLiveLightingTexture.SetPixels(rotationColumn, 0, 384, 512,
                    mLiveNightTexture.GetPixels(rotationColumn, 0, 384, 512));
                rotationColumn += 384;
            }
            twilightColumn = 0;
            // paint twilight
            while (twilightColumn < 64)
            {
                var night = mLiveNightTexture.GetPixels(rotationColumn, 0, 1, 512);
                var day = mLiveDayTexture.GetPixels(rotationColumn, 0, 1, 512);
                var twilight = new Color[day.Length];
                for (var i = 0; i < night.Length; i++)
                {
                    var nc = new Vector3(night[i].r, night[i].g, night[i].b);
                    var dc = new Vector3(day[i].r, day[i].g, day[i].b);
                    var tc = dc + ((nc - dc) * (1.0f - (twilightColumn / 64.0f)));
                    twilight[i] = new Color(tc.x, tc.y, tc.z);
                }
                mLiveLightingTexture.SetPixels(rotationColumn++, 0, 1, 512, twilight);
                if (rotationColumn > 1023)
                    rotationColumn = 0;
                ++twilightColumn;
            }

            // paint day
            if (rotationColumn + 512 > 1023) // need to wrap
            {
                var remainder = (rotationColumn + 512) - 1024;
                var balance = 512 - remainder;
                var src = mLiveDayTexture.GetPixels(rotationColumn, 0, balance, 512);
                mLiveLightingTexture.SetPixels(rotationColumn, 0, balance, 512, src);
                rotationColumn = 0;
                src = mLiveDayTexture.GetPixels(rotationColumn, 0, remainder, 512);
                mLiveLightingTexture.SetPixels(rotationColumn, 0, remainder, 512, src);
            }
            else
            {
                mLiveLightingTexture.SetPixels(rotationColumn, 0, 512, 512,
                    mLiveDayTexture.GetPixels(rotationColumn, 0, 512, 512));
            }
            mLiveLightingTexture.Apply();

            renderer.material.mainTexture = mLiveLightingTexture;
        }

        #endregion Private Methods
    }
}