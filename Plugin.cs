using BepInEx;
using BepInEx.Configuration;
using Bsg.GameSettings;
using Comfort.Common;
using EFT;
using EFT.UI;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace BulletTime
{
    [BepInPlugin("com.dvize.BulletTime", "dvize.BulletTime", "1.4.2")]
    
    public class BulletTime : BaseUnityPlugin
    {
        public static ConfigEntry<bool> PluginEnabled { get; set; }
        public static ConfigEntry<float> BulletTimeScale { get; set; }
        public static ConfigEntry<float> BulletTimeStaminaBurnRatePerSecond { get; set; }
        public static ConfigEntry<KeyboardShortcut> KeyBulletTime { get; set; }
        
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

            BulletTimeStaminaBurnRatePerSecond = Config.Bind(
                "Main Settings",
                "Bullet Time Stamina Burn Rate Per Second",
                55.0f,
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

        }


        float staminaBurn = 0;
        bool startBulletTime = false;
        bool firstTimeTriggered = false;
        Player player;
        public void Update()
        {
            if (!BulletTime.PluginEnabled.Value)
            {
                return;
            }

            if (!Singleton<GameWorld>.Instantiated || Camera.main == null)
            {
                return;
            }

            try
            {
                player = Singleton<GameWorld>.Instance.MainPlayer;

                if (BulletTime.KeyBulletTime.Value.IsDown())
                {
                    //if key is down and bullet time is not active, activate it
                    if (!firstTimeTriggered && !startBulletTime)
                    {
                        Logger.LogInfo("Starting Bullet Time");
                        startBulletTime = true;
                        Singleton<GUISounds>.Instance.PlaySound(BulletTime.EnterBulletAudioClip);
                        Time.timeScale = BulletTime.BulletTimeScale.Value;
                        firstTimeTriggered = true;

                        setRecoil(player);
                    }
                    else if (firstTimeTriggered && startBulletTime)
                    {
                        Logger.LogInfo("Ending Bullet Time Early by Keypress");
                        startBulletTime = false;
                        Singleton<GUISounds>.Instance.PlaySound(BulletTime.ExitBulletAudioClip);
                        Time.timeScale = 1.0f;
                        firstTimeTriggered = false;

                        setRecoil(player);

                    }
                }
               
                //in update loop, if we are in bullet time and the firsttimetriggered is true, then do stuff
                if (startBulletTime)
                {
                    try
                    {

                        //determine rate at which stamina burns based on BulletTime.BulletTimeStaminaBurnRatePerSecond.Value and Time.deltaTime
                        staminaBurn = BulletTime.BulletTimeStaminaBurnRatePerSecond.Value * Time.unscaledDeltaTime;
                        //Logger.LogInfo("StaminaCurrent: " + player.Physical.Stamina.Current);
                        player.Physical.Stamina.UpdateStamina((player.Physical.Stamina.Current - staminaBurn));
                    }
                    catch
                    {
                        Logger.LogError("Unable to Update Stamina.");
                    }

                    //no point in checking the stamina since player will have to trigger it off.
                    /*if (player.Physical.Stamina.Current <= 0f)
                    {
                        // player has no stamina left, exit bullet time with default values

                        //Logger.LogInfo("Ending Bullet Time Out of Stamina");

                        
                        Time.timeScale = 1.0f;
                        startBulletTime = false;
                        firstTimeTriggered = false;
                        Singleton<GUISounds>.Instance.PlaySound(BulletTime.ExitBulletAudioClip);
                        setRecoil(player);
                    }

                    //Does timescale have to come at the end?  why is it not even executing the loginfo statment.
                    Time.timeScale = BulletTime.BulletTimeScale.Value;*/

                }
            }
            catch
            {
                Debug.Log("Something is broken");
                return;
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
                Debug.Log("Failed getting Playerspring and setting FixedUpdate");
            }
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
                    Debug.LogError($"Can't load audio at path: '{uri}', error: {web.error}");
                    return null;
                }
            }
        }

    }

}