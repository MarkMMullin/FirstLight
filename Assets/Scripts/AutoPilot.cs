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

#endregion References

namespace Assets.Scripts
{
    public class AutoPilot
    {
        #region Private Fields

        private readonly string[] mAutopilotCommands = { "fly", "go", "look", "orbit", "track", "follow", "jump" };

        private readonly string[] mBinaryCommandObjects = { "orbit", "time", "speed", "elevation", "weather", "climate" };

        private readonly string[] mBinaryCommands = { "enter", "exit", "leave", "faster", "slower", "increase", "decrease", "double", "half", "halve" };

        private readonly string[] mImperativeCommands = { "picture", "snapshot", "clouds", "atmosphere", "reset", "zero", "pause", "resume", "stop", "faster", "slower", "lock", "unlock", "mark", "set", "front", "credits" };

        private readonly List<string[]> mPhrases = new List<string[]> { new[] { "cthulhu", "rlyeh", "fhtagn" }, new[] { "what", "is", "that" }, new[] { "what's", "that" }, new[] { "drop", "dead" } };

        private readonly Stack<AutopilotProgram> mProgramStack = new Stack<AutopilotProgram>();

        private float mFollowDistance;

        private float mLastOrbitAngle;

        private Quaternion mMarkAtt;

        private Vector3 mMarkPos;

        private object mObject;

        private bool mSpaceCraftAttitudeLocked = true;

        #endregion Private Fields

        #region Public Constructors

        public AutoPilot()
        {
            OrbitElevation = 0.05f;
            Velocity = 0.5f;
        }

        #endregion Public Constructors

        #region Public Enums

        public enum ECommandCompletionState
        {
            Ok,
            What,
            How
        };

        #endregion Public Enums

        #region Private Enums

        private enum EPhrase
        {
            Unknown,
            WhatsThat1,
            WhatsThat2,
            DropDead
        };

        #endregion Private Enums

        #region Public Properties

        /// <summary>
        /// Identifies the simulation element (planet,moon, etc) the camera is moving towards
        /// </summary>
        public OrbitalSystem Destination { get; set; }

        /// <summary>
        /// Identifies the simulation element (planet,moon, etc) the camera is automatically following
        /// </summary>
        public OrbitalSystem Follow { get; set; }

        public Vector3 LastOrbitPosition { get; private set; }
        /// <summary>
        /// Identifies the simulation element (planet,moon, etc) the camera is automatically orbiting
        /// </summary>
        public OrbitalSystem Orbit { get; set; }

        /// <summary>
        /// Orbital distance when orbiting something
        /// </summary>
        public float OrbitElevation { get; set; }

        /// <summary>
        /// Maps all of the simulation elements by name for easy access
        /// </summary>
        public Dictionary<string, OrbitalSystem> SolarBodies { get; set; }

        /// <summary>
        /// Root of the simulation model
        /// </summary>
        public SolarSystem SolarSystem { get; set; }

        /// <summary>
        /// Effectively the users POV - camera object
        /// </summary>
        public GameObject SpaceCraft { get; set; }
        /// <summary>
        /// Identifies the simulation element (planet,moon, etc) the camera is automatically tracking
        /// </summary>
        public OrbitalSystem Track { get; set; }

        /// <summary>
        /// Velocity of the camera
        /// </summary>
        public float Velocity { get; set; }

        #endregion Public Properties

        #region Public Methods

        public void FlyTo(OrbitalSystem os)
        {
            mObject = os;
            if (Orbit != null)
                HandleOrbitalTransition(null, false);
            Orbit = null;
            Destination = os;
        }

