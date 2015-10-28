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

using System.Diagnostics.CodeAnalysis;
using UnityEngine;

#endregion

namespace Assets.Scripts
{
    /// <summary>
    /// High level interface to the Java library that provides access to Google Voice
    /// </summary>
    [SuppressMessage("ReSharper", "UseNullPropagation")]
    public class SpeechToText : MonoBehaviour
    {
        private static bool smInitialized;
#pragma warning disable 649
        private static AndroidJavaObject speechActivityClass;
#pragma warning restore 649
        public static string Result = "Empty";

        /// <summary>
        /// Randomizer of obnoxious responses
        /// </summary>
        private static readonly System.Random MESSAGE_SEED = new System.Random();

        private static readonly string[] WHAT_RESPONSES = {
            "I did't get that",
            "What you say makes no sense",
            "Excuse me",
            "I didn't understand what",
            "WAT",
            "what",
            "please speak more clearly",
            "did you loose the manual",
            "google isn't exactly helping here"
        };

        private static readonly string[] HOW_RESPONSES = {
            "That cannot be done",
            "I can't do that",
            "I'm only a computer, I can't do that",
            "Unreasonable request terminated",
            "Unreasonable request terminated, terminating operator next",
            "Exactly how would you recommend I do that"
        };

        private static void initialize()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        unityRootActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
        speechActivityClass = new AndroidJavaClass("com.ntx24.speechservice.speechservice.SpeechController");
        speechActivityClass.CallStatic("StartSpeechFragment", unityRootActivity);
#endif
            smInitialized = true;
        }

        public static void Listen()
        {
            if (!smInitialized)
                initialize();
            if (speechActivityClass != null)
                speechActivityClass.CallStatic("ListenQuietly");
        }

        /// <summary>
        /// True if listening should be preceded by an audio cue.
        /// </summary>
        public static void ListenVisibly()
        {
            if (!smInitialized)
                initialize();
            if (speechActivityClass != null)
                speechActivityClass.CallStatic("ListenVisibly");
        }

        /// <summary>
        /// True if the system is currently actively listening for speech
        /// Please do not use this to create race conditions, it's only safe
        /// insofar as it's telling you what it read last, not what the exact
        /// state of the flag is at this instance in time
        /// </summary>
        /// <returns></returns>
        public static bool IsListening()
        {
            if (speechActivityClass == null)
                return false;
            return speechActivityClass.CallStatic<bool>("IsListening");
        }

        /// <summary>
        /// Get the text for the last processed speech sample
        /// </summary>
        /// <returns></returns>
        public static string SpeechResult()
        {
            if (speechActivityClass == null)
                return null;
            Result = speechActivityClass.CallStatic<string>("LastConvertedSpeech");
            if (Result.Equals("No speech input"))
                Result = null;
            return Result;
        }

        /// <summary>
        /// Put up a message - this really doesn't factor well here
        /// </summary>
        /// <param name="toastmsg"></param>
        public static void MakeToast(string toastmsg)
        {
            if (!smInitialized)
                initialize();
            if (speechActivityClass != null)
                speechActivityClass.CallStatic("MakeToast", toastmsg);
        }

        /// <summary>
        /// Used to generate spoken output per standard Android text to speech settings
        /// </summary>
        /// <param name="text"></param>
        public static void Speak(string text)
        {
            if (!smInitialized)
                initialize();
            if (speechActivityClass != null)
                speechActivityClass.CallStatic("Speak", text);
        }

        /// <summary>
        /// I have no idea what you're talking about (credit to Bill Gates, seriously)
        /// </summary>
        public static void WhatMessage()
        {
            Speak(WHAT_RESPONSES[MESSAGE_SEED.Next(0, WHAT_RESPONSES.Length - 1)]);
        }

        /// <summary>
        /// I understand, but you really must be crazy (credit to Bill Gates, seriously)
        /// </summary>
        public static void HowMessage()
        {
            Speak(HOW_RESPONSES[MESSAGE_SEED.Next(0, HOW_RESPONSES.Length - 1)]);
        }
    }
}