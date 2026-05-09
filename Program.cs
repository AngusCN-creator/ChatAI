namespace ChatAI.UI.Forms
{
    internal static class Program
    {
        // 🔥 加一个全局标记：是否需要退出整个程序
        public static bool IsExitApplication = false;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            while (true)
            {
                // 如果标记为 true，直接退出，不回登录
                if (IsExitApplication)
                    break;

                FrmLogin loginForm = new FrmLogin();
                if (loginForm.ShowDialog() != DialogResult.OK)
                {
                    break;
                }

                FrmChatMain mainForm = new FrmChatMain(loginForm.LoginUsername);
                Application.Run(mainForm);

                // 👆 主窗口关闭后，根据标记决定是否继续循环
            }

            Application.Exit();
        }
    }
}