        public ECommandCompletionState HandleAutopilotCommand(string command, OrbitalSystem sObj)
        {
            switch (command)
            {
                case "fly":
                    goto case "go";
                case "go":
                    FlyTo(sObj);
                    break;

                case "look":
                    SpaceCraft.transform.LookAt(sObj.Root.transform);
                    SolarSystem.PoseController.transform.LookAt(sObj.Root.transform);
                    break;

                case "orbit":
                    HandleOrbitalTransition(sObj, true);
                    break;

                case "track":
                    Track = sObj;
                    break;

                case "follow":
                    mFollowDistance = (sObj.Root.transform.position - SpaceCraft.transform.position).magnitude;
                    Follow = sObj;
                    break;

                case "jump":
                    HandleOrbitalTransition(sObj, true);
                    //Vector3 dirVec = mselectedTarget.GetBody().transform.position - SpaceCraft.transform.position;
                    //float orbrad = mselectedTarget.mSimulationRadius + (mselectedTarget.mSimulationRadius * 0.021f);
                    //Vector3 dvn = dirVec;
                    //dvn.Normalize();
                    //float mag = (mselectedTarget.GetBody().transform.position - SpaceCraft.transform.position).magnitude;
                    //Vector3 pos = SpaceCraft.transform.position + (dvn * (mag - orbrad));
                    //SpaceCraft.transform.position = pos;
                    //SpaceCraft.transform.LookAt(mselectedTarget.GetBody().transform);
                    break;

                default:
                    return ECommandCompletionState.What;
            }
            mObject = sObj;
            return ECommandCompletionState.Ok;
        }

        public void HandleOrbitalTransition(OrbitalSystem target, bool enteringOrbit)
        {
            if (enteringOrbit)
            {
                if (target == null)
                    return;
                Destination = null;
                //mOrbitElevation = (target.GetBody().transform.position - SpaceCraft.transform.position).magnitude;
                OrbitElevation = (target.mSimulationRadius / 2) + Math.Max(target.mSimulationRadius * 0.1f, 1.3f);
                Orbit = target;
                Vector3 dirv = Vector3.Normalize(Orbit.Root.transform.position * -1.0f);

                SpaceCraft.transform.position = Orbit.Root.transform.position + (dirv * OrbitElevation);
                SpaceCraft.transform.LookAt(Orbit.Root.transform.position + Orbit.LastRelativeMove, Orbit.Root.transform.up);
                float tp = (float)(2.0f * Math.PI);
                // compute matching velocity from spin rate
                float circumference = tp * Orbit.mSimulationRadius;
                float velRat = Orbit.Spin / tp;
                Velocity = circumference * velRat;

                float x = SpaceCraft.transform.position.x - Orbit.Root.transform.position.x;
                float y = SpaceCraft.transform.position.x - Orbit.Root.transform.position.x;

                float dp = Vector2.Dot(new Vector2(1, 0), new Vector2(x, y));
                mLastOrbitAngle = (float)Math.Acos(dp / (x * y));
                SpeechToText.Speak("now orbiting " + Orbit.GetName());
                SolarSystem.PoseController.transform.LookAt(Orbit.Body.transform.position + Orbit.LastRelativeMove, Orbit.Root.transform.up);
                mObject = target;
            }
            else
            {
                if (Orbit == null)
                    return;
                Vector3 crv = OrbitalSystem.OrbitRotationVector(ref mLastOrbitAngle, Orbit.mSimulationRadius, 0, OrbitElevation);
                Vector3 dvPc = Vector3.Normalize(crv);
                Vector3 up = Orbit.Body.transform.up;
                Vector3 vv = Vector3.Cross(up, dvPc);
                SpaceCraft.transform.forward = vv;
                //SpaceCraft.transform.LookAt(mOrbit.GetBody().transform.position + mOrbit.LastAbsoluteDisplacement);
                // convert velocity to orbited bodies rotation
                //SolarSystem.PoseController.Transform.position = SpaceCraft.transform.position;
                //SolarSystem.PoseController.Transform.rotation = SpaceCraft.transform.rotation;
                float scaler = (Orbit.Radius / 160) * (Orbit.SimulationRadius / 64);
                Velocity = Orbit.SumOfAbsoluteDisplacementDistance * scaler;
                string theName = Orbit.GetName();
                Orbit = null;
                SpeechToText.Speak("leaving orbit of " + theName);
            }
        }

