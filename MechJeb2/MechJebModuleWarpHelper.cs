﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleWarpHelper : DisplayModule
    {
        public enum WarpTarget { Periapsis, Apoapsis, Node, SoI, Time }
        static string[] warpTargetStrings = new string[] { "periapsis", "apoapsis", "maneuver node", "SoI transition", "Time" };
        static readonly int numWarpTargets = Enum.GetNames(typeof(WarpTarget)).Length;
        [Persistent(pass = (int)Pass.Global)]
        public WarpTarget warpTarget = WarpTarget.Periapsis;

        [Persistent(pass = (int)Pass.Global)]
        public EditableTime leadTime = 0;

        public bool warping = false;

        EditableTime timeOffset = 0;

        double targetUT = 0;

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Warp to: ", GUILayout.ExpandWidth(false));
            warpTarget = (WarpTarget)GuiUtils.ArrowSelector((int)warpTarget, numWarpTargets, warpTargetStrings[(int)warpTarget]);
            GUILayout.EndHorizontal();

            if (warpTarget == WarpTarget.Time)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Warp for: ", GUILayout.ExpandWidth(true));
                timeOffset.text = GUILayout.TextField(timeOffset.text, GUILayout.Width(100));
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();

            GuiUtils.SimpleTextBox("Lead time: ", leadTime, "");

            if (warping)
            {
                if (GUILayout.Button("Abort"))
                {
                    warping = false;
                    core.warp.MinimumWarp(true);
                }
            }
            else
            {
                if (GUILayout.Button("Warp")) 
                {
                    warping = true;

                    switch (warpTarget)
                    {
                        case WarpTarget.Periapsis:
                            targetUT = orbit.NextPeriapsisTime(vesselState.time);
                            break;

                        case WarpTarget.Apoapsis:
                            if (orbit.eccentricity < 1) targetUT = orbit.NextApoapsisTime(vesselState.time);
                            break;

                        case WarpTarget.SoI:
                            if (orbit.patchEndTransition != Orbit.PatchTransitionType.FINAL) targetUT = orbit.EndUT;
                            break;

                        case WarpTarget.Node:
                            if (vessel.patchedConicSolver.maneuverNodes.Any()) targetUT = vessel.patchedConicSolver.maneuverNodes[0].UT;
                            break;

                        case WarpTarget.Time:
                            targetUT = vesselState.time + timeOffset;
                            break;

                        default:
                            targetUT = vesselState.time;
                            break;
                    }
                }
            }

            GUILayout.EndHorizontal();

            if (warping) GUILayout.Label("Warping to " + (leadTime > 0 ? GuiUtils.TimeToDHMS(leadTime) + " before " : "") + warpTargetStrings[(int)warpTarget] + ".");

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override void OnFixedUpdate()
        {
            if (!warping) return;

            double target = targetUT - leadTime;

            if (target < vesselState.time + 1)
            {
                core.warp.MinimumWarp(true);
                warping = false;
            }
            else
            {
                core.warp.WarpToUT(target);
            }
        }

        public override UnityEngine.GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(240), GUILayout.Height(50) };
        }

        public override string GetName()
        {
            return "Warp Helper";
        }

        public MechJebModuleWarpHelper(MechJebCore core) : base(core) { }
    }
}
