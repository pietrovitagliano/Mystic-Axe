// Author: Pietro Vitagliano

namespace MysticAxe
{
    public class PauseMenu : Singleton<PauseMenu>
    {
        public void OnMainMenu()
        {
            MenuManager.Instance.MainMenuFunction();
        }

        public void OnQuit()
        {
            MenuManager.Instance.QuitFunction();
        }
    }
}