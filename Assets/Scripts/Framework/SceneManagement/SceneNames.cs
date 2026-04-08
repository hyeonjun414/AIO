using System;

namespace Game.Framework.SceneManagement
{
    public static class SceneNames
    {
        public const string Splash = "SplashScene";
        public const string Intro = "IntroScene";
        public const string Lobby = "LobbyScene";
        public const string Game = "GameScene";

        public static string GetName(SceneId sceneId)
        {
            return sceneId switch
            {
                SceneId.Splash => Splash,
                SceneId.Intro => Intro,
                SceneId.Lobby => Lobby,
                SceneId.Game => Game,
                _ => throw new ArgumentOutOfRangeException(nameof(sceneId), sceneId, null)
            };
        }

        public static bool TryGetId(string sceneName, out SceneId sceneId)
        {
            switch (sceneName)
            {
                case Splash:
                    sceneId = SceneId.Splash;
                    return true;
                case Intro:
                    sceneId = SceneId.Intro;
                    return true;
                case Lobby:
                    sceneId = SceneId.Lobby;
                    return true;
                case Game:
                    sceneId = SceneId.Game;
                    return true;
                default:
                    sceneId = default;
                    return false;
            }
        }
    }
}
