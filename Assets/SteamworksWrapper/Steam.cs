﻿using UnityEngine;
using System.Collections;
using System;

namespace SteamworksWrapper {
    public sealed partial class Steam : MonoBehaviour {

        static Steam instance;
        static bool steamInitialized = false;
        static bool needsStatsToStore = false;
        private static uint appId;

        private Steam() {

        }

        public static Steam Instance {
            get {
                return instance;
            }
        }

        public static bool IsInitialized {
            get {
                return steamInitialized;
            }
        }

        private static void CancelCallback(ref PolledCallback callback) {
            if (callback != null) {
                callback.Cancel();
            }
        }
        
        private static PolledCallback WaitForDone(IPollEntity entity, Action callback) {
            return new PolledCallback(instance, entity, callback);
        }

        public static void Init(uint appId, GameObject obj) {
            //TODO check if steam exists
            try {
                if (NativeMethods.IsRestartRequired(appId)) {
                    Application.Quit();
                    return;
                }

                if (!NativeMethods.InitializeSteam()) {
                    throw new Exception("SteamAPI_Init() failed. Refer to Valve's documentation or the comment above this line for more information.");
                }

                steamInitialized = true;
                Steam.appId = appId;

                instance = obj.AddComponent<Steam>();
            } catch (Exception e) {
                if (e is DllNotFoundException) {
                    Debug.LogWarning("No SteamWrapper libraries found. Running in Steam-free mode.");
                } else {
                    Debug.LogWarning("There was a problem running Steam: " + e.Message);
                }

                steamInitialized = false;
            }
        }

        void Awake() {
            Debug.Log("Steam API initialized: " + steamInitialized);
        }

        public static void Shutdown() {
            if (!steamInitialized) {
                return;
            }

            UGC.Dispose();

            Debug.Log("Shutting down the Steam API.");
            NativeMethods.ShutdownSteam();
        }

        static void SteamDebug(int nSeverity, System.Text.StringBuilder pchDebugText) {
            Debug.LogWarning(pchDebugText);
        }

        IEnumerator TryStoringStatsAgain() {
            yield return new WaitForSeconds(5);
            needsStatsToStore = true;
        }

        private void Update() {
            if (!steamInitialized) {
                return;
            }

            if (needsStatsToStore) {
                needsStatsToStore = false;

                if (!NativeMethods.UserStats_StoreStats()) {
                    Debug.LogError("Failed to store Steam stats. Check your internet connection. Trying again in 5 seconds.");
                    StartCoroutine(TryStoringStatsAgain());
                }
            }

            NativeMethods.RunCallbacks();
        }
    }
}