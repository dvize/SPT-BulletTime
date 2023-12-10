using System;
using System.Threading.Tasks;
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

            if (BulletTime.KeyBulletTime.Value.IsDown())
            {
                //if key is down and bullet time is not active, activate it
                if (!firstTimeTriggered && !startBulletTime)
                {
                    StartBulletTime();
                    setRecoil(player);
                }
                else if (firstTimeTriggered && startBulletTime)
                {
                    StopBulletTime();
                    setRecoil(player);
                }
            }

            //in update loop, if we are in bullet time and the firsttimetriggered is true, then do stuff
            CheckStamina();
        }

        private void StartBulletTime()
        {
            //Logger.LogInfo("Starting Bullet Time");
            startBulletTime = true;
            Singleton<GUISounds>.Instance.PlaySound(BulletTime.EnterBulletAudioClip);
            Time.timeScale = BulletTime.BulletTimeScale.Value;
            firstTimeTriggered = true;
        }

        private void StopBulletTime()
        {
            //Logger.LogInfo("Ending Bullet Time Early by Keypress");
            startBulletTime = false;
            Singleton<GUISounds>.Instance.PlaySound(BulletTime.ExitBulletAudioClip);
            Time.timeScale = 1.0f;
            firstTimeTriggered = false;
        }

        private void CheckStamina()
        {
            if (startBulletTime)
            {
                //determine rate at which stamina burns based on BulletTime.BulletTimeStaminaBurnRatePerSecond.Value and Time.deltaTime
                staminaBurn = BulletTime.StaminaBurnRatePerSecond.Value * Time.unscaledDeltaTime;
                
                //update stamina unless the staminaBurn will cause it to be less than 0, then just set to 0
                if ((player.Physical.Stamina.Current - staminaBurn) <= 0f)
                {
                    player.Physical.Stamina.Current = 0f;
                }
                else
                {
                    player.Physical.Stamina.Current -= staminaBurn;
                }


                if (player.Physical.Stamina.Current == 0)
                {
                    //stop bullettime
                    StopBulletTime();
                    setRecoil(player);
                }
            }
        }

        public void setRecoil(Player player)
        {
            try
            {
                //Logger.LogInfo("Original FixedUpdate Time: " + Time.deltaTime);
                player.ProceduralWeaponAnimation.HandsContainer.Recoil.FixedUpdate(Time.deltaTime);

                //Logger.LogInfo("Set the FixedUpdate of Recoil to: " + Time.deltaTime);
            }
            catch
            {
            }
        }


    }
}

