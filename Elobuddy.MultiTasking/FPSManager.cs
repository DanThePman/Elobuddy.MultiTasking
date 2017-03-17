using System;
using System.Collections.Generic;
using EloBuddy;

namespace Elobuddy.MultiTasking
{
    static class FPSManager
    {
        private static List<float> FpsList = new List<float>(100);

        public static void Init()
        {
            set = Environment.TickCount;
            Game.OnUpdate += GameOnOnUpdate;
            Drawing.OnDraw += DrawingOnOnDraw;
        }

        private static int calls, set;
        private static float FPS;
        public static float FPS_DROP_PERCENTAGE { get; internal set; } = 20;
        private static void DrawingOnOnDraw(EventArgs args)
        {
            calls++;
        }

        private static int LastFpsDrop;
        public static bool HasFpsDrop(float t) => Environment.TickCount - LastFpsDrop < t;

        private static void GameOnOnUpdate(EventArgs args)
        {
            GetFPS();

            if (FpsList.Count > 1)
            {
                var previous = FpsList[FpsList.Count - 2];
                var current = FpsList[FpsList.Count - 1];
                var m = current - previous;

                if (m < -FPS * FPS_DROP_PERCENTAGE / 100)
                {
                    LastFpsDrop = Environment.TickCount;
                }
            }
        }

        private static void GetFPS()
        {
            if (Environment.TickCount - set < 1000)
            {
                return;
            }
            set = Environment.TickCount;

            FPS = calls;
            calls = 0;

            if (FpsList.Count >= 100)
                FpsList.Clear();

            FpsList.Add(FPS);
        }
    }
}
