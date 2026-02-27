namespace MossLib;

public class Tools
{
    public static void Alert(string text, bool important = false)
    {
        PlayerCamera.main.DoAlert(text, important);
    }
}