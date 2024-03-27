using System;
using System.Linq;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.UI;
using UnityEngine;

namespace BulletTime
{
    internal class BulletTimeComponent : MonoBehaviour
    {
        internal static float staminaBurn = 0;
        internal static bool startBulletTime = false;
        internal static bool firstTimeTriggered = false;
        internal static Player player;

        protected static ManualLogSource Logger
        {
            get; private set;
        }
        private BulletTimeComponent()
        {
            if (Logger == null)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(BulletTimeComponent));
            }
        }
        internal static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<BulletTimeComponent>();

                Logger.LogDebug("BulletTimeComponent enabled");
            }
        }
        private void Start()
        {
            player = Singleton<GameWorld>.Instance.MainPlayer;
            staminaBurn = 0;
            startBulletTime = false;
            firstTimeTriggered = false;
        }
        public async Task Update()
        {
            if (!BulletTime.PluginEnabled.Value)
            {
                return;
            }

            if (IsKeyPressed(BulletTime.KeyBulletTime.Value))
            {
                ToggleBulletTime();
            }

            CheckStamina();
        }

        private void ToggleBulletTime()
        {
            try
            {
                if (!firstTimeTriggered)
                {
                    StartBulletTime();
                }
                else if (startBulletTime)
                {
                    StopBulletTime();
                }
                //SetRecoil(player);

            }
            catch (Exception ex) { }
        }
        private void StartBulletTime()
        {
            startBulletTime = true;
            Singleton<GUISounds>.Instance.PlaySound(BulletTime.EnterBulletAudioClip);
            Time.timeScale = BulletTime.BulletTimeScale.Value;
            firstTimeTriggered = true;
        }

        private void StopBulletTime()
        {
            startBulletTime = false;
            Singleton<GUISounds>.Instance.PlaySound(BulletTime.ExitBulletAudioClip);
            Time.timeScale = 1.0f;
            firstTimeTriggered = false;
        }

        private void CheckStamina()
        {
            if (!startBulletTime) return;

            staminaBurn = BulletTime.StaminaBurnRatePerSecond.Value * Time.unscaledDeltaTime;
            player.Physical.Stamina.Current = Mathf.Max(0, player.Physical.Stamina.Current - staminaBurn);

            if (player.Physical.Stamina.Current == 0)
            {
                StopBulletTime();
                //SetRecoil(player);
            }
        }

        /*public void SetRecoil(Player player)
        {
            //Logger.LogInfo("Original FixedUpdate Time: " + Time.deltaTime);
            player.ProceduralWeaponAnimation.HandsContainer.Recoil.FixedUpdate(Time.deltaTime);

            //Logger.LogInfo("Set the FixedUpdate of Recoil to: " + Time.deltaTime);

        }*/

        bool IsKeyPressed(KeyboardShortcut key)
        {
            if (!UnityInput.Current.GetKeyDown(key.MainKey)) return false;

            return key.Modifiers.All(modifier => UnityInput.Current.GetKey(modifier));
        }

    }
}

