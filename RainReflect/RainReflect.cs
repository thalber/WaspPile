using BepInEx;

namespace WaspPile.RR
{
    [BepInPlugin("RAINREF", "RainReflect", "1.0.0.0")]
    public class RainReflect : BaseUnityPlugin
    {
        private RainReflect()
        {
            Logger.Log(BepInEx.Logging.LogLevel.Message, "Helo");
            RainReflectDetours.ApplyAllDetours();
        }
    }
}
