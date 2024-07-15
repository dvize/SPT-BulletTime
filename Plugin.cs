using System;
using System.Reflection;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using EFT;
using UnityEngine;
using UnityEngine.Networking;

namespace BulletTime
{
    [BepInPlugin("com.dvize.BulletTime", "dvize.BulletTime", "1.8.0")]

    public class BulletTime : BaseUnityPlugin
    {
        public static ConfigEntry<bool> PluginEnabled
        {
            get; set;
        }
        public static ConfigEntry<float> BulletTimeScale
        {
            get; set;
        }
        public static ConfigEntry<float> StaminaBurnRatePerSecond
        {
            get; set;
        }
        public static ConfigEntry<KeyboardShortcut> KeyBulletTime
        {
            get; set;
        }

        public static AudioClip EnterBulletAudioClip;
        public static AudioClip ExitBulletAudioClip;
        public async void Awake()
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

            StaminaBurnRatePerSecond = Config.Bind(
                "Main Settings",
                "Bullet Time Stamina Burn Rate Per Second",
                8f,
                "How fast stamina burns");

            KeyBulletTime = Config.Bind(
                "Main Settings",
                "Bullet Time Key",
                new KeyboardShortcut(KeyCode.Mouse4),
                "Key for Bullet Time toggle");

            string uri = "file://" + (BepInEx.Paths.PluginPath + "\\dvize.BulletTime\\enterbullet.ogg");
            string uri2 = "file://" + (BepInEx.Paths.PluginPath + "\\dvize.BulletTime\\exitbullet.ogg");

            EnterBulletAudioClip = await LoadAudioClip(uri);
            ExitBulletAudioClip = await LoadAudioClip(uri2);

            new NewGamePatch().Enable();
        }

        public async Task<AudioClip> LoadAudioClip(string uri)
        {
            using (UnityWebRequest web = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))
            {
                var asyncOperation = web.SendWebRequest();

                while (!asyncOperation.isDone)
                    await Task.Yield();

                if (!web.isNetworkError && !web.isHttpError)
                {
                    return DownloadHandlerAudioClip.GetContent(web);
                }
                else
                {
                    UnityEngine.Debug.LogError($"Can't load audio at path: '{uri}', error: {web.error}");
                    return null;
                }
            }
        }

    }


    internal class NewGamePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

        [PatchPrefix]
        private static void PatchPrefix()
        {
            BulletTimeComponent.Enable();
        }
    }

    
}
