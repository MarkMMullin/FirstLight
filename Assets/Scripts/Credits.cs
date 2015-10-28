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
using UnityEngine;
using Random = System.Random;

#endregion

namespace Assets.Scripts
{
    /// <summary>
    /// Used to carry and display the credits crawl
    /// </summary>
    public class Credits
    {
        #region Private Fields

        private readonly Random mCrawlRandomizer = new Random();

        private readonly string[] mCredits = {
            "Foremost, this time, to my long suffering dog",
            "Pippin the Newfoundland, who has been wondering",
            "just what the hell has happened.",
            "and to my son Kalen, who I have not seen enough",
            "of.  I can only say I hope it has been worth it",
            "",
            "",
            "Many thanks to Thomas Meyers, a voice of sanity",
            "in the maelstrom.  For voices less sane, but",
            "brilliance follows few rules, thanks to Chuck",
            "Knowledge and Kris Kitchen.  And to all the",
            "others of you with whom I have interacted",
            "",
            "",
            "A deep thanks to Brenda Laurel - I remembered",
            "the story of calibrating for the Hoodoo and",
            "narrowly avoided breaking my damn neck.  For",
            "the rest of you, this means stay in a damn",
            "chair while you are using this app or you're",
            "going to be spending your time in a hospital bed",
            "",
            "",
            "A deep thanks to all I ever met and worked with",
            "at Taligent.  It was an amazing place, I learned",
            "so much from all of you, and I am amazed by how",
            "many of those lessons are so current in this",
            "finally aborning technology",
            "",
            "",
            "And finally, thanks to all that led the progression",
            "from A^2+B^2=C^2, through 0..9, and into e^iπ+1=0",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            ""
        };

        private int mCreditLine;
        private float mLastTime;
        private int mLineCount;
        private int mLineLimit;

        #endregion Private Fields

        #region Public Methods

        public bool Advance(UnityEngine.UI.Text textDisplay)
        {
            if (Math.Abs(mLastTime) > 0 && Math.Abs(Time.time - mLastTime) < 0.25)
                return true;
            if (mCreditLine == 0 && mLineCount == 0 && mLineLimit == 0)
            {
                // starting the crawl
                mLineLimit = mCreditLine + mCrawlRandomizer.Next(15, 21);
            }
            textDisplay.text = textDisplay.text + "\n" + mCredits[mCreditLine++];
            if (mCreditLine >= mLineLimit)
            {
                textDisplay.text = "";
                mLineLimit = mCreditLine + mCrawlRandomizer.Next(15, 21);
            }
            if (mCreditLine >= mCredits.Length)
            {
                mCreditLine = 0;
                mLineCount = 0;
                mLineLimit = 0;
                textDisplay.text = "";
                return false;
            }
            mLastTime = Time.time;
            return true;
        }

        #endregion Public Methods
    }
}