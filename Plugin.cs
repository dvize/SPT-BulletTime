using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.UI;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace dvize.BulletTime
{
    [BepInPlugin("com.dvize.BulletTime", "dvize.BulletTime", "1.3.0")]

    public class Plugin : BaseUnityPlugin
    {
        private static ConfigEntry<bool> PluginEnabled;
        private static ConfigEntry<float> BulletTimeScale;
        private static ConfigEntry<int> MaxBulletTimeSeconds;
        private static ConfigEntry<int> CooldownPeriodSeconds;
        private static ConfigEntry<KeyboardShortcut> KeyBulletTime;
        private static AudioClip EnterBulletAudioClip;
        private static AudioClip ExitBulletAudioClip;
        private async void Awake()
        {
            PluginEnabled = Config.Bind(
                "Main Settings",
                "Plugin on/off",
                true,
                "Plugin Enable/Disable");

            BulletTimeScale = Config.Bind(
                "Main Settings",
                "Bullet Time Scale",
                0.40f,
                "Set how slow the Bullet Time Scale goes to");

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
                "Main Settings",
                "Bullet Time Key",
                new KeyboardShortcut(KeyCode.Mouse4),
                "Key for Bullet Time toggle");


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

        private float CoolDownElapsedSecond = 0f;
        private float BulletTimeElapsedSecond = 0f;
        private bool startBulletTime = false;
        private bool firstTimeTriggered = false;
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

                try
                {
                    Player player = Singleton<GameWorld>.Instance.AllPlayers[0];


                    if (Plugin.KeyBulletTime.Value.IsDown())
                    {
                        if (!firstTimeTriggered)
                        {
                            //Debug.Log("BulletTime: First Key Press - Activate Bullet Time");
                            firstTimeTriggered = true;
                            startBulletTime = true;
                            Singleton<GUISounds>.Instance.PlaySound(Plugin.EnterBulletAudioClip);
                            Time.timeScale = Plugin.BulletTimeScale.Value;
                            if (player)
                            {
                                setRecoil(player);
                            }

                        }
                        else if (firstTimeTriggered)
                        {
                            //Debug.Log("BulletTime: Second Key Press - Deactivate Bullet Time");
                            startBulletTime = false;
                            firstTimeTriggered = false;
                            BulletTimeElapsedSecond = 0f;
                            CoolDownElapsedSecond = 0f;
                            Singleton<GUISounds>.Instance.PlaySound(Plugin.ExitBulletAudioClip);
                            Time.timeScale = 1.0f;
                            undoRecoil(player);

                        }
                    }

                    if (startBulletTime)
                    {

                        if (CoolDownElapsedSecond >= Plugin.CooldownPeriodSeconds.Value)
                        {
                            Time.timeScale = Plugin.BulletTimeScale.Value;
                            BulletTimeElapsedSecond += Time.unscaledDeltaTime;
                            setRecoil(player);
                        }

                        if (BulletTimeElapsedSecond >= Plugin.MaxBulletTimeSeconds.Value)
                        {
                            //Logger.LogInfo("BulletTime: Stamina Ran Out");
                            Time.timeScale = 1.0f;
                            BulletTimeElapsedSecond = 0f;
                            CoolDownElapsedSecond = 0f;
                            startBulletTime = false;
                            firstTimeTriggered = false;
                            Singleton<GUISounds>.Instance.PlaySound(Plugin.ExitBulletAudioClip);
                            undoRecoil(player);
                        }
                    }
                }
                catch
                {
                    return;
                }
                
            }
        }

        private void setRecoil(Player player)
        {
            try
            {
                //Logger.LogInfo("Original FixedUpdate Time: " + Time.deltaTime);
                player.ProceduralWeaponAnimation.HandsContainer.Recoil.FixedUpdate(Time.deltaTime);

                //Logger.LogInfo("Set the FixedUpdate of Recoil to: " + Time.deltaTime);
            }
            catch
            {
                Debug.Log("Failed getting Playerspring and setting FixedUpdate");
            }
        }

        private void undoRecoil(Player player)
        {
            try
            {

                player.ProceduralWeaponAnimation.HandsContainer.Recoil.FixedUpdate(Time.deltaTime);
            }
            catch
            {
                Debug.Log("Failed resetting original FixedUpdate");
            }

        }
    }
}