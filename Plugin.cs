using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.AssetsManager;
using EFT.UI;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace dvize.BulletTime
{
    [BepInPlugin("com.dvize.BulletTime", "dvize.BulletTime", "1.2.0")]

    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> PluginEnabled;
        public static ConfigEntry<float> TimeTestMultiplier;
        public static ConfigEntry<float> BulletTimeScale;
        public static ConfigEntry<int> MaxBulletTimeSeconds;
        public static ConfigEntry<int> CooldownPeriodSeconds;
        public static ConfigEntry<KeyboardShortcut> KeyBulletTime;
        public static ConfigEntry<Vector3> MoveScopeCamera;

        public float CoolDownElapsedSecond = 0f;
        public float BulletTimeElapsedSecond = 0f;
        public bool startBulletTime = false;
        public bool firstTimeTriggered = false;
        private static AudioClip EnterBulletAudioClip;
        private static AudioClip ExitBulletAudioClip;
        async void Awake()
        {
            PluginEnabled = Config.Bind(
                "Main Settings",
                "Plugin on/off",
                true,
                "");

            BulletTimeScale = Config.Bind(
                "Main Settings",
                "Bullet Time Scale",
                0.40f,
                "Set how slow the Bullet Time Scale goes to");

            TimeTestMultiplier = Config.Bind(
                "Main Settings",
                "Time Test Multiplier",
                0.50f,
                "Multiplies Time.DeltaTime");

            MaxBulletTimeSeconds = Config.Bind(
                "Main Settings",
                "Maximum Bullet Time (in Seconds)",
                10,
                "Set how long the bullet time can last max before cooldown.");

            CooldownPeriodSeconds = Config.Bind(
                "Main Settings",
                "Cooldown (in Seconds)",
                120,
                "Set the cooldown period before being able to trigger Bullet Time.");

            KeyBulletTime = Config.Bind(
                "Hotkey for Bullet Time",
                "Use Bullet Time",
                new KeyboardShortcut(KeyCode.Mouse4),
                "Key for Bullet Time toggle");

            MoveScopeCamera = Config.Bind(
                "Move Scope Camera When Zoomed",
                "fix aiming alignment",
                new Vector3(0f, 0f, 0f),
                "Move Scope Camera Position");

            string uri = "file://" + (BepInEx.Paths.PluginPath + "\\dvize.BulletTime\\enterbullet.ogg");
            string uri2 = "file://" + (BepInEx.Paths.PluginPath + "\\dvize.BulletTime\\exitbullet.ogg");

            using (UnityWebRequest web = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
            {
                var asyncOperation = web.SendWebRequest();

                while (!asyncOperation.isDone)
                    await Task.Yield();

                if (!web.isNetworkError && !web.isHttpError)
                {
                    EnterBulletAudioClip = DownloadHandlerAudioClip.GetContent(web);
                }
                else
                {
                    Debug.LogError($"Can't load audio at path: '{uri}', error: {web.error}");
                }
            }

            using (UnityWebRequest web = UnityWebRequestMultimedia.GetAudioClip(uri2, AudioType.OGGVORBIS))
            {
                var asyncOperation = web.SendWebRequest();

                while (!asyncOperation.isDone)
                    await Task.Yield();

                if (!web.isNetworkError && !web.isHttpError)
                {
                    ExitBulletAudioClip = DownloadHandlerAudioClip.GetContent(web);
                }
                else
                {
                    Debug.LogError($"Can't load audio at path: '{uri}', error: {web.error}");
                }
            }

        }

        
        private void Update()
        {
            if (Plugin.PluginEnabled.Value)
            {

                if (!Singleton<GameWorld>.Instantiated)
                {
                    return;
                }

                if (Camera.main == null)
                {
                    return;
                }

                CoolDownElapsedSecond += Time.unscaledDeltaTime;
                

                if (Plugin.KeyBulletTime.Value.IsUp())
                {
                    if (!firstTimeTriggered)
                    {
                        //Logger.LogInfo("BulletTime: First Key Press - Activate Bullet Time");
                        firstTimeTriggered = true;
                        startBulletTime = true;
                        Singleton<GUISounds>.Instance.PlaySound(Plugin.EnterBulletAudioClip);
                        Time.timeScale = Plugin.BulletTimeScale.Value;

                        setRecoil();
                    }
                    else if (firstTimeTriggered)
                    {
                        //Logger.LogInfo("BulletTime: Second Key Press - Deactivate Bullet Time");
                        startBulletTime = false;
                        firstTimeTriggered = false;
                        BulletTimeElapsedSecond = 0f;
                        CoolDownElapsedSecond = 0f;
                        Singleton<GUISounds>.Instance.PlaySound(Plugin.ExitBulletAudioClip);
                        Time.timeScale = 1.0f;
                        undoRecoil();

                    }  
                }
                //bullets are shooting above target when full auto more than if not shooting with slo-mo on
                // fire rate seems not affected by time.timescale

                
                
                if (startBulletTime)
                {
                    //as this is per frame, sufficient stamina only needs to be above 0
                    //bool staminasufficient = player.Physical.Stamina.Current > 0;
                    bool cooldowndone = CoolDownElapsedSecond >= Plugin.CooldownPeriodSeconds.Value;

                    if (cooldowndone)
                    {
                        Time.timeScale = Plugin.BulletTimeScale.Value;
                        BulletTimeElapsedSecond += Time.unscaledDeltaTime;
                        //player.Physical.Stamina.Current -= (elapsedSecond / Plugin.StaminaDrainPerSecond.Value);
                        setRecoil();
                    }

                    if (BulletTimeElapsedSecond >= Plugin.MaxBulletTimeSeconds.Value)
                    {
                        //Logger.LogInfo("BulletTime: Stamina Ran Out");
                        Time.timeScale = 1.0f;
                        BulletTimeElapsedSecond = 0f;
                        CoolDownElapsedSecond = 0f;
                        startBulletTime = false;
                        firstTimeTriggered = false; //reset key incase stamina runs out
                        Singleton<GUISounds>.Instance.PlaySound(Plugin.ExitBulletAudioClip);
                        undoRecoil();
                        //add coroutinie to scale the maincamera fov?  also need to figure out the math to do a smooth fov change.

                    }
                }


            }
            

        }

        public void setRecoil()
        {
            try
            {
                var player = Singleton<GameWorld>.Instance.AllPlayers[0];
               

                Logger.LogInfo("Original FixedUpdate Time: " + Time.deltaTime);
                player.ProceduralWeaponAnimation.HandsContainer.Recoil.FixedUpdate(Time.deltaTime);
                Logger.LogInfo("Set the FixedUpdate of Recoil to: " + Time.deltaTime);
            }
            catch
            {
                Logger.LogInfo("Failed getting Playerspring and setting FixedUpdate");
            }
        }

        public void undoRecoil()
        {
            try
            {
                var player = Singleton<GameWorld>.Instance.AllPlayers[0];
                player.ProceduralWeaponAnimation.HandsContainer.Recoil.FixedUpdate(Time.deltaTime);
            }
            catch
            {
                Logger.LogInfo("Failed resetting original FixedUpdate");
            }
        }

        //public void FixedUpdate()
        //{
        //    var ballistics = Singleton<GameWorld>.Instance.SharedBallisticsCalculator;
        //    //almost like physics calculation for muzzle recoil calc is too slow

        //    for (int i = 0; i < ballistics.ActiveShotsCount; i++)
        //    {
        //        Logger.LogInfo("Active Bullet #: " + i + "HitPitch: " + ballistics.GetActiveShot(i).HitPitch);
        //        Logger.LogInfo("Active Bullet #: " + i + "HitYaw: " + ballistics.GetActiveShot(i).HitYaw);
        //        Logger.LogInfo("Active Bullet #: " + i + "HitPoint: " + ballistics.GetActiveShot(i).HitPoint);
        //        Logger.LogInfo("Active Bullet #: " + i + "CurrentDirection: " + ballistics.GetActiveShot(i).CurrentDirection);
        //        Logger.LogInfo("Active Bullet #: " + i + "CurrentPosition: " + ballistics.GetActiveShot(i).CurrentPosition);
        //        Logger.LogInfo("Active Bullet #: " + i + "CurrentVelocity: " + ballistics.GetActiveShot(i).CurrentVelocity);
        //        Logger.LogInfo("Active Bullet #: " + i + "VelocityMagnitude: " + ballistics.GetActiveShot(i).VelocityMagnitude);
        //        Logger.LogInfo("Active Bullet #: " + i + "Initial Speed: " + ballistics.GetActiveShot(i).InitialSpeed);
        //        Logger.LogInfo("Active Bullet #: " + i + "Speed: " + ballistics.GetActiveShot(i).Speed);
        //        ballistics.GetActiveShot(i).Tick(Time.fixedDeltaTime);
        //        //GStruct220 bullet shot
        //        //GStruct221 Start Position and Start Velocity
        //        //ballistics.GetActiveShot(i).Speed = .001f;

        //    }
        //}



    }
}