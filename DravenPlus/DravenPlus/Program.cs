using EloBuddy.SDK.Events;

namespace DravenPlus
{
    class Program
    {
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += eventArgs => Draven.OnLoad();
        }
    }
}