        /// <summary>
        /// Take the text response from Google and try and make some kind of sense of it
        /// </summary>
        /// <param name="theCommand"></param>
        /// <returns></returns>
        public ECommandCompletionState ParseCommand(string theCommand)
        {
            if (string.IsNullOrEmpty(theCommand))
                return ECommandCompletionState.What;
            string[] words = theCommand.Split(' ');
            List<string> imperatives = new List<string>();
            List<string> binaries = new List<string>();
            List<string> binaryCommandObjects = new List<string>();
            List<string> autopilotCommands = new List<string>();
            List<OrbitalSystem> systemObjects = new List<OrbitalSystem>();
            EPhrase testPhrase = scanPhrases(words);
            if (testPhrase != EPhrase.Unknown)
            {
                switch (testPhrase)
                {
                    case EPhrase.WhatsThat1:
                        goto case EPhrase.WhatsThat2;
                    case EPhrase.WhatsThat2:
                        WhatsThat();
                        return ECommandCompletionState.Ok;

                    case EPhrase.DropDead:
                        Application.Quit();
                        return ECommandCompletionState.Ok;
                }
            }
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = words[i].ToLower();
                if (SolarBodies.ContainsKey(words[i]))
                    systemObjects.Add(SolarBodies[words[i]]);
                if (mImperativeCommands.Contains(words[i]))
                    imperatives.Add(words[i]);
                if (mBinaryCommands.Contains(words[i]))
                    binaries.Add(words[i]);
                if (mAutopilotCommands.Contains(words[i]))
                    autopilotCommands.Add(words[i]);
                if (mBinaryCommandObjects.Contains(words[i]))
                    binaryCommandObjects.Add(words[i]);
            }
            if ((words.Length == 4 && words[0].Equals("set") && words[2].Equals("to") && binaryCommandObjects.Count == 1) ||
                (words.Length == 3 && words[0].Equals("set") && binaryCommandObjects.Count == 1))
                return handleSetCommand(binaryCommandObjects[0], words[2]);
            if (words.Length == 1 && imperatives.Count == 1)
                return handleImperativeCommand(imperatives[0]);
            if (autopilotCommands.Count == 1 && systemObjects.Count == 1)
                return HandleAutopilotCommand(autopilotCommands[0], systemObjects[0]);
            if (binaries.Count == 1 && binaryCommandObjects.Count == 1)
                return handleBinaryCommand(binaries[0], binaryCommandObjects[0]);
            return ECommandCompletionState.What;
        }

