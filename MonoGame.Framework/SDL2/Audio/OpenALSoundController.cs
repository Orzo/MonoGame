#region License
// /*
// Microsoft Public License (Ms-PL)
// MonoGame - Copyright © 2009 The MonoGame Team
// 
// All rights reserved.
// 
// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
// accept the license, do not use the software.
// 
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under
// U.S. copyright law.
// 
// A "contribution" is the original software, or any additions or changes to the software.
// A "contributor" is any person that distributes its contribution under this license.
// "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
// your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
// notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
// a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
// code form, you may only do so under a license that complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
// or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
// permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
// purpose and non-infringement.
// */
#endregion License

#region Using Statements
using System;
using System.Collections.Generic;

#if IOS
using System.Runtime.InteropServices;
#endif

using OpenTK;
using OpenTK.Audio.OpenAL;
#endregion

namespace Microsoft.Xna.Framework.Audio
{
    internal sealed class OpenALSoundController : IDisposable
    {
        // Stores the controller instance and if if there is a working context in it.
        private static OpenALSoundController INTERNAL_instance = null;
        private bool INTERNAL_soundAvailable = false;

        // OpenAL Device/Context Handles
        private IntPtr _device;
        private ContextHandle _context;

        // Used to store SoundEffectInstances generated internally.
        internal List<SoundEffectInstance> instancePool;

        // Used to store looping SoundEffectInstances
        internal List<SoundEffectInstance> loopingInstances;

        private void CheckALError()
        {
            ALError err = AL.GetError();

            if (err == ALError.NoError)
            {
                return;
            }

            System.Console.WriteLine("OpenAL Error: " + err);
        }

        private bool CheckALCError(string message)
        {
            AlcError err = Alc.GetError(_device);

            if (err == AlcError.NoError)
            {
                return false;
            }

            System.Console.WriteLine(message + " - OpenAL Device Error: " + err);

            return true;
        }

        private bool INTERNAL_initSoundController()
        {
#if IOS
            alcMacOSXMixerOutputRate(44100);
#endif
            try
            {
                _device = Alc.OpenDevice(string.Empty);
            }
            catch
            {
                return false;
            }
            if (CheckALCError("Could not open AL device") || _device == IntPtr.Zero)
            {
                return false;
            }

            int[] attribute = new int[0];
            _context = Alc.CreateContext(_device, attribute);
            if (CheckALCError("Could not create OpenAL context") || _context == ContextHandle.Zero)
            {
                Dispose(true);
                return false;
            }

            Alc.MakeContextCurrent(_context);
            if (CheckALCError("Could not make OpenAL context current"))
            {
                Dispose(true);
                return false;
            }

            return true;
        }

        private OpenALSoundController()
        {
            INTERNAL_soundAvailable = INTERNAL_initSoundController();
            instancePool = new List<SoundEffectInstance>();
            loopingInstances = new List<SoundEffectInstance>();
        }

        public void Dispose()
        {
            Dispose(false);
        }

        private void Dispose(bool force)
        {
            if (INTERNAL_soundAvailable || force)
            {
                Alc.MakeContextCurrent(ContextHandle.Zero);
                if (_context != ContextHandle.Zero)
                {
                    Alc.DestroyContext (_context);
                    _context = ContextHandle.Zero;
                }
                if (_device != IntPtr.Zero)
                {
                    Alc.CloseDevice (_device);
                    _device = IntPtr.Zero;
                }
                INTERNAL_soundAvailable = false;
            }
        }

        public static OpenALSoundController GetInstance
        {
            get
            {
                if (INTERNAL_instance == null)
                {
                    INTERNAL_instance = new OpenALSoundController();
                }
                return INTERNAL_instance;
            }
        }

        public void Update()
        {
            if (!INTERNAL_soundAvailable)
            {
                return;
            }
#if DEBUG
            CheckALError();
#endif
            for (int i = 0; i < instancePool.Count; i++)
            {
                if (instancePool[i].State == SoundState.Stopped)
                {
                    instancePool[i].Dispose();
                    instancePool.RemoveAt(i);
                    i--;
                }
            }
            foreach (SoundEffectInstance sfi in loopingInstances)
            {
                sfi.INTERNAL_checkLoop();
            }
        }

#if IOS
        [DllImport("/System/Library/Frameworks/OpenAL.framework/OpenAL", EntryPoint = "alcMacOSXMixerOutputRate")]
        static extern void alcMacOSXMixerOutputRate(double rate);
#endif

    }
}