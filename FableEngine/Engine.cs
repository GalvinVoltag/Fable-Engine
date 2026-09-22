using System.Diagnostics;
using FableEngine.Core;
using FableEngine.Debugging;
using FableEngine.Math;
using Debug = FableEngine.Debugging.Debug;

namespace FableEngine
{
    public enum GlobalMethodPosition  // todo: carry to Engine
    {
        BeforeWake = 0,
        AfterWake = 1,
        BeforeFrame = 1,
        AfterFrame = 2,
        BeforeFixed = 2,
        AfterFixed = 3,
        BeforePostFrame = 3,
        AfterPostFrame = 4
    }
    
    public static class Engine
    {
        /// <summary>
        /// How many frames has been calculated since the start of the whole program
        /// </summary>
        public static int frameCount { get; private set; } = 0;
        /// <summary>
        /// this is the upper FPS limit for the whole program, and all windows. In order to set FPS limit per window,
        /// use separate threads.
        /// todo add multi-threading support
        /// </summary>
        public static double MaxFPS = 60;
        /// <summary>
        /// True if current code is executing in the context of a window, False if the code is window-agnostic
        /// </summary>
        public static bool isInWindowContext { get; private set; } = false;

        /// <summary>
        /// current Frames Per Second of the whole program
        /// </summary>
        public static float FPS { get; private set; } = 1;
        
        /// <summary>
        /// start running the application. This will trigger the update and render loop for all GameWindows
        /// and ManagedRoutines. Any code written after this will only execute after the application and
        /// all the loops have ended.
        /// </summary>
        public static void Run()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            double lastTime = 0;
        
            while (GameWindow.openWindowCount > 0)
            {
                double currentTime = stopwatch.Elapsed.TotalSeconds;
                double deltaTime = currentTime - lastTime;
                lastTime = currentTime;
                frameCount++;
            
                GlobalUpdate(deltaTime);
            
                // limit framerate
                double targetFrameTime = 1.0 / MaxFPS;
                double elapsed = stopwatch.Elapsed.TotalSeconds - currentTime;
                if (!(elapsed < targetFrameTime)) continue;
                int sleepTime = (int)((targetFrameTime - elapsed) * 1000);
                if (sleepTime > 0)
                    Thread.Sleep(sleepTime);
            }
        }
        
        private static readonly Action?[] GlobalMethods = new Action?[5];

        public static void RegisterGlobalMethod(GlobalMethodPosition globalMethodPosition, Action method)
        {
            if (GlobalMethods[(int)globalMethodPosition] == null) 
                GlobalMethods[(int)globalMethodPosition] = new Action(method);
            else
                GlobalMethods[(int)globalMethodPosition] += method;
        }

        public static void UnregisterGlobalMethod(Action method, GlobalMethodPosition? methodPosition = null)
        {
            if (methodPosition == null)
            {
                for (int i = 0; i < 5; i++)
                {
                    GlobalMethods[i] -= method;
                }
            }
            else
            {
                GlobalMethods[(int)methodPosition] -= method;
            }
        }
        
        /// <summary>
        /// Run a method through a try catch and continue without crashing current loop.
        /// </summary>
        /// <returns>true when executed successfully, and false when an error occurs</returns>
        public static bool RunEnclosed(Action method, Action? errorAction = null)
        {
            try { method(); return true; }catch (Exception e) { Debug.Log(e, DebugLogLevel.Error); errorAction?.Invoke(); return false; }
        }
        
        //pending for optimization and rework
        private static void GlobalUpdate(double deltaTime) // update all windows and ManagedRoutines
        {
            Time.OverrideRawDelta((float)deltaTime);

            if (Time.deltaTime != 0)
                FPS = 1 / Time.deltaTime;
            
            GlobalMethods[0]?.Invoke();
            
            foreach (var MR in ManagedRoutine.NewBorns) {
                MR.Wake(); MR.newBorn = false;
            }
            ManagedRoutine.NewBorns.Clear();
            
            int loops = (int)(Time.fixedTimeAccumulation / Time.fixedTimeRate); // how many fixedupdate in this frame
            Time.fixedTimeAccumulation -= loops*Time.fixedTimeRate - Time.deltaTime; // subtract the remainder and add the current deltaTime
            // Time.fixedTimeAccumulation += Time.deltaTime;
            
            Time.OverrideTemporalLayer(0); // todo implement temporal layers
            
            GlobalMethods[1]?.Invoke();

            foreach (var MR in ManagedRoutine.FrameRunners)
                RunEnclosed(MR.Frame, () =>
                {
                    Debug.Log("The error casting ManagedRoutine will be deactivated to avoid further errors!",
                        DebugLogLevel.Error);
                    MR.active = false;
                });
            
            GlobalMethods[2]?.Invoke();
            
            foreach (var MR in ManagedRoutine.FixedRunners)
                for (int i=0; i < loops; i++)
                    RunEnclosed(MR.Fixed, () =>
                    {
                        Debug.Log("The error casting ManagedRoutine will be deactivated to avoid further errors!",
                            DebugLogLevel.Error);
                        MR.active = false;
                    });
            
            GlobalMethods[3]?.Invoke();
                
            // delete dead ManagedRoutines outside the loop 
            foreach (var MR in ManagedRoutine.deathRow)
                RunEnclosed(MR.Dispose, () =>
                {
                    Debug.Log("The error casting ManagedRoutine will be deactivated to avoid further errors!",
                        DebugLogLevel.Error);
                    MR.active = false;
                });

            foreach (var MR in ManagedRoutine.activationRow)
            {
                if (MR.active)
                {
                    ManagedRoutine.FrameRunners.Add(MR);
                    ManagedRoutine.InputRunners.Add(MR);
                    ManagedRoutine.FixedRunners.Add(MR);
                    ManagedRoutine.DrawRunners.Add(MR);
                    ManagedRoutine.PostFrameRunners.Add(MR);
                }
                else
                {
                    ManagedRoutine.FrameRunners.Remove(MR);
                    ManagedRoutine.InputRunners.Remove(MR);
                    ManagedRoutine.FixedRunners.Remove(MR);
                    ManagedRoutine.DrawRunners.Remove(MR);
                    ManagedRoutine.PostFrameRunners.Remove(MR);
                }
            }
            
            ManagedRoutine.deathRow.Clear();

            isInWindowContext = true; // code from here is window-dependent
            // render and update window by window
            foreach (var win in GameWindow.allWindowsList)
                win.WindowUpdate();
            
            isInWindowContext = false; // code from here is window-agnostic
            
            foreach (var MR in ManagedRoutine.PostFrameRunners) // PostFrame is after render
                RunEnclosed(MR.PostFrame, () =>
                {
                    Debug.Log("The error casting ManagedRoutine will be deactivated to avoid further errors!",
                        DebugLogLevel.Error);
                    MR.active = false;
                });
            
            GlobalMethods[4]?.Invoke();
            
            // Input.current = null;
        }
    }
}