        public void Update(float timebase)
        {
            //if (mHoldoff == 1)
            //{
            //    mHoldoff = 0;
            //    HandleOrbitalTransition(null, false);
            //    mOrbit = null;
            //}
            //else if (mHoldoff > 0)
            //    --mHoldoff;
            string aps = (Orbit != null ? "Orbit:" : "") + (Track != null ? "Track:" : "") +
                         (Follow != null ? "Follow:" : "") + (Destination != null ? "Fly:" : "");
            SolarSystem.AutopilotStatus.text = aps;
            SolarSystem.PoseController.transform.position = SpaceCraft.transform.position;
            if (Orbit != null)
            {
                Vector3 crv = OrbitalSystem.OrbitRotationVector(ref mLastOrbitAngle, Orbit.mSimulationRadius, Velocity * timebase, OrbitElevation);
                Vector3 newPosition = Orbit.Root.transform.position + crv;
                Vector3 dvPc = Vector3.Normalize(crv);
                Vector3 up = Orbit.Root.transform.up;
                Vector3 vv = Vector3.Cross(up, dvPc);
                SpaceCraft.transform.position = newPosition;
                SolarSystem.PoseController.ZeroPosition();
                SpaceCraft.transform.LookAt(newPosition + vv, up);
                SolarSystem.PoseController.transform.position = SpaceCraft.transform.position;
                LastOrbitPosition = SpaceCraft.transform.position;
            }
            if (Track != null)
            {
                Vector3 np = Track.Root.transform.position;
                np = (np - SpaceCraft.transform.position).normalized;
                SpaceCraft.transform.forward = np;
                //mc->LocalCoordinateSystem()->SetAbsoluteUp(SFVec3f(0, -1, 0));
                //mc->LocalCoordinateSystem()->SetAbsoluteViewpoint(np);
            }
            if (Follow != null)
            {
                Vector3 followvector = (SpaceCraft.transform.position - Follow.Root.transform.position).normalized;

                SpaceCraft.transform.position = Follow.Root.transform.position + (followvector * mFollowDistance);
            }
            if (Destination != null)
            {
                Vector3 dirVec = Destination.Root.transform.position - SpaceCraft.transform.position;
                float dist = dirVec.magnitude;
                if (dist <= Destination.mSimulationRadius * 0.53f)
                {
                    HandleOrbitalTransition(Destination, true);
                    Destination = null;
                }
                Vector3 dvn = Vector3.Normalize(dirVec);
                RaycastHit hit;
                if (Physics.Raycast(SpaceCraft.transform.position, dvn, out hit, 50))
                {
                    string hitname = hit.collider.gameObject.name.ToLower();
                    if (Destination != null)
                    {
                        string myname = Destination.Body.name.ToLower();
                        if (!hitname.Equals(myname))
                        {
                            float deflect = 1.0f - hit.distance / 50.0f;
                            dvn = Quaternion.AngleAxis(140 * deflect, new Vector3(1, 0, 0)) * dvn;
                        }
                    }
                }
                if (Velocity * 80 >= dist)
                    Velocity = Velocity - (Velocity * 0.22f);
                else if (Velocity * 200 < dist && Velocity < 100)
                    Velocity = Velocity + (Velocity * 0.06f);

                SpaceCraft.transform.position = SpaceCraft.transform.position + dvn * Velocity * SolarSystem.TimeBase;
                SpaceCraft.transform.forward = dvn;
                SolarSystem.PoseController.transform.position = SpaceCraft.transform.position;
            }
            if (Orbit == null && Follow == null && Destination == null)
            {
                Vector3 dirv = mSpaceCraftAttitudeLocked
                    ? SolarSystem.PoseController.transform.forward
                    : SpaceCraft.transform.forward;
                SpaceCraft.transform.position = SpaceCraft.transform.position + (dirv * Velocity * SolarSystem.TimeBase);
                //SpaceCraft.transform.rotation = SolarSystem.PoseController.transform.rotation;
                SolarSystem.PoseController.transform.position = SpaceCraft.transform.position;
                //SolarSystem.PoseController.transform.rotation = SpaceCraft.transform.rotation;
            }
            SolarSystem.StarSphere.transform.position = SpaceCraft.transform.position;
        }

