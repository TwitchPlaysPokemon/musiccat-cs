using System.ServiceProcess;

namespace ServiceWrapper
{
    public partial class MusicCatService : ServiceBase
    {
        public MusicCatService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //TODO: Load config
            MusicCat.Listener.Config = MusicCat.Config.DefaultConfig;
            MusicCat.Listener.Start();
        }

        protected override void OnStop()
        {
            MusicCat.Listener.Stop();
        }
    }
}
