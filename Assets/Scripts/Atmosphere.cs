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
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// Used to surround a solar body with an atmosphere that can spin at
    /// an offset rate and distance from the underlying body
    /// </summary>
    public class Atmosphere
    {
        #region Public Constructors

        public Atmosphere()
        {
        }

        public Atmosphere(GameObject spaceCraft, OrbitalSystem rootObject, string name,
            int idx, float dist, float spin, float offset, Vector3 hdg,
            bool enterable, Texture2D txtr = null,
            AtmosphereTriggerScript.AtmosphereEntryHandler entryHandler = null)
        {
            SpinSum = 0; Name = name; Index = idx; Distance = dist; Spin = spin; SpinOffset = offset;
            Texture = txtr;
            Distance = dist;
            Shape = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Shape.name = name;
            SphereCollider sc = Shape.GetComponent<SphereCollider>();
            //sc.radius = 1;
            float baseSize = rootObject.SimulationRadius;
            baseSize += Distance;
            Shape.transform.parent = rootObject.Root.transform;
            Shape.transform.position = rootObject.Root.transform.position;
            Shape.transform.rotation = rootObject.Root.transform.rotation;
            if (!enterable)
            {
                Rigidbody rb = Shape.AddComponent<Rigidbody>();
                rb.drag = 0.0f;
                rb.mass = 0;
                rb.angularDrag = 0;
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            if (enterable)
            {
                sc.isTrigger = true;
                AtmosphereTriggerScript ts = Shape.AddComponent<AtmosphereTriggerScript>();
                ts.SpaceCraft = spaceCraft;
                ts.EntryHandler = entryHandler;
            }
            if (Texture == null)
            {
                Shape.GetComponent<MeshRenderer>().enabled = false;
            }
            else
            {
                MeshRenderer mr = Shape.GetComponent<MeshRenderer>();
                mr.enabled = true;
                //mr.material.SetOverrideTag("RenderType", "Transparent");
                mr.material.SetFloat("_Mode", 2);
                mr.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mr.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mr.material.SetInt("_ZWrite", 0);
                mr.material.DisableKeyword("_ALPHATEST_ON");
                mr.material.DisableKeyword("_ALPHABLEND_ON");
                mr.material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                mr.material.renderQueue = 3000;

                mr.material.mainTexture = Texture;
            }
            Shape.transform.localScale = new Vector3(baseSize, baseSize, baseSize);

            // m_shape->SetOrderHint(idx + 1);
            BaseHeading = hdg;
        }

        #endregion Public Constructors

        #region Public Properties

        public Vector3 BaseHeading { get; private set; }
        public float Distance { get; private set; }
        public int Index { get; set; }
        public Vector3 LbHeading { get; private set; }
        public Vector2 LightBand { get; private set; }
        public string Name { get; set; }
        public GameObject Shape { get; private set; }
        public float Spin { get; set; }
        public float SpinOffset { get; set; }
        public float SpinSum { get; set; }
        public Texture2D Texture { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public Texture2D GetTexture()
        {
            return Texture;
        }

        public void OnTriggerEnter(Collider other)
        {
        }
        public void SetTexture(Texture2D t)
        {
            Texture = t;
            Shape.GetComponent<Renderer>().material.mainTexture = Texture;
        }

        #endregion Public Methods

        #region Private Methods

        // Use this for initialization
        [UsedImplicitly]
        private
        // ReSharper disable once InconsistentNaming
        void Start()
        {
        }

        // Update is called once per frame
        [UsedImplicitly]
        private
        // ReSharper disable once InconsistentNaming
        void Update()
        {
        }

        #endregion Private Methods
    }
}