        public void WhatsThat()
        {
            List<OrbitalSystem> visibleElements = new List<OrbitalSystem>();
            List<OrbitalSystem> orbelems = SolarSystem.OrbitalSystems;
            OrbitalSystem testObj = mObject as OrbitalSystem;
            foreach (GameObject t in SolarSystem.OrbitalElements)
            {
                if (t.GetComponent<MeshRenderer>().GetComponent<Renderer>().isVisible)
                {
                    foreach (OrbitalSystem orbelem in orbelems)
                    {
                        if (orbelem.Body == t)
                        {
                            if (orbelem != testObj)
                                visibleElements.Add(orbelem);
                            break;
                        }
                    }
                }
            }
            if (visibleElements.Count == 0)
                SpeechToText.Speak("I don't see anything");
            else if (visibleElements.Count == 1)
            {
                OrbitalSystem os = visibleElements[0];
                if (os != null)
                {
                    OrbitalSystem osp = os.GetParent();
                    if (osp != null && osp.GetParent() == null)
                        SpeechToText.Speak("That is the planet " + os.GetName());
                    else if (osp != null)
                    {
                        SpeechToText.Speak("That is the moon " + os.GetName() + " of " + osp.GetName());
                    }
                    mObject = os;
                }
            }
            else
            {
                List<OrbitalSystem> planets = new List<OrbitalSystem>();
                List<OrbitalSystem> moons = new List<OrbitalSystem>();
                foreach (OrbitalSystem os in visibleElements)
                {
                    if (os.IsPlanet)
                        planets.Add(os);
                    else
                    {
                        moons.Add(os);
                    }
                }
                SpeechToText.Speak(planets.Count + " planets and " + moons.Count + " moons");
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Handle a command with two modifiers
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="sObj"></param>
        /// <returns></returns>
        private ECommandCompletionState handleBinaryCommand(string cmd, string sObj)
        {
            float multiplier = 0.05f;
            switch (cmd)
            {
                case "enter":
                    switch (sObj)
                    {
                        case "orbit":
                            OrbitalSystem os = mObject as OrbitalSystem;
                            if (os == null)
                                return ECommandCompletionState.How;
                            HandleOrbitalTransition(os, true);
                            break;

                        default:
                            return ECommandCompletionState.What;
                    }
                    break;

                case "exit":
                    switch (sObj)
                    {
                        case "orbit":
                            HandleOrbitalTransition(null, false);
                            break;

                        default:
                            return ECommandCompletionState.What;
                    }
                    break;

                case "faster":
                    switch (sObj)
                    {
                        case "time":
                            SolarSystem.TimeBase = SolarSystem.TimeBase + (SolarSystem.TimeBase * multiplier);
                            break;

                        case "speed":
                            Velocity = Velocity + (Velocity * multiplier);
                            break;

                        case "elevation":
                            OrbitElevation += (OrbitElevation * multiplier);
                            break;

                        default:
                            return ECommandCompletionState.What;
                    }
                    break;

                case "slower":
                    switch (sObj)
                    {
                        case "time":
                            SolarSystem.TimeBase = SolarSystem.TimeBase - (SolarSystem.TimeBase * multiplier);
                            break;

                        case "speed":
                            Velocity = Velocity - (Velocity * multiplier);
                            break;

                        case "elevation":
                            OrbitElevation -= (OrbitElevation * multiplier);
                            break;

                        default:
                            return ECommandCompletionState.What;
                    }
                    break;

                case "increase":
                    goto case "faster";
                case "decrease":
                    goto case "faster";
                case "double":
                    multiplier = 2;
                    goto case "faster";
                case "half":
                    multiplier = 0.5f;
                    goto case "slower";
                case "halve":
                    goto case "half";
            }
            mObject = sObj;
            return ECommandCompletionState.Ok;
        }

        /// <summary>
        /// Driver logic for imperitive unmodified action commands
        /// </summary>
        /// <param name="cmd">Imperitive command</param>
        /// <returns>Result of command action</returns>
        private ECommandCompletionState handleImperativeCommand(string cmd)
        {
            switch (cmd)
            {
                case "snapshot":
                    goto case "picture";
                case "picture":
                    SolarSystem.TakeSnapshot();
                    break;

                case "clouds":
                    goto case "atmosphere";
                case "atmosphere":
                    SolarSystem.EarthAtmosphereVisible = !SolarSystem.EarthAtmosphereVisible;
                    break;

                case "reset":
                    PoseProvider.ResetMotionTracking();
                    goto case "zero";
                case "zero":
                    SolarSystem.PoseController.transform.position = SpaceCraft.transform.position;
                    SolarSystem.PoseController.transform.rotation = SpaceCraft.transform.rotation;
                    break;

                case "pause":
                    mProgramStack.Push(new AutopilotProgram
                    {
                        Destination = Destination,
                        Follow = Follow,
                        Orbit = Orbit,
                        Track = Track,
                        Velocity = Velocity
                    });
                    Destination = null;
                    Follow = null;
                    Orbit = null;
                    Track = null;
                    Velocity = 0;
                    break;

                case "resume":
                    var pgm = mProgramStack.Pop();
                    Destination = pgm.Destination;
                    Follow = pgm.Follow;
                    Orbit = pgm.Orbit;
                    Track = pgm.Track;
                    Velocity = pgm.Velocity;
                    break;

                case "stop":
                    Destination = null;
                    Follow = null;
                    Orbit = null;
                    Track = null;
                    Velocity = 0;
                    break;

                case "faster":
                    Velocity = Velocity + (Velocity * 0.05f);
                    break;

                case "slower":
                    Velocity = Velocity - (Velocity * 0.05f);
                    break;

                case "lock":
                    mSpaceCraftAttitudeLocked = true;
                    break;

                case "unlock":
                    mSpaceCraftAttitudeLocked = false;
                    break;

                case "mark":
                    // when release is called and the transform reset, this will be the new basis
                    mMarkPos = SolarSystem.PoseController.Transform.position;
                    mMarkAtt = SolarSystem.PoseController.Transform.rotation;
                    break;

                case "set":
                    SpaceCraft.transform.position = mMarkPos;
                    SpaceCraft.transform.rotation = mMarkAtt;
                    SolarSystem.PoseController.transform.position = mMarkPos;
                    SolarSystem.PoseController.transform.rotation = mMarkAtt;
                    break;

                case "front":
                    SolarSystem.PoseController.transform.position = SpaceCraft.transform.position;
                    SolarSystem.PoseController.transform.rotation = SpaceCraft.transform.rotation;
                    break;

                case "credits":
                    SolarSystem.RunCredits();
                    break;

                default:
                    return ECommandCompletionState.What;
            }
            return ECommandCompletionState.Ok;
        }

        private ECommandCompletionState handleSetCommand(string bObj, string val)
        {
            float theValue;
            if (!float.TryParse(val, out theValue))
                return ECommandCompletionState.How;
            switch (bObj)
            {
                case "time":
                    SolarSystem.TimeBase = theValue / 100.0f;
                    break;

                case "speed":
                    Velocity = theValue;
                    break;

                case "elevation":
                    OrbitElevation = theValue;
                    break;

                case "weather":
                    goto case "climate";
                case "climate":
                    SolarSystem.EarthAtmosphereSpin = theValue;
                    break;

                default:
                    return ECommandCompletionState.What;
            }
            return ECommandCompletionState.Ok;
        }

        /// <summary>
        /// Identify which phrase a series of words identifies
        /// </summary>
        /// <param name="words">The words received from Google</param>
        /// <returns>The phrase this corresponds to</returns>
        private EPhrase scanPhrases(string[] words)
        {
            for (int i = 0; i < mPhrases.Count; i++)
            {
                string[] test = mPhrases[i];
                bool failed = false;
                if (test.Length == words.Length)
                {
                    if (words.Where((t, j) => !t.Equals(test[j])).Any())
                    {
                        failed = true;
                    }
                    if (!failed)
                        return (EPhrase)i;
                }
            }
            return EPhrase.Unknown;
        }

        #endregion Private Methods

        #region Public Classes

        public class AutopilotProgram
        {
            #region Public Properties

            public OrbitalSystem Destination { get; set; }
            public OrbitalSystem Follow { get; set; }
            public OrbitalSystem Orbit { get; set; }
            public OrbitalSystem Track { get; set; }
            public float Velocity { get; set; }

            #endregion Public Properties
        }

        #endregion Public Classes
    